/*
 * ScriptOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#if COMPRESSION
using System.IO.Compression;
#endif

using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using ArrayPair = System.Collections.Generic.KeyValuePair<string, object>;

namespace Eagle._Components.Private
{
    [ObjectId("81c28526-dd5a-4ba8-b056-b62bbd3b8d90")]
    internal static class ScriptOps
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static int DefaultSubCommandNameIndex = 1;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string DefaultVariableValue = null;
        private static readonly string DefaultGetVariableValue = null;
        private static readonly string DefaultSetVariableValue = null;
        private static readonly string DefaultUnsetVariableValue = null;

        ///////////////////////////////////////////////////////////////////////

        #region Default Shell Executable File Names
        private static readonly string DefaultShellFileName =
            "EagleShell" + FileExtension.Executable;

        private static readonly string DefaultShell32FileName =
            "EagleShell32" + FileExtension.Executable;

        private static readonly string DefaultKitFileName =
            "EagleKit" + FileExtension.Executable;

        private static readonly string DefaultKit32FileName =
            "EagleKit32" + FileExtension.Executable;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Constants
        private static readonly string HarpyPackageName = "Harpy"; // primary
        private static readonly string BadgePackageName = "Badge"; // secondary

        ///////////////////////////////////////////////////////////////////////

        private static readonly string SecurityPackageAlternateSuffix =
            ".Basic";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string[] SecurityPackageNames = {
            HarpyPackageName, BadgePackageName
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly string[] SecurityAlternatePackageNames = {
            HarpyPackageName + SecurityPackageAlternateSuffix,
            BadgePackageName + SecurityPackageAlternateSuffix
        };

        ///////////////////////////////////////////////////////////////////////

        private static readonly string SecurityCertificateResourceName =
            String.Format("{0}.Resources.Certificates.certificate.xml",
            GlobalState.GetPackageName());

        private static readonly string SecurityCertificateRequestName =
            "AboutCertificate";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string HarpyPackageIndexPattern =
            String.Format("*/{0}*{1}/*", HarpyPackageName,
            GlobalState.GetPackageVersion());

        private static readonly string BadgePackageIndexPattern =
            String.Format("*/{0}*{1}/*", BadgePackageName,
            GlobalState.GetPackageVersion());

        ///////////////////////////////////////////////////////////////////////

        private static readonly string HarpyAssemblyFileNamePattern =
            String.Format("*/{0}*{1}", HarpyPackageName,
            FileExtension.Library);

        private static readonly string BadgeAssemblyFileNamePattern =
            String.Format("*/{0}*{1}", BadgePackageName,
            FileExtension.Library);

        ///////////////////////////////////////////////////////////////////////

        private const string EnableSecurityScriptName = "enableSecurity";
        private const string DisableSecurityScriptName = "disableSecurity";
        private const string RemoveCommandsScriptName = "removeCommands";
        private const string RemoveVariablesScriptName = "removeVariables";

        ///////////////////////////////////////////////////////////////////////

        #region Security Package Loader Warning Message
#if !DEBUG
        private const string SecurityErrorMessage =
            "It is likely that the security plugins will fail to load in this configuration,\n" +
            "please use one of the following supported workarounds:\n\n" +
            "{0}1. Force this process to run as 32-bit, e.g. using \"{1}\",\n"+
            "{0}   etc.\n\n" +
            "{0}2. Modify \"{2}{3}\", setting its \"supportedRuntime\"\n" +
            "{0}   version to \"v4.0.30319\" (or higher).\n\n" +
            "{0}3. Set the \"{4}\" environment variable (to anything); however,\n" +
            "{0}   while this will bypass this error message, it will do nothing to\n" +
            "{0}   address the underlying issue, should it still exist.\n";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Health Support Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static int HealthTimeout = 1000;
        private static string HealthScript = "list [expr {2 + 2}] a b c";
        private static Result HealthResult = "4 a b c";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Zip Archive Support Constants
#if NETWORK
        //
        // NOTE: The name of the resource, relative to the base auxiliary
        //       URI for this assembly (e.g. "https://urn.to/r"), that
        //       should result in the "unzip.exe" tool being downloaded.
        //
        private static readonly string UnzipResourceName = "unzip";

        //
        // NOTE: The name of the Windows-only executable file name that
        //       is used to extract a zip archive file.
        //
        private static readonly string UnzipFileNameOnly =
            PlatformOps.IsWindowsOperatingSystem() ? "unzip.exe" : "unzip";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Variable Name Lists
        private static StringDictionary defaultVariableNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable "Safe" Name / Element Lists
        private static StringDictionary safeVariableNames;
        private static StringDictionary safeTclPlatformElementNames;
        private static StringDictionary safeEaglePlatformElementNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached "Safe" Interpreter
        private static Interpreter cachedSafeInterpreter;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Exited Event Handler Methods
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_ScriptOps_Exited",
                    ConfigurationFlags.ScriptOps))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= ScriptOps_Exited;
                        appDomain.DomainUnload += ScriptOps_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= ScriptOps_Exited;
                        appDomain.ProcessExit += ScriptOps_Exited;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ScriptOps_Exited(
            object sender,
            EventArgs e
            )
        {
            ClearInterpreterCache();

            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= ScriptOps_Exited;
                else
                    appDomain.ProcessExit -= ScriptOps_Exited;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Unknown Support Methods
        public static string GetPackageUnknownScript(
            string text,
            string name,
            Version version
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder(text);

            if (name != null)
            {
                builder.Append(Characters.Space);
                builder.Append(Parser.Quote(name));

                if (version != null)
                {
                    builder.Append(Characters.Space);
                    builder.Append(Parser.Quote(version.ToString()));
                }
            }

            return builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Methods
        private static void GetSecurityAssemblyPaths(
            Interpreter interpreter, /* in */
            string path,             /* in */
            ref StringList paths     /* in, out */
            )
        {
            if (String.IsNullOrEmpty(path))
            {
                TraceOps.DebugTrace(
                    "GetSecurityAssemblyPaths: invalid path",
                    typeof(ScriptOps).Name, TracePriority.PackageError);

                return;
            }

            string[] fileNames = Directory.GetFiles(
                PathOps.GetNativePath(path), Characters.Asterisk.ToString(),
                FileOps.GetSearchOption(true));

            if ((fileNames == null) || (fileNames.Length == 0))
            {
                TraceOps.DebugTrace(
                    "GetSecurityAssemblyPaths: no file names were found",
                    typeof(ScriptOps).Name, TracePriority.PackageError);

                return;
            }

            TraceOps.DebugTrace(String.Format(
                "GetSecurityAssemblyPaths: input path list: {0}",
                FormatOps.WrapOrNull(paths)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug3);

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                string directory = Path.GetDirectoryName(fileName);

                if (String.IsNullOrEmpty(directory))
                    continue;

                if (Parser.StringMatch(
                        interpreter, PathOps.GetUnixPath(fileName), 0,
                        HarpyAssemblyFileNamePattern, 0, PathOps.NoCase) ||
                    Parser.StringMatch(
                        interpreter, PathOps.GetUnixPath(fileName), 0,
                        BadgeAssemblyFileNamePattern, 0, PathOps.NoCase))
                {
                    if (paths == null)
                        paths = new StringList();

                    paths.Add(directory);
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetSecurityAssemblyPaths: output path list: {0}",
                FormatOps.WrapOrNull(paths)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug3);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetSecurityPackageIndexPaths(
            Interpreter interpreter, /* in */
            ref StringList paths     /* in, out */
            )
        {
            StringList autoPathList = GlobalState.GetAutoPathList(
                interpreter, false);

            if (autoPathList == null)
            {
                TraceOps.DebugTrace(
                    "GetSecurityPackageIndexPaths: no auto-path list",
                    typeof(ScriptOps).Name, TracePriority.PackageError);

                return;
            }

            string indexFileName = FormatOps.ScriptTypeToFileName(
                ScriptTypes.PackageIndex, PackageType.None, true, false);

            if (String.IsNullOrEmpty(indexFileName))
            {
                TraceOps.DebugTrace(
                    "GetSecurityPackageIndexPaths: no index file name",
                    typeof(ScriptOps).Name, TracePriority.PackageError);

                return;
            }

            TraceOps.DebugTrace(String.Format(
                "GetSecurityPackageIndexPaths: input path list: {0}",
                FormatOps.WrapOrNull(autoPathList)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug4);

            foreach (string path in autoPathList)
            {
                if (String.IsNullOrEmpty(path))
                    continue;

                string[] fileNames = Directory.GetFiles(
                    PathOps.GetNativePath(path), indexFileName,
                    FileOps.GetSearchOption(true));

                if ((fileNames == null) || (fileNames.Length == 0))
                    continue;

                foreach (string fileName in fileNames)
                {
                    if (String.IsNullOrEmpty(fileName))
                        continue;

                    string directory = Path.GetDirectoryName(fileName);

                    if (String.IsNullOrEmpty(directory))
                        continue;

                    if (Parser.StringMatch(
                            interpreter, PathOps.GetUnixPath(fileName), 0,
                            HarpyPackageIndexPattern, 0, PathOps.NoCase) ||
                        Parser.StringMatch(
                            interpreter, PathOps.GetUnixPath(fileName), 0,
                            BadgePackageIndexPattern, 0, PathOps.NoCase))
                    {
                        if (paths == null)
                            paths = new StringList();

                        paths.Add(directory);
                        continue;
                    }

                    //
                    // HACK: Do not use fileName here... Instead, check for
                    //       the "Harpy*.dll" / "Badge*.dll" patterns using
                    //       the directory.  This is being done in order to
                    //       support loading out-of-tree plugins running on
                    //       the .NET Core runtime, where they may not have
                    //       a matching directory name pattern (above).
                    //
                    GetSecurityAssemblyPaths(
                        interpreter, directory, ref paths);
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetSecurityPackageIndexPaths: output path list: {0}",
                FormatOps.WrapOrNull(paths)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug4);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveSecurityPackageIndexes(
            StringList paths,                     /* in */
            PackageIndexDictionary packageIndexes /* in */
            )
        {
            if ((paths == null) || (packageIndexes == null))
                return false;

            string indexFileName = FormatOps.ScriptTypeToFileName(
                ScriptTypes.PackageIndex, PackageType.None, true, false);

            if (String.IsNullOrEmpty(indexFileName))
                return false;

            foreach (string path in paths)
            {
                if (String.IsNullOrEmpty(path))
                    continue;

                if (!packageIndexes.ContainsKey(Path.Combine(
                        path, indexFileName)))
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode MaybeFindSecurityPackageIndexes(
            Interpreter interpreter, /* in */
            bool force,              /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // HACK: If the interpreter was already initialized, skip finding
            //       the security package indexes because they should already
            //       be loaded.
            //
            // NOTE: Created "safe" interpreters must be allowed to make use
            //       of this handling.
            //
            if (!force && interpreter.InternalInitialized &&
                !interpreter.InternalIsSafe())
            {
                return ReturnCode.Ok;
            }

            StringList paths = null;

            /* NO RESULT */
            GetSecurityPackageIndexPaths(interpreter, ref paths);

            if (paths == null)
                return ReturnCode.Ok;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                PackageIndexDictionary packageIndexes =
                    interpreter.CopyPackageIndexes();

                //
                // HACK: If all the security package indexes are present in
                //       the interpreter, skip trying to find and load them
                //       again.
                //
                if (HaveSecurityPackageIndexes(paths, packageIndexes))
                    return ReturnCode.Ok;

                PackageIndexFlags savedPackageIndexFlags =
                    interpreter.PackageIndexFlags;

                try
                {
                    interpreter.PackageIndexFlags =
                        PackageIndexFlags.SecurityPackage;

                    PackageFlags savedPackageFlags = interpreter.PackageFlags;

                    try
                    {
                        interpreter.PackageFlags |=
                            PackageFlags.SecurityPackageMask;

                        if (PackageOps.FindAll(
                                interpreter, paths,
                                interpreter.PackageIndexFlags,
                                interpreter.PathComparisonType,
                                ref packageIndexes,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }
                    finally
                    {
                        interpreter.PackageFlags = savedPackageFlags;
                    }
                }
                finally
                {
                    interpreter.PackageIndexFlags = savedPackageIndexFlags;
                }

                interpreter.PackageIndexes = packageIndexes;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefaultShellFileName()
        {
            return IsDefaultShellFileName(PathOps.GetExecutableNameOnly());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsDefaultShellFileName(
            string fileNameOnly /* in */
            )
        {
            if (PathOps.IsEqualFileName(fileNameOnly, DefaultShellFileName))
                return true;

            if (PathOps.IsEqualFileName(fileNameOnly, DefaultShell32FileName))
                return true;

            if (PathOps.IsEqualFileName(fileNameOnly, DefaultKitFileName))
                return true;

            if (PathOps.IsEqualFileName(fileNameOnly, DefaultKit32FileName))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if !DEBUG
        private static string GetShellFileName(
            bool wow64 /* in */
            )
        {
            return GetShellFileName(PathOps.GetExecutableNameOnly(), wow64);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetShellFileName(
            string fileNameOnly, /* in */
            bool wow64           /* in */
            )
        {
            if (String.IsNullOrEmpty(fileNameOnly))
            {
                return wow64 ?
                    DefaultShell32FileName : DefaultShellFileName;
            }

            if (!wow64)
                return fileNameOnly;

            return String.Format(
                "{0}32{1}", Path.GetFileNameWithoutExtension(fileNameOnly),
                FileExtension.Executable);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool AreSecurityPackagesLikelyBroken(
            ref Result error /* out */
            )
        {
            //
            // HACK: When running as a 64-bit process on the .NET Framework
            //       2.0 runtime, the security plugins will not load due to
            //       broken obfuscation provided by LogicNP Software, which
            //       they refuse to fix.  Sorry guys, please fix your code,
            //       which is apparently broken on 64-bit .NET 2.0.  It is
            //       possible to skip this error by setting the environment
            //       variable "ForceSecurity" [to anything]; however, that
            //       will only enable this class to *attempt* to loading of
            //       the security plugins, which will (quite likely) still
            //       fail due to the aforementioned reasons.
            //
            // NOTE: Eagle Enterprise Edition (EEE) licensees may request
            //       the official non-obfuscated binaries for all Eagle
            //       Enterprise Edition plugins associated with a specific
            //       release.  Additionally, Eagle Enterprise Edition (EEE)
            //       source code licensees are permitted to customize the
            //       plugins and/or rebuild them without any obfuscation.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ForceSecurity))
            {
                return false;
            }

            ///////////////////////////////////////////////////////////////////

            #region Release Builds Only
#if !DEBUG
            //
            // BUGBUG: Technically, the (release build) security plugins
            //         may not load right on Mono either.
            //
            if (PlatformOps.Is64BitProcess() &&
                CommonOps.Runtime.IsRuntime20() &&
                !CommonOps.Runtime.IsMono() &&
                !CommonOps.Runtime.IsDotNetCore())
            {
                error = String.Format(
                    SecurityErrorMessage, Characters.HorizontalTab,
                    GetShellFileName(true), GetShellFileName(false),
                    FileExtension.Configuration, EnvVars.ForceSecurity);

                return true;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckForSecurityUpdate(
            Interpreter interpreter, /* in */
            bool verbose             /* in */
            )
        {
            if (interpreter == null) /* garbage in, garbage out. */
                return false;

            if (!interpreter.InternalInteractive) /* batch mode?  skip it. */
                return false;

            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.NoSecurityUpdate, GlobalConfiguration.GetFlags(
                    ConfigurationFlags.ScriptOps, verbose))) /* forbid? */
            {
                return false;
            }

            return true; /* default, do perform security update check */
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode EnableOrDisableSecurity(
            Interpreter interpreter, /* in */
            bool enable,             /* in */
            bool force,              /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter"; /* NO TRACE */
                return ReturnCode.Error;
            }

            if (AreSecurityPackagesLikelyBroken(ref error))
            {
                TraceOps.DebugTrace(String.Format(
                    "EnableOrDisableSecurity: security packages likely " +
                    "broken, error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            if (MaybeFindSecurityPackageIndexes(
                    interpreter, force, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "EnableOrDisableSecurity: package indexes scan " +
                    "failed, error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            //
            // NOTE: It should be noted that the "enableSecurity" and/or
            //       "disableSecurity" script must be signed and trusted
            //       if the interpreter used is configured with security
            //       enabled.  The very first time the "enableSecurity"
            //       is evaluated here, its signature will generally not
            //       be checked (i.e. because it is the script used to
            //       enable signed script policy enforcement); however,
            //       any subsequent attempts to evaluate it in the same
            //       interpreter may cause its signature to be checked,
            //       (i.e. unless signed script policy enforcement has
            //       been disabled in the meantime).  Since the script
            //       flags used here should force the designated script
            //       to be loaded only from within the compiled core
            //       library assembly itself (i.e. which is typically
            //       strong name and/or Authenticode signed), we should
            //       be OK security-wise.  It should be noted that this
            //       assumption requires the core library to be built
            //       with the embedded library option enabled in order
            //       for it to be valid.
            //
            string name = enable ?
                EnableSecurityScriptName : DisableSecurityScriptName;

            ScriptFlags scriptFlags =
                ScriptFlags.CoreLibrarySecurityRequiredFile;

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    name, ref scriptFlags, ref clientData,
                    ref localResult) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "EnableOrDisableSecurity: no script available, " +
                    "error = {0}", FormatOps.WrapOrNull(localResult)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                error = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should use several "unsafe" commands
            //       (i.e. within Harpy); therefore, evaluate it as
            //       an "unsafe" one.
            //
            string text = localResult;

            //
            // NOTE: Measure how long it takes (in microseconds) to
            //       enable -OR- disable security for the specified
            //       interpreter.
            //
            IProfilerState profiler = null;
            bool dispose = true;

            try
            {
                profiler = ProfilerState.Create(
                    interpreter, ref dispose);

                if (profiler != null)
                    profiler.Start();

                if (FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.File, true))
                {
                    if (interpreter.EvaluateTrustedFile(
                            null, text, TrustFlags.SecurityPackage,
                            ref localResult) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "EnableOrDisableSecurity: script file " +
                            "failed, error = {0}", FormatOps.WrapOrNull(
                            localResult)), typeof(ScriptOps).Name,
                            TracePriority.SecurityError);

                        error = localResult;
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    if (interpreter.EvaluateTrustedScript(
                            text, TrustFlags.SecurityPackage,
                            ref localResult) != ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "EnableOrDisableSecurity: script text " +
                            "failed, error = {0}", FormatOps.WrapOrNull(
                            localResult)), typeof(ScriptOps).Name,
                            TracePriority.SecurityError);

                        error = localResult;
                        return ReturnCode.Error;
                    }
                }
            }
            finally
            {
                if (profiler != null)
                {
                    profiler.Stop();

                    TraceOps.DebugTrace(String.Format(
                        "EnableOrDisableSecurity: {0}{1} security in {2}",
                        force ? "forcibly " : String.Empty, enable ?
                        "enabled" : "disabled", FormatOps.MaybeNull(
                        profiler)), typeof(ScriptOps).Name,
                        TracePriority.SecurityDebug);

                    if (dispose)
                    {
                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                            interpreter, ref profiler);
                    }

                    profiler = null;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static byte[] ParsePublicKeyToken(
            string value,            /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            long longValue = 0;

            if (Value.GetWideInteger2(
                    FormatOps.HexadecimalPrefix + value,
                    ValueFlags.AnyInteger, cultureInfo,
                    ref longValue, ref error) == ReturnCode.Ok)
            {
                try
                {
                    byte[] bytes = BitConverter.GetBytes(
                        longValue);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    return bytes;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IPlugin FindSecurityPlugin(
            Interpreter interpreter, /* in */
            Priority priority,       /* in */
            bool alternate,          /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            string[] packageNames = alternate ?
                SecurityAlternatePackageNames : SecurityPackageNames;

            if (packageNames == null)
            {
                error = "security packages unavailable";
                return null;
            }

            if ((priority < 0) ||
                ((int)priority >= packageNames.Length))
            {
                error = String.Format(
                    "missing security package {0}", (int)priority);

                return null;
            }

            byte[] publicKeyToken = ParsePublicKeyToken(
                PublicKeyToken.Security, interpreter.InternalCultureInfo,
                ref error);

            if (publicKeyToken == null)
                return null;

            string pluginName = packageNames[(int)priority];

            return interpreter.InternalFindPlugin(
                null, MatchMode.Exact, pluginName, null, publicKeyToken,
                LookupFlags.Default, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSecurityCertificateStream(
            ref Stream stream, /* out */
            ref Result error   /* out */
            )
        {
            try
            {
                Assembly assembly = GlobalState.GetAssembly();

                stream = AssemblyOps.GetResourceStream(
                    assembly, SecurityCertificateResourceName,
                    ref error);

                if (stream != null)
                    return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSecurityCertificateBytes(
            Stream stream,    /* in */
            ref byte[] bytes, /* in */
            ref Result result /* in, out */
            )
        {
            if (stream == null)
            {
                result = "invalid stream";
                return ReturnCode.Error;
            }

            try
            {
                int length = (int)stream.Length; /* throw */

                using (BinaryReader binaryReader = new BinaryReader(
                        stream))
                {
                    byte[] localBytes = binaryReader.ReadBytes(length);

                    if (localBytes == null) /* SANITY */
                    {
                        result = "invalid certificate bytes";
                        return ReturnCode.Error;
                    }

                    if (localBytes.Length != length) /* SANITY */
                    {
                        result = "wrong number of certificate bytes";
                        return ReturnCode.Error;
                    }

                    bytes = localBytes;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CheckSecurityCertificate(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            byte[] bytes,            /* in */
            ref Result result        /* in, out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (plugin == null)
            {
                result = "invalid plugin";
                return ReturnCode.Error;
            }

            if (bytes == null)
            {
                result = "invalid bytes";
                return ReturnCode.Error;
            }

            IPlugin corePlugin = interpreter.GetCorePlugin(
                ref result);

            if (corePlugin == null)
            {
                result = "invalid core plugin";
                return ReturnCode.Error;
            }

            try
            {
                IClientData clientData = new ClientData(
                    SecurityCertificateRequestName);

                object[] request = {
                    interpreter, corePlugin, bytes, result
                };

                object response = null;

                if (plugin.Execute(
                        interpreter, clientData, request,
                        ref response, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (response == null)
                {
                    result = "invalid response";
                    return ReturnCode.Error;
                }

                result = StringOps.GetStringFromObject(response);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckSecurityCertificate(
            Interpreter interpreter, /* in */
            ref Result result        /* in, out */
            )
        {
            IPlugin plugin = null; /* REUSED */
            ResultList errors = null;

            foreach (bool alternate in new bool[] { false, true })
            {
                Result error = null;

                plugin = FindSecurityPlugin(
                    interpreter, Priority.Highest, alternate,
                    ref error);

                if (plugin != null)
                    break;

                if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }
            }

            if (plugin == null)
            {
                result = errors;
                return ReturnCode.Error;
            }

            Stream stream = null;

            try
            {
                if (GetSecurityCertificateStream(
                        ref stream, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                byte[] bytes = null;

                if (GetSecurityCertificateBytes(
                        stream, ref bytes, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return CheckSecurityCertificate(
                    interpreter, plugin, bytes, ref result);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Location Support Methods
        public static ReturnCode GetAndCheckProcedureLocation(
            Interpreter interpreter,
            IProcedure procedure,
            ref IScriptLocation location,
            ref Result error
            )
        {
            ReturnCode code = GetProcedureLocation(
                interpreter, procedure, ref location, ref error);

            if (code != ReturnCode.Ok)
                return code;

            ProcedureFlags procedureFlags = procedure.Flags;

            if (!FlagOps.HasFlags(
                    procedureFlags, ProcedureFlags.Private, true))
            {
                return ReturnCode.Ok;
            }

            IScriptLocation scriptLocation = null;
            ICallFrame frame = interpreter.ProcedureFrame;

            if (frame != null)
            {
                //
                // NOTE: There is an active procedure, attempt to grab
                //       the location from it.
                //
                IProcedure scriptProcedure = frame.Execute as IProcedure;

                if (scriptProcedure == null)
                {
                    error = "invalid procedure in procedure frame";
                    return ReturnCode.Error;
                }

                scriptLocation = scriptProcedure.Location;
            }
            else
            {
                //
                // NOTE: No active procedure, use script scope.
                //
                code = GetLocation(
                    interpreter, true, ref scriptLocation, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            IScriptLocation procedureLocation = (location != null) &&
                (location.FileName != null) ? location : null;

            if (!ScriptLocation.MatchFileName(
                    interpreter, procedureLocation, scriptLocation, true))
            {
                error = "cannot execute private procedure, " +
                    "script location mismatch";

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetProcedureLocation(
            Interpreter interpreter,
            IProcedure procedure,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (procedure == null)
            {
                error = "invalid procedure";
                return ReturnCode.Error;
            }

            if (FlagOps.HasFlags(
                    procedure.Flags, ProcedureFlags.ScriptLocation, true))
            {
                return GetLocation(
                    interpreter, false, ref location, ref error);
            }

            location = procedure.Location;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            ArgumentList arguments,
            int startIndex,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if ((startIndex < 0) || (startIndex >= arguments.Count))
            {
                error = "argument index out of range";
                return ReturnCode.Error;
            }

            Argument firstArgument = arguments[startIndex];
            Argument lastArgument = arguments[arguments.Count - 1];

            if ((firstArgument == null) && (lastArgument == null))
            {
                location = ScriptLocation.Create((IScriptLocation)null);
                return ReturnCode.Ok;
            }

            if (firstArgument != null)
            {
                location = ScriptLocation.Create(interpreter,
                    firstArgument.FileName, firstArgument.StartLine,
                    (lastArgument != null) ? lastArgument.EndLine :
                        firstArgument.EndLine,
                    firstArgument.ViaSource);
            }
            else
            {
                location = ScriptLocation.Create(interpreter,
                    lastArgument.FileName, lastArgument.StartLine,
                    lastArgument.EndLine, lastArgument.ViaSource);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            bool scrub,
            ref string fileName,
            ref Result error
            )
        {
            int currentLine = Parser.UnknownLine;

            return GetLocation(
                interpreter, viaSource, scrub, ref fileName,
                ref currentLine, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            bool scrub,
            ref string fileName,
            ref int currentLine,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                ReturnCode code;
                IScriptLocation location = null;

                code = GetLocation(
                    interpreter, viaSource, ref location, ref error);

                if (code == ReturnCode.Ok)
                {
                    string scriptFileName = (location != null) ?
                        location.FileName : null;

                    if (scrub && (scriptFileName != null))
                    {
                        fileName = PathOps.ScrubPath(
                            GlobalState.GetBasePath(), scriptFileName);
                    }
                    else
                    {
                        fileName = scriptFileName; /* NOTE: May be null. */
                    }

                    currentLine = (location != null) ?
                        location.StartLine : Parser.UnknownLine;

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (interpreter != null)
            {
#if !THREADING
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
#endif
                {
                    //
                    // NOTE: Grab whatever the caller previously manually
                    //       set the current script file name to, if any.
                    //
                    location = interpreter.ScriptLocation;

                    if (location == null)
                    {
                        ScriptLocationList locations =
                            interpreter.ScriptLocations;

                        if (locations != null)
                        {
                            int count = locations.Count;

                            if (count > 0)
                            {
                                for (int index = count - 1; index >= 0; index--)
                                {
                                    IScriptLocation thisLocation = locations[index];

                                    if (thisLocation == null)
                                        continue;

                                    if (!viaSource || thisLocation.ViaSource)
                                    {
                                        //
                                        // NOTE: Grab the last (most recent) script
                                        //       location from the stack of active
                                        //       script locations that matches the
                                        //       via [source] flag set by the caller.
                                        //
                                        location = thisLocation;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptPath(
            Interpreter interpreter, /* in */
            bool directoryOnly,      /* in */
            ref string path,         /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                string fileName = null;

                if (GetLocation(
                        interpreter, true, false, ref fileName,
                        ref error) == ReturnCode.Ok)
                {
                    if (directoryOnly)
                        path = Path.GetDirectoryName(fileName);
                    else
                        path = fileName;

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Settings Support Methods
        public static int ClearInterpreterCache()
        {
            return MaybeClearInterpreterCache(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int MaybeClearInterpreterCache(
            long? groupId /* in */
            )
        {
            Interpreter localInterpreter;

            lock (syncRoot)
            {
                localInterpreter = cachedSafeInterpreter;
            }

            int result = 0;

            if ((localInterpreter != null) &&
                localInterpreter.IsInGroup(groupId, true))
            {
                result++;

                Interpreter savedLocalInterpreter = localInterpreter;

                ObjectOps.TryDisposeOrComplain<Interpreter>(
                    localInterpreter, ref localInterpreter);

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Make sure the cached "safe" interpreter
                    //       is still the same -AND- set it to null
                    //       in that case (i.e. because it has been
                    //       disposed).
                    //
                    if (Object.ReferenceEquals(cachedSafeInterpreter,
                            savedLocalInterpreter))
                    {
                        cachedSafeInterpreter = null;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (cachedSafeInterpreter != null))
                {
                    localList.Add("CachedSafeInterpreter",
                        FormatOps.InterpreterNoThrow(cachedSafeInterpreter));
                }

                if (empty || ((defaultVariableNames != null) &&
                    (defaultVariableNames.Count > 0)))
                {
                    localList.Add("DefaultVariableNames",
                        (defaultVariableNames != null) ?
                            defaultVariableNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((safeVariableNames != null) &&
                    (safeVariableNames.Count > 0)))
                {
                    localList.Add("SafeVariableNames",
                        (safeVariableNames != null) ?
                            safeVariableNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((safeTclPlatformElementNames != null) &&
                    (safeTclPlatformElementNames.Count > 0)))
                {
                    localList.Add("SafeTclPlatformElementNames",
                        (safeTclPlatformElementNames != null) ?
                            safeTclPlatformElementNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((safeEaglePlatformElementNames != null) &&
                    (safeEaglePlatformElementNames.Count > 0)))
                {
                    localList.Add("SafeEaglePlatformElementNames",
                        (safeEaglePlatformElementNames != null) ?
                            safeEaglePlatformElementNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Script Caches");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is designed to check the *COMPLETE* list of
        //          system variables that may be set in created interpreters.
        //          If additional variables need to be set during interpreter
        //          creation, they will need to be added here as well.
        //
        private static bool IsDefaultVariableName(
            string name,     /* in */
            bool anyReserved /* in */
            )
        {
            lock (syncRoot)
            {
                if (defaultVariableNames == null)
                {
                    defaultVariableNames = new StringDictionary(new string[] {
                        TclVars.Core.ShellArgumentCount,
                        TclVars.Core.ShellArguments,
                        TclVars.Core.ShellArgument0,
                        TclVars.Core.AutoExecutables,
                        TclVars.Core.AutoIndex,
                        TclVars.Core.AutoNoExecute,
                        TclVars.Core.AutoNoLoad,
                        TclVars.Core.AutoOldPath,
                        TclVars.Core.AutoPath,
                        TclVars.Core.AutoSourcePath,
                        Vars.Platform.Name,
                        Vars.Core.Debugger,
                        Vars.Core.Paths,
                        Vars.Core.Shell,
                        Vars.Core.Tests,
                        TclVars.Core.Environment,
                        TclVars.Core.ErrorCode,
                        TclVars.Core.ErrorInfo,
                        /* NOT CORE: Vars.Core.No, */
                        Vars.Core.Null,
                        TclVars.Core.Interactive,
                        TclVars.Core.InteractiveLoops,
                        TclVars.Core.Library,
                        TclVars.Core.LibraryPath,
                        TclVars.Core.NonWordCharacters,
                        TclVars.Package.PatchLevelName,
                        TclVars.Core.PackagePath,
                        TclVars.Platform.Name,
                        TclVars.Core.PrecisionName,
                        TclVars.Core.Prompt1,
                        TclVars.Core.Prompt2,
                        TclVars.Core.RunCommandsFileName,
                        TclVars.Core.RunCommandsResourceName,
                        TclVars.Core.ShellLibrary,
                        TclVars.Core.TraceCompile,
                        TclVars.Core.TraceExecute,
                        TclVars.Package.VersionName,
                        TclVars.Core.WordCharacters
                    }, true, false);
                }

                if (name != null)
                {
                    if (defaultVariableNames.ContainsKey(name))
                        return true;

                    if (anyReserved)
                    {
                        //
                        // NOTE: Check if the name starts with "tcl_".
                        //
                        if (name.StartsWith(TclVars.Package.Prefix,
                                SharedStringOps.SystemComparisonType))
                        {
                            return true;
                        }

                        //
                        // NOTE: Check if the name starts with "eagle_".
                        //
                        if (name.StartsWith(Vars.Package.Prefix,
                                SharedStringOps.SystemComparisonType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PrepareForStaticData(
            Interpreter interpreter, /* in */
            bool noEvaluate,         /* in */
            ref Result error         /* out */
            )
        {
            if (noEvaluate)
            {
                if (ClearVariables(
                        interpreter, true, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (RemoveVariables(
                        interpreter, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            Result localResult = null;

            if (CallFrameOps.Purge(
                    interpreter, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            if (!noEvaluate && (RemoveCommands(
                    interpreter, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode RemoveVariables(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: It should be noted that the "removeVariables" script
            //       must be signed and trusted if the interpreter used
            //       is configured with security enabled.
            //
            ScriptFlags scriptFlags =
                ScriptFlags.CoreLibrarySecurityRequiredFile;

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    RemoveVariablesScriptName, ref scriptFlags,
                    ref clientData, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should not use any "unsafe" commands;
            //       therefore, do not evaluate it as an "unsafe" one.
            //
            string text = localResult;

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
            {
                if (interpreter.EvaluateFile(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.EvaluateScript(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is only for use by the LoadSettingsViaFile
        //       method.  It uses a very brute-force technique to clear
        //       the variables in the specified interpreter and/or call
        //       frame (e.g. it does not cause traces to fire).  It is
        //       *not* designed for use by other (general purpose) call
        //       frame management code.  This method assumes that any
        //       necessary locks are already held.
        //
        // NOTE: This method assumes that the interpreter lock is held.
        //
        private static ReturnCode ClearVariables(
            ICallFrame frame, /* in */
            bool markOnly,    /* in */
            ref Result error  /* out */
            )
        {
            if (frame == null)
            {
                error = "invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary variables = frame.Variables;

            if (variables == null)
            {
                error = "call frame does not support variables";
                return ReturnCode.Error;
            }

            if (markOnly)
            {
                foreach (KeyValuePair<string, IVariable> pair in variables)
                {
                    IVariable variable = pair.Value;

                    if (variable == null)
                        continue;

                    EntityOps.SetUndefined(variable, true);
                }
            }
            else
            {
                variables.Clear();
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ClearVariables(
            Interpreter interpreter, /* in */
            bool markOnly,           /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                return ClearVariables(
                    interpreter.CurrentGlobalFrame, markOnly, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode RemoveCommands(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: It should be noted that the "removeCommands" script
            //       must be signed and trusted if the interpreter used
            //       is configured with security enabled.
            //
            ScriptFlags scriptFlags =
                ScriptFlags.CoreLibrarySecurityRequiredFile;

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    RemoveCommandsScriptName, ref scriptFlags,
                    ref clientData, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should not use any "unsafe" commands;
            //       therefore, do not evaluate it as an "unsafe" one.
            //
            string text = localResult;

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
            {
                if (interpreter.EvaluateFile(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.EvaluateScript(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }

            interpreter.RemoveNonBaseObjects(true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeSetParentInterpreter(
            Interpreter interpreter,      /* in */
            Interpreter parentInterpreter /* in */
            )
        {
            if (interpreter == null)
                return;

            interpreter.ParentInterpreter = parentInterpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeEnablePluginFlags(
            ScriptDataFlags flags,      /* in */
            ref PluginFlags pluginFlags /* in, out */
            )
        {
#if ISOLATED_PLUGINS
            if (FlagOps.HasFlags(flags,
                    ScriptDataFlags.NoIsolatedPlugins,
                    true))
            {
                pluginFlags |= PluginFlags.NoIsolated;
            }

#if SHELL
            if (FlagOps.HasFlags(flags,
                    ScriptDataFlags.NoPluginUpdateCheck,
                    true))
            {
                pluginFlags |= PluginFlags.NoUpdateCheck;
            }
#endif

            if (FlagOps.HasFlags(flags,
                    ScriptDataFlags.NoPluginIsolatedOnly,
                    true))
            {
                pluginFlags |= PluginFlags.NoIsolatedOnly;
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExtractInterpreterCreationFlags(
            Interpreter interpreter,                    /* in: OPTIONAL */
            CreationFlagTypes creationFlagTypes,        /* in */
            CreateFlags? fallbackCreateFlags,           /* in: OPTIONAL */
            HostCreateFlags? fallbackHostCreateFlags,   /* in: OPTIONAL */
            InitializeFlags? fallbackInitializeFlags,   /* in: OPTIONAL */
            ScriptFlags? fallbackScriptFlags,           /* in: OPTIONAL */
            InterpreterFlags? fallbackInterpreterFlags, /* in: OPTIONAL */
            PluginFlags? fallbackPluginFlags,           /* in: OPTIONAL */
            out CreateFlags createFlags,                /* out */
            out HostCreateFlags hostCreateFlags,        /* out */
            out InitializeFlags initializeFlags,        /* out */
            out ScriptFlags scriptFlags,                /* out */
            out InterpreterFlags interpreterFlags,      /* out */
            out PluginFlags pluginFlags                 /* out */
            )
        {
            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentCreateFlags, true))
            {
                createFlags = interpreter.CreateFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultCreateFlags, true))
            {
                createFlags = interpreter.DefaultCreateFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackCreateFlags, true))
            {
                createFlags = (fallbackCreateFlags != null) ?
                    (CreateFlags)fallbackCreateFlags :
                    Defaults.CreateFlags;
            }
            else
            {
                createFlags = CreateFlags.None;
            }

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentHostCreateFlags, true))
            {
                hostCreateFlags = interpreter.HostCreateFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultHostCreateFlags, true))
            {
                hostCreateFlags = interpreter.DefaultHostCreateFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackCreateFlags, true))
            {
                hostCreateFlags = (fallbackHostCreateFlags != null) ?
                    (HostCreateFlags)fallbackHostCreateFlags :
                    Defaults.HostCreateFlags;
            }
            else
            {
                hostCreateFlags = HostCreateFlags.None;
            }

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentInitializeFlags, true))
            {
                initializeFlags = interpreter.InitializeFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultInitializeFlags, true))
            {
                initializeFlags = interpreter.DefaultInitializeFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackInitializeFlags, true))
            {
                initializeFlags = (fallbackInitializeFlags != null) ?
                    (InitializeFlags)fallbackInitializeFlags :
                    Defaults.InitializeFlags;
            }
            else
            {
                initializeFlags = InitializeFlags.None;
            }

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentScriptFlags, true))
            {
                scriptFlags = interpreter.ScriptFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultScriptFlags, true))
            {
                scriptFlags = interpreter.DefaultScriptFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackScriptFlags, true))
            {
                scriptFlags = (fallbackScriptFlags != null) ?
                    (ScriptFlags)fallbackScriptFlags :
                    Defaults.ScriptFlags;
            }
            else
            {
                scriptFlags = ScriptFlags.None;
            }

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentInterpreterFlags, true))
            {
                interpreterFlags = interpreter.InterpreterFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultInterpreterFlags, true))
            {
                interpreterFlags = interpreter.DefaultInterpreterFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackInterpreterFlags, true))
            {
                interpreterFlags = (fallbackInterpreterFlags != null) ?
                    (InterpreterFlags)fallbackInterpreterFlags :
                    Defaults.InterpreterFlags;
            }
            else
            {
                interpreterFlags = InterpreterFlags.None;
            }

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentPluginFlags, true))
            {
                pluginFlags = interpreter.PluginFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultPluginFlags, true))
            {
                pluginFlags = interpreter.DefaultPluginFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackPluginFlags, true))
            {
                pluginFlags = (fallbackPluginFlags != null) ?
                    (PluginFlags)fallbackPluginFlags :
                    Defaults.PluginFlags;
            }
            else
            {
                pluginFlags = PluginFlags.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter CreateInterpreterForSettings(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ScriptDataFlags flags,   /* in */
            ref Result result        /* out */
            )
        {
            string childName = null;

            return CreateInterpreterForSettings(
                interpreter, clientData, flags, ref childName, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter CreateInterpreterForSettings(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ScriptDataFlags flags,   /* in */
            ref string childName,    /* out */
            ref Result result        /* out */
            )
        {
            bool useSafe = FlagOps.HasFlags(
                flags, ScriptDataFlags.UseSafeInterpreter, true);

            bool staticData = FlagOps.HasFlags(
                flags, ScriptDataFlags.UseStaticDataOnly, true);

            CreateFlags defaultCreateFlags = useSafe ?
                CreateFlags.SafeSettingsUse : CreateFlags.SettingsUse;

            if (staticData)
                defaultCreateFlags |= CreateFlags.StaticDataUse;

            HostCreateFlags defaultHostCreateFlags = useSafe ?
                HostCreateFlags.SafeSettingsUse : HostCreateFlags.SettingsUse;

            bool disableHost = FlagOps.HasFlags(
                flags, ScriptDataFlags.DisableHost, true);

            if (disableHost)
                defaultHostCreateFlags |= HostCreateFlags.Disable;
            else
                defaultHostCreateFlags |= HostCreateFlags.MustCreate;

            bool noConsoleHost = FlagOps.HasFlags(
                flags, ScriptDataFlags.NoConsoleHost, true);

            if (noConsoleHost || staticData)
            {
#if CONSOLE
                defaultHostCreateFlags |= HostCreateFlags.NoNativeConsole;
#endif

                defaultHostCreateFlags |= HostCreateFlags.NoConsole;
            }

            CreateFlags createFlags;
            HostCreateFlags hostCreateFlags;
            InitializeFlags initializeFlags;
            ScriptFlags scriptFlags;
            InterpreterFlags interpreterFlags;
            PluginFlags pluginFlags;

            ExtractInterpreterCreationFlags(
                interpreter, CreationFlagTypes.SettingsFlags,
                defaultCreateFlags, defaultHostCreateFlags,
                null, null, null, null, out createFlags,
                out hostCreateFlags, out initializeFlags,
                out scriptFlags, out interpreterFlags,
                out pluginFlags);

            bool disableSecurity = FlagOps.HasFlags(
                flags, ScriptDataFlags.DisableSecurity, true);

            if (disableSecurity)
                initializeFlags &= ~InitializeFlags.Security;

            bool useIsolated = FlagOps.HasFlags(
                flags, ScriptDataFlags.UseIsolatedInterpreter, true);

            bool noStartup = FlagOps.HasFlags(
                flags, ScriptDataFlags.NoStartup, true);

            if (noStartup)
                initializeFlags &= ~InitializeFlags.ShellOrStartup;

            //
            // HACK: If requested by the caller, set special plugin flags to
            //       prevent potential conflicts between the settings loader
            //       and plugin isolation.
            //
            MaybeEnablePluginFlags(flags, ref pluginFlags);

            NewHostCallback savedNewHostCallback = null;

            Interpreter.BeginNoNewHostCallback(
                ref savedNewHostCallback);

            try
            {
                if (useIsolated)
                {
                    if (interpreter == null)
                    {
                        result = "invalid interpreter";
                        return null;
                    }

                    Result localResult = null; /* REUSED */

                    if (interpreter.CreateChildInterpreter(
                            null, clientData, null, createFlags,
                            hostCreateFlags, initializeFlags,
                            scriptFlags, interpreterFlags,
                            pluginFlags, useIsolated,
                            !disableSecurity, false,
                            ref localResult) == ReturnCode.Ok)
                    {
                        string path = localResult;
                        Interpreter otherInterpreter = null;

                        localResult = null;

                        if (Value.GetInterpreter(
                                interpreter, path,
                                InterpreterType.Default,
                                ref otherInterpreter,
                                ref localResult) == ReturnCode.Ok)
                        {
                            childName = path;
                            return otherInterpreter;
                        }
                    }

                    result = localResult;
                    return null;
                }
                else
                {
                    Interpreter otherInterpreter = Interpreter.Create(
                        clientData, null, createFlags, hostCreateFlags,
                        initializeFlags, scriptFlags, interpreterFlags,
                        pluginFlags, ref result);

                    //
                    // HACK: Even though the created interpreter is not
                    //       a child of the specified interpreter, set
                    //       its parent property; otherwise, the parent
                    //       property for the created interpreter would
                    //       always be null.  Normally, this would be
                    //       less than ideal because the parent property
                    //       should reflect reality; however, the created
                    //       interpreter is never used outside of this
                    //       subsystem (i.e. we are primarily responsible
                    //       for its entire lifecycle).
                    //
                    MaybeSetParentInterpreter(otherInterpreter, interpreter);

                    //
                    // HACK: Establish which interpreter was primarily
                    //       responsible for creating the new interpreter
                    //       and is now responsible for it.  This is not
                    //       required when creating isolated interpreters
                    //       because those are already disposed by their
                    //       parents.
                    //
                    Interpreter.PutInGroup(otherInterpreter, interpreter);

                    return otherInterpreter;
                }
            }
            finally
            {
                Interpreter.EndNoNewHostCallback(
                    ref savedNewHostCallback);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFileForSettingsPending(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.SettingLevels > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateFileForSettings(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
#if NETWORK
            bool trusted,            /* in */
#endif
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

#if NETWORK
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }
#endif

                //
                // NOTE: *SPECIAL* Keep track of the nesting level
                //       of all script evaluations being done only
                //       to load settings files.  The primary user
                //       of this feature is Harpy, which needs to
                //       differentiate between script evaluations
                //       that happen during interpreter creation
                //       and setup (e.g. "removeCommands" and/or
                //       "removeVariables") and script evaluations
                //       for the actual loading of settings files.
                //
                /* IGNORED */
                interpreter.EnterSettingLevel();

                try
                {
                    Result result = null;

                    if (interpreter.EvaluateFile(
                            fileName, ref result) == ReturnCode.Ok)
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = result;
                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    /* IGNORED */
                    interpreter.ExitSettingLevel();
                }
#if NETWORK
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringDictionary CopySettings(
            StringDictionary settings, /* in */
            bool create                /* in */
            )
        {
            if (settings == null)
                return create ? new StringDictionary() : null;

            return new StringDictionary(
                settings as IDictionary<string, string>);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes that the interpreter lock is held.
        //
        private static ReturnCode ExtractSettingsFromFrame(
            ICallFrame frame,              /* in */
            ScriptDataFlags flags,         /* in */
            ref StringDictionary settings, /* in, out */
            ref Result error               /* out */
            )
        {
            if (frame == null)
            {
                error = "invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary variables = frame.Variables;

            if (variables == null)
            {
                error = "call frame does not support variables";
                return ReturnCode.Error;
            }

            //
            // NOTE: If the caller specified some settings to be loaded,
            //       use that list verbatim; otherwise, add settings based
            //       on the global variables now present in the interpreter
            //       that were NOT added during the interpreter creation
            //       process.
            //
            StringDictionary localSettings = CopySettings(settings, true);

            //
            // NOTE: Figure out which kind(s) of variables that the caller
            //       wants saved to the resulting settings dictionary.
            //
            bool existingOnly = FlagOps.HasFlags(
                flags, ScriptDataFlags.ExistingOnly, true);

            bool copyScalars = FlagOps.HasFlags(
                flags, ScriptDataFlags.CopyScalars, true);

            bool copyArrays = FlagOps.HasFlags(
                flags, ScriptDataFlags.CopyArrays, true);

            bool errorOnScalar = FlagOps.HasFlags(
                flags, ScriptDataFlags.ErrorOnScalar, true);

            bool errorOnArray = FlagOps.HasFlags(
                flags, ScriptDataFlags.ErrorOnArray, true);

            if (existingOnly && (localSettings.Count > 0))
            {
                //
                // NOTE: Since a dictionary cannot be changed while it is in
                //       use (by the foreach statement), we need to create a
                //       copy of the variable names (only) for the foreach
                //       statement to use.
                //
                StringList varNames = new StringList(localSettings.Keys);

                foreach (string varName in varNames)
                {
                    if (varName == null)
                        continue;

                    IVariable variable;

                    if (!variables.TryGetValue(varName, out variable) ||
                        (variable == null))
                    {
                        continue;
                    }

                    //
                    // NOTE: A setting with this name may or may not already
                    //       exist in the dictionary provided by the caller;
                    //       therefore, add or update the setting.
                    //
                    ElementDictionary arrayValue = null;

                    if (EntityOps.IsArray(variable, ref arrayValue))
                    {
                        if (copyArrays)
                        {
                            foreach (ArrayPair pair2 in arrayValue)
                            {
                                string key = FormatOps.SettingKey(
                                    variable, arrayValue, pair2.Key);

                                if (key == null)
                                    continue;

                                localSettings[key] =
                                    StringOps.GetStringFromObject(
                                        pair2.Value);
                            }
                        }
                        else if (errorOnArray)
                        {
                            error = String.Format(
                                "array variable \"{0}\" is not allowed",
                                varName);

                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (copyScalars)
                        {
                            localSettings[varName] =
                                StringOps.GetStringFromObject(
                                    variable.Value);
                        }
                        else if (errorOnScalar)
                        {
                            error = String.Format(
                                "scalar variable \"{0}\" is not allowed",
                                varName);

                            return ReturnCode.Error;
                        }
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<string, IVariable> pair in variables)
                {
                    if (IsDefaultVariableName(pair.Key, true))
                        continue;

                    IVariable variable = pair.Value;

                    if (variable == null)
                        continue;

                    //
                    // NOTE: A setting with this name may or may not already
                    //       exist in the dictionary provided by the caller;
                    //       therefore, add or update the setting.
                    //
                    ElementDictionary arrayValue = null;

                    if (EntityOps.IsArray(variable, ref arrayValue))
                    {
                        if (copyArrays)
                        {
                            foreach (ArrayPair pair2 in arrayValue)
                            {
                                string key = FormatOps.SettingKey(
                                    variable, arrayValue, pair2.Key);

                                if (key == null)
                                    continue;

                                localSettings[key] =
                                    StringOps.GetStringFromObject(
                                        pair2.Value);
                            }
                        }
                        else if (errorOnArray)
                        {
                            error = String.Format(
                                "array variable \"{0}\" is not allowed",
                                pair.Key);

                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (copyScalars)
                        {
                            localSettings[pair.Key] =
                                StringOps.GetStringFromObject(
                                    variable.Value);
                        }
                        else if (errorOnScalar)
                        {
                            error = String.Format(
                                "scalar variable \"{0}\" is not allowed",
                                pair.Key);

                            return ReturnCode.Error;
                        }
                    }
                }
            }

            settings = localSettings;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadSettingsViaFile(
            Interpreter interpreter,        /* in */
            IClientData pushClientData,     /* in: OPTIONAL */
            IClientData callbackClientData, /* in: OPTIONAL */
            string fileName,                /* in */
            ref ScriptDataFlags flags,      /* in, out */
            ref StringDictionary settings,  /* in, out */
            ref Result error                /* out */
            )
        {
            GlobalState.PushActiveInterpreter(interpreter, pushClientData);

            try
            {
                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    bool useIsolated = FlagOps.HasFlags(
                        flags, ScriptDataFlags.UseIsolatedInterpreter, true);

                    bool noCreate = FlagOps.HasFlags(
                        flags, ScriptDataFlags.NoCreateInterpreter, true);

                    bool staticData = FlagOps.HasFlags(
                        flags, ScriptDataFlags.UseStaticDataOnly, true);

                    bool fastData = FlagOps.HasFlags(
                        flags, ScriptDataFlags.FastStaticDataOnly, true);

#if NETWORK
                    bool trusted = FlagOps.HasFlags(
                        flags, ScriptDataFlags.ForceTrustedUri, true);
#endif

                    //
                    // NOTE: *POLICY* The default behavior is to create a new
                    //       interpreter for each "settings" (script) file to
                    //       be loaded (i.e. evaluated).
                    //
                    if (noCreate)
                    {
                        //
                        // HACK: Isolated interpreter support is not available
                        //       unless a new interpreter is being created, due
                        //       to them being created as child interpreters in
                        //       the primary interpreter (i.e. their lifetimes
                        //       are limited to that of the primary interpreter
                        //       hence they cannot easily be cached).
                        //
                        if (useIsolated)
                        {
                            error = "isolated interpreters must be created";
                            code = ReturnCode.Error;

                            return code;
                        }

                        Interpreter localInterpreter = null;

                        try
                        {
                            bool wasCreated = false;

                            bool useSafe = FlagOps.HasFlags(
                                flags, ScriptDataFlags.UseSafeInterpreter, true);

                            bool cacheSafe = FlagOps.HasFlags(
                                flags, ScriptDataFlags.CacheSafeInterpreter, true);

                            //
                            // NOTE: *POLICY* For security, only "safe" interpreters
                            //       can be cached for later use.  This will help to
                            //       avoid confusion in the event (various?) callers
                            //       request both "safe" and "unsafe" interpreters.
                            //
                            if (useSafe && cacheSafe)
                            {
                                AddExitedEventHandler();

                                lock (syncRoot) /* TRANSACTIONAL */
                                {
                                    if (cachedSafeInterpreter != null)
                                        localInterpreter = cachedSafeInterpreter;
                                }

                                if (localInterpreter == null)
                                {
                                    localInterpreter = CreateInterpreterForSettings(
                                        interpreter, callbackClientData, flags,
                                        ref result);

                                    if (localInterpreter == null)
                                    {
                                        error = result;
                                        code = ReturnCode.Error;

                                        return code;
                                    }

                                    wasCreated = true;
                                }

                                if (staticData)
                                {
                                    if (wasCreated)
                                    {
                                        code = PrepareForStaticData(
                                            localInterpreter, fastData, ref error);

                                        if (code != ReturnCode.Ok)
                                            return code;

                                        lock (syncRoot) /* TRANSACTIONAL */
                                        {
                                            cachedSafeInterpreter = localInterpreter;
                                        }
                                    }
                                    else
                                    {
                                        //
                                        // HACK: Since a cached interpreter is being
                                        //       reused, it may have leftover global
                                        //       variables; make sure to clear them.
                                        //       Since this subsystem always uses the
                                        //       global call frame, there is no need
                                        //       to worry about namespace variables.
                                        //
                                        code = ClearVariables(
                                            localInterpreter, false, ref error);

                                        if (code != ReturnCode.Ok)
                                            return code;
                                    }
                                }
                            }
                            else
                            {
                                localInterpreter = interpreter;

                                if (localInterpreter == null)
                                {
                                    error = "invalid interpreter";
                                    code = ReturnCode.Error;

                                    return code;
                                }

                                if (useSafe != localInterpreter.InternalIsSafe())
                                {
                                    error = String.Format(
                                        "interpreter is not \"{0}\"",
                                        useSafe ? "safe" : "unsafe");

                                    code = ReturnCode.Error;

                                    return code;
                                }

                                if (staticData)
                                {
                                    code = PrepareForStaticData(
                                        localInterpreter, fastData, ref error);

                                    if (code != ReturnCode.Ok)
                                        return code;
                                }
                            }

                            if (!wasCreated)
                            {
                                EventCallback useInterpreterCallback =
                                    Interpreter.UseInterpreterCallback;

                                if (useInterpreterCallback != null)
                                {
                                    code = useInterpreterCallback(
                                        localInterpreter, callbackClientData,
                                        ref result);

                                    if (code != ReturnCode.Ok)
                                    {
                                        error = result;
                                        return code;
                                    }
                                }
                            }

                            code = localInterpreter.ResetCancel(
                                CancelFlags.Settings, ref error);

                            if (code != ReturnCode.Ok)
                                return ReturnCode.Error;

                            code = EvaluateFileForSettings(
                                localInterpreter, fileName,
#if NETWORK
                                trusted,
#endif
                                ref error);

                            if (code != ReturnCode.Ok)
                                return code;

                            lock (localInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                StringDictionary localSettings = CopySettings(
                                    settings, false);

                                code = ExtractSettingsFromFrame(
                                    localInterpreter.CurrentGlobalFrame,
                                    flags, ref localSettings, ref error);

                                if (code == ReturnCode.Ok)
                                    settings = localSettings;
                            }
                        }
                        finally
                        {
                            if (localInterpreter != null)
                            {
                                EventCallback freeInterpreterCallback =
                                    Interpreter.FreeInterpreterCallback;

                                if (freeInterpreterCallback != null)
                                {
                                    ReturnCode freeCode;
                                    Result freeResult = null;

                                    freeCode = freeInterpreterCallback(
                                        localInterpreter, callbackClientData,
                                        ref freeResult);

                                    if (freeCode != ReturnCode.Ok)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "LoadSettingsViaFile: free error = {0}",
                                            FormatOps.WrapOrNull(freeResult)),
                                            typeof(ScriptOps).Name,
                                            TracePriority.EngineError);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Interpreter localInterpreter = null;
                        string childName = null;

                        try
                        {
                            localInterpreter = CreateInterpreterForSettings(
                                interpreter, callbackClientData, flags,
                                ref childName, ref result);

                            if (localInterpreter == null)
                            {
                                error = result;
                                code = ReturnCode.Error;

                                return code;
                            }

                            if (staticData)
                            {
                                code = PrepareForStaticData(
                                    localInterpreter, fastData, ref error);

                                if (code != ReturnCode.Ok)
                                    return code;
                            }

                            code = localInterpreter.ResetCancel(
                                CancelFlags.Settings, ref error);

                            if (code != ReturnCode.Ok)
                                return ReturnCode.Error;

                            code = EvaluateFileForSettings(
                                localInterpreter, fileName,
#if NETWORK
                                trusted,
#endif
                                ref error);

                            if (code != ReturnCode.Ok)
                                return code;

                            lock (localInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                StringDictionary localSettings = CopySettings(
                                    settings, false);

                                code = ExtractSettingsFromFrame(
                                    localInterpreter.CurrentGlobalFrame,
                                    flags, ref localSettings, ref error);

                                if (code == ReturnCode.Ok)
                                    settings = localSettings;
                            }
                        }
                        finally
                        {
                            if (localInterpreter != null)
                            {
                                EventCallback freeInterpreterCallback =
                                    Interpreter.FreeInterpreterCallback;

                                if (freeInterpreterCallback != null)
                                {
                                    ReturnCode freeCode;
                                    Result freeResult = null;

                                    freeCode = freeInterpreterCallback(
                                        localInterpreter, callbackClientData,
                                        ref freeResult);

                                    if (freeCode != ReturnCode.Ok)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "LoadSettingsViaFile: free error = {0}",
                                            FormatOps.WrapOrNull(freeResult)),
                                            typeof(ScriptOps).Name,
                                            TracePriority.EngineError);
                                    }
                                }
                            }

                            //
                            // BUGFIX: If the created interpreter is isolated
                            //         then it was created as a child of the
                            //         primary.  In that case, we must remove
                            //         its entry from the primary.
                            //
                            if (childName != null)
                            {
                                ReturnCode removeCode;
                                Result removeError = null;

                                removeCode = interpreter.RemoveChildInterpreter(
                                    childName, null, ref removeError);

                                if (removeCode != ReturnCode.Ok)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "LoadSettingsViaFile: remove error = {0}",
                                        FormatOps.WrapOrNull(removeError)),
                                        typeof(ScriptOps).Name,
                                        TracePriority.EngineError);
                                }
                            }
                            else if (localInterpreter != null)
                            {
                                ReturnCode disposeCode;
                                Result disposeError = null;

                                disposeCode = ObjectOps.TryDispose<Interpreter>(
                                    ref localInterpreter, ref disposeError);

                                if (disposeCode != ReturnCode.Ok)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "LoadSettingsViaFile: dispose error = {0}",
                                        FormatOps.WrapOrNull(disposeError)),
                                        typeof(ScriptOps).Name,
                                        TracePriority.EngineError);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    code = ReturnCode.Error;
                }
                finally
                {
                    TraceOps.DebugTrace(String.Format(
                        "LoadSettingsViaFile: interpreter = {0}, " +
                        "pushClientData = {1}, callbackClientData = {2}, " +
                        "fileName = {3}, flags = {4}, settings = {5}, " +
                        "code = {6}, result = {7}, error = {8}",
                        FormatOps.InterpreterNoThrow(interpreter),
                        FormatOps.WrapOrNull(pushClientData),
                        FormatOps.WrapOrNull(callbackClientData),
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.WrapOrNull(flags),
                        FormatOps.KeysAndValues(settings, true, true, true),
                        code, FormatOps.WrapOrNull(true, true, result),
                        FormatOps.WrapOrNull(true, true, error)),
                        typeof(ScriptOps).Name, TracePriority.EngineDebug);
                }

                return code;
            }
            finally
            {
                GlobalState.PopActiveInterpreter();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTemporaryFileName(
            ref string fileName, /* out */
            ref Result error     /* out */
            )
        {
            ReturnCode code = ReturnCode.Error;
            string[] fileNames = { null, null };

            try
            {
                //
                // NOTE: First, just obtain a temporary file name from the
                //       operating system.
                //
                fileNames[0] = PathOps.GetTempFileName(); /* throw */

                if (!String.IsNullOrEmpty(fileNames[0]))
                {
                    //
                    // NOTE: Next, append the script file extension (i.e.
                    //       ".eagle") to it.
                    //
                    fileNames[1] = String.Format(
                        "{0}{1}", fileNames[0], FileExtension.Script);

                    //
                    // NOTE: Finally, move the temporary file, atomically,
                    //       to the new name.
                    //
                    File.Move(fileNames[0], fileNames[1]); /* throw */

                    //
                    // NOTE: If we got this far, everything should be
                    //       completely OK.
                    //
                    fileName = fileNames[1];
                    code = ReturnCode.Ok;
                }
                else
                {
                    error = "invalid temporary file name";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // NOTE: If we created a temporary file, always delete it
                //       prior to returning from this method.
                //
                if (code != ReturnCode.Ok)
                {
                    if (fileNames[1] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[1]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[1] = null;
                    }

                    if (fileNames[0] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[0]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[0] = null;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateTemporaryFile(
            string text,         /* in */
            Encoding encoding,   /* in: OPTIONAL */
            ref string fileName, /* out */
            ref Result error     /* out */
            )
        {
            if (text == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Error;
            string localFileName = null;

            try
            {
                //
                // NOTE: First, attempt to obtain a temporary script file
                //       name (i.e. with an ".eagle" extension).
                //
                code = GetTemporaryFileName(ref localFileName, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                //
                // NOTE: Next, attempt to write the specified script text
                //       into the temporary file, maybe using an encoding
                //       specified by the caller.
                //
                if (encoding != null)
                {
                    File.WriteAllText(
                        localFileName, text, encoding); /* throw */
                }
                else
                {
                    File.WriteAllText(localFileName, text); /* throw */
                }

                //
                // NOTE: If we got this far, everything should have
                //       succeeded.  Make sure the caller has the
                //       script file name containing their specified
                //       content.
                //
                fileName = localFileName;
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                //
                // NOTE: If we created a temporary file, always delete it
                //       prior to returning from this method.
                //
                if (code != ReturnCode.Ok)
                {
                    if (localFileName != null)
                    {
                        try
                        {
                            File.Delete(localFileName); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        localFileName = null;
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Support Methods
        public static ScriptFlags GetFlags(
            Interpreter interpreter, /* in */
            ScriptFlags scriptFlags, /* in */
            bool getDataFile,        /* in */
            bool noFileSystem        /* in */
            )
        {
            if (getDataFile && FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.UseDefault, true))
            {
                scriptFlags |= ScriptFlags.UseDefaultGetDataFile;
            }

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    HostCreateFlags hostCreateFlags = interpreter.HostCreateFlags;

                    if (FlagOps.HasFlags(
                            hostCreateFlags, HostCreateFlags.UseLibrary, true))
                    {
                        scriptFlags &= ~ScriptFlags.PreferFileSystem;
                    }
                }
            }

            if (noFileSystem)
            {
                //
                // BUGFIX: Forbid "host-only" package index files
                //         from being read from the file system.
                //
                scriptFlags &= ~ScriptFlags.PreferFileSystem;
                scriptFlags |= ScriptFlags.NoFileSystem;
            }

            return scriptFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetFile(
            Interpreter interpreter,
            string directory,
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData, /* NOT USED */
            ref Result result
            )
        {
            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoLibraryFile, true))
            {
                result = String.Format(
                    "cannot find a suitable \"{0}\" script, file system " +
                    "disallowed", name);

                return ReturnCode.Error;
            }

            //
            // NOTE: Check for the script in the specified directory.
            //
            string fileName = PathOps.GetNativePath(PathOps.CombinePath(
                null, directory, name));

            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                scriptFlags |= ScriptFlags.File;
                result = fileName;

                return ReturnCode.Ok;
            }
            else if (!FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoAutoPath, true))
            {
                //
                // NOTE: Check for the script on disk in the directories
                //       listed in the auto-path.
                //
                StringList autoPathList = GlobalState.GetAutoPathList(
                    interpreter, false);

                if (autoPathList != null)
                {
                    foreach (string path in autoPathList)
                    {
                        fileName = PathOps.GetNativePath(
                            PathOps.CombinePath(null, path, name));

                        if (!String.IsNullOrEmpty(fileName) &&
                            File.Exists(fileName))
                        {
                            scriptFlags |= ScriptFlags.File;
                            result = fileName;

                            return ReturnCode.Ok;
                        }
                    }
                }

                result = String.Format(
                    "cannot find a suitable \"{0}\" script in \"{1}\"",
                    name, autoPathList);
            }
            else
            {
                result = String.Format(
                    "cannot find a suitable \"{0}\" script in \"{1}\"",
                    name, fileName);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetLibraryFile(
            Interpreter interpreter,
            string directory,
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result,
            ref ResultList errors
            )
        {
            Result localResult = null;

            if (GetFile(
                    interpreter, directory, name, ref scriptFlags,
                    ref clientData, ref localResult) == ReturnCode.Ok)
            {
                result = localResult;

                return ReturnCode.Ok;
            }
            else if (localResult != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localResult);
            }

            //
            // TODO: Under what conditions should the following block of code
            //       be necessary?
            //
            if (!FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoLibraryFileNameOnly, true))
            {
                localResult = null;

                if (PathOps.HasDirectory(name) && (GetFile(
                        interpreter, directory, PathOps.ScriptFileNameOnly(
                        name), ref scriptFlags, ref clientData,
                        ref localResult) == ReturnCode.Ok))
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLibrary(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            bool direct,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            )
        {
            ResultList errors = null;
            Result localResult = null;

            //
            // NOTE: Query the primary root directory where the Eagle core
            //       script library files should be found (e.g. something
            //       like "<dir>\lib\Eagle1.0\init.eagle", where "<dir>" is
            //       the value we are looking for)?
            //
            string directory = GlobalState.GetLibraryPath(
                interpreter, false, false);

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.PreferFileSystem, true))
            {
                localResult = null;

                if (GetLibraryFile(interpreter,
                        directory, name, ref scriptFlags, ref clientData,
                        ref localResult, ref errors) == ReturnCode.Ok)
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else
                {
                    localResult = null;

                    if (HostOps.GetScript(
                            interpreter, fileSystemHost, name,
                            direct, ref scriptFlags, ref clientData,
                            ref localResult) == ReturnCode.Ok)
                    {
                        result = localResult;

                        return ReturnCode.Ok;
                    }
                    else if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }
                }
            }
            else
            {
                localResult = null;

                if (HostOps.GetScript(
                        interpreter, fileSystemHost, name,
                        direct, ref scriptFlags, ref clientData,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else
                {
                    if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }

                    localResult = null;

                    if (GetLibraryFile(interpreter,
                            directory, name, ref scriptFlags, ref clientData,
                            ref localResult, ref errors) == ReturnCode.Ok)
                    {
                        result = localResult;

                        return ReturnCode.Ok;
                    }
                }
            }

            result = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetStartup(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            bool direct,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result,
            ref ResultList errors
            )
        {
            Result localResult = null;

            if (HostOps.GetScript(
                    interpreter, fileSystemHost, name,
                    direct, ref scriptFlags, ref clientData,
                    ref localResult) == ReturnCode.Ok)
            {
                result = localResult;

                return ReturnCode.Ok;
            }
            else
            {
                if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }

                localResult = null;

                if ((interpreter != null) && interpreter.GetVariableValue(
                        VariableFlags.GlobalOnly | VariableFlags.ViaShell,
                        name, ref localResult) == ReturnCode.Ok)
                {
                    string localName = localResult;

                    localResult = null;

                    if (HostOps.GetScript(
                            interpreter, fileSystemHost, localName,
                            direct, ref scriptFlags, ref clientData,
                            ref localResult) == ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(
                                scriptFlags, ScriptFlags.File, true) &&
                            !PathOps.IsRemoteUri(localResult))
                        {
                            result = PathOps.ResolveFullPath(
                                interpreter, localResult);
                        }
                        else
                        {
                            result = localResult;
                        }

                        return ReturnCode.Ok;
                    }
                    else if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }
                }
                else if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        public static int GetSubCommandNameIndex()
        {
            return DefaultSubCommandNameIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ISubCommand NewDefaultSubCommand(
            string name,
            IClientData clientData,
            ICommand command,
            SubCommandFlags subCommandFlags,
            Delegate @delegate,
            DelegateFlags delegateFlags
            )
        {
            return new _SubCommands.Default(new SubCommandData(
                name, null, null, clientData,
                typeof(_SubCommands.Default).FullName,
                GetSubCommandNameIndex(), CommandFlags.None,
                subCommandFlags, command, 0), new DelegateData(
                @delegate, delegateFlags, 0));
        }

        ///////////////////////////////////////////////////////////////////////

        public static ISubCommand NewCommandSubCommand(
            string name,
            IClientData clientData,
            ICommand command,
            StringList scriptCommand,
            int nameIndex,
            SubCommandFlags subCommandFlags
            )
        {
            return new _SubCommands.Command(new SubCommandData(
                name, null, null, ClientData.WrapOrReplace(clientData,
                scriptCommand), typeof(_SubCommands.Command).FullName,
                nameIndex, CommandFlags.None, subCommandFlags, command,
                0));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameForExecute(
            string name,
            ISubCommand subCommand
            ) /* MAY RETURN NULL */
        {
            if (subCommand == null)
                return name; /* NULL? */

            string commandName = null;
            ICommand command = subCommand.Command;

            if (command != null)
                commandName = command.Name;

            string subCommandName = subCommand.Name;

            if (commandName != null)
            {
                if (name != null)
                {
                    return StringList.MakeList(
                        commandName, name); /* NOT NULL */
                }
                else if (subCommandName != null)
                {
                    return StringList.MakeList(
                        commandName, subCommandName); /* NOT NULL */
                }
                else
                {
                    return commandName; /* NOT NULL */
                }
            }
            else if (name != null)
            {
                return name; /* NOT NULL */
            }
            else if (subCommandName != null)
            {
                return subCommandName; /* NOT NULL */
            }
            else
            {
                return null; /* NULL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ArgumentList GetArgumentsForExecute(
            IExecute execute, /* NOT USED */
            StringList scriptCommand,
            ArgumentList oldArguments,
            int oldStartIndex
            ) /* CANNOT RETURN NULL */
        {
            ArgumentList arguments = new ArgumentList();

            if (scriptCommand != null)
                arguments.AddRange(scriptCommand);

            if (oldArguments != null)
            {
                ArgumentList newArguments = new ArgumentList();

                for (int index = oldStartIndex;
                        index < oldArguments.Count; index++)
                {
                    Argument oldArgument = oldArguments[index];

                    if (oldArgument == null)
                    {
                        newArguments.Add(null);
                        continue;
                    }

                    Argument newArgument = (Argument)oldArgument.Clone();

                    newArguments.Add(newArgument);
                }

                arguments.Add(Argument.InternalCreate(newArguments));
            }

            return arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Ensemble Support Methods
        public static void LookupObjectsInArguments(
            Interpreter interpreter,      /* in */
            ArgumentList oldArguments,    /* in */
            out ArgumentList newArguments /* out */
            )
        {
            if (oldArguments == null)
            {
                newArguments = null;
                return;
            }

            ArgumentList localArguments = new ArgumentList();

            foreach (Argument oldArgument in oldArguments)
            {
                if (oldArgument == null)
                {
                    localArguments.Add(null);
                    continue;
                }

                IObject @object = null;

                if (interpreter.GetObject(
                        oldArgument, LookupFlags.NoVerbose,
                        ref @object) != ReturnCode.Ok)
                {
                    localArguments.Add(oldArgument);
                    continue;
                }

                localArguments.Add(Argument.FromObject(
                    @object.Value, false, false, false));
            }

            newArguments = localArguments;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,    /* in */
            IEnsemble ensemble,         /* in: OPTIONAL */
            string type,                /* in */
            bool strict,                /* in */
            bool noCase,                /* in */
            ref string name,            /* in, out */
            ref ISubCommand subCommand, /* out */
            ref Result error            /* out */
            )
        {
            return SubCommandFromEnsemble(
                interpreter, ensemble, PolicyOps.OnlyAllowedSubCommands,
                type, strict, noCase, ref name, ref subCommand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name                    /* in, out */
            )
        {
            Result error = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, callback, type, strict,
                noCase, ref name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref Result error                   /* out */
            )
        {
            ISubCommand subCommand = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, callback, type, strict,
                noCase, ref name, ref subCommand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref ISubCommand subCommand,        /* out */
            ref Result error                   /* out */
            )
        {
            return SubCommandFromEnsemble(
                interpreter, ensemble, PolicyOps.GetSubCommandsUnsafe(
                ensemble), callback, type, strict, noCase, ref name,
                ref subCommand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,        /* in */
            EnsembleDictionary subCommands, /* in */
            string type,                    /* in */
            bool strict,                    /* in */
            bool noCase,                    /* in */
            ref string name,                /* in, out */
            ref Result error                /* out */
            )
        {
            return SubCommandFromEnsemble(
                interpreter, null, subCommands, null, type,
                strict, noCase, ref name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref Result error                   /* out */
            )
        {
            ISubCommand subCommand = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, subCommands, callback,
                type, strict, noCase, ref name, ref subCommand,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in: OPTIONAL */
            string type,                       /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref ISubCommand subCommand,        /* out */
            ref Result error                   /* out */
            )
        {
            //
            // NOTE: *WARNING* Empty sub-command names are allowed, please
            //       do not change this to "!String.IsNullOrEmpty".
            //
            if (name == null)
            {
                error = "invalid sub-command name";
                return ReturnCode.Error;
            }

            if (subCommands == null)
            {
                error = "invalid sub-commands";
                return ReturnCode.Error;
            }

            if (subCommands.Count == 0)
            {
                if (strict)
                {
                    error = BadSubCommand(interpreter,
                        null, type, name, (EnsembleDictionary)null,
                        null, null);

                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            //
            // NOTE: Always try for an exact match first.  Some callers
            //       of this method may require this behavior, e.g. the
            //       built-in sub-command policy implementation.  Upon
            //       a successful match here, skip setting the name
            //       output parameter because it already contains the
            //       correct value.
            //
            ISubCommand localSubCommand;

            if (subCommands.TryGetValue(name, out localSubCommand))
            {
                subCommand = localSubCommand;
                return ReturnCode.Ok;
            }

            bool exact = false;

            IList<KeyValuePair<string, ISubCommand>> matches =
                new List<KeyValuePair<string, ISubCommand>>();

            int nameLength = name.Length;

            StringComparison comparisonType =
                SharedStringOps.GetSystemComparisonType(noCase);

            foreach (KeyValuePair<string, ISubCommand> pair in subCommands)
            {
                string key = pair.Key;

                if ((key == null) || !SharedStringOps.Equals(
                        key, 0, name, 0, nameLength, comparisonType))
                {
                    continue;
                }

                //
                // NOTE: Did we match the whole string, regardless of
                //       case?
                //
                bool whole = (key.Length == nameLength);

                //
                // NOTE: Was it an exact match or did we match at least
                //       one character in a partial match?
                //
                if (whole || (nameLength > 0))
                {
                    //
                    // NOTE: Store the exact or partial match in the
                    //       result list.
                    //
                    matches.Add(pair);

                    //
                    // NOTE: It was a match; however, was it exact?
                    //       This condition cannot be hit now unless
                    //       the noCase flag is set because the exact
                    //       matches are now short-circuited before
                    //       this loop.
                    //
                    if (whole)
                    {
                        //
                        // NOTE: For the purposes of this method, an
                        //       "exact" match requires a comparison
                        //       type of case-sensitive.
                        //
                        exact = !noCase;

                        //
                        // NOTE: Always stop on the first exact match.
                        //
                        break;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (callback != null)
            {
                //
                // NOTE: Use the callback to filter the list of matched
                //       sub-commands.  This is (always?) necessary just
                //       in case the caller specified an unfiltered list
                //       of sub-commands to match against.
                //
                Result localError = null;

                matches = callback(
                    interpreter, ensemble, matches, ref localError)
                    as IList<KeyValuePair<string, ISubCommand>>;

                //
                // NOTE: If the callback returns null, that indicates an
                //       unexpected failure and we cannot continue.
                //
                if (matches == null)
                {
                    if (localError != null)
                    {
                        error = localError;
                    }
                    else
                    {
                        //
                        // TODO: Good fallback error message?
                        //
                        error = "sub-command filter failed (matched)";
                    }

                    return ReturnCode.Error;
                }

                //
                // NOTE: If there are now no matches, use the callback to
                //       filter the list of available sub-commands, which
                //       will be used to build the error message (below).
                //
                if (matches.Count == 0)
                {
                    IList<KeyValuePair<string, ISubCommand>> localSubCommands;

                    localError = null;

                    localSubCommands = callback(
                        interpreter, ensemble, subCommands, ref localError)
                        as IList<KeyValuePair<string, ISubCommand>>;

                    //
                    // NOTE: If the callback returns null, that indicates an
                    //       unexpected failure and we cannot continue.
                    //
                    if (localSubCommands == null)
                    {
                        if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            //
                            // TODO: Good fallback error message?
                            //
                            error = "sub-command filter failed (all)";
                        }

                        return ReturnCode.Error;
                    }

                    //
                    // TODO: At this point, the list of sub-commands is only
                    //       going to be used when building the error message;
                    //       therefore, make sure it is set to the (possibly
                    //       filtered) new list of sub-commands first.  This
                    //       list is ONLY used when the number of matches is
                    //       exactly zero.  If this method ever changes that
                    //       assumption, the containing "if" statement will
                    //       need to be updated as well.
                    //
                    subCommands = new EnsembleDictionary(localSubCommands);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (matches.Count == 1)
            {
                //
                // NOTE: Normal "success" case, exactly one sub-command
                //       matched.  If this was an exact match, including
                //       case, skip setting the name output parameter
                //       because it already contains the correct value.
                //
                if (!exact)
                    name = matches[0].Key;

                subCommand = matches[0].Value;

                return ReturnCode.Ok;
            }
            else if (matches.Count > 1)
            {
                error = BadSubCommand(
                    interpreter, "ambiguous", type, name, matches,
                    null, null);

                return ReturnCode.Error;
            }
            else if (strict)
            {
                error = BadSubCommand(
                    interpreter, null, type, name, subCommands,
                    null, null);

                return ReturnCode.Error;
            }
            else
            {
                //
                // NOTE: Non-strict mode, leave the original sub-command
                //       unchanged and let the caller deal with it.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteSubCommandFromEnsemble(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ISubCommand subCommand,  /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref bool tried,          /* out */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: Do not allow arbitrary nesting levels for sub-commands
            //       as we could easily run out of native stack space.
            //
            if (interpreter.EnterSubCommandLevel() < 2)
            {
                try
                {
                    //
                    // NOTE: Indicate to the caller that the sub-command has
                    //       been dispatched (i.e. there is no need for the
                    //       caller to handle this sub-command).  Even if
                    //       the execution fails, we still tried to execute
                    //       it and the caller should not try to handle it.
                    //
                    tried = true;

                    return interpreter.Execute(
                        name, subCommand, clientData, arguments, ref result);
                }
                finally
                {
                    //
                    // NOTE: Remove the sub-command level added by the if
                    //       statement above.
                    //
                    interpreter.ExitSubCommandLevel();
                }
            }
            else
            {
                //
                // NOTE: Remove the "trial" sub-command level added by the
                //       if statement above.
                //
                interpreter.ExitSubCommandLevel();
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter, /* in */
            IEnsemble ensemble,      /* in: OPTIONAL */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            bool strict,             /* in */
            bool noCase,             /* in */
            ref string name,         /* in, out */
            ref bool tried,          /* out */
            ref Result result        /* out */
            )
        {
            ISubCommand subCommand = null;

            return TryExecuteSubCommandFromEnsemble(
                interpreter, ensemble, clientData, arguments, null, strict,
                noCase, ref name, ref subCommand, ref tried, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,    /* in */
            IEnsemble ensemble,         /* in: OPTIONAL */
            IClientData clientData,     /* in */
            ArgumentList arguments,     /* in */
            string type,                /* in */
            bool strict,                /* in */
            bool noCase,                /* in */
            ref string name,            /* in, out */
            ref ISubCommand subCommand, /* out */
            ref bool tried,             /* out */
            ref Result result           /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to lookup the sub-command based on the
            //       name and the parent ensemble.
            //
            if (SubCommandFromEnsemble(
                    interpreter, ensemble, PolicyOps.OnlyAllowedSubCommands,
                    type, strict, noCase, ref name, ref subCommand,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: If the sub-command was found and is null, treat
            //       that as "handled by the caller" and just return
            //       success; otherwise, attempt to redispatch it.
            //
            if (subCommand == null)
                return ReturnCode.Ok;

            return ExecuteSubCommandFromEnsemble(
                interpreter, GetNameForExecute(name, subCommand),
                subCommand, clientData, arguments, ref tried,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Delegate Support Methods
        public static ReturnCode ExecuteOrInvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            ArgumentList arguments,
            int nameCount,
            DelegateFlags delegateFlags,
            ref Result result
            )
        {
            if (FlagOps.HasFlags(
                    delegateFlags, DelegateFlags.UseEngine, true))
            {
                return Engine.ExecuteDelegate(
                    @delegate, arguments, ref result);
            }
            else
            {
                return ObjectOps.InvokeDelegate(
                    interpreter, @delegate, delegateFlags,
                    arguments, nameCount, ref result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Message Support Methods
        public static Exception GetBaseException(
            Exception exception
            )
        {
            if (exception == null)
                return null;

            Exception baseException = exception.GetBaseException();

            if (baseException != null)
                return baseException;

            return exception;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static Exception GetInnerException(
            Exception exception
            )
        {
            if (exception == null)
                return null;

            Exception innerException = exception.InnerException;

            if (innerException != null)
                return innerException;

            return exception;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            StringSortedList values,
            string prefix,
            string suffix
            )
        {
            if ((values != null) && (values.Count > 0))
            {
                return String.Format("{0} {1} \"{2}\": must be {3}{4}",
                    !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                    !String.IsNullOrEmpty(type) ? type : "value", value,
                    GenericOps<string>.DictionaryToEnglish(
                        values, ", ", Characters.Space.ToString(),
                        !String.IsNullOrEmpty(suffix) ? null : "or ",
                        prefix, null),
                    suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return String.Format(
                "{0} {1} \"{2}\"",
                !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                !String.IsNullOrEmpty(type) ? type : "value", value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            IEnumerable<string> values,
            string prefix,
            string suffix
            )
        {
            return BadValue(
                adjective, type, value, (values != null) ?
                new StringSortedList(values) : null, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            IEnsemble ensemble,
            string prefix,
            string suffix
            )
        {
            return BadSubCommand(
                interpreter, adjective, type, subCommand,
                PolicyOps.GetSubCommandsSafe(interpreter, ensemble),
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter, /* NOT USED */
            string adjective,
            string type,
            string subCommand,
            EnsembleDictionary subCommands,
            string prefix,
            string suffix
            )
        {
            if ((subCommands != null) && (subCommands.Count > 0))
            {
                bool exists = (subCommand != null) ?
                    subCommands.ContainsKey(subCommand) /* EXEMPT */ :
                    false;

                //
                // BUGFIX: If the sub-command exists in the dictionary,
                //         it must simply be "unsupported" (i.e. not
                //         really implemented) by the parent command.
                //         In that case, construct a good error message.
                //
                EnsembleDictionary localSubCommands;

                if (exists)
                {
                    //
                    // NOTE: Clone the dictionary and then remove the
                    //       "unsupported" sub-command so that it will
                    //       NOT appear in the error message.
                    //
                    localSubCommands = new EnsembleDictionary(
                        subCommands);

                    /* IGNORED */
                    localSubCommands.Remove(subCommand);
                }
                else
                {
                    localSubCommands = subCommands;
                }

                return BadValue(!String.IsNullOrEmpty(adjective) ?
                    adjective : (exists ? "unsupported" : "bad"),
                    !String.IsNullOrEmpty(type) ? type : "option",
                    subCommand, localSubCommands.Keys, prefix, suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return String.Format("{0} {1} \"{2}\"",
                !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                !String.IsNullOrEmpty(type) ? type : "option", subCommand);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            IEnumerable<KeyValuePair<string, ISubCommand>> subCommands,
            string prefix,
            string suffix
            )
        {
            return BadValue(
                adjective, !String.IsNullOrEmpty(type) ? type : "option",
                subCommand, (subCommands != null) ? new StringSortedList(
                subCommands) : null, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result WrongNumberOfArguments(
            IIdentifierName identifierName, /* NOT USED */
            int count,
            ArgumentList arguments,
            string suffix
            )
        {
            if ((count > 0) &&
                (arguments != null) &&
                (arguments.Count > 0))
            {
                return String.Format(
                    "wrong # args: should be \"{0}{1}{2}\"",
                    ArgumentList.GetRange(arguments, 0, Math.Min(count - 1,
                        arguments.Count - 1)), !String.IsNullOrEmpty(
                    suffix) ? Characters.Space.ToString() : null, suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return "wrong # args";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Support Methods
        public static ReturnCode GetOptionValue(
            Interpreter interpreter,
            StringList list,
            Type type,
            OptionFlags optionFlags,
            bool force,
            bool allowInteger,
            bool strict,
            bool noCase,
            CultureInfo cultureInfo,
            ref Variant value,
            ref int nextIndex,
            ref Result error
            )
        {
            if ((nextIndex < list.Count) && (force || FlagOps.HasFlags(
                    optionFlags, OptionFlags.MustHaveValue, true)))
            {
                if (FlagOps.HasFlags(
                        optionFlags, OptionFlags.MatchOldValueType, true))
                {
                    OptionFlags notHasFlags = OptionFlags.MustBeMask;

                    if ((type != null) && type.IsEnum)
                        notHasFlags &= ~OptionFlags.MustBeEnumMask;

                    if (FlagOps.HasFlags(optionFlags, notHasFlags, false))
                    {
                        error = String.Format(
                            "cannot convert old value for option with flags {0}",
                            FormatOps.WrapOrNull(optionFlags));

                        return ReturnCode.Error;
                    }

                    if ((type != null) && type.IsEnum)
                    {
                        object enumValue;

                        if (EnumOps.IsFlags(type))
                        {
                            enumValue = EnumOps.TryParseFlags(
                                interpreter, type, null, list[nextIndex],
                                cultureInfo, allowInteger, strict, noCase,
                                ref error);
                        }
                        else
                        {
                            enumValue = EnumOps.TryParse(
                                type, list[nextIndex], allowInteger, noCase,
                                ref error);
                        }

                        if (enumValue == null)
                            return ReturnCode.Error;

                        value = new Variant((Enum)enumValue);
                    }
                    else
                    {
                        value = new Variant(list[nextIndex]);
                    }
                }
                else
                {
                    value = new Variant(list[nextIndex]);
                }

                nextIndex++;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Support Methods
        public static ReturnCode WriteViaIExecute(
            Interpreter interpreter,
            string commandName, /* NOTE: Almost always null, for [puts]. */
            string channelId,   /* NOTE: Almost always null, for "stdout". */
            string value,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (commandName == null)
            {
                commandName = ScriptOps.TypeNameToEntityName(
                    typeof(_Commands.Puts));
            }

            if (channelId == null)
                channelId = StandardChannel.Output;

            ReturnCode code;
            IExecute execute = null;

            code = interpreter.GetIExecuteViaResolvers(
                interpreter.GetResolveEngineFlagsNoLock(true), commandName,
                null, LookupFlags.Default, ref execute, ref result);

            if (code != ReturnCode.Ok)
                return code;

            //
            // WARNING: This (indirectly) uses ContextEngineFlags.
            //
            code = Engine.ExternalExecuteWithFrame(
                commandName, execute, interpreter, null, new ArgumentList(
                    commandName, channelId, value), interpreter.EngineFlags,
                interpreter.SubstitutionFlags, interpreter.EngineEventFlags,
                interpreter.ExpressionFlags, ref result);

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Support Methods
        public static ReturnCode CheckVariableStatus(
            string varName,
            IVariable localVariable,
            ref VariableFlags variableFlags,
            ref Result error
            )
        {
            //
            // NOTE: Now, see if they require that this be an undefined
            //       variable (or not).
            //
            bool isUndefined = EntityOps.IsUndefined(localVariable);

            bool wantDefined = FlagOps.HasFlags(
                variableFlags, VariableFlags.Defined, true);

            bool wantUndefined = FlagOps.HasFlags(
                variableFlags, VariableFlags.Undefined, true);

            if ((wantDefined || wantUndefined) &&
                (!wantDefined || isUndefined) &&
                (!wantUndefined || !isUndefined))
            {
                //
                // BUGFIX: If the variable is undefined, we must set
                //         the "NotFound" flag because an undefined
                //         variable should be treated just like one
                //         that is physically missing from the call
                //         frame.
                //
                if (isUndefined)
                    variableFlags |= VariableFlags.NotFound;

                //
                // HACK: This is on the "hot path" for WaitVariable
                //       on variables that do not exist; therefore,
                //       nothing expensive (e.g. String.Format, etc)
                //       should be done here.
                //
                // BUGBUG: Just how slow is String.Format and why?
                //
                // error = String.Format(
                //     "can't get {0}: variable {1} defined",
                //     FormatOps.ErrorVariableName(varName, null),
                //     isUndefined ? "isn't" : "is");
                //
                error = isUndefined ?
                    "variable isn't defined" : "variable is defined";

                return ReturnCode.Error;
            }

            //
            // NOTE: Allow virtual variables to be returned?  If not,
            //       raise an error now.
            //
            bool isVirtual = EntityOps.IsVirtual(localVariable);

            bool wantVirtual = !FlagOps.HasFlags(
                variableFlags, VariableFlags.NonVirtual, true);

            if (!isUndefined && !wantVirtual && isVirtual)
            {
                //
                // NOTE: The variable is virtual and according to the
                //       caller, it should not be.
                //
                variableFlags |= VariableFlags.WasVirtual;

                error = String.Format(
                    "can't get {0}: variable is virtual",
                    FormatOps.ErrorVariableName(varName));

                return ReturnCode.Error;
            }

            //
            // NOTE: Do they want us to verify that the variable does
            //       NOT contain a link index (i.e. is it an [upvar]
            //       link to an array element)?
            //
            bool hasLinkIndex = (localVariable != null) &&
                (localVariable.LinkIndex != null);

            bool noLinkIndex = FlagOps.HasFlags(
                variableFlags, VariableFlags.NoLinkIndex, true);

            if (!isUndefined && hasLinkIndex && noLinkIndex)
            {
                //
                // NOTE: The variable has a link index (to an array
                //       element) and according to the caller, it
                //       should not.
                //
                variableFlags |= VariableFlags.HasLinkIndex;

                error = String.Format(
                    "can't get {0}: variable is array element link",
                    FormatOps.ErrorVariableName(varName));

                return ReturnCode.Error;
            }

            //
            // NOTE: Now, see if they require that this be an array
            //       variable (or not).
            //
            bool isArray = EntityOps.IsArray2(localVariable);

            bool wantArray = FlagOps.HasFlags(
                variableFlags, VariableFlags.Array, true);

            bool wantNoArray = FlagOps.HasFlags(
                variableFlags, VariableFlags.NoArray, true);

            if ((wantArray || wantNoArray) && FlagOps.HasFlags(
                    variableFlags, VariableFlags.NoGetArray, true))
            {
                wantArray = false;
                wantNoArray = false;
            }

            if (!isUndefined && (wantArray || wantNoArray) &&
                (!wantArray || !isArray) && (!wantNoArray || isArray))
            {
                error = String.Format(
                    "can't get {0}: variable {1} array",
                    FormatOps.ErrorVariableName(varName),
                    isArray ? "is" : "isn't");

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Variable Tracing Support Methods
        public static BreakpointType GetBreakpointType(
            bool names,
            bool values
            )
        {
            if (names)
            {
                return values ?
                    BreakpointType.BeforeVariableArrayGet :
                    BreakpointType.BeforeVariableArrayNames;
            }
            else if (values)
            {
                return BreakpointType.BeforeVariableArrayValues;
            }
            else
            {
                return BreakpointType.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetDefaultValue(
            BreakpointType breakpointType
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    return DefaultGetVariableValue;
                case BreakpointType.BeforeVariableSet:
                    return DefaultSetVariableValue;
                case BreakpointType.BeforeVariableUnset:
                    return DefaultUnsetVariableValue;
            }

            return DefaultVariableValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetDefaultValue(
            BreakpointType breakpointType,
            object @default
            )
        {
            if (@default != null)
                return @default;

            return GetDefaultValue(breakpointType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWriteValueTrace(
            BreakpointType breakpointType
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableSet:
                case BreakpointType.BeforeVariableReset:
                case BreakpointType.BeforeVariableUnset:
                    {
                        return true;
                    }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool NeedValueForTrace(
            BreakpointType breakpointType,
            bool old
            )
        {
            if (old)
            {
                switch (breakpointType)
                {
                    case BreakpointType.BeforeVariableReset:
                    case BreakpointType.BeforeVariableUnset:
                        {
                            return true;
                        }
                }
            }
            else if (breakpointType == BreakpointType.BeforeVariableSet)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GatherTraceValues(
            string varName,
            string varIndex,
            object value,
            ElementDictionary arrayValue,
            ref StringList values
            )
        {
            if ((varName == null) && (varIndex == null) &&
                (value == null) && (arrayValue == null))
            {
                return;
            }

            if (varName != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(varName);
            }

            if (varIndex != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(varIndex);
            }

            if (value != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(StringOps.GetStringFromObject(value));
            }

            if ((arrayValue != null) && (arrayValue.Count > 0))
            {
                if (values == null)
                    values = new StringList();

                values.Add(arrayValue.Keys);

                foreach (ArrayPair pair in arrayValue)
                {
                    object localValue = pair.Value;

                    if (localValue == null)
                        continue;

                    values.Add(StringOps.GetStringFromObject(localValue));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessOldObjectsForTrace(
            Interpreter interpreter,             /* in */
            IList<_Wrappers._Object> oldObjects, /* in */
            ref ReturnCode code,                 /* out */
            ref ResultList errors                /* out */
            )
        {
            //
            // If there are no old objects, skip this block.
            //
            if (oldObjects == null)
                return;

            //
            // NOTE: For each old value (i.e. there are potentially multiple
            //       values, maybe even duplicate values, when handling an
            //       array).
            //
            foreach (_Wrappers._Object oldWrapper in oldObjects)
            {
                //
                // NOTE: If the old wrapper object is valid, release a single
                //       reference from it.
                //
                if (oldWrapper == null)
                    continue;

                //
                // NOTE: Grab the object flags now, we may need to use them
                //       multiple times.
                //
                ObjectFlags flags = oldWrapper.ObjectFlags;

                //
                // NOTE: Do not attempt to manage reference counts for locked
                //       objects.
                //
                if (FlagOps.HasFlags(flags, ObjectFlags.Locked, true))
                    continue;

                //
                // NOTE: If there are no more outstanding references to the
                //       underlying object, dipose and remove it now.
                //
                if (oldWrapper.RemoveReference() > 0)
                    continue;

                //
                // NOTE: If there is no interpreter, we cannot remove opaque
                //       object handles.
                //
                if (interpreter == null)
                    continue;

                //
                // NOTE: We know the opaque object handle must be removed;
                //       however, if the opaque object handle is flagged
                //       as "no automatic disposal", we must honor that and
                //       not dispose the actual underlying object instance.
                //
                if (FlagOps.HasFlags(
                        flags, ObjectFlags.NoAutoDispose, true))
                {
                    //
                    // HACK: Prevent the RemoveObject method from actually
                    //       disposing of the object.
                    //
                    flags |= ObjectFlags.NoDispose;
                    oldWrapper.ObjectFlags = flags;
                }

                //
                // NOTE: Attempt to remove the opaque object handle from the
                //       interpreter now.
                //
                ReturnCode removeCode;
                Result removeResult = null;

                removeCode = interpreter.RemoveObject(
                    EntityOps.GetToken(oldWrapper), null,
                    ObjectOps.GetDefaultSynchronous(), ref removeResult);

                if (removeCode != ReturnCode.Ok)
                {
                    //
                    // NOTE: Complain loudly if we could not remove the object
                    //       because this indicates an error probably occurred
                    //       during the disposal of the object?
                    //
                    if (!FlagOps.HasFlags(
                            flags, ObjectFlags.NoRemoveComplain, true))
                    {
                        DebugOps.Complain(
                            interpreter, removeCode, removeResult);
                    }

                    //
                    // NOTE: Keep track of all errors that occur when removing
                    //       any of the opaque object handles.
                    //
                    if (removeResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(removeResult);
                    }

                    //
                    // NOTE: If any of the objects cannot be removed, then the
                    //       overall result will be an error (even if some of
                    //       the objects are successfully removed).
                    //
                    code = ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessNewObjectsForTrace(
            IList<_Wrappers._Object> newObjects /* in */
            )
        {
            //
            // If there are no new objects, skip this block.
            //
            if (newObjects == null)
                return;

            //
            // NOTE: For each new value (i.e. there are potentially multiple
            //       values, maybe even duplicate values, when handling an
            //       array).
            //
            foreach (_Wrappers._Object newWrapper in newObjects)
            {
                //
                // NOTE: If the new wrapper object is valid, add a single
                //       reference to it.
                //
                if (newWrapper == null)
                    continue;

                //
                // NOTE: Grab the object flags now, we may need to use them
                //       multiple times.
                //
                ObjectFlags flags = newWrapper.ObjectFlags;

                //
                // NOTE: Do not attempt to manage reference counts for locked
                //       objects.
                //
                if (!FlagOps.HasFlags(flags, ObjectFlags.Locked, true))
                    newWrapper.AddReference();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void AddOldValuesToTraceInfo(
            EventWaitHandle variableEvent,
            ITraceInfo traceInfo,
            ElementDictionary oldValues
            )
        {
            if ((traceInfo == null) || (oldValues == null))
                return;

            ElementDictionary localOldValues = traceInfo.OldValues;

            if (localOldValues != null)
            {
                localOldValues.Add(oldValues);
            }
            else
            {
                localOldValues = new ElementDictionary(variableEvent);
                localOldValues.Add(oldValues);

                traceInfo.OldValues = localOldValues;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method must return non-zero only if the first trace
        //       list ("traces1") contains all the trace callbacks present
        //       in the second trace list ("traces2").  The caller should
        //       not pass a null value for either parameter as the results
        //       are officially undefined in that case.
        //
        public static bool HasTraceCallbacks(
            TraceList traces1,
            TraceList traces2
            )
        {
            if (traces2 == null)
                return true;

            if (traces1 == null)
                return false;

            foreach (ITrace trace2 in traces2) /* O(N) */
            {
                if (trace2 == null)
                    continue;

                if (AppDomainOps.IsTransparentProxy(trace2))
                    continue;

                bool found = false;

                foreach (ITrace trace1 in traces1) /* O(M) */
                {
                    if (trace1 == null)
                        continue;

                    if (AppDomainOps.IsTransparentProxy(trace1))
                        continue;

                    if (trace1.Callback == trace2.Callback)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ITrace NewCoreTrace(
            TraceCallback callback,
            IClientData clientData,
            TraceFlags traceFlags,
            IPlugin plugin,
            ref Result error
            )
        {
            if (callback != null)
            {
                MethodInfo methodInfo = callback.Method;

                if (methodInfo != null)
                {
                    Type type = methodInfo.DeclaringType;

                    if (type != null)
                    {
                        _Traces.Core trace = new _Traces.Core(new TraceData(
                            FormatOps.TraceDelegateName(callback), null,
                            null, clientData, type.FullName, methodInfo.Name,
                            ObjectOps.GetBindingFlags(MetaBindingFlags.Delegate,
                            true), AttributeOps.GetMethodFlags(methodInfo),
                            traceFlags, plugin, 0));

                        trace.Callback = callback;
                        return trace;
                    }
                    else
                    {
                        error = "invalid trace callback method type";
                    }
                }
                else
                {
                    error = "invalid trace callback method";
                }
            }
            else
            {
                error = "invalid trace callback";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ITraceInfo NewTraceInfo(
            Interpreter interpreter,
            ITrace trace,
            BreakpointType breakpointType,
            ICallFrame frame,
            IVariable variable,
            string name,
            string index,
            VariableFlags flags,
            object oldValue,
            object newValue,
            ElementDictionary oldValues,
            ElementDictionary newValues,
            StringList list,
            bool force,
            bool cancel,
            bool postProcess,
            ReturnCode returnCode
            )
        {
            //
            // HACK: This method is used to prevent creating a ton of redundant
            //       TraceInfo objects on the heap (i.e. whenever a variable is
            //       read, set, or unset).  Now, there is one TraceInfo object
            //       per-thread and it will be re-used as necessary.
            //
            ITraceInfo traceInfo;

            if (!force && (interpreter != null))
            {
                traceInfo = interpreter.TraceInfo;

                if (traceInfo != null)
                {
                    traceInfo = traceInfo.Update(
                       trace, breakpointType, frame, variable, name, index,
                       flags, oldValue, newValue, oldValues, newValues, list,
                       cancel, postProcess, returnCode);

                    if (traceInfo != null)
                        return traceInfo;
                }
            }

            traceInfo = new TraceInfo(
                trace, breakpointType, frame, variable, name, index,
                flags, oldValue, newValue, oldValues, newValues, list,
                cancel, postProcess, returnCode);

            if (!force && (interpreter != null))
                interpreter.TraceInfo = traceInfo;

            return traceInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do NOT call this method from "get" operation traces.  This
        //          method is ONLY for use by variable operations that cannot
        //          return a value (e.g. "set", "unset", "reset", "add").
        //
        public static ReturnCode FireTraces(
            IVariable variable,
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result error
            )
        {
            Result value = null;

            return FireTraces(variable, breakpointType, interpreter,
                traceInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FireTraces(
            IVariable variable,
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result value,
            ref Result error
            )
        {
            if (variable == null)
            {
                error = "invalid variable";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                error = "invalid trace";
                return ReturnCode.Error;
            }

            //
            // NOTE: Save the original return code.  We will need it
            //       later to figure out how to process the trace
            //       callback results.
            //
            ReturnCode localCode = traceInfo.ReturnCode;

            //
            // NOTE: Start off with the original old value from the trace
            //       information object as the local result.  This value
            //       may be overwritten via the fired traces if necessary.
            //
            Result localResult = StringOps.GetResultFromObject(
                traceInfo.OldValue, false);

            //
            // NOTE: Attempt to fire the traces for the variable, if any.
            //
            if (variable.FireTraces(
                    breakpointType, interpreter, traceInfo,
                    ref localResult) == ReturnCode.Ok)
            {
                //
                // HACK: For "get" traces, we need a little bit more magic
                //       here.
                //
                if (breakpointType == BreakpointType.BeforeVariableGet)
                {
                    //
                    // NOTE: Did a trace callback cancel processing of a
                    //       variable operation that was previously regarded
                    //       as unsuccessful?
                    //
                    if ((localCode != ReturnCode.Ok) && traceInfo.Cancel)
                    {
                        //
                        // NOTE: This was a failed "get" operation; however,
                        //       it has been canceled by a trace callback
                        //       (presumably after taking some more meaningful
                        //       action) and is now considered to be successful;
                        //       therefore, place the trace result into the
                        //       OldValue property of the trace object itself,
                        //       if necessary (i.e. it is still null).  Also,
                        //       this relies upon the old value being an actual
                        //       string, not a Result object.
                        //
                        if (traceInfo.OldValue == null)
                        {
                            traceInfo.OldValue = (localResult != null) ?
                                localResult.Value : null;
                        }
                    }
                    else if ((localCode == ReturnCode.Ok) && traceInfo.Cancel)
                    {
                        //
                        // NOTE: This was a successful "get" operation; however,
                        //       it has now been canceled and the OldValue
                        //       property of the trace object will not be used.
                        //       Set the trace result for the caller to grab.
                        //
                        value = localResult;
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: Give the caller the error from the trace callback.
                //
                error = localResult;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Enumerable Variable Support Methods
        public static ReturnCode GetEnumerableVariableItemValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            object value,
            ref object itemValue,
            ref Result error
            )
        {
            IMutableAnyTriplet<IEnumerable, IEnumerator, bool> anyTriplet =
                value as IMutableAnyTriplet<IEnumerable, IEnumerator, bool>;

            if (anyTriplet == null)
            {
                error = String.Format(
                    "can't {0} {1}: broken enumerable",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            IEnumerable collection = anyTriplet.X;

            if (collection == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing enumerable",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            IEnumerator enumerator = anyTriplet.Y;

            if (enumerator == null)
            {
                try
                {
                    //
                    // NOTE: Initially, there is no enumerator for the
                    //       variable.  It is created automatically.
                    //
                    enumerator = anyTriplet.Y = collection.GetEnumerator();
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }

            bool autoReset = anyTriplet.Z;

            try
            {
                if (!enumerator.MoveNext()) /* throw */
                {
                    if (autoReset)
                        enumerator.Reset(); /* throw */

                    error = "no more items";
                    return ReturnCode.Error;
                }

                itemValue = enumerator.Current; /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Linked Variable Support Methods
        public static void GetLinkedVariableMemberValue(
            object value,
            ref object memberValue
            )
        {
            ReturnCode code;
            MemberInfo memberInfo = null;
            Type type = null;
            object @object = null;
            Result error = null;

            code = GetLinkedVariableMemberAndValue(
                BreakpointType.None, null, null, null, value,
                ref memberInfo, ref type, ref @object,
                ref memberValue, ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetLinkedVariableMemberValue: code = {0}, error = {1}",
                    code, FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.VariableError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLinkedVariableMemberAndValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            object value,
            ref MemberInfo memberInfo,
            ref Type type,
            ref object @object,
            ref object memberValue,
            ref Result error
            )
        {
            IAnyPair<MemberInfo, object> anyPair =
                value as IAnyPair<MemberInfo, object>;

            if (anyPair == null)
            {
                error = String.Format(
                    "can't {0} {1}: broken link",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            memberInfo = anyPair.X;

            if (memberInfo == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing member",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;

                type = fieldInfo.FieldType;

                if (type == null)
                {
                    error = String.Format(
                        "can't {0} {1}: missing field type",
                        FormatOps.Breakpoint(breakpointType),
                        FormatOps.ErrorVariableName(
                            variable, null, name, index));

                    return ReturnCode.Error;
                }

                @object = anyPair.Y;

                try
                {
                    memberValue = fieldInfo.GetValue(@object); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                type = propertyInfo.PropertyType;

                if (type == null)
                {
                    error = String.Format(
                        "can't {0} {1}: missing property type",
                        FormatOps.Breakpoint(breakpointType),
                        FormatOps.ErrorVariableName(
                            variable, null, name, index));

                    return ReturnCode.Error;
                }

                @object = anyPair.Y;

                try
                {
                    //
                    // BUGBUG: Only non-indexed properties are currently
                    //         supported.
                    //
                    memberValue = propertyInfo.GetValue(
                        @object, null); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "can't {0} {1}: member must be field or property",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetLinkedVariableArrayValues(
            EventWaitHandle variableEvent,
            ElementDictionary arrayValue,
            ref ElementDictionary values
            )
        {
            if (arrayValue == null)
                return;

            ElementDictionary localValues = new ElementDictionary(
                variableEvent);

            foreach (ArrayPair pair in arrayValue)
            {
                object memberValue = null;

                GetLinkedVariableMemberValue(pair.Value, ref memberValue);

                if (memberValue == null)
                    continue;

                localValues.Add(pair.Key, memberValue);
            }

            if (localValues.Count > 0)
            {
                if (values == null)
                    values = new ElementDictionary(variableEvent);

                values.Add(localValues);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetLinkedVariableMemberValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            MemberInfo memberInfo,
            object @object,
            object memberValue,
            ref Result error
            )
        {
            if (memberInfo == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing member",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;

                try
                {
                    fieldInfo.SetValue(@object, memberValue); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                try
                {
                    //
                    // BUGBUG: Only non-indexed properties are currently
                    //         supported.
                    //
                    propertyInfo.SetValue(
                        @object, memberValue, null); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "can't {0} {1}: member must be field or property",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Wait Support Methods
        public static void MaybeModifyEventWaitFlags(
            ref EventWaitFlags eventWaitFlags /* in, out */
            )
        {
            //
            // HACK: The call to ThreadOps.IsStaThread here is made
            //       under the assumption that no user-interface
            //       thread can exist without also being an STA
            //       thread.  This may eventually prove to be false;
            //       however, currently WinForms, WPF, et al require
            //       this (i.e. an STA thread).
            //
            if (!FlagOps.HasFlags(
                    eventWaitFlags, EventWaitFlags.UserInterface,
                    true) && ThreadOps.IsStaThread())
            {
                eventWaitFlags |= EventWaitFlags.UserInterface;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Name Support Methods
        public static ReturnCode SplitVariableName(
            Interpreter interpreter,
            VariableFlags flags,
            string name,
            ref string varName,
            ref string varIndex,
            ref Result error
            )
        {
            if (name != null)
            {
                if (name.Length > 0)
                {
                    if (FlagOps.HasFlags(flags, VariableFlags.NoSplit, true))
                    {
                        //
                        // HACK: Skip parsing, use the supplied name verbatim.
                        //
                        varName = name;
                        varIndex = null;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        string localVarName = null;
                        string localVarIndex = null;

                        if (Parser.SplitVariableName(name, ref localVarName,
                                ref localVarIndex, ref error) == ReturnCode.Ok)
                        {
                            if (localVarIndex != null)
                            {
                                if (!FlagOps.HasFlags(flags,
                                        VariableFlags.NoElement, true))
                                {
                                    varName = localVarName;
                                    varIndex = localVarIndex;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    error = "name refers to an element in an array";
                                }
                            }
                            else
                            {
                                //
                                // BUGFIX: Use the supplied name verbatim.
                                //
                                varName = name;
                                varIndex = null;

                                return ReturnCode.Ok;
                            }
                        }
                    }
                }
                else
                {
                    varName = String.Empty;
                    varIndex = null;

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid variable name";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Frame Support Methods
        public static ReturnCode LinkVariable(
            Interpreter interpreter, /* in */
            ICallFrame localFrame,   /* in */
            string localName,        /* in */
            ICallFrame otherFrame,   /* in */
            string otherName,        /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (localFrame == null)
                {
                    error = "invalid \"local\" call frame";
                    return ReturnCode.Error;
                }

                if (otherFrame == null)
                {
                    error = "invalid \"other\" call frame";
                    return ReturnCode.Error;
                }

                //
                // NOTE: *WARNING* Empty variable names are allowed, please
                //       do not change these to "!String.IsNullOrEmpty".
                //
                if (localName == null)
                {
                    error = "invalid \"local\" variable name";
                    return ReturnCode.Error;
                }

                if (otherName == null)
                {
                    error = "invalid \"other\" variable name";
                    return ReturnCode.Error;
                }

                string localVarName = null;
                string localVarIndex = null;

                if (SplitVariableName(
                        interpreter, VariableFlags.NoElement, localName,
                        ref localVarName, ref localVarIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                string otherVarName = null;
                string otherVarIndex = null;

                //
                // BUGFIX: Allow the other side of the link to be an array
                //         element.
                //
                if (SplitVariableName(
                        interpreter, VariableFlags.None, otherName,
                        ref otherVarName, ref otherVarIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                //
                // NOTE: Is the interpreter running with namespaces enabled?
                //       If so, extra steps must be taken later.
                //
                bool useNamespaces = interpreter.AreNamespacesEnabled();

                //
                // NOTE: *NAMESPACES* Need to make sure the correct frame is
                //       being used if the local frame is marked to use the
                //       associated namespace.
                //
                localFrame = CallFrameOps.FollowNext(localFrame);

                if (useNamespaces && CallFrameOps.IsUseNamespace(localFrame))
                {
                    INamespace localNamespace = NamespaceOps.GetCurrent(
                        interpreter, localFrame);

                    if (localNamespace != null)
                    {
                        if (NamespaceOps.IsGlobal(
                                interpreter, localNamespace))
                        {
                            localFrame = interpreter.CurrentGlobalFrame;
                        }
                        else
                        {
                            localFrame = localNamespace.VariableFrame;
                        }
                    }
                }

                string newLocalVarName = useNamespaces ?
                    NamespaceOps.MakeRelativeName(
                        interpreter, localFrame, localVarName) :
                    MakeVariableName(localVarName);

                //
                // NOTE: *NAMESPACES* Need to make sure the correct frame is
                //       being used if the other frame is marked to use the
                //       associated namespace.
                //
                otherFrame = CallFrameOps.FollowNext(otherFrame);

                if (useNamespaces && CallFrameOps.IsUseNamespace(otherFrame))
                {
                    INamespace otherNamespace = NamespaceOps.GetCurrent(
                        interpreter, otherFrame);

                    if (otherNamespace != null)
                    {
                        if (NamespaceOps.IsGlobal(
                                interpreter, otherNamespace))
                        {
                            otherFrame = interpreter.CurrentGlobalFrame;
                        }
                        else
                        {
                            otherFrame = otherNamespace.VariableFrame;
                        }
                    }
                }

                string newOtherVarName = useNamespaces ?
                    NamespaceOps.MakeRelativeName(
                        interpreter, otherFrame, otherVarName) :
                    MakeVariableName(otherVarName);

                if (CallFrameOps.IsSame(
                        interpreter, localFrame, otherFrame, newLocalVarName,
                        newOtherVarName))
                {
                    error = "can't upvar from variable to itself";
                    return ReturnCode.Error;
                }

                //
                // NOTE: After this point, both the local and other variable
                //       names must be stripped of their qualifiers (i.e. if
                //       they were qualified to begin with).
                //
                if (useNamespaces)
                {
                    newLocalVarName = NamespaceOps.TailOnly(newLocalVarName);
                    newOtherVarName = NamespaceOps.TailOnly(newOtherVarName);
                }

                VariableDictionary localVariables = localFrame.Variables;

                if (localVariables == null)
                {
                    error = "local call frame does not support variables";
                    return ReturnCode.Error;
                }

                IVariable localVariable = null;

                if (interpreter.GetVariableViaResolversWithSplit(
                        localFrame, localVarName /* FULL NAME*/,
                        ref localVariable) == ReturnCode.Ok)
                {
                    Result localUsableError = null;

                    if (!EntityOps.IsUsable(
                            localVariable, ref localUsableError))
                    {
                        error = String.Format(
                            "variable \"{0}\" not usable: {1}",
                            localVarName /* FULL NAME */,
                            FormatOps.DisplayString(localUsableError));

                        return ReturnCode.Error;
                    }

                    //
                    // NOTE: If the local variable has been flagged as undefined
                    //       then go ahead and allow them to use it (it was not
                    //       purged?).
                    //
                    if (!EntityOps.IsUndefined(localVariable))
                    {
                        //
                        // BUGFIX: If the local variable is a link then go
                        //         ahead and allow them to use it.  We do
                        //         this for Tcl compatibility, which allows
                        //         for this "re-targeting" of variable links
                        //         to a different variable.
                        //
                        if (!EntityOps.IsLink(localVariable))
                        {
                            error = String.Format(
                                "variable \"{0}\" already exists",
                                localVarName /* FULL NAME */);

                            return ReturnCode.Error;
                        }
                    }
                }

                EventWaitHandle variableEvent = interpreter.TryGetVariableEvent(
                    ref error);

                if (variableEvent == null)
                    return ReturnCode.Error;

                VariableDictionary otherVariables = otherFrame.Variables;
                IVariable otherVariable = null;

                if (interpreter.GetVariableViaResolversWithSplit(
                        otherFrame, otherVarName /* FULL NAME*/,
                        ref otherVariable) == ReturnCode.Ok)
                {
                    Result otherUsableError = null;

                    if (!EntityOps.IsUsable(
                            otherVariable, ref otherUsableError))
                    {
                        error = String.Format(
                            "variable \"{0}\" not usable: {1}",
                            otherVarName /* FULL NAME */,
                            FormatOps.DisplayString(otherUsableError));

                        return ReturnCode.Error;
                    }

                    IVariable targetVariable = otherVariable;

                    if (EntityOps.IsLink(targetVariable))
                    {
                        targetVariable = EntityOps.FollowLinks(
                            otherVariable, VariableFlags.None, ref error);

                        if (targetVariable == null)
                            return ReturnCode.Error;
                    }

                    //
                    // NOTE: Make double sure now that we are not trying to
                    //       create a link to ourselves.
                    //
                    if ((localVariable != null) &&
                        Object.ReferenceEquals(targetVariable, localVariable))
                    {
                        error = "can't upvar from variable to itself";
                        return ReturnCode.Error;
                    }

                    //
                    // BUGFIX: If the other variable is currently undefined,
                    //         make sure all of its state is reset prior to
                    //         being used; otherwise, issues can arise like
                    //         "leftover" array elements.  For an example,
                    //         see test "array-1.26".
                    //
                    if ((otherVariable != null) &&
                        EntityOps.IsUndefined(otherVariable))
                    {
                        bool isGlobalCallFrame = interpreter.IsGlobalCallFrame(
                            otherFrame);

                        otherVariable.Reset(variableEvent);

                        otherVariable.Flags =
                            CallFrameOps.GetNewVariableFlags(otherFrame) |
                            interpreter.GetNewVariableFlags(isGlobalCallFrame);

                        if (isGlobalCallFrame)
                            EntityOps.SetGlobal(otherVariable, true);
                        else
                            EntityOps.SetLocal(otherVariable, true);

                        EntityOps.SetUndefined(otherVariable, true);
                    }
                }
                else if (otherVariables != null)
                {
                    if (otherVariables.ContainsKey(newOtherVarName))
                    {
                        //
                        // BUGBUG: Really, this can only happen if the variable
                        //         resolver lies to us (i.e. it does not return
                        //         the variable when asked yet if appears to be
                        //         present in the target call frame).
                        //
                        error = String.Format(
                            "other variable \"{0}\" already exists",
                            otherVarName /* FULL NAME */);

                        return ReturnCode.Error;
                    }

                    bool isGlobalCallFrame = interpreter.IsGlobalCallFrame(
                        otherFrame);

                    otherVariable = new Variable(
                        otherFrame, newOtherVarName,
                        CallFrameOps.GetNewVariableFlags(otherFrame) |
                        interpreter.GetNewVariableFlags(isGlobalCallFrame),
                        null, interpreter.GetTraces(null, newOtherVarName,
                        null, null, null), variableEvent);

                    interpreter.MaybeSetQualifiedName(otherVariable);

                    if (isGlobalCallFrame)
                        EntityOps.SetGlobal(otherVariable, true);
                    else
                        EntityOps.SetLocal(otherVariable, true);

                    EntityOps.SetUndefined(otherVariable, true);

                    otherVariables.Add(newOtherVarName, otherVariable);
                }
                else
                {
                    error = "other call frame does not support variables";
                    return ReturnCode.Error;
                }

                if (localVariable != null)
                {
                    localVariable.Reset(variableEvent);
                    localVariable.Link = otherVariable;
                    localVariable.LinkIndex = otherVarIndex;
                }
                else
                {
                    localVariable = new Variable( /* EXEMPT */
                        localFrame, newLocalVarName, null, otherVariable,
                        otherVarIndex, variableEvent);

                    interpreter.MaybeSetQualifiedName(localVariable);
                }

                //
                // NOTE: Make sure to flag the local variable as a link to the
                //       real one.
                //
                EntityOps.SetLink(localVariable, true);

                //
                // NOTE: If we get to this point and the local variable exists
                //       in the call frame, it should be replaced; otherwise,
                //       it should be added.
                //
                localVariables[newLocalVarName] = localVariable;

                //
                // BUGFIX: Mark the variable as "dirty" AFTER the actual
                //         modifications have been completed.
                //
                EntityOps.SetDirty(localVariable, true);

                //
                // NOTE: If we get this far, we have succeeded.
                //
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable "Safe" Support Methods
        public static bool IsSafeVariableName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeVariableNames == null) /* ONCE */
                {
                    safeVariableNames = new StringDictionary(new string[] {
                        Vars.Core.Null,
                        Vars.Platform.Name,
                        TclVars.Core.Interactive,
                        TclVars.Package.PatchLevelName,
                        TclVars.Platform.Name,
                        TclVars.Package.VersionName
                    }, true, false);
                }

                return (name != null) &&
                    safeVariableNames.ContainsKey(name);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafeTclPlatformElementName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeTclPlatformElementNames == null) /* ONCE */
                {
                    safeTclPlatformElementNames = new StringDictionary(
                        new string[] {
                        TclVars.Platform.ByteOrder,
                        TclVars.Platform.CharacterSize,
#if DEBUG
                        TclVars.Platform.Debug,
#endif
                        TclVars.Platform.DirectorySeparator,
                        TclVars.Platform.Engine,
                        TclVars.Platform.PatchLevel,
                        TclVars.Platform.PathSeparator,
                        TclVars.Platform.PlatformName,
                        TclVars.Platform.PointerSize,
                        TclVars.Platform.Threaded,
                        TclVars.Platform.Unicode,
                        TclVars.Platform.Version,
                        TclVars.Platform.WordSize
                    }, true, false);
                }

                return (name != null) &&
                    safeTclPlatformElementNames.ContainsKey(name);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafeEaglePlatformElementName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeEaglePlatformElementNames == null) /* ONCE */
                {
                    safeEaglePlatformElementNames = new StringDictionary(
                        new string[] {
                        Vars.Platform.Configuration,
                        Vars.Platform.InterpreterTimeStamp,
                        Vars.Platform.PatchLevel,
                        Vars.Platform.Suffix,
                        Vars.Platform.TextOrSuffix,
                        Vars.Platform.Version,
                        Vars.Platform.Vendor
                    }, true, false);
                }

                return (name != null) &&
                    safeEaglePlatformElementNames.ContainsKey(name);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Entity Naming Support Methods
        public static string TypeNameToEntityName(
            Type type
            )
        {
            return TypeNameToEntityName(type, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string TypeNameToEntityName(
            Type type,
            bool noCase
            )
        {
            if (type == null)
                return null;

            return MemberNameToEntityName(type.Name, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MemberNameToEntityName(
            string name,
            bool noCase
            )
        {
            string result = name;

            if (result != null)
            {
                //
                // HACK: All core entity names are lowercase; culture is
                //       invariant because these are considered to be
                //       "system" identifiers.
                //
                if (noCase)
                    result = result.ToLowerInvariant();

                //
                // HACK: Remove leading underscore from core entity names
                //       to accommodate the special circumstance where we
                //       were using a leading underscore in order to get
                //       around .NET Framework "reserved" type names (e.g.
                //       Decimal, Double, File, String, Object, etc).
                //
                if ((result.Length > 0) &&
                    (result[0] == Characters.Underscore))
                {
                    result = result.Substring(1);
                }

                //
                // HACK: When we do not want to lowercase the entire name,
                //       we still want to make the first letter lowercase;
                //       this must be done _after_ removing the leading
                //       underscore, if any.
                //
                if (!noCase)
                    result = StringOps.ToLowerInitial(result, null);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute/IVariable Naming Support Methods
        public static string MakeCommandPrefix(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeCommandName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeCommandPattern(
            string name
            )
        {
            return NamespaceOps.TrimAll(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeVariableName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Command Support Methods
        public static ICommand NewStubCommand(
            string name,
            IClientData clientData,
            IPlugin plugin,
            bool useSubCommands
            )
        {
            Type type = typeof(_Commands.Stub);
            CommandFlags flags = AttributeOps.GetCommandFlags(type);

            string localName = name;

            if (localName == null)
                localName = AttributeOps.GetObjectName(type);

            if (localName == null)
                localName = ScriptOps.TypeNameToEntityName(type);

            ICommand command = new _Commands.Stub(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            command.SubCommands = useSubCommands ?
                new EnsembleDictionary() : null;

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ICommand NewEnsembleCommand(
            string name,
            IClientData clientData,
            IPlugin plugin
            )
        {
            Type type = typeof(_Commands.Ensemble);
            CommandFlags flags = AttributeOps.GetCommandFlags(type);

            string localName = name;

            if (localName == null)
                localName = AttributeOps.GetObjectName(type);

            if (localName == null)
                localName = ScriptOps.TypeNameToEntityName(type);

            ICommand command = new _Commands.Ensemble(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ICommand NewSubDelegateCommand(
            string name,
            IClientData clientData,
            IPlugin plugin
            )
        {
            Type type = typeof(_Commands.SubDelegate);
            CommandFlags flags = AttributeOps.GetCommandFlags(type);

            string localName = name;

            if (localName == null)
                localName = AttributeOps.GetObjectName(type);

            if (localName == null)
                localName = ScriptOps.TypeNameToEntityName(type);

            ICommand command = new _Commands.SubDelegate(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ICommand NewExternalCommand(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            IPlugin plugin,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            NewCommandCallback newCommandCallback;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                newCommandCallback = interpreter.NewCommandCallback;
            }

            if (newCommandCallback == null)
            {
                error = "invalid command creation callback";
                return null;
            }

            try
            {
                return newCommandCallback( /* throw */
                    interpreter, clientData, name, plugin, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode EachLoopCommand(
            IIdentifierName identifierName, /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierName != null) ?
                identifierName.Name : "foreach";

            if ((arguments.Count < 4) || ((arguments.Count % 2) != 0))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varList list ?varList list ...? script\"",
                    commandName);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            int numLists = ((arguments.Count - 2) / 2);
            List<StringList> variableLists = new List<StringList>();
            List<StringList> valueLists = new List<StringList>();
            IntList valueIndexes = new IntList();
            int maximumIterations = 0;

            for (int listIndex = 0; listIndex < numLists; listIndex++)
            {
                int argumentIndex = 1 + (listIndex * 2);
                StringList variableList = null;

                code = ListOps.GetOrCopyOrSplitList(
                    interpreter, arguments[argumentIndex], true,
                    ref variableList, ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                if (variableList.Count < 1)
                {
                    result = String.Format(
                        "{0} varlist is empty",
                        commandName);

                    code = ReturnCode.Error;
                    goto done;
                }

                variableLists.Add(variableList);
                argumentIndex = 2 + (listIndex * 2);

                StringList valueList = null;

                code = ListOps.GetOrCopyOrSplitList(
                    interpreter, arguments[argumentIndex], true,
                    ref valueList, ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                valueLists.Add(valueList);
                valueIndexes.Add(0);

                int iterations = valueList.Count / variableList.Count;

                if ((valueList.Count % variableList.Count) != 0)
                    iterations++;

                if (iterations > maximumIterations)
                    maximumIterations = iterations;
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;

            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                for (int listIndex = 0; listIndex < numLists; listIndex++)
                {
                    for (int variableIndex = 0;
                            variableIndex < variableLists[listIndex].Count;
                            variableIndex++)
                    {
                        int valueIndex = valueIndexes[listIndex]++;
                        string value = String.Empty;

                        if (valueIndex < valueLists[listIndex].Count)
                            value = valueLists[listIndex][valueIndex];

                        string variableName =
                            variableLists[listIndex][variableIndex];

                        code = interpreter.SetVariableValue(
                            VariableFlags.None, variableName, value, null,
                            ref result);

                        if (code != ReturnCode.Ok)
                        {
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} loop variable \"{2}\"",
                                    Environment.NewLine, commandName,
                                    FormatOps.Ellipsis(variableName)));

                            goto done;
                        }
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1}\" body line {2})",
                                Environment.NewLine, commandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ArrayNamesLoopCommand(
            IIdentifierName identifierName, /* in */
            string subCommandName,          /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierName != null) ?
                identifierName.Name : "array";

            if (subCommandName == null)
                subCommandName = "foreach";

            if ((arguments.Count < 5) || (((arguments.Count - 1) % 2) != 0))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} varList arrayName " +
                    "?varList arrayName ...? script\"",
                    commandName, subCommandName);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            int numLists = ((arguments.Count - 3) / 2);
            List<StringList> variableLists = new List<StringList>();
            List<IEnumerator> valueLists = new List<IEnumerator>();
            int maximumIterations = 0;

            for (int listIndex = 0; listIndex < numLists; listIndex++)
            {
                int argumentIndex = 2 + (listIndex * 2);
                StringList variableList = null;

                code = ListOps.GetOrCopyOrSplitList(
                    interpreter, arguments[argumentIndex], true,
                    ref variableList, ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                if (variableList.Count < 1)
                {
                    result = String.Format(
                        "{0} {1} varlist is empty",
                        commandName, subCommandName);

                    code = ReturnCode.Error;
                    goto done;
                }

                variableLists.Add(variableList);
                argumentIndex = 3 + (listIndex * 2);

                VariableFlags variableFlags = VariableFlags.NoElement |
                    VariableFlags.NoLinkIndex | VariableFlags.Defined |
                    VariableFlags.NonVirtual;

                IVariable variable = null;

                code = interpreter.GetVariableViaResolversWithSplit(
                    arguments[argumentIndex], ref variableFlags,
                    ref variable, ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                Result linkError = null;

                if (EntityOps.IsLink(variable))
                {
                    variable = EntityOps.FollowLinks(
                        variable, variableFlags, ref linkError);
                }

                if ((variable == null) ||
                    EntityOps.IsUndefined(variable) ||
                    !EntityOps.IsArray(variable))
                {
                    if (linkError != null)
                    {
                        result = linkError;
                    }
                    else
                    {
                        result = String.Format(
                            "\"{0}\" isn't an array",
                            arguments[argumentIndex]);
                    }

                    code = ReturnCode.Error;
                    goto done;
                }

                ICollection valueList;

                if (interpreter.IsEnvironmentVariable(variable))
                {
                    IDictionary environment =
                        Environment.GetEnvironmentVariables();

                    if (environment == null)
                    {
                        result = "environment variables unavailable";
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = environment.Keys;
                }
                else if (interpreter.IsTestsVariable(variable))
                {
                    StringDictionary tests =
                        interpreter.GetAllTestInformation(
                            false, ref result);

                    if (tests == null)
                    {
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = tests.Keys;
                }
                else
                {
                    ThreadVariable threadVariable = null;

                    if (interpreter.IsThreadVariable(
                            variable, ref threadVariable))
                    {
                        ObjectDictionary thread =
                            threadVariable.GetList(
                                interpreter, true, false,
                                ref result);

                        if (thread == null)
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }

                        valueList = thread.Keys;
                    }
                    else
                    {
#if DATA
                        DatabaseVariable databaseVariable = null;

                        if (interpreter.IsDatabaseVariable(
                                variable, ref databaseVariable))
                        {
                            ObjectDictionary database =
                                databaseVariable.GetList(
                                    interpreter, true, false,
                                    ref result);

                            if (database == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            valueList = database.Keys;
                        }
                        else
#endif
                        {
#if NETWORK && WEB
                            NetworkVariable networkVariable = null;

                            if (interpreter.IsNetworkVariable(
                                    variable, ref networkVariable))
                            {
                                ObjectDictionary network =
                                    networkVariable.GetList(
                                        interpreter, null, false, true,
                                        false, ref result);

                                if (network == null)
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                valueList = network.Keys;
                            }
                            else
#endif
                            {
#if !NET_STANDARD_20 && WINDOWS
                                RegistryVariable registryVariable = null;

                                if (interpreter.IsRegistryVariable(
                                        variable, ref registryVariable))
                                {
                                    ObjectDictionary registry =
                                        registryVariable.GetList(
                                            interpreter, true, false,
                                            ref result);

                                    if (registry == null)
                                    {
                                        code = ReturnCode.Error;
                                        goto done;
                                    }

                                    valueList = registry.Keys;
                                }
                                else
#endif
                                {
                                    valueList = variable.ArrayValue.Keys;
                                }
                            }
                        }
                    }
                }

                valueLists.Add(valueList.GetEnumerator());

                int iterations = valueList.Count / variableList.Count;

                if ((valueList.Count % variableList.Count) != 0)
                    iterations++;

                if (iterations > maximumIterations)
                    maximumIterations = iterations;
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;

            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                for (int listIndex = 0; listIndex < numLists; listIndex++)
                {
                    for (int variableIndex = 0;
                            variableIndex < variableLists[listIndex].Count;
                            variableIndex++)
                    {
                        IEnumerator valueList = valueLists[listIndex];
                        object value = null;

                        if (valueList != null)
                        {
                            if (valueList.MoveNext())
                                value = valueList.Current;
                            else
                                valueLists[listIndex] = null;
                        }

                        string variableName =
                            variableLists[listIndex][variableIndex];

                        code = interpreter.SetVariableValue(
                            VariableFlags.None, variableName,
                            StringOps.GetStringFromObject(value),
                            null, ref result);

                        if (code != ReturnCode.Ok)
                        {
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} {2} loop variable \"{3}\"",
                                    Environment.NewLine, commandName, subCommandName,
                                    FormatOps.Ellipsis(variableName)));

                            goto done;
                        }
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1} {2}\" body line {3})",
                                Environment.NewLine, commandName,
                                subCommandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ArrayNamesAndValuesLoopCommand(
            IIdentifierName identifierName, /* in */
            string subCommandName,          /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierName != null) ?
                identifierName.Name : "array";

            if (subCommandName == null)
                subCommandName = "for";

            if (arguments.Count != 5)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} " +
                    "{2}keyVarName valueVarName{3} arrayName script\"",
                    commandName, subCommandName, Characters.OpenBrace,
                    Characters.CloseBrace);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            StringList variableList = null;

            code = ListOps.GetOrCopyOrSplitList(
                interpreter, arguments[2], true, ref variableList,
                ref result);

            if (code != ReturnCode.Ok)
                goto done;

            if ((variableList.Count != 1) &&
                (variableList.Count != 2))
            {
                result = "must have one or two variable names";

                code = ReturnCode.Error;
                goto done;
            }

            VariableFlags variableFlags = VariableFlags.NoElement |
                VariableFlags.NoLinkIndex | VariableFlags.Defined |
                VariableFlags.NonVirtual;

            string varName = arguments[3];
            IVariable variable = null;

            code = interpreter.GetVariableViaResolversWithSplit(
                varName, ref variableFlags, ref variable, ref result);

            if (code != ReturnCode.Ok)
                goto done;

            Result linkError = null;

            if (EntityOps.IsLink(variable))
            {
                variable = EntityOps.FollowLinks(
                    variable, variableFlags, ref linkError);
            }

            if ((variable == null) ||
                EntityOps.IsUndefined(variable) ||
                !EntityOps.IsArray(variable))
            {
                if (linkError != null)
                {
                    result = linkError;
                }
                else
                {
                    result = String.Format(
                        "\"{0}\" isn't an array",
                        varName);
                }

                code = ReturnCode.Error;
                goto done;
            }

            ICollection valueList;

            if (interpreter.IsEnvironmentVariable(variable))
            {
                IDictionary environment =
                    Environment.GetEnvironmentVariables();

                if (environment == null)
                {
                    result = "environment variables unavailable";
                    code = ReturnCode.Error;
                    goto done;
                }

                valueList = environment.Keys;
            }
            else if (interpreter.IsTestsVariable(variable))
            {
                StringDictionary tests = interpreter.GetAllTestInformation(
                    false, ref result);

                if (tests == null)
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                valueList = tests.Keys;
            }
            else
            {
                ThreadVariable threadVariable = null;

                if (interpreter.IsThreadVariable(
                        variable, ref threadVariable))
                {
                    ObjectDictionary thread =
                        threadVariable.GetList(
                            interpreter, true, false, ref result);

                    if (thread == null)
                    {
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = thread.Keys;
                }
                else
                {
#if DATA
                    DatabaseVariable databaseVariable = null;

                    if (interpreter.IsDatabaseVariable(
                            variable, ref databaseVariable))
                    {
                        ObjectDictionary database =
                            databaseVariable.GetList(
                                interpreter, true, false, ref result);

                        if (database == null)
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }

                        valueList = database.Keys;
                    }
                    else
#endif
                    {
#if NETWORK && WEB
                        NetworkVariable networkVariable = null;

                        if (interpreter.IsNetworkVariable(
                                variable, ref networkVariable))
                        {
                            ObjectDictionary network =
                                networkVariable.GetList(
                                    interpreter, null, false, true,
                                    false, ref result);

                            if (network == null)
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }

                            valueList = network.Keys;
                        }
                        else
#endif
                        {
#if !NET_STANDARD_20 && WINDOWS
                            RegistryVariable registryVariable = null;

                            if (interpreter.IsRegistryVariable(
                                    variable, ref registryVariable))
                            {
                                ObjectDictionary registry =
                                    registryVariable.GetList(
                                        interpreter, true, false,
                                        ref result);

                                if (registry == null)
                                {
                                    code = ReturnCode.Error;
                                    goto done;
                                }

                                valueList = registry.Keys;
                            }
                            else
#endif
                            {
                                valueList = variable.ArrayValue.Keys;
                            }
                        }
                    }
                }
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;
            IEnumerator valueEnumerator = valueList.GetEnumerator();

            while (true)
            {
                if (!valueEnumerator.MoveNext())
                    break;

                string varIndex = StringOps.GetStringFromObject(
                    valueEnumerator.Current);

                Result varValue = null;

                code = interpreter.GetVariableValue2(
                    VariableFlags.None, varName, varIndex, ref varValue,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (getting {1} {2} loop variable \"{3}\"",
                            Environment.NewLine, commandName, subCommandName,
                            FormatOps.VariableName(varName, varIndex)));

                    goto done;
                }

                code = interpreter.SetVariableValue(
                    VariableFlags.None, variableList[0], varIndex, null,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (setting {1} {2} loop name variable \"{3}\"",
                            Environment.NewLine, commandName, subCommandName,
                            FormatOps.Ellipsis(variableList[0])));

                    goto done;
                }

                if (variableList.Count >= 2)
                {
                    code = interpreter.SetVariableValue(
                        VariableFlags.None, variableList[1],
                        StringOps.GetStringFromObject(varValue), null,
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format(
                                "{0}    (setting {1} {2} loop value variable \"{3}\"",
                                Environment.NewLine, commandName, subCommandName,
                                FormatOps.Ellipsis(variableList[1])));

                        goto done;
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1} {2}\" body line {3})",
                                Environment.NewLine, commandName,
                                subCommandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Behavior Support Methods
        public static bool HasFlags(
            Interpreter interpreter,
            InterpreterFlags hasFlags,
            bool all
            )
        {
            if (interpreter == null)
                return false;

            return FlagOps.HasFlags( /* EXEMPT */
                interpreter.InterpreterFlags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Evaluation Support Methods
        public static void ExtractTrustFlags(
            TrustFlags trustFlags,       /* in */
            out bool exclusive,          /* out */
            out bool withEvents,         /* out */
            out bool markTrusted,        /* out */
            out bool allowUnsafe,        /* out */
            out bool ignoreHidden,       /* out */
            out bool useSecurityLevels,  /* out */
            out bool pushScriptLocation  /* out */
#if ISOLATED_PLUGINS
            , out bool noIsolatedPlugins /* out */
#endif
            , out bool withScopeFrame    /* out */
            )
        {
            exclusive = !FlagOps.HasFlags(
                trustFlags, TrustFlags.Shared, true);

            withEvents = FlagOps.HasFlags(
                trustFlags, TrustFlags.WithEvents, true);

            markTrusted = FlagOps.HasFlags(
                trustFlags, TrustFlags.MarkTrusted, true);

            allowUnsafe = FlagOps.HasFlags(
                trustFlags, TrustFlags.AllowUnsafe, true);

            ignoreHidden = !FlagOps.HasFlags(
                trustFlags, TrustFlags.NoIgnoreHidden, true);

            useSecurityLevels = FlagOps.HasFlags(
                trustFlags, TrustFlags.UseSecurityLevels, true);

            pushScriptLocation = FlagOps.HasFlags(
                trustFlags, TrustFlags.PushScriptLocation, true);

#if ISOLATED_PLUGINS
            noIsolatedPlugins = FlagOps.HasFlags(
                trustFlags, TrustFlags.NoIsolatedPlugins, true);
#endif

            withScopeFrame = FlagOps.HasFlags(
                trustFlags, TrustFlags.WithScopeFrame, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Health Support Methods
        public static ReturnCode TryLockForHealth(
            Interpreter interpreter,
            ref ResultList errors
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            bool locked = false;

            try
            {
                interpreter.InternalTryLock(HealthTimeout, ref locked);

                if (locked)
                {
                    interpreter.TouchLastHealthCheck();
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("unable to acquire interpreter lock");
                    return ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateForHealth(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(HealthScript, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyForHealth(
            ReturnCode code,
            Result result,
            ref ResultList errors
            )
        {
            if (code != ReturnCode.Ok)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("return code is not ok");
                return false;
            }

            if (!SharedStringOps.SystemEquals(result, HealthResult))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("result has unexpected value");
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Zip Archive Support Methods
#if NETWORK
        private static ReturnCode ExtractToDirectory(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            string downloadDirectory, /* in: OPTIONAL (Windows) */
            string downloadFileName,  /* in */
            string extractDirectory,  /* in */
            EventFlags? eventFlags,   /* in */
            ref Result error          /* out */
            )
        {
            if (String.IsNullOrEmpty(downloadFileName))
            {
                error = "invalid download file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(downloadFileName))
            {
                error = "download file does not exist";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(extractDirectory))
            {
                error = "invalid extract directory name";
                return ReturnCode.Error;
            }

            if (!Directory.Exists(extractDirectory))
            {
                error = "extract directory does not exist";
                return ReturnCode.Error;
            }

            string resourceName = UnzipResourceName;

            Uri uri = PathOps.BuildAuxiliaryUri(
                ref resourceName, ref error);

            if (uri == null)
                return ReturnCode.Error;

            bool deleteUnzipFileName = false;
            string unzipFileName = null;

            try
            {
#if TEST
                if (WebOps.SetSecurityProtocol(
                        false, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
#endif

                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: The downloaded command line tool
                    //       only works on Windows.
                    //
                    if (String.IsNullOrEmpty(downloadDirectory))
                    {
                        error = "invalid download directory name";
                        return ReturnCode.Error;
                    }

                    if (!Directory.Exists(downloadDirectory))
                    {
                        error = "download directory does not exist";
                        return ReturnCode.Error;
                    }

                    unzipFileName = Path.Combine(
                        downloadDirectory, UnzipFileNameOnly);

                    deleteUnzipFileName = true;

                    if (WebOps.DownloadFile(
                            interpreter, clientData, uri,
                            unzipFileName, null, false,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    //
                    // NOTE: The downloaded command line tool
                    //       must be "trusted" by Windows in
                    //       order to be used; either way, it
                    //       will deleted at the end of this
                    //       method.
                    //
                    if (!RuntimeOps.IsFileTrusted(
                            unzipFileName, IntPtr.Zero))
                    {
                        error = "command line tool is untrusted";
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // HACK: Just assume that all non-Windows
                    //       operating systems have an "unzip"
                    //       executable somewhere along their
                    //       PATH.
                    //
                    unzipFileName = UnzipFileNameOnly;
                }

                //
                // HACK: The following switches to the "unzip"
                //       command line tool are being used here
                //       -AND- are hard-coded:
                //
                //       -n : never overwrite existing files
                //       -d : extract files into exdir
                //
                string unzipArguments = RuntimeOps.BuildCommandLine(
                    new string[] { "-n", downloadFileName, "-d",
                    extractDirectory }, false);

                if (unzipArguments == null)
                {
                    error = "no command line tool arguments";
                    return ReturnCode.Error;
                }

                EventFlags localEventFlags;

                if (eventFlags != null)
                    localEventFlags = (EventFlags)eventFlags;
                else if (interpreter != null)
                    localEventFlags = interpreter.EngineEventFlags;
                else
                    localEventFlags = EventFlags.Default;

                ExitCode exitCode = ResultOps.UnknownExitCode();
                Result result = null;

                if (ProcessOps.ExecuteProcess(
                        interpreter, unzipFileName, unzipArguments,
                        localEventFlags, ref exitCode, ref result,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (exitCode != ResultOps.SuccessExitCode())
                {
                    error = String.Format(
                        "command line tool {0} {1} failed: {2}",
                        FormatOps.WrapOrNull(unzipFileName),
                        FormatOps.WrapOrNull(unzipArguments),
                        FormatOps.WrapOrNull(result));

                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
            finally
            {
                if (deleteUnzipFileName &&
                    (unzipFileName != null) && File.Exists(unzipFileName))
                {
                    File.Delete(unzipFileName);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadAndExtractZipFile(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            string extractDirectory, /* in */
            string resourceName,     /* in */
            bool? useFallback,       /* in */
            ref Result error         /* out */
            )
        {
            if (String.IsNullOrEmpty(extractDirectory))
            {
                error = "invalid extract directory name";
                return ReturnCode.Error;
            }

            if (!Directory.Exists(extractDirectory))
            {
                error = "extract directory does not exist";
                return ReturnCode.Error;
            }

            Uri uri = PathOps.BuildAuxiliaryUri(
                ref resourceName, ref error);

            if (uri == null)
                return ReturnCode.Error;

            string temporaryDirectory = PathOps.GetTempPath(
                interpreter);

            if (temporaryDirectory == null)
            {
                error = "invalid temporary directory name";
                return ReturnCode.Error;
            }

            if (!Directory.Exists(temporaryDirectory))
            {
                error = "temporary directory does not exist";
                return ReturnCode.Error;
            }

            string downloadDirectory = null;

            try
            {
#if TEST
                if (WebOps.SetSecurityProtocol(
                        false, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
#endif

                downloadDirectory = PathOps.GetUniquePath(
                    interpreter, temporaryDirectory, null,
                    null, ref error);

                if (downloadDirectory == null)
                    return ReturnCode.Error;

                Directory.CreateDirectory(
                    downloadDirectory); /* throw */

                string downloadFileName = Path.Combine(
                    downloadDirectory, resourceName);

                if (WebOps.DownloadFile(
                        interpreter, clientData, uri,
                        downloadFileName, null, false,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

#if COMPRESSION
                if ((useFallback == null) || !(bool)useFallback)
                {
                    ZipFile.ExtractToDirectory(
                        downloadFileName, extractDirectory);

                    return ReturnCode.Ok;
                }
#endif

                //
                // HACK: Attempt to fallback to using the
                //       "unzip" command line tool, which
                //       will be downloaded via auxiliary
                //       base URI for this assembly.  It
                //       must be digitally signed with a
                //       trusted certificate in order to
                //       be used.
                //
                if ((useFallback == null) || (bool)useFallback)
                {
                    if (ExtractToDirectory(interpreter,
                            clientData, downloadDirectory,
                            downloadFileName, extractDirectory,
                            null, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }

                error = "cannot extract zip file";
                return ReturnCode.Error;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                if (downloadDirectory != null)
                {
                    /* IGNORED */
                    FileOps.CleanupDirectory(downloadDirectory,
                        new string[] { resourceName }, true);
                }
            }
        }
#endif
        #endregion
    }
}
