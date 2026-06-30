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
using System.Text.RegularExpressions;
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

using ArgumentPair = System.Collections.Generic.KeyValuePair<string,
    Eagle._Interfaces.Public.IAnyPair<int, Eagle._Components.Public.Argument>>;

using ObjectWrapper = Eagle._Wrappers._Object;

using DelegateTriplet = Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

using PackageIndexPair = System.Collections.Generic.KeyValuePair<string,
    Eagle._Components.Public.MutableAnyPair<string,
    Eagle._Components.Public.PackageIndexFlags>>;

using PackageIndexAnyPair = Eagle._Components.Public.MutableAnyPair<
    string, Eagle._Components.Public.PackageIndexFlags>;

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods and shared
    /// constants used to support the creation, evaluation, and management of
    /// scripts within the Eagle interpreter, including package handling,
    /// security integration, and "safe" interpreter setup.
    /// </summary>
    [ObjectId("81c28526-dd5a-4ba8-b056-b62bbd3b8d90")]
    internal static class ScriptOps
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default index, within the argument list, of the sub-command
        /// name for an ensemble command.
        /// </summary>
        private static int DefaultSubCommandNameIndex = 1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value used for a variable (a null string).
        /// </summary>
        private static readonly string DefaultVariableValue = null;
        /// <summary>
        /// The default value used when getting a variable (a null string).
        /// </summary>
        private static readonly string DefaultGetVariableValue = null;
        /// <summary>
        /// The default value used when setting a variable (a null string).
        /// </summary>
        private static readonly string DefaultSetVariableValue = null;
        /// <summary>
        /// The default value used when unsetting a variable (a null string).
        /// </summary>
        private static readonly string DefaultUnsetVariableValue = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, sub-command name matching is performed without regard
        /// to case.
        /// </summary>
        private static bool SubCommandNoCase = false;

        ///////////////////////////////////////////////////////////////////////

        #region Default Shell Executable File Names
        /// <summary>
        /// The default file name for the Eagle shell executable.
        /// </summary>
        private static readonly string DefaultShellFileName =
            "EagleShell" + FileExtension.Executable;

        /// <summary>
        /// The default file name for the 32-bit Eagle shell executable.
        /// </summary>
        private static readonly string DefaultShell32FileName =
            "EagleShell32" + FileExtension.Executable;

        /// <summary>
        /// The default file name for the Eagle kit executable.
        /// </summary>
        private static readonly string DefaultKitFileName =
            "EagleKit" + FileExtension.Executable;

        /// <summary>
        /// The default file name for the 32-bit Eagle kit executable.
        /// </summary>
        private static readonly string DefaultKit32FileName =
            "EagleKit32" + FileExtension.Executable;

        /// <summary>
        /// The default file name for the enterprise Eagle kit executable.
        /// </summary>
        private static readonly string DefaultEnterpriseKitFileName =
            "EeeKit" + FileExtension.Executable;

        /// <summary>
        /// The default file name for the 32-bit enterprise Eagle kit
        /// executable.
        /// </summary>
        private static readonly string DefaultEnterpriseKit32FileName =
            "EeeKit32" + FileExtension.Executable;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Constants
        /// <summary>
        /// The name of the primary security package.
        /// </summary>
        private static readonly string HarpyPackageName = "Harpy"; // primary
        /// <summary>
        /// The name of the secondary security package.
        /// </summary>
        private static readonly string BadgePackageName = "Badge"; // secondary

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The suffix appended to a security package name to form the name of
        /// its alternate (basic) variant.
        /// </summary>
        private static readonly string SecurityPackageAlternateSuffix =
            ".Basic";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The names of the security packages, in order of preference.
        /// </summary>
        private static readonly string[] SecurityPackageNames = {
            HarpyPackageName, BadgePackageName
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The names of the alternate (basic) security packages, in order of
        /// preference.
        /// </summary>
        private static readonly string[] SecurityAlternatePackageNames = {
            HarpyPackageName + SecurityPackageAlternateSuffix,
            BadgePackageName + SecurityPackageAlternateSuffix
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the embedded resource that contains the security
        /// certificate.
        /// </summary>
        private static readonly string SecurityCertificateResourceName =
            String.Format("{0}.Resources.Certificates.certificate.exml",
            GlobalState.GetPackageName());

        /// <summary>
        /// The name of the request used to obtain information about the
        /// security certificate.
        /// </summary>
        private static readonly string SecurityCertificateRequestName =
            "AboutCertificate";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The pattern used to match the package index of the primary security
        /// package.
        /// </summary>
        private static readonly string HarpyPackageIndexPattern =
            String.Format("*/{0}*{1}/*", HarpyPackageName,
            GlobalState.GetPackageVersion(null));

        /// <summary>
        /// The pattern used to match the package index of the secondary
        /// security package.
        /// </summary>
        private static readonly string BadgePackageIndexPattern =
            String.Format("*/{0}*{1}/*", BadgePackageName,
            GlobalState.GetPackageVersion(null));

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The pattern used to match the assembly file name of the primary
        /// security package.
        /// </summary>
        private static readonly string HarpyAssemblyFileNamePattern =
            String.Format("*/{0}*{1}", HarpyPackageName,
            FileExtension.Library);

        /// <summary>
        /// The pattern used to match the assembly file name of the secondary
        /// security package.
        /// </summary>
        private static readonly string BadgeAssemblyFileNamePattern =
            String.Format("*/{0}*{1}", BadgePackageName,
            FileExtension.Library);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the script used to enable security.
        /// </summary>
        private const string EnableSecurityScriptName = "enableSecurity";
        /// <summary>
        /// The name of the script used to disable security.
        /// </summary>
        private const string DisableSecurityScriptName = "disableSecurity";
        /// <summary>
        /// The name of the script used to remove commands.
        /// </summary>
        private const string RemoveCommandsScriptName = "removeCommands";
        /// <summary>
        /// The name of the script used to remove variables.
        /// </summary>
        private const string RemoveVariablesScriptName = "removeVariables";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the script used to fetch the key ring.
        /// </summary>
        private const string FetchKeyRingScriptName = "fetchKeyRing";
        /// <summary>
        /// The name of the script used to merge the key ring.
        /// </summary>
        private const string MergeKeyRingScriptName = "mergeKeyRing";
        /// <summary>
        /// The file name (without directory) of the key ring script.
        /// </summary>
        private const string KeyRingFileNameOnly = "keyRing.eagle";

        ///////////////////////////////////////////////////////////////////////

        #region Security Package Loader Warning Message
#if !DEBUG
        /// <summary>
        /// The warning message displayed when it is likely that the security
        /// plugins will fail to load in the current configuration.
        /// </summary>
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
#if THREADING
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The script evaluated to verify the health of an interpreter.
        /// </summary>
        private static string HealthScript =
            "string is list [list [expr {2 + 2}] a b c]";

        /// <summary>
        /// The result expected from evaluating the health-check script.
        /// </summary>
        private static Result HealthResult = "True";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Remote Time Support Constants
#if NETWORK
        //
        // NOTE: The name of the resource, relative to the base auxiliary
        //       URI for this assembly (e.g. "https://urn.to/r"), that
        //       should return the (current) time as the whole number of
        //       milliseconds since the (Unix) epoch.
        //
        /// <summary>
        /// The name of the resource, relative to the base auxiliary URI for
        /// this assembly, that returns the current time as the whole number of
        /// milliseconds since the Unix epoch.
        /// </summary>
        private static readonly string TimeResourceName = "time";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Zip Archive Support Constants
#if NETWORK
        //
        // NOTE: The name of the resource, relative to the base auxiliary
        //       URI for this assembly (e.g. "https://urn.to/r"), that
        //       should result in the "unzip.exe" tool being downloaded.
        //
        /// <summary>
        /// The name of the resource, relative to the base auxiliary URI for
        /// this assembly, that results in the "unzip.exe" tool being
        /// downloaded.
        /// </summary>
        private static readonly string UnzipResourceName = "unzip";

        //
        // NOTE: The name of the Windows-only executable file name that
        //       is used to extract a zip archive file.
        //
        /// <summary>
        /// The file name (without directory) of the executable used to extract
        /// a zip archive file.
        /// </summary>
        private static readonly string UnzipFileNameOnly =
            PlatformOps.IsWindowsOperatingSystem() ? "unzip.exe" : "unzip";

        //
        // NOTE: The number of milliseconds to wait before attempting to
        //       delete the (temporary) downloaded "unzip.exe" tool file.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The number of milliseconds to wait before attempting to delete the
        /// temporary downloaded "unzip.exe" tool file.
        /// </summary>
        private static int UnzipDeleteDelayMilliseconds = 1000;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Script Class Support Constants
        /// <summary>
        /// The regular expression used to extract the embedded identifier from
        /// the text of a core script class.
        /// </summary>
        private static Regex EmbeddedIdRegEx = RegExOps.Create(
            "^\\s*#\\s*<Id>([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-" +
            "[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})</Id>$", RegexOptions.Multiline |
            RegexOptions.Compiled);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Variable Name Lists
        /// <summary>
        /// The collection of default variable names.
        /// </summary>
        private static StringDictionary defaultVariableNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable "Safe" Name / Element Lists
        /// <summary>
        /// The collection of variable names considered "safe" for use in a safe
        /// interpreter.
        /// </summary>
        private static StringDictionary safeVariableNames;
        /// <summary>
        /// The collection of "tcl_platform" array element names considered
        /// "safe" for use in a safe interpreter.
        /// </summary>
        private static StringDictionary safeTclPlatformElementNames;
        /// <summary>
        /// The collection of "eagle_platform" array element names considered
        /// "safe" for use in a safe interpreter.
        /// </summary>
        private static StringDictionary safeEaglePlatformElementNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached "Safe" Interpreter
        /// <summary>
        /// The cached "safe" interpreter instance, if any.
        /// </summary>
        private static Interpreter cachedSafeInterpreter;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Exited Event Handler Methods
        /// <summary>
        /// This method adds the event handler used to clear cached interpreter
        /// state when the application domain or process is exiting, unless that
        /// behavior has been disabled via configuration.
        /// </summary>
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

        /// <summary>
        /// This method removes the event handler used to clear cached
        /// interpreter state when the application domain or process is exiting.
        /// </summary>
        private static void RemoveExitedEventHandler()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= ScriptOps_Exited;
                else
                    appDomain.ProcessExit -= ScriptOps_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the event handler invoked when the application domain
        /// or process is exiting.  It clears the cached interpreter state and
        /// removes itself as an event handler.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the event.
        /// </param>
        private static void ScriptOps_Exited(
            object sender,
            EventArgs e
            )
        {
            ClearInterpreterCache();
            RemoveExitedEventHandler();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Unknown Support Methods
        /// <summary>
        /// This method builds the script used to handle an unknown package by
        /// appending the optionally specified package name and version to the
        /// specified base script text.
        /// </summary>
        /// <param name="text">
        /// The base script text.
        /// </param>
        /// <param name="name">
        /// The name of the package, or null if there is no package name.
        /// </param>
        /// <param name="version">
        /// The version of the package, or null if there is no package version.
        /// </param>
        /// <returns>
        /// The constructed package-unknown script.
        /// </returns>
        public static string GetPackageUnknownScript(
            string text,
            string name,
            Version version
            )
        {
            StringBuilder builder = StringBuilderFactory.Create(text);

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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Methods
        /// <summary>
        /// This method scans the specified path for security plugin assembly
        /// files and adds the directories containing any matching files to the
        /// list of paths.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for file name pattern matching.
        /// </param>
        /// <param name="path">
        /// The directory to search for security plugin assembly files.
        /// </param>
        /// <param name="paths">
        /// Upon success, this list receives the directories that contain a
        /// matching security plugin assembly file.
        /// </param>
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

            Array.Sort(fileNames); /* O(N) */

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

        /// <summary>
        /// This method scans the auto-path list of the specified interpreter
        /// for security package index files and adds the directories that
        /// contain them to the list of paths.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose auto-path list is to be scanned.
        /// </param>
        /// <param name="paths">
        /// Upon success, this list receives the directories that contain a
        /// matching security package index file.
        /// </param>
        public static void GetSecurityPackageIndexPaths(
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

            string[] searchPatterns = {
                PackageOps.GetIndexFilePattern( /* "pkgIndex_*.eagle" */
                    interpreter, PackageType.None, true, false),
                PackageOps.GetIndexFilePattern( /* "pkgIndex.eagle" */
                    interpreter, PackageType.None, false, false)
            };

            TraceOps.DebugTrace(String.Format(
                "GetSecurityPackageIndexPaths: input path list: {0}",
                FormatOps.WrapOrNull(autoPathList)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug4);

            SearchOption searchOption = FileOps.GetSearchOption(true);

            foreach (string path in autoPathList)
            {
                if (String.IsNullOrEmpty(path))
                    continue;

                foreach (string searchPattern in searchPatterns)
                {
                    if (String.IsNullOrEmpty(searchPattern))
                        continue;

                    string[] fileNames = Directory.GetFiles(
                        PathOps.GetNativePath(path), searchPattern,
                        searchOption);

                    if ((fileNames == null) || (fileNames.Length == 0))
                        continue;

                    Array.Sort(fileNames); /* O(N) */

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
            }

            TraceOps.DebugTrace(String.Format(
                "GetSecurityPackageIndexPaths: output path list: {0}",
                FormatOps.WrapOrNull(paths)),
                typeof(ScriptOps).Name, TracePriority.PackageDebug4);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the security package index paths for a file
        /// with the specified name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose security package index paths are to be
        /// searched.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, to search for.
        /// </param>
        /// <returns>
        /// The fully qualified name of the first matching file that exists, or
        /// null if no matching file was found.
        /// </returns>
        public static string FindSecurityPackageFile(
            Interpreter interpreter, /* in */
            string fileNameOnly      /* in */
            )
        {
            StringList paths = null;

            GetSecurityPackageIndexPaths(interpreter, ref paths);

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    if (String.IsNullOrEmpty(path))
                        continue;

                    string fileName = Path.Combine(path, fileNameOnly);

                    if (!File.Exists(fileName))
                        continue;

                    return fileName;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits a package index file name into its directory and
        /// file name components.
        /// </summary>
        /// <param name="fileName">
        /// The package index file name to split.
        /// </param>
        /// <param name="directory">
        /// Upon success, receives the directory portion of the file name.
        /// </param>
        /// <param name="fileNameOnly">
        /// Upon success, receives the file name portion, without any directory
        /// information.
        /// </param>
        /// <returns>
        /// True if the file name was split successfully; otherwise, false.
        /// </returns>
        private static bool SplitPackageIndexFileName(
            string fileName,        /* in */
            out string directory,   /* out */
            out string fileNameOnly /* out */
            )
        {
            directory = null;
            fileNameOnly = null;

            try
            {
                directory = Path.GetDirectoryName(fileName);
                fileNameOnly = Path.GetFileName(fileName);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ScriptOps).Name,
                    TracePriority.PathError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified package indexes already
        /// contain a security package index for every one of the specified
        /// paths.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for file name pattern matching.
        /// </param>
        /// <param name="paths">
        /// The list of directories that should each be represented by a
        /// security package index.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes already known to the interpreter.
        /// </param>
        /// <returns>
        /// True if every path has a matching security package index; otherwise,
        /// false.
        /// </returns>
        private static bool HaveSecurityPackageIndexes(
            Interpreter interpreter,              /* in */
            StringList paths,                     /* in */
            PackageIndexDictionary packageIndexes /* in */
            )
        {
            if ((paths == null) || (packageIndexes == null))
                return false;

            string[] searchPatterns = {
                PackageOps.GetIndexFilePattern( /* "pkgIndex_*.eagle" */
                    interpreter, PackageType.None, true, false),
                PackageOps.GetIndexFilePattern( /* "pkgIndex.eagle" */
                    interpreter, PackageType.None, false, false)
            };

            int count = 0;

            foreach (string searchPattern in searchPatterns)
            {
                if (String.IsNullOrEmpty(searchPattern))
                    continue;

                foreach (string path in paths)
                {
                    if (String.IsNullOrEmpty(path))
                        continue;

                    foreach (PackageIndexPair pair in packageIndexes)
                    {
                        string directory;
                        string fileNameOnly;

                        if (!SplitPackageIndexFileName(pair.Key,
                                out directory, out fileNameOnly) ||
                            String.IsNullOrEmpty(directory) ||
                            String.IsNullOrEmpty(fileNameOnly))
                        {
                            continue;
                        }

                        if (!PathOps.IsEqualFileName(directory, path))
                            continue;

                        if (!StringOps.Match(interpreter,
                                MatchMode.Glob, fileNameOnly,
                                searchPattern, PathOps.NoCase))
                        {
                            continue;
                        }

                        count++; // MATCHED
                    }
                }
            }

            TraceOps.DebugTrace(String.Format(
                "HaveSecurityPackageIndexes: interpreter = {0}, " +
                "paths = {1}, packageIndexes = {2}, count = {3}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(paths),
                FormatOps.WrapOrNull(packageIndexes), count),
                typeof(ScriptOps).Name, TracePriority.PackageDebug4);

            return (count == paths.Count);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds and loads the security package indexes for the
        /// specified interpreter, unless they appear to already be present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the security package indexes are to be
        /// found and loaded.
        /// </param>
        /// <param name="force">
        /// When non-zero, the scan is performed even if the interpreter is
        /// already initialized.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.Disposed)
                {
                    error = "interpreter is disposed";
                    return ReturnCode.Error;
                }

                StringList paths = null;

                /* NO RESULT */
                GetSecurityPackageIndexPaths(interpreter, ref paths);

                if (paths == null)
                    return ReturnCode.Ok;

                PackageIndexDictionary packageIndexes =
                    interpreter.CopyPackageIndexes();

                //
                // HACK: If all the security package indexes are present in
                //       the interpreter, skip trying to find and load them
                //       again.
                //
                if (HaveSecurityPackageIndexes(
                        interpreter, paths, packageIndexes))
                {
                    return ReturnCode.Ok;
                }

                PackageIndexFlags savedPackageIndexFlags =
                    interpreter.ContextPackageIndexFlags;

                try
                {
                    interpreter.ContextPackageIndexFlags =
                        PackageIndexFlags.SecurityPackage;

                    PackageFlags savedPackageFlags =
                        interpreter.ContextPackageFlags;

                    try
                    {
                        interpreter.ContextPackageFlags |=
                            PackageFlags.SecurityPackageMask;

                        if (PackageOps.FindAll(
                                interpreter, paths,
                                interpreter.ContextPackageIndexFlags,
                                interpreter.PathComparisonType,
                                ref packageIndexes,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }
                    finally
                    {
                        interpreter.ContextPackageFlags =
                            savedPackageFlags;
                    }
                }
                finally
                {
                    interpreter.ContextPackageIndexFlags =
                        savedPackageIndexFlags;
                }

                interpreter.PackageIndexes = packageIndexes;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the file name of the current
        /// executable is one of the default shell file names.
        /// </summary>
        /// <returns>
        /// True if the current executable name is a default shell file name;
        /// otherwise, false.
        /// </returns>
        public static bool IsDefaultShellFileName()
        {
            return IsDefaultShellFileName(PathOps.GetExecutableNameOnly());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file name is one of the
        /// default shell file names.
        /// </summary>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, to check.
        /// </param>
        /// <returns>
        /// True if the file name is a default shell file name; otherwise,
        /// false.
        /// </returns>
        private static bool IsDefaultShellFileName(
            string fileNameOnly /* in */
            )
        {
            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultShellFileName))
            {
                return true;
            }

            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultShell32FileName))
            {
                return true;
            }

            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultKitFileName))
            {
                return true;
            }

            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultKit32FileName))
            {
                return true;
            }

            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultEnterpriseKitFileName))
            {
                return true;
            }

            if (PathOps.IsEqualFileName(
                    fileNameOnly, DefaultEnterpriseKit32FileName))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if !DEBUG
        /// <summary>
        /// This method returns the shell file name for the current executable,
        /// optionally using the WoW64 variant.
        /// </summary>
        /// <param name="wow64">
        /// When non-zero, the 32-bit (WoW64) variant of the file name is
        /// returned.
        /// </param>
        /// <returns>
        /// The shell file name for the current executable.
        /// </returns>
        private static string GetShellFileName(
            bool wow64 /* in */
            )
        {
            return GetShellFileName(
                PathOps.GetExecutableNameOnly(), wow64);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the shell file name for the specified file name,
        /// optionally using the WoW64 variant.
        /// </summary>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, to use as the
        /// basis for the result.
        /// </param>
        /// <param name="wow64">
        /// When non-zero, the 32-bit (WoW64) variant of the file name is
        /// returned.
        /// </param>
        /// <returns>
        /// The shell file name corresponding to the specified file name.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the security plugins are likely to be
        /// broken in the current runtime environment (e.g. a 64-bit process on
        /// the .NET Framework 2.0 runtime).
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about why the security packages
        /// are likely broken.
        /// </param>
        /// <returns>
        /// True if the security packages are likely broken; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a security update check should be
        /// performed for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the security update check is being
        /// considered.
        /// </param>
        /// <param name="verbose">
        /// When non-zero, additional diagnostic output may be produced while
        /// reading the relevant configuration.
        /// </param>
        /// <returns>
        /// True if a security update check should be performed; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method looks up a named security script and evaluates it as a
        /// trusted script in the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the security script is to be evaluated.
        /// </param>
        /// <param name="name">
        /// The name of the security script to look up and evaluate.
        /// </param>
        /// <param name="temporaryFileName">
        /// The name of a temporary file to be formatted into the script text,
        /// or null if none is required.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of evaluating the script; upon
        /// failure, receives information about the error that was encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode EvaluateNamedSecurityScript(
            Interpreter interpreter,  /* in */
            string name,              /* in */
            string temporaryFileName, /* in */
            ref Result result         /* out */
            )
        {
            ScriptFlags scriptFlags = ScriptOps.GetFlags(interpreter,
                ScriptFlags.CoreLibrarySecurityRequiredFile, false,
                false);

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    name, ref scriptFlags, ref clientData,
                    ref localResult) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "EvaluateNamedSecurityScript: no {0} " +
                    "script available, error = {1}",
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(localResult)),
                    typeof(ScriptOps).Name,
                    TracePriority.SecurityError);

                result = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should use several "unsafe" commands
            //       (i.e. within Harpy); therefore, we must evaluate
            //       it as an "unsafe" one.
            //
            string text = localResult;

            if (temporaryFileName != null)
            {
                text = String.Format(
                    text, Parser.Quote(temporaryFileName));
            }

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
                            "EvaluateNamedSecurityScript: " +
                            "script file {0} failed, error = {1}",
                            FormatOps.WrapOrNull(name),
                            FormatOps.WrapOrNull(localResult)),
                            typeof(ScriptOps).Name,
                            TracePriority.SecurityError);

                        result = localResult;
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
                            "EvaluateNamedSecurityScript: " +
                            "script text {0} failed, error = {1}",
                            FormatOps.WrapOrNull(name),
                            FormatOps.WrapOrNull(localResult)),
                            typeof(ScriptOps).Name,
                            TracePriority.SecurityError);

                        result = localResult;
                        return ReturnCode.Error;
                    }
                }

                result = localResult;
                return ReturnCode.Ok;
            }
            finally
            {
                if (profiler != null)
                {
                    profiler.Stop();

                    TraceOps.DebugTrace(String.Format(
                        "EvaluateNamedSecurityScript: {0} in {1}",
                        FormatOps.WrapOrNull(name),
                        FormatOps.MaybeNull(profiler)),
                        typeof(ScriptOps).Name,
                        TracePriority.SecurityDebug);

                    if (dispose)
                    {
                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                            interpreter, ref profiler);
                    }

                    profiler = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether security can be enabled for the
        /// specified interpreter, ensuring the security package indexes are
        /// available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which security is being considered.
        /// </param>
        /// <param name="force">
        /// When non-zero, the security package index scan is performed even if
        /// the interpreter is already initialized.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if security can be enabled; otherwise,
        /// an error code.
        /// </returns>
        private static ReturnCode CanEnableSecurity(
            Interpreter interpreter, /* in */
            bool force,              /* in */
            ref Result error         /* out */
            )
        {
            if (AreSecurityPackagesLikelyBroken(ref error))
            {
                TraceOps.DebugTrace(String.Format(
                    "CanEnableSecurity: security packages likely broken, " +
                    "error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            if (MaybeFindSecurityPackageIndexes(
                    interpreter, force, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "CanEnableSecurity: package indexes scan failed, " +
                    "error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables security for the specified
        /// interpreter by evaluating the appropriate security script.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which security is to be enabled or disabled.
        /// </param>
        /// <param name="enable">
        /// When non-zero, security is enabled; otherwise, security is disabled.
        /// </param>
        /// <param name="force">
        /// When non-zero, the security package index scan is performed even if
        /// the interpreter is already initialized.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

            //
            // NOTE: First, check if it may be possible to load the security
            //       plugins (i.e. Harpy and Badge).  This means that either
            //       their associated package indexes must already be loaded
            //       -OR- they can be found in one of the locations where we
            //       expect to find them.
            //
            if (CanEnableSecurity(
                    interpreter, force, ref error) != ReturnCode.Ok)
            {
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
            Result localResult = null;

            try
            {
                if (EvaluateNamedSecurityScript(interpreter,
                        enable ? EnableSecurityScriptName :
                        DisableSecurityScriptName, null,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            TraceOps.DebugTrace(String.Format(
                "EnableOrDisableSecurity: {0}{1} security",
                force ? "forcibly " : String.Empty,
                enable ? "enabled" : "disabled"),
                typeof(ScriptOps).Name, TracePriority.SecurityDebug);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fetches the security key ring and merges it into the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the key ring is to be fetched and merged.
        /// </param>
        /// <param name="force">
        /// When non-zero, the security package index scan is performed even if
        /// the interpreter is already initialized.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode FetchAndMergeKeyRing(
            Interpreter interpreter,
            bool force,
            ref Result error
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
                    "FetchAndMergeKeyRing: security packages likely " +
                    "broken, error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            if (MaybeFindSecurityPackageIndexes(
                    interpreter, force, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "FetchAndMergeKeyRing: package indexes scan " +
                    "failed, error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name, TracePriority.SecurityError);

                return ReturnCode.Error;
            }

            string temporaryDirectory = null;

            try
            {
                Result localResult; /* REUSED */

                temporaryDirectory = PathOps.GetUniquePath(
                    null, PathOps.GetTempPath(interpreter),
                    null, null, ref error);

                if (temporaryDirectory == null)
                    return ReturnCode.Error;

                Directory.CreateDirectory(
                    temporaryDirectory); /* throw */

                string temporaryFileName = Path.Combine(
                    temporaryDirectory, KeyRingFileNameOnly);

                localResult = null;

                if (EvaluateNamedSecurityScript(interpreter,
                        FetchKeyRingScriptName, null,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }

                string text = localResult;

                File.WriteAllBytes(temporaryFileName,
                    Convert.FromBase64String(text)); /* throw */

                localResult = null;

                if (EvaluateNamedSecurityScript(interpreter,
                        MergeKeyRingScriptName, temporaryFileName,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                if (temporaryDirectory != null)
                {
                    /* IGNORED */
                    Utility.CleanupDirectory(temporaryDirectory,
                        new string[] { KeyRingFileNameOnly }, true);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a hexadecimal public key token string into its
        /// byte array representation.
        /// </summary>
        /// <param name="value">
        /// The hexadecimal public key token string to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the numeric value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The parsed public key token bytes, or null if the value could not be
        /// parsed.
        /// </returns>
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

        /// <summary>
        /// This method finds the security plugin with the specified priority
        /// within the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to search for the security plugin.
        /// </param>
        /// <param name="priority">
        /// The priority that selects which security package name to look up.
        /// </param>
        /// <param name="alternate">
        /// When non-zero, the alternate set of security package names is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The matching security plugin, or null if it could not be found.
        /// </returns>
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

        /// <summary>
        /// This method opens a stream over the embedded security certificate
        /// resource.
        /// </summary>
        /// <param name="stream">
        /// Upon success, receives the stream over the security certificate
        /// resource.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method reads all of the bytes of the security certificate from
        /// the specified stream.
        /// </summary>
        /// <param name="stream">
        /// The stream from which to read the security certificate bytes.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the security certificate bytes.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method asks the specified security plugin to check the supplied
        /// security certificate bytes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when executing the security plugin.
        /// </param>
        /// <param name="plugin">
        /// The security plugin used to check the certificate.
        /// </param>
        /// <param name="bytes">
        /// The security certificate bytes to check.
        /// </param>
        /// <param name="asDictionary">
        /// When non-zero, the result is requested in dictionary form.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the certificate check; upon
        /// failure, receives information about the error that was encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode CheckSecurityCertificate(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            byte[] bytes,            /* in */
            bool asDictionary,       /* in */
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
                using (AnyClientData anyClientData = new AnyClientData(
                        SecurityCertificateRequestName))
                {
                    anyClientData.TrySetAny(
                        DataNames.AsDictionary, asDictionary);

                    object[] request = {
                        interpreter, corePlugin, bytes, result
                    };

                    object response = null;

                    if (plugin.Execute(
                            interpreter, anyClientData, request,
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
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates the security plugin and uses it to check the
        /// embedded security certificate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when locating and executing the
        /// security plugin.
        /// </param>
        /// <param name="asDictionary">
        /// When non-zero, the result is requested in dictionary form.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the certificate check; upon
        /// failure, receives information about the error that was encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CheckSecurityCertificate(
            Interpreter interpreter, /* in */
            bool asDictionary,       /* in */
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
                    interpreter, plugin, bytes, asDictionary, ref result);
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

        #region Procedure Support Methods
        /// <summary>
        /// This method formats the specified annotation name as it would
        /// appear within the body of a procedure (i.e. wrapped in angle
        /// brackets).
        /// </summary>
        /// <param name="annotation">
        /// The annotation name to be formatted.
        /// </param>
        /// <returns>
        /// The formatted annotation string.
        /// </returns>
        public static string FormatAnnotation(
            string annotation
            )
        {
            return String.Format(
                "{0}{0}{1}{2}{2}", Characters.LessThanSign,
                annotation, Characters.GreaterThanSign);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a specific named annotation is
        /// present within the specified collection of annotations and, if so,
        /// what its associated boolean value is.
        /// </summary>
        /// <param name="annotations">
        /// The collection of annotations to query.
        /// </param>
        /// <param name="name">
        /// The name of the annotation to look up.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when converting the annotation value to a
        /// boolean.
        /// </param>
        /// <param name="value">
        /// Upon success, receives non-zero if the annotation is present and its
        /// value is true (or it is present with no value); otherwise, receives
        /// false.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives any error messages generated while querying
        /// the annotation.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode HaveAnnotation(
            StringDictionary annotations,
            string name,
            CultureInfo cultureInfo,
            ref bool value,
            ref ResultList errors
            )
        {
            if (annotations == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid annotations");
                return ReturnCode.Error;
            }

            if (name == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid annotation name");
                return ReturnCode.Error;
            }

            string stringValue;

            if (annotations.TryGetValue(name, out stringValue))
            {
                if (!String.IsNullOrEmpty(stringValue))
                {
                    bool boolValue = false;
                    Result localError = null;

                    if (Value.GetBoolean2(
                            stringValue, ValueFlags.AnyBoolean,
                            cultureInfo, ref boolValue,
                            ref localError) != ReturnCode.Ok)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        return ReturnCode.Error;
                    }

                    value = boolValue;
                }
                else
                {
                    value = true;
                }
            }
            else
            {
                value = false; /* REDUNDANT? */
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the final collection of procedure arguments by
        /// starting with the supplied procedure arguments and then removing any
        /// arguments that are slated to be overwritten.
        /// </summary>
        /// <param name="procedureArguments">
        /// The original collection of procedure arguments.
        /// </param>
        /// <param name="overwriteArguments">
        /// The list of arguments that should be removed from the final
        /// collection.  This parameter may be null.
        /// </param>
        /// <param name="finalArguments">
        /// Upon return, receives the resulting collection of arguments, or null
        /// if there were no procedure arguments.
        /// </param>
        public static void GetFinalArguments(
            ArgumentDictionary procedureArguments, /* in */
            ArgumentList overwriteArguments,       /* in */
            out ArgumentDictionary finalArguments  /* out */
            )
        {
            finalArguments = null;

            if (procedureArguments == null)
                return;

            finalArguments = new ArgumentDictionary(procedureArguments);

            if (overwriteArguments == null)
                return;

            foreach (Argument overwriteArgument in overwriteArguments)
            {
                if (overwriteArgument == null)
                    continue;

                string overwriteName = overwriteArgument.Name;

                if (overwriteName == null)
                    continue;

                finalArguments.Remove(overwriteName);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the final list of procedure arguments by starting
        /// with the supplied procedure arguments and then removing any arguments
        /// that are slated to be overwritten.
        /// </summary>
        /// <param name="procedureArguments">
        /// The original list of procedure arguments.
        /// </param>
        /// <param name="overwriteArguments">
        /// The list of arguments that should be removed from the final list.
        /// This parameter may be null.
        /// </param>
        /// <param name="finalArguments">
        /// Upon return, receives the resulting list of arguments, or null if
        /// there were no procedure arguments.
        /// </param>
        public static void GetFinalArguments(
            ArgumentList procedureArguments, /* in */
            ArgumentList overwriteArguments, /* in */
            out ArgumentList finalArguments  /* out */
            )
        {
            finalArguments = null;

            if (procedureArguments == null)
                return;

            finalArguments = new ArgumentList(procedureArguments);

            if (overwriteArguments == null)
                return;

            foreach (Argument overwriteArgument in overwriteArguments)
            {
                if (overwriteArgument == null)
                    continue;

                string overwriteName = overwriteArgument.Name;

                if (overwriteName == null)
                    continue;

                int count = finalArguments.Count;

                for (int index = count - 1; index >= 0; index--)
                {
                    Argument finalArgument = finalArguments[index];

                    if (finalArgument == null)
                        continue;

                    string finalName = finalArgument.Name;

                    if (finalName == null)
                        continue;

                    if (!SharedStringOps.SystemEquals(
                            finalName, overwriteName))
                    {
                        continue;
                    }

                    finalArguments.RemoveAt(index);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines which procedure arguments should be unset by
        /// examining the supplied list of "clean" arguments and selecting those
        /// that are present among the procedure arguments.
        /// </summary>
        /// <param name="procedureArguments">
        /// The collection of procedure arguments to examine.
        /// </param>
        /// <param name="cleanArguments">
        /// The list of arguments that should be unset, if present.
        /// </param>
        /// <param name="unsetArguments">
        /// Upon return, receives the collection of arguments that should be
        /// unset, or null if there are none.
        /// </param>
        private static void GetUnsetArguments(
            ArgumentDictionary procedureArguments, /* in */
            ArgumentList cleanArguments,           /* in */
            out ArgumentDictionary unsetArguments  /* out */
            )
        {
            unsetArguments = null;

            if (procedureArguments == null)
                return;

            if (cleanArguments == null)
                return;

            foreach (Argument cleanArgument in cleanArguments)
            {
                if (cleanArgument == null)
                    continue;

                string cleanName = cleanArgument.Name;

                if (cleanName == null)
                    continue;

                if (procedureArguments.ContainsKey(cleanName))
                {
                    if (unsetArguments == null)
                        unsetArguments = new ArgumentDictionary();

                    unsetArguments[cleanName] = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines which procedure arguments should be unset by
        /// examining the supplied list of "clean" arguments and selecting those
        /// that are present among the procedure arguments.
        /// </summary>
        /// <param name="procedureArguments">
        /// The list of procedure arguments to examine.
        /// </param>
        /// <param name="cleanArguments">
        /// The list of arguments that should be unset, if present.
        /// </param>
        /// <param name="unsetArguments">
        /// Upon return, receives the list of arguments that should be unset, or
        /// null if there are none.
        /// </param>
        private static void GetUnsetArguments(
            ArgumentList procedureArguments, /* in */
            ArgumentList cleanArguments,     /* in */
            out ArgumentList unsetArguments  /* out */
            )
        {
            unsetArguments = null;

            if (procedureArguments == null)
                return;

            if (cleanArguments == null)
                return;

            foreach (Argument cleanArgument in cleanArguments)
            {
                if (cleanArgument == null)
                    continue;

                string cleanName = cleanArgument.Name;

                if (cleanName == null)
                    continue;

                bool found = false;
                int count = procedureArguments.Count;

                for (int index = 0; index < count; index++)
                {
                    Argument procedureArgument = procedureArguments[index];

                    if (procedureArgument == null)
                        continue;

                    string procedureArgumentName = procedureArgument.Name;

                    if (procedureArgumentName == null)
                        continue;

                    if (SharedStringOps.SystemEquals(
                            procedureArgumentName, cleanName))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    if (unsetArguments == null)
                        unsetArguments = new ArgumentList();

                    unsetArguments.Add(cleanArgument);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unsets the procedure arguments identified by the supplied
        /// list of "clean" arguments, complaining about any errors encountered
        /// while doing so.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when unsetting the arguments.
        /// </param>
        /// <param name="frame">
        /// The call frame containing the variables to be unset.
        /// </param>
        /// <param name="procedureArguments">
        /// The collection of procedure arguments to examine.
        /// </param>
        /// <param name="cleanArguments">
        /// The list of arguments that should be unset, if present.
        /// </param>
        public static void UnsetArgumentsOrComplain(
            Interpreter interpreter,               /* in */
            ICallFrame frame,                      /* in */
            ArgumentDictionary procedureArguments, /* in */
            ArgumentList cleanArguments            /* in */
            )
        {
            ArgumentDictionary unsetArguments;

            GetUnsetArguments(
                procedureArguments, cleanArguments, out unsetArguments);

            if (unsetArguments == null)
                return;

            ResultList errors = null;

            foreach (ArgumentPair pair in unsetArguments)
            {
                IAnyPair<int, Argument> anyPair = pair.Value;

                if (anyPair == null)
                    continue;

                Argument unsetArgument = anyPair.Y;

                if (unsetArgument == null)
                    continue;

                Result error = null;

                if (interpreter.UnsetVariable2(
                        VariableFlags.None, frame, unsetArgument,
                        null, null, ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }
                }
            }

            if (errors != null)
            {
                DebugOps.Complain(
                    interpreter, ReturnCode.Error, errors);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unsets the procedure arguments identified by the supplied
        /// list of "clean" arguments, complaining about any errors encountered
        /// while doing so.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when unsetting the arguments.
        /// </param>
        /// <param name="frame">
        /// The call frame containing the variables to be unset.
        /// </param>
        /// <param name="procedureArguments">
        /// The list of procedure arguments to examine.
        /// </param>
        /// <param name="cleanArguments">
        /// The list of arguments that should be unset, if present.
        /// </param>
        public static void UnsetArgumentsOrComplain(
            Interpreter interpreter,         /* in */
            ICallFrame frame,                /* in */
            ArgumentList procedureArguments, /* in */
            ArgumentList cleanArguments      /* in */
            )
        {
            ArgumentList unsetArguments;

            GetUnsetArguments(
                procedureArguments, cleanArguments, out unsetArguments);

            if (unsetArguments == null)
                return;

            ResultList errors = null;

            foreach (Argument unsetArgument in unsetArguments)
            {
                Result error = null;

                if (interpreter.UnsetVariable2(
                        VariableFlags.None, frame, unsetArgument,
                        null, null, ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }
                }
            }

            if (errors != null)
            {
                DebugOps.Complain(
                    interpreter, ReturnCode.Error, errors);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method examines the body of a procedure to determine which
        /// flags it should have, based on the annotations present in its text.
        /// This overload ignores the "private" annotation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when parsing the procedure body.
        /// </param>
        /// <param name="name">
        /// The name of the procedure being examined.
        /// </param>
        /// <param name="text">
        /// The body text of the procedure being examined.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when converting annotation values to booleans.
        /// </param>
        /// <param name="isLibrary">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// a library procedure.
        /// </param>
        /// <param name="isFast">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// fast.
        /// </param>
        /// <param name="isAtomic">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// atomic.
        /// </param>
        /// <param name="isInline">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// inline.
        /// </param>
        /// <param name="isNonCaching">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// non-caching.
        /// </param>
        /// <param name="isMatchTypes">
        /// Upon return, receives non-zero if the procedure should be flagged to
        /// match argument types.
        /// </param>
        /// <param name="overwriteArguments">
        /// Upon return, receives the list of arguments to be overwritten, if
        /// any.
        /// </param>
        /// <param name="cleanArguments">
        /// Upon return, receives the list of arguments to be cleaned (unset),
        /// if any.
        /// </param>
        public static void ShouldProcedureHaveFlags(
            Interpreter interpreter,             /* in */
            string name,                         /* in */
            string text,                         /* in */
            CultureInfo cultureInfo,             /* in */
            out bool isLibrary,                  /* out */
            out bool isFast,                     /* out */
            out bool isAtomic,                   /* out */
            out bool isInline,                   /* out */
#if ARGUMENT_CACHE || PARSE_CACHE
            out bool isNonCaching,               /* out */
#endif
            out bool isMatchTypes,               /* out */
            out ArgumentList overwriteArguments, /* out */
            out ArgumentList cleanArguments      /* out */
            )
        {
            bool isPrivate; /* NOT USED */

            ShouldProcedureHaveFlags(
                interpreter, name, text, cultureInfo,
                out isLibrary, out isPrivate, out isFast,
                out isAtomic, out isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                out isNonCaching,
#endif
                out isMatchTypes, out overwriteArguments,
                out cleanArguments);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This procedure is used to determine if a procedure body
        //       passed to the [proc] command should be "Private", which
        //       means that procedure may only be called from within its
        //       own namespace (i.e. external callers, including global,
        //       are disallowed).  Here is an example:
        //
        //       namespace eval ::Examples {
        //         proc example1 {} {; # <<private>>
        //           #
        //           # NOTE: This procedure is private.
        //           #
        //           puts stdout "Did something private."
        //         }
        //
        //         proc example2 {} {; # <<private:0>>
        //           #
        //           # NOTE: This procedure is private.
        //           #
        //           puts stdout "Doing something public..."
        //           return [example1]
        //         }
        //       }
        //
        //       namespace eval ::Other {
        //         proc other1 {} {
        //           return [::Examples::example1]; # wrong namespace
        //         }
        //       }
        //
        //       ::Examples::example1; # cannot be called globally
        //       ::Examples::example2; # ok, also, can call example1
        //       ::Other::other1;      # cross-namespace disallowed
        //
        //       When annotation is present with no value, that is the
        //       treated the same as an explicit non-zero value, e.g.:
        //
        //       ANNOTATION_NOT_FOUND ==> false
        //
        //       ANNOTATION_WAS_FOUND <<private>> ==> true
        //
        //       ANNOTATION_WAS_FOUND <<private:false>> ==> false
        //       ANNOTATION_WAS_FOUND <<private:0>> ==> false
        //
        //       ANNOTATION_WAS_FOUND <<private:true>> ==> true
        //       ANNOTATION_WAS_FOUND <<private:1>> ==> true
        //
        /// <summary>
        /// This method examines the body of a procedure to determine which
        /// flags it should have, based on the annotations present in its text,
        /// including whether the procedure should be considered private.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when parsing the procedure body.
        /// </param>
        /// <param name="name">
        /// The name of the procedure being examined.
        /// </param>
        /// <param name="text">
        /// The body text of the procedure being examined.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when converting annotation values to booleans.
        /// </param>
        /// <param name="isLibrary">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// a library procedure.
        /// </param>
        /// <param name="isPrivate">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// private.
        /// </param>
        /// <param name="isFast">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// fast.
        /// </param>
        /// <param name="isAtomic">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// atomic.
        /// </param>
        /// <param name="isInline">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// inline.
        /// </param>
        /// <param name="isNonCaching">
        /// Upon return, receives non-zero if the procedure should be flagged as
        /// non-caching.
        /// </param>
        /// <param name="isMatchTypes">
        /// Upon return, receives non-zero if the procedure should be flagged to
        /// match argument types.
        /// </param>
        /// <param name="overwriteArguments">
        /// Upon return, receives the list of arguments to be overwritten, if
        /// any.
        /// </param>
        /// <param name="cleanArguments">
        /// Upon return, receives the list of arguments to be cleaned (unset),
        /// if any.
        /// </param>
        public static void ShouldProcedureHaveFlags(
            Interpreter interpreter,             /* in */
            string name,                         /* in */
            string text,                         /* in */
            CultureInfo cultureInfo,             /* in */
            out bool isLibrary,                  /* out */
            out bool isPrivate,                  /* out */
            out bool isFast,                     /* out */
            out bool isAtomic,                   /* out */
            out bool isInline,                   /* out */
#if ARGUMENT_CACHE || PARSE_CACHE
            out bool isNonCaching,               /* out */
#endif
            out bool isMatchTypes,               /* out */
            out ArgumentList overwriteArguments, /* out */
            out ArgumentList cleanArguments      /* out */
            )
        {
            ResultList errors = null;

            if ((interpreter != null) && FlagOps.HasFlags(
                    interpreter.ProcedureFlags,
                    ProcedureFlags.Library, true))
            {
                isLibrary = true;
            }
            else
            {
                isLibrary = false;
            }

            ///////////////////////////////////////////////////////////////////

            isPrivate = false;
            isFast = false;
            isAtomic = false;
            isInline = false;

#if ARGUMENT_CACHE || PARSE_CACHE
            isNonCaching = false;
#endif

            isMatchTypes = false;
            overwriteArguments = null;
            cleanArguments = null;

            try
            {
                StringDictionary annotations = null;
                Result error = null;

                if (Value.ExtractAnnotations(
                        text, ref annotations,
                        ref error) != ReturnCode.Ok)
                {
                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    return;
                }

                if (annotations != null)
                {
                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.Private,
                        cultureInfo, ref isPrivate,
                        ref errors);

                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.Fast,
                        cultureInfo, ref isFast,
                        ref errors);

                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.Atomic,
                        cultureInfo, ref isAtomic,
                        ref errors);

                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.Inline,
                        cultureInfo, ref isInline,
                        ref errors);

#if ARGUMENT_CACHE || PARSE_CACHE
                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.NonCaching,
                        cultureInfo, ref isNonCaching,
                        ref errors);
#endif

                    /* IGNORED */
                    HaveAnnotation(
                        annotations, Annotations.MatchTypes,
                        cultureInfo, ref isMatchTypes,
                        ref errors);

                    ///////////////////////////////////////////////////////////

                    string stringValue; /* REUSED */
                    StringList list; /* REUSED */

                    list = null;

                    if (annotations.TryGetValue(
                            Annotations.Overwrite, out stringValue) &&
                        Parser.SplitList(
                            interpreter, stringValue, 0, Length.Invalid,
                            true, ref list, ref error) == ReturnCode.Ok)
                    {
                        overwriteArguments = new ArgumentList(
                            list, ArgumentFlags.NameOnly);
                    }

                    list = null;

                    if (annotations.TryGetValue(
                            Annotations.Clean, out stringValue) &&
                        Parser.SplitList(
                            interpreter, stringValue, 0, Length.Invalid,
                            true, ref list, ref error) == ReturnCode.Ok)
                    {
                        cleanArguments = new ArgumentList(
                            list, ArgumentFlags.NameOnly);
                    }
                }

                return; /* REDUNDANT */
            }
            catch (Exception e) // TODO: Remove before beta 56.
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }
            finally
            {
                if (errors != null)
                {
                    TraceOps.DebugTrace(String.Format(
                        "ShouldProcedureHaveFlags: errors = {0}",
                        FormatOps.WrapOrNull(true, false, errors)),
                        typeof(ScriptOps).Name,
                        TracePriority.AnnotationError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method overload is ONLY for use by the
        //          [apply] and [napply] command implementations.
        //
        /// <summary>
        /// This method validates the supplied combination of procedure flag
        /// values for use by the [apply] and [napply] command implementations.
        /// </summary>
        /// <param name="isLibrary">
        /// Non-zero if the procedure is flagged as a library procedure.
        /// </param>
        /// <param name="isFast">
        /// Non-zero if the procedure is flagged as fast.
        /// </param>
        /// <param name="isAtomic">
        /// Non-zero if the procedure is flagged as atomic.
        /// </param>
        /// <param name="isInline">
        /// Non-zero if the procedure is flagged as inline.
        /// </param>
        /// <param name="isNonCaching">
        /// Non-zero if the procedure is flagged as non-caching.
        /// </param>
        /// <param name="isMatchTypes">
        /// Non-zero if the procedure is flagged to match argument types.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the invalid
        /// combination of flag values.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SanityCheckProcedureFlags(
            bool isLibrary,    /* in */
            bool isFast,       /* in */
            bool isAtomic,     /* in */
            bool isInline,     /* in */
#if ARGUMENT_CACHE || PARSE_CACHE
            bool isNonCaching, /* in */
#endif
            bool isMatchTypes, /* in */
            ref Result error   /* out */
            )
        {
            ProcedureFlags procedureFlags = ProcedureFlags.None; /* NOT USED */

            return SanityCheckAndModifyProcedureFlags(
                isLibrary, false, isFast, isAtomic, isInline,
#if ARGUMENT_CACHE || PARSE_CACHE
                isNonCaching,
#endif
                isMatchTypes, ref procedureFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates the supplied combination of procedure flag
        /// values and, if valid, modifies the supplied procedure flags to
        /// reflect them.
        /// </summary>
        /// <param name="isLibrary">
        /// Non-zero if the procedure is flagged as a library procedure.
        /// </param>
        /// <param name="isPrivate">
        /// Non-zero if the procedure is flagged as private.
        /// </param>
        /// <param name="isFast">
        /// Non-zero if the procedure is flagged as fast.
        /// </param>
        /// <param name="isAtomic">
        /// Non-zero if the procedure is flagged as atomic.
        /// </param>
        /// <param name="isInline">
        /// Non-zero if the procedure is flagged as inline.
        /// </param>
        /// <param name="isNonCaching">
        /// Non-zero if the procedure is flagged as non-caching.
        /// </param>
        /// <param name="isMatchTypes">
        /// Non-zero if the procedure is flagged to match argument types.
        /// </param>
        /// <param name="procedureFlags">
        /// Upon return, receives the procedure flags updated to reflect the
        /// supplied values.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the invalid
        /// combination of flag values.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SanityCheckAndModifyProcedureFlags(
            bool isLibrary,                    /* in */
            bool isPrivate,                    /* in */
            bool isFast,                       /* in */
            bool isAtomic,                     /* in */
            bool isInline,                     /* in */
#if ARGUMENT_CACHE || PARSE_CACHE
            bool isNonCaching,                 /* in */
#endif
            bool isMatchTypes,                 /* in */
            ref ProcedureFlags procedureFlags, /* in, out */
            ref Result error                   /* out */
            )
        {
            if (isInline && (isFast || isMatchTypes))
            {
                error = String.Format(
                    "cannot use the procedure annotations {0} or {1} " +
                    "with the {2} procedure annotation.",
                    FormatOps.WrapOrNull(
                        FormatAnnotation(Annotations.Fast)),
                    FormatOps.WrapOrNull(
                        FormatAnnotation(Annotations.MatchTypes)),
                    FormatOps.WrapOrNull(
                        FormatAnnotation(Annotations.Inline)));

                return ReturnCode.Error;
            }

            if (isPrivate)
                procedureFlags |= ProcedureFlags.Private;

            if (!isInline && isLibrary)
                procedureFlags |= ProcedureFlags.Library;

            if (isFast)
                procedureFlags |= ProcedureFlags.Fast;

            if (isAtomic)
                procedureFlags |= ProcedureFlags.Atomic;

            if (isInline)
                procedureFlags |= ProcedureFlags.NoPushFrame;

#if ARGUMENT_CACHE || PARSE_CACHE
            if (isLibrary || isNonCaching)
                procedureFlags |= ProcedureFlags.NonCaching;
#endif

            if (isMatchTypes)
                procedureFlags |= ProcedureFlags.MatchTypes;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the calling context is permitted to call
        /// the specified procedure, taking into account the procedure's
        /// "private" flag and the current namespace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when checking the caller.
        /// </param>
        /// <param name="procedure">
        /// The procedure whose caller is to be checked.
        /// </param>
        /// <param name="procedureFlags">
        /// Upon return, receives the flags of the specified procedure.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the call is
        /// not permitted.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode MaybeCheckProcedureCaller(
            Interpreter interpreter,           /* in */
            IProcedure procedure,              /* in */
            ref ProcedureFlags procedureFlags, /* out */
            ref Result error                   /* out */
            )
        {
            if (interpreter == null) /* REDUNDANT? */
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (procedure == null) /* REDUNDANT? */
            {
                error = "invalid procedure";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: If there is no "Private" flag for the procedure,
            //       always allow it, at least from the perspective of
            //       this method.
            //
            procedureFlags = procedure.Flags;

            if (!FlagOps.HasFlags(
                    procedureFlags, ProcedureFlags.Private, true))
            {
                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The "Private" flag is unsupported (and ignored)
            //       when namespace support has not been enabled for
            //       the interpreter.
            //
            if (!interpreter.InternalAreNamespacesEnabled())
                return ReturnCode.Ok;

            ICallFrame frame = null;

            if (interpreter.GetVariableFrameViaResolvers(
                    LookupFlags.Default, ref frame,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string name = procedure.Name;

            INamespace currentNamespace = NamespaceOps.GetCurrent(
                interpreter, frame);

            if (!NamespaceOps.IsQualifiedName(name) &&
                ((currentNamespace == null) ||
                    interpreter.IsGlobalNamespace(currentNamespace)))
            {
                error = "global procedures cannot be private";
                return ReturnCode.Error;
            }

            INamespace procedureNamespace = NamespaceOps.LookupParent(
                interpreter, name, true, true, false, ref error);

            if (procedureNamespace == null)
                return ReturnCode.Error;

            if ((currentNamespace == null) || !NamespaceOps.IsSame(
                    procedureNamespace, currentNamespace))
            {
                error = String.Format(
                    "procedure {0} cannot be called from namespace {1}",
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(
                        EntityOps.GetName(currentNamespace)));

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Location Support Methods
        /// <summary>
        /// This method obtains the script location associated with the specified
        /// procedure and, for internal procedures, verifies that it matches the
        /// active script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the location.
        /// </param>
        /// <param name="procedure">
        /// The procedure whose location is to be obtained and checked.
        /// </param>
        /// <param name="procedureFlags">
        /// The flags associated with the specified procedure.
        /// </param>
        /// <param name="location">
        /// Upon success, receives the script location of the procedure.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetAndCheckProcedureLocation(
            Interpreter interpreter,
            IProcedure procedure,
            ProcedureFlags procedureFlags,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (GetProcedureLocation(
                    interpreter, procedure, ref location,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(
                    procedureFlags, ProcedureFlags.Internal, true))
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
                if (GetLocation(
                        interpreter, true, ref scriptLocation,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
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

        /// <summary>
        /// This method obtains the script location associated with the specified
        /// procedure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the location.
        /// </param>
        /// <param name="procedure">
        /// The procedure whose location is to be obtained.
        /// </param>
        /// <param name="location">
        /// Upon success, receives the script location of the procedure.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method obtains the script location spanning the arguments in the
        /// specified argument list, starting at the specified index.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when creating the location.
        /// </param>
        /// <param name="arguments">
        /// The argument list to derive the script location from.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first argument to include in the location.
        /// </param>
        /// <param name="location">
        /// Upon success, receives the resulting script location.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method obtains the file name of the current script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero to consider only script locations established via the
        /// [source] command.
        /// </param>
        /// <param name="scrub">
        /// Non-zero to scrub the returned file name relative to the base path.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name of the current script location.
        /// This value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method obtains the file name and starting line of the current
        /// script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero to consider only script locations established via the
        /// [source] command.
        /// </param>
        /// <param name="scrub">
        /// Non-zero to scrub the returned file name relative to the base path.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name of the current script location.
        /// This value may be null.
        /// </param>
        /// <param name="currentLine">
        /// Upon success, receives the starting line of the current script
        /// location.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method obtains the current script location for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the location.
        /// </param>
        /// <param name="viaSource">
        /// Non-zero to consider only script locations established via the
        /// [source] command.
        /// </param>
        /// <param name="location">
        /// Upon success, receives the current script location, or null if there
        /// is none.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
                    location = interpreter.ManualScriptLocation;

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

        /// <summary>
        /// This method obtains the file system path of the current script,
        /// optionally returning only its containing directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when obtaining the script path.
        /// </param>
        /// <param name="directoryOnly">
        /// Non-zero to return only the directory portion of the script path.
        /// </param>
        /// <param name="path">
        /// Upon success, receives the resulting file system path.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method clears the cached "safe" interpreter, if any.
        /// </summary>
        /// <returns>
        /// The number of cached interpreters that were cleared.
        /// </returns>
        public static int ClearInterpreterCache()
        {
            return MaybeClearInterpreterCache(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cached "safe" interpreter if it belongs to the
        /// specified interpreter group.
        /// </summary>
        /// <param name="groupId">
        /// The identifier of the interpreter group whose cached interpreter
        /// should be cleared, or null to match any group.
        /// </param>
        /// <returns>
        /// The number of cached interpreters that were cleared.
        /// </returns>
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

                localInterpreter = null;

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
        /// <summary>
        /// This method adds introspection information about the script
        /// subsystem caches to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the introspection information will be added.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included.
        /// </param>
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
        /// <summary>
        /// This method determines whether the specified variable name is one
        /// of the default (system) variable names that may be set when an
        /// interpreter is created.
        /// </summary>
        /// <param name="name">
        /// The variable name to check.
        /// </param>
        /// <param name="anyReserved">
        /// Non-zero to also treat any name having a reserved package prefix as
        /// a default variable name.
        /// </param>
        /// <returns>
        /// True if the name is a default variable name; otherwise, false.
        /// </returns>
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
                        Vars.Core.WhatIfShellArgumentCount,
                        Vars.Core.WhatIfShellArguments,
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
                        /* BEGIN: Tcl Shell Only */
                        TclVars.Core.Prompt1,
                        TclVars.Core.Prompt2,
                        /* END: Tcl Shell Only */
                        /* BEGIN: Eagle Shell Only */
                        TclVars.Core.Prompt3,
                        TclVars.Core.Prompt4,
                        TclVars.Core.Prompt5,
                        TclVars.Core.Prompt6,
                        TclVars.Core.Prompt7,
                        TclVars.Core.Prompt8,
                        /* END: Eagle Shell Only */
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

        /// <summary>
        /// This method prepares the specified interpreter for use with static
        /// data only by clearing or removing its variables, purging its call
        /// frames, and (optionally) removing its commands.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to prepare.
        /// </param>
        /// <param name="noEvaluate">
        /// Non-zero to clear variables directly instead of evaluating the
        /// removal scripts, and to skip command removal.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message that
        /// describes the failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code is returned.
        /// </returns>
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

        /// <summary>
        /// This method removes the variables from the specified interpreter by
        /// evaluating the associated removal script.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose variables will be removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message that
        /// describes the failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code is returned.
        /// </returns>
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
            ScriptFlags scriptFlags = GetFlags(
                interpreter, ScriptFlags.CoreLibrarySecurityRequiredFile,
                false, false);

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
        /// <summary>
        /// This method clears the variables contained in the specified call
        /// frame using a brute-force technique that does not cause traces to
        /// fire.
        /// </summary>
        /// <param name="frame">
        /// The call frame whose variables will be cleared.
        /// </param>
        /// <param name="markOnly">
        /// Non-zero to mark the variables as undefined instead of removing them
        /// from the call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message that
        /// describes the failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code is returned.
        /// </returns>
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

        /// <summary>
        /// This method clears the variables contained in the current global
        /// call frame of the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose global call frame variables will be cleared.
        /// </param>
        /// <param name="markOnly">
        /// Non-zero to mark the variables as undefined instead of removing them
        /// from the call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message that
        /// describes the failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code is returned.
        /// </returns>
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

        /// <summary>
        /// This method removes the commands from the specified interpreter by
        /// evaluating the associated removal script.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose commands will be removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive an error message that
        /// describes the failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code is returned.
        /// </returns>
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
            ScriptFlags scriptFlags = GetFlags(
                interpreter, ScriptFlags.CoreLibrarySecurityRequiredFile,
                false, false);

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

        /// <summary>
        /// This method sets the parent interpreter of the specified
        /// interpreter, when that interpreter is not null.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose parent interpreter will be set.
        /// </param>
        /// <param name="parentInterpreter">
        /// The interpreter to use as the parent interpreter.
        /// </param>
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

        /// <summary>
        /// This method conditionally enables additional interpreter flags based
        /// on the specified script data flags and on properties of the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose properties may influence the interpreter
        /// flags.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The script data flags used to control which interpreter flags are
        /// enabled.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags to be modified in place.
        /// </param>
        private static void MaybeEnableInterpreterFlags(
            Interpreter interpreter,              /* in: OPTIONAL */
            ScriptDataFlags flags,                /* in */
            ref InterpreterFlags interpreterFlags /* in, out */
            )
        {
            //
            // HACK: Disable all [package unknown] handling for interpreters
            //       created by this subsystem as they are unnecessary -AND-
            //       can cause downstream issues.  There is an override for
            //       this; however, it should not be used.
            //
            if (!FlagOps.HasFlags(
                    flags, ScriptDataFlags.AllowPackageUnknown, true))
            {
                interpreterFlags |= InterpreterFlags.NoPackageFallback;
                interpreterFlags |= InterpreterFlags.NoPackageUnknown;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptDataFlags.NoThreadAbort, true))
            {
                interpreterFlags |= InterpreterFlags.NoThreadAbort;
            }

            if ((interpreter != null) &&
                interpreter.InternalNoThreadAbort)
            {
                interpreterFlags |= InterpreterFlags.NoThreadAbort;
            }

            //
            // HACK: Disable all use of temporary packages as these
            //       can interfere with the Harpy key ring loader.
            //
            if (!FlagOps.HasFlags(
                    flags, ScriptDataFlags.AllowTemporaryPackages, true))
            {
                interpreterFlags &= ~InterpreterFlags.TemporaryPackages;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin flags match the
        /// typical (default) plugin flags used by this subsystem.
        /// </summary>
        /// <param name="pluginFlags">
        /// The plugin flags to check.
        /// </param>
        /// <param name="defaultsOnly">
        /// Non-zero to compare against the default plugin flags only; zero to
        /// also consider the batch (non-interactive) plugin flags.
        /// </param>
        /// <returns>
        /// True if the plugin flags are typical; otherwise, false.
        /// </returns>
        public static bool AreTypicalPluginFlagsInUse(
            PluginFlags pluginFlags, /* in */
            bool defaultsOnly        /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                PluginFlags defaultsPluginFlags = Defaults.PluginFlags;

                if (pluginFlags == defaultsPluginFlags)
                    return true;

                if (defaultsOnly)
                    return false;

                PluginFlags batchPluginFlags = defaultsPluginFlags;

                batchPluginFlags &= ~PluginFlags.NonInteractiveMask;

                if (pluginFlags == batchPluginFlags)
                    return true;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally enables additional plugin flags based on
        /// the specified script data flags.
        /// </summary>
        /// <param name="flags">
        /// The script data flags used to control which plugin flags are
        /// enabled.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags to be modified in place.
        /// </param>
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

        /// <summary>
        /// This method extracts the various interpreter creation flags from the
        /// specified interpreter, falling back to the supplied default values
        /// or to the global defaults as directed by the creation flag types.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter from which to extract the creation flags.  This
        /// parameter may be null.
        /// </param>
        /// <param name="creationFlagTypes">
        /// The flags used to control which sources are consulted for each
        /// category of creation flags.
        /// </param>
        /// <param name="fallbackCreateFlags">
        /// The fallback create flags to use when no other source is selected.
        /// This parameter may be null.
        /// </param>
        /// <param name="fallbackHostCreateFlags">
        /// The fallback host create flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="fallbackInitializeFlags">
        /// The fallback initialize flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="fallbackScriptFlags">
        /// The fallback script flags to use when no other source is selected.
        /// This parameter may be null.
        /// </param>
        /// <param name="fallbackInterpreterFlags">
        /// The fallback interpreter flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="fallbackInterpreterTestFlags">
        /// The fallback interpreter test flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="fallbackPluginFlags">
        /// The fallback plugin flags to use when no other source is selected.
        /// This parameter may be null.
        /// </param>
        /// <param name="fallbackFindFlags">
        /// The fallback native Tcl find flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="fallbackLoadFlags">
        /// The fallback native Tcl load flags to use when no other source is
        /// selected.  This parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// Upon return, this parameter will receive the extracted create flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// Upon return, this parameter will receive the extracted host create
        /// flags.
        /// </param>
        /// <param name="initializeFlags">
        /// Upon return, this parameter will receive the extracted initialize
        /// flags.
        /// </param>
        /// <param name="scriptFlags">
        /// Upon return, this parameter will receive the extracted script flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// Upon return, this parameter will receive the extracted interpreter
        /// flags.
        /// </param>
        /// <param name="interpreterTestFlags">
        /// Upon return, this parameter will receive the extracted interpreter
        /// test flags.
        /// </param>
        /// <param name="pluginFlags">
        /// Upon return, this parameter will receive the extracted plugin flags.
        /// </param>
        /// <param name="findFlags">
        /// Upon return, this parameter will receive the extracted native Tcl
        /// find flags.
        /// </param>
        /// <param name="loadFlags">
        /// Upon return, this parameter will receive the extracted native Tcl
        /// load flags.
        /// </param>
        public static void ExtractInterpreterCreationFlags(
            Interpreter interpreter,                            /* in: OPTIONAL */
            CreationFlagTypes creationFlagTypes,                /* in */
            CreateFlags? fallbackCreateFlags,                   /* in: OPTIONAL */
            HostCreateFlags? fallbackHostCreateFlags,           /* in: OPTIONAL */
            InitializeFlags? fallbackInitializeFlags,           /* in: OPTIONAL */
            ScriptFlags? fallbackScriptFlags,                   /* in: OPTIONAL */
            InterpreterFlags? fallbackInterpreterFlags,         /* in: OPTIONAL */
            InterpreterTestFlags? fallbackInterpreterTestFlags, /* in: OPTIONAL */
            PluginFlags? fallbackPluginFlags,                   /* in: OPTIONAL */
#if NATIVE && TCL
            FindFlags? fallbackFindFlags,                       /* in: OPTIONAL */
            LoadFlags? fallbackLoadFlags,                       /* in: OPTIONAL */
#endif
            out CreateFlags createFlags,                        /* out */
            out HostCreateFlags hostCreateFlags,                /* out */
            out InitializeFlags initializeFlags,                /* out */
            out ScriptFlags scriptFlags,                        /* out */
            out InterpreterFlags interpreterFlags,              /* out */
            out InterpreterTestFlags interpreterTestFlags,      /* out */
            out PluginFlags pluginFlags                         /* out */
#if NATIVE && TCL
            , out FindFlags findFlags,                          /* out */
            out LoadFlags loadFlags                             /* out */
#endif
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

            ///////////////////////////////////////////////////////////////////

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

            ///////////////////////////////////////////////////////////////////

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

            ///////////////////////////////////////////////////////////////////

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

            ///////////////////////////////////////////////////////////////////

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

            ///////////////////////////////////////////////////////////////////

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentInterpreterTestFlags, true))
            {
                interpreterTestFlags = interpreter.InterpreterTestFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultInterpreterTestFlags, true))
            {
                interpreterTestFlags = interpreter.DefaultInterpreterTestFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackInterpreterTestFlags, true))
            {
                interpreterTestFlags = (fallbackInterpreterTestFlags != null) ?
                    (InterpreterTestFlags)fallbackInterpreterTestFlags :
                    Defaults.InterpreterTestFlags;
            }
            else
            {
                interpreterTestFlags = InterpreterTestFlags.None;
            }

            ///////////////////////////////////////////////////////////////////

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

            ///////////////////////////////////////////////////////////////////

#if NATIVE && TCL
            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentFindFlags, true))
            {
                findFlags = interpreter.TclFindFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultFindFlags, true))
            {
                findFlags = interpreter.DefaultTclFindFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackFindFlags, true))
            {
                findFlags = (fallbackFindFlags != null) ?
                    (FindFlags)fallbackFindFlags :
                    Defaults.FindFlags;
            }
            else
            {
                findFlags = FindFlags.None;
            }

            ///////////////////////////////////////////////////////////////////

            if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.CurrentLoadFlags, true))
            {
                loadFlags = interpreter.TclLoadFlags;
            }
            else if ((interpreter != null) && FlagOps.HasFlags(
                    creationFlagTypes,
                    CreationFlagTypes.DefaultLoadFlags, true))
            {
                loadFlags = interpreter.DefaultTclLoadFlags;
            }
            else if (FlagOps.HasFlags(creationFlagTypes,
                    CreationFlagTypes.FallbackLoadFlags, true))
            {
                loadFlags = (fallbackLoadFlags != null) ?
                    (LoadFlags)fallbackLoadFlags :
                    Defaults.LoadFlags;
            }
            else
            {
                loadFlags = LoadFlags.None;
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an interpreter suitable for use with the
        /// settings subsystem, using the specified script data flags.
        /// </summary>
        /// <param name="interpreter">
        /// The parent interpreter, which may influence the configuration of the
        /// created interpreter.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the created interpreter.
        /// </param>
        /// <param name="flags">
        /// The script data flags used to control how the interpreter is
        /// created.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will receive any associated result data;
        /// upon failure, it will receive an error message that describes the
        /// failure.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null if it could not be created.
        /// </returns>
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

        /// <summary>
        /// This method creates an interpreter suitable for use with the
        /// settings subsystem, using the specified script data flags, and
        /// returns the name of any child interpreter that was created.
        /// </summary>
        /// <param name="interpreter">
        /// The parent interpreter, which may influence the configuration of the
        /// created interpreter.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the created interpreter.
        /// </param>
        /// <param name="flags">
        /// The script data flags used to control how the interpreter is
        /// created.
        /// </param>
        /// <param name="childName">
        /// Upon success, when an isolated interpreter is created, this parameter
        /// will receive the name (path) of the created child interpreter.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will receive any associated result data;
        /// upon failure, it will receive an error message that describes the
        /// failure.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null if it could not be created.
        /// </returns>
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
            InterpreterTestFlags interpreterTestFlags;
            PluginFlags pluginFlags;

#if NATIVE && TCL
            FindFlags findFlags;
            LoadFlags loadFlags;
#endif

            ExtractInterpreterCreationFlags(
                interpreter, CreationFlagTypes.SettingsFlags,
                defaultCreateFlags, defaultHostCreateFlags,
                null, null, null, null, null,
#if NATIVE && TCL
                null, null,
#endif
                out createFlags,
                out hostCreateFlags, out initializeFlags,
                out scriptFlags, out interpreterFlags,
                out interpreterTestFlags, out pluginFlags
#if NATIVE && TCL
                , out findFlags, out loadFlags
#endif
                );

            initializeFlags &= ~InitializeFlags.TrustedRemote;
            initializeFlags |= InitializeFlags.NoTrustedRemote;

            bool enableSecurity = FlagOps.HasFlags(
                flags, ScriptDataFlags.EnableSecurity, true);

            if (enableSecurity)
            {
                initializeFlags |= InitializeFlags.Scan;
                initializeFlags |= InitializeFlags.Security;
            }

            bool disableSecurity = FlagOps.HasFlags(
                flags, ScriptDataFlags.DisableSecurity, true);

            if (disableSecurity)
            {
                initializeFlags &= ~InitializeFlags.Scan;
                initializeFlags &= ~InitializeFlags.Security;
            }

            bool noStartup = FlagOps.HasFlags(
                flags, ScriptDataFlags.NoStartup, true);

            if (noStartup)
                initializeFlags &= ~InitializeFlags.ShellOrStartup;

            //
            // HACK: Maybe change interpreter flags based on properties of
            //       the (parent) interpreter?  Important for users of the
            //       Harpy SDK.
            //
            MaybeEnableInterpreterFlags(
                interpreter, flags, ref interpreterFlags);

            //
            // HACK: If requested by the caller, set special plugin flags
            //       to prevent potential conflicts between the settings
            //       loader and plugin isolation.
            //
            MaybeEnablePluginFlags(flags, ref pluginFlags);

            NewHostCallback savedNewHostCallback = null;

            Interpreter.BeginNoNewHostCallback(
                ref savedNewHostCallback);

            try
            {
                bool useIsolated = FlagOps.HasFlags(
                    flags, ScriptDataFlags.UseIsolatedInterpreter, true);

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
                            interpreterTestFlags, pluginFlags,
#if NATIVE && TCL
                            findFlags, loadFlags,
#endif
                            useIsolated,
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
                            //
                            // HACK: Copy the list of trusted
                            //       hashes to the newly created
                            //       interpreter so Harpy will
                            //       be able to load properly.
                            //
                            PolicyOps.CopyTrustedHashes(
                                interpreter, otherInterpreter);

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
                        interpreterTestFlags, pluginFlags, ref result);

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

                    //
                    // HACK: Copy the list of trusted hashes to the newly
                    //       created interpreter so Harpy will be able to
                    //       load properly.
                    //
                    PolicyOps.CopyTrustedHashes(interpreter, otherInterpreter);

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

        /// <summary>
        /// This method determines whether the specified interpreter is
        /// currently in the process of evaluating one or more package index
        /// files.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is evaluating one or more package index
        /// files; otherwise, false.
        /// </returns>
        public static bool IsFileForPackageIndexPending(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.PackageIndexLevels > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter is
        /// currently in the process of evaluating one or more settings files.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is evaluating one or more settings files;
        /// otherwise, false.
        /// </returns>
        public static bool IsFileForSettingsPending(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.SettingLevels > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified settings file in the context of
        /// the specified interpreter, keeping track of the setting nesting
        /// level for the duration of the evaluation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to evaluate the settings file.
        /// </param>
        /// <param name="fileName">
        /// The name of the settings file to evaluate.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to evaluate the settings file as trusted, temporarily
        /// enabling trust for the duration of the evaluation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                    UpdateOps.TryTrustedLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        trusted, ref error) != ReturnCode.Ok))
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

                UpdateOps.ExitTrustedLock(ref locked);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified settings dictionary.
        /// </summary>
        /// <param name="settings">
        /// The settings dictionary to copy.  This value may be null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create and return a new empty dictionary when
        /// <paramref name="settings" /> is null; zero to return null in that
        /// case.
        /// </param>
        /// <returns>
        /// A new dictionary containing a copy of the specified settings, or
        /// null if <paramref name="settings" /> is null and
        /// <paramref name="create" /> is zero.
        /// </returns>
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
        /// <summary>
        /// This method extracts settings from the variables present in the
        /// specified call frame, adding them to the specified settings
        /// dictionary.  The interpreter lock must already be held by the
        /// caller.
        /// </summary>
        /// <param name="frame">
        /// The call frame whose variables should be extracted as settings.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which kinds of variables are extracted and
        /// how unsupported variables are handled.
        /// </param>
        /// <param name="settings">
        /// Upon input, an optional dictionary of settings to be included in the
        /// result.  Upon success, this contains the extracted settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

                    if (!variables.TryGetValue(varName, out variable))
                        continue;

                    if (variable == null)
                        continue;

                    //
                    // BUGFIX: Do *NOT* use any undefined variables here;
                    //         lack of this was causing failures of tests
                    //         "interp-1.37" and "interp-1.43", once the
                    //         undefined "tag" global variable was present
                    //         in newly created interpreters, due to the
                    //         binary plugin loader being used by various
                    //         EEE package index files.
                    //
                    if (EntityOps.IsUndefined(variable))
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
                    // BUGFIX: Do *NOT* use any undefined variables here;
                    //         lack of this was causing failures of tests
                    //         "interp-1.37" and "interp-1.43", once the
                    //         undefined "tag" global variable was present
                    //         in newly created interpreters, due to the
                    //         binary plugin loader being used by various
                    //         EEE package index files.
                    //
                    if (EntityOps.IsUndefined(variable))
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

        /// <summary>
        /// This method loads settings by evaluating the specified script file,
        /// optionally using a newly created, cached, or isolated interpreter,
        /// and then extracting the resulting settings from its global call
        /// frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to create or host the interpreter that
        /// evaluates the settings file.
        /// </param>
        /// <param name="pushClientData">
        /// Optional client data to associate with the active interpreter while
        /// the settings file is being loaded.  This value may be null.
        /// </param>
        /// <param name="callbackClientData">
        /// Optional client data passed to the interpreter creation, use, and
        /// free callbacks.  This value may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the settings file to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the interpreter is created or reused
        /// and how settings are extracted.
        /// </param>
        /// <param name="settings">
        /// Upon input, an optional dictionary of settings to be included in the
        /// result.  Upon success, this contains the loaded settings.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

                                    freeCode = freeInterpreterCallback( /* EXEMPT */
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

                                    freeCode = freeInterpreterCallback( /* EXEMPT */
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

                                localInterpreter = null;

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

        /// <summary>
        /// This method obtains a temporary file name suitable for use as a
        /// script file, ensuring the resulting name has the script file
        /// extension.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the temporary file name.  This value
        /// may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to use when generating the temporary file name.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this contains the temporary script file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode GetTemporaryFileName(
            Interpreter interpreter, /* in: OPTIONAL */
            string prefix,           /* in */
            ref string fileName,     /* out */
            ref Result error         /* out */
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
                fileNames[0] = PathOps.GetTempFileName(
                    interpreter, prefix); /* throw */

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
                    // BUGFIX: Do this only if the file exists.  If not,
                    //         that is fine and the final file will be
                    //         created later by our caller.
                    //
                    if (File.Exists(fileNames[0]))
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

        /// <summary>
        /// This method creates a temporary script file containing the specified
        /// text, optionally using the specified encoding.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain the temporary file name.  This value
        /// may be null.
        /// </param>
        /// <param name="text">
        /// The script text to write into the temporary file.
        /// </param>
        /// <param name="encoding">
        /// The optional encoding to use when writing the script text.  This
        /// value may be null.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this contains the name of the created temporary script
        /// file.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode CreateTemporaryFile(
            Interpreter interpreter, /* in: OPTIONAL */
            string text,             /* in */
            Encoding encoding,       /* in: OPTIONAL */
            ref string fileName,     /* out */
            ref Result error         /* out */
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
                code = GetTemporaryFileName(
                    interpreter, "etsf_", /* Eagle Temporary Script File */
                    ref localFileName, ref error);

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
        /// <summary>
        /// This method determines the effective value of the "via get script"
        /// flag, using the explicitly specified value when one is available and
        /// otherwise consulting the specified script flags.
        /// </summary>
        /// <param name="viaGetScript">
        /// The explicit flag value to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult when no explicit value is specified.
        /// </param>
        /// <returns>
        /// Non-zero if the script is being fetched via the host "get script"
        /// mechanism, zero if it is not, or null if this cannot be determined.
        /// </returns>
        public static bool? ViaGetScriptFlag(
            bool? viaGetScript,         /* in */
            ref ScriptFlags scriptFlags /* in, out */
            )
        {
            if (viaGetScript != null)
                return (bool)viaGetScript;

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.ViaGetScript, true))
            {
                return false;
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the <see cref="ScriptFlags.ExactNameOnly" /> flag
        /// to the specified script flags when the specified name refers to a
        /// non-relative script file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the script being looked up.
        /// </param>
        /// <param name="viaGetScript">
        /// The optional explicit "via get script" flag value.  This parameter
        /// may be null.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to be modified, if applicable.
        /// </param>
        public static void MaybeExactNameOnly(
            Interpreter interpreter,    /* in */
            string name,                /* in */
            bool? viaGetScript,         /* in */
            ref ScriptFlags scriptFlags /* in, out */
            )
        {
            if (PathOps.IsScriptFile(
                    interpreter, name, ViaGetScriptFlag(
                    viaGetScript, ref scriptFlags), false,
                    false) &&
                (PathOps.GetPathType(name) != PathType.Relative))
            {
                scriptFlags |= ScriptFlags.ExactNameOnly;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified script flags based on the
        /// interpreter configuration and the specified lookup options.  This
        /// overload uses <see cref="PackageType.None" /> as the package type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to be adjusted.
        /// </param>
        /// <param name="getDataFile">
        /// Non-zero if a data file (instead of a script file) is being fetched.
        /// </param>
        /// <param name="noFileSystem">
        /// Non-zero to forbid the file system from being used.
        /// </param>
        /// <returns>
        /// The adjusted script flags.
        /// </returns>
        public static ScriptFlags GetFlags(
            Interpreter interpreter, /* in */
            ScriptFlags scriptFlags, /* in */
            bool getDataFile,        /* in */
            bool noFileSystem        /* in */
            )
        {
            return GetFlags(
                interpreter, scriptFlags, PackageType.None,
                getDataFile, noFileSystem);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified script flags based on the
        /// interpreter configuration, the package type, and the specified
        /// lookup options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to be adjusted.
        /// </param>
        /// <param name="packageType">
        /// The type of package being looked up.
        /// </param>
        /// <param name="getDataFile">
        /// Non-zero if a data file (instead of a script file) is being fetched.
        /// </param>
        /// <param name="noFileSystem">
        /// Non-zero to forbid the file system from being used.
        /// </param>
        /// <returns>
        /// The adjusted script flags.
        /// </returns>
        public static ScriptFlags GetFlags(
            Interpreter interpreter, /* in */
            ScriptFlags scriptFlags, /* in */
            PackageType packageType, /* in */
            bool getDataFile,        /* in */
            bool noFileSystem        /* in */
            )
        {
            if (packageType == PackageType.Bundle)
                scriptFlags &= ~ScriptFlags.Library;

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

        /// <summary>
        /// This method attempts to locate a script file with the specified name
        /// within the specified directory or, when permitted, within the
        /// directories listed in the auto-path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="directory">
        /// The primary directory in which to search for the script file.
        /// </param>
        /// <param name="name">
        /// The name of the script file being looked up.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult and update.  Upon success, the
        /// <see cref="ScriptFlags.File" /> flag is added.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this lookup.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the resolved file name.
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method attempts to locate a library script file with the
        /// specified name, optionally retrying with just the file name portion
        /// when the full name cannot be found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="directory">
        /// The directory in which to search for the library script file.
        /// </param>
        /// <param name="name">
        /// The name of the library script file being looked up.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult and update during the lookup.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this lookup.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the resolved file name.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered during the lookup, appended to as
        /// necessary.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
            ScriptFlags localScriptFlags = scriptFlags;
            Result localResult = null;

            if (GetFile(
                    interpreter, directory, name, ref localScriptFlags,
                    ref clientData, ref localResult) == ReturnCode.Ok)
            {
                scriptFlags = localScriptFlags;
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
                localScriptFlags = scriptFlags;
                localResult = null;

                if (PathOps.HasDirectory(name) && (GetFile(
                        interpreter, directory, PathOps.ScriptFileNameOnly(
                        name), ref localScriptFlags, ref clientData,
                        ref localResult) == ReturnCode.Ok))
                {
                    scriptFlags = localScriptFlags;
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

        /// <summary>
        /// This method attempts to locate a core script library file, searching
        /// the file system and the host as directed by the specified script
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="fileSystemHost">
        /// The file system host used to fetch the script, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the library script being looked up.
        /// </param>
        /// <param name="direct">
        /// Non-zero to request that the host fetch the script directly.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult and update during the lookup.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this lookup.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the located script or its
        /// resolved file name.  Upon failure, this parameter will contain the
        /// list of errors encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
            ScriptFlags localScriptFlags; /* REUSED */
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
                localScriptFlags = scriptFlags;
                localResult = null;

                if (GetLibraryFile(interpreter,
                        directory, name, ref localScriptFlags, ref clientData,
                        ref localResult, ref errors) == ReturnCode.Ok)
                {
                    scriptFlags = localScriptFlags;
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else
                {
                    localScriptFlags = scriptFlags;

                    MaybeExactNameOnly(
                        interpreter, name, true, ref localScriptFlags);

                    localResult = null;

                    if (HostOps.GetScript(
                            interpreter, fileSystemHost, name,
                            direct, ref localScriptFlags, ref clientData,
                            ref localResult) == ReturnCode.Ok)
                    {
                        scriptFlags = localScriptFlags;
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
                localScriptFlags = scriptFlags;

                MaybeExactNameOnly(
                    interpreter, name, true, ref localScriptFlags);

                localResult = null;

                if (HostOps.GetScript(
                        interpreter, fileSystemHost, name,
                        direct, ref localScriptFlags, ref clientData,
                        ref localResult) == ReturnCode.Ok)
                {
                    scriptFlags = localScriptFlags;
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

                    localScriptFlags = scriptFlags;
                    localResult = null;

                    if (GetLibraryFile(interpreter,
                            directory, name, ref localScriptFlags, ref clientData,
                            ref localResult, ref errors) == ReturnCode.Ok)
                    {
                        scriptFlags = localScriptFlags;
                        result = localResult;

                        return ReturnCode.Ok;
                    }
                }
            }

            result = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate a startup script, using the specified
        /// name and, if necessary, the value of a global interpreter variable
        /// with that name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="fileSystemHost">
        /// The file system host used to fetch the script, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the startup script being looked up.
        /// </param>
        /// <param name="direct">
        /// Non-zero to request that the host fetch the script directly.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult and update during the lookup.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this lookup.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will contain the located script or its
        /// resolved file name.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered during the lookup, appended to as
        /// necessary.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
            ScriptFlags localScriptFlags = scriptFlags;

            MaybeExactNameOnly(
                interpreter, name, null, ref localScriptFlags);

            Result localResult = null;

            if (HostOps.GetScript(
                    interpreter, fileSystemHost, name,
                    direct, ref localScriptFlags, ref clientData,
                    ref localResult) == ReturnCode.Ok)
            {
                scriptFlags = localScriptFlags;
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

                    localScriptFlags = scriptFlags;

                    MaybeExactNameOnly(
                        interpreter, localName, null, ref localScriptFlags);

                    localResult = null;

                    if (HostOps.GetScript(
                            interpreter, fileSystemHost, localName,
                            direct, ref localScriptFlags, ref clientData,
                            ref localResult) == ReturnCode.Ok)
                    {
                        scriptFlags = localScriptFlags;

                        if (FlagOps.HasFlags(
                                localScriptFlags, ScriptFlags.File, true) &&
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the engine flags that should be used when
        /// reading a script stream, based on the interpreter and the specified
        /// script flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags associated with the stream being read.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to consult when computing the engine flags.
        /// </param>
        /// <returns>
        /// The engine flags to use when reading the script stream.
        /// </returns>
        public static EngineFlags GetEngineFlagsForReadScriptStream(
            Interpreter interpreter, /* in */
            DataFlags dataFlags,     /* in: NOT USED */
            ScriptFlags scriptFlags  /* in */
            )
        {
            //
            // NOTE: Grab the engine flags as we need them for the calls
            //       into the engine.
            //
            EngineFlags engineFlags = EngineFlags.None;

            if (interpreter != null)
                engineFlags |= interpreter.EngineFlags;

#if XML
            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.NoXml, true))
                engineFlags |= EngineFlags.NoXml;
#endif

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.NoPolicy, true))
                engineFlags |= EngineFlags.NoPolicy;

            return engineFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        /// <summary>
        /// This method returns the default argument index at which a
        /// sub-command name is expected.
        /// </summary>
        /// <returns>
        /// The default sub-command name index.
        /// </returns>
        public static int GetSubCommandNameIndex()
        {
            return DefaultSubCommandNameIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new default sub-command that dispatches to the
        /// specified delegate.
        /// </summary>
        /// <param name="name">
        /// The name of the sub-command.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the sub-command.
        /// </param>
        /// <param name="command">
        /// The parent command that owns the sub-command.
        /// </param>
        /// <param name="subCommandFlags">
        /// The flags to associate with the sub-command.
        /// </param>
        /// <param name="delegate">
        /// The delegate to be invoked when the sub-command is executed.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the delegate is invoked.
        /// </param>
        /// <returns>
        /// The newly created sub-command.
        /// </returns>
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
                typeof(_SubCommands.Default),
                GetSubCommandNameIndex(),
                CommandFlags.None, subCommandFlags,
                command, 0), new DelegateData(@delegate,
                delegateFlags, 0)
            );
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new sub-command that dispatches to the
        /// specified script command.
        /// </summary>
        /// <param name="name">
        /// The name of the sub-command.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the sub-command.
        /// </param>
        /// <param name="command">
        /// The parent command that owns the sub-command.
        /// </param>
        /// <param name="scriptCommand">
        /// The script command to be evaluated when the sub-command is executed.
        /// </param>
        /// <param name="nameIndex">
        /// The argument index at which the sub-command name is expected.
        /// </param>
        /// <param name="subCommandFlags">
        /// The flags to associate with the sub-command.
        /// </param>
        /// <returns>
        /// The newly created sub-command.
        /// </returns>
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
                name, null, null, ClientData.WrapOrReplace(
                clientData, scriptCommand),
                typeof(_SubCommands.Command).FullName,
                typeof(_SubCommands.Command), nameIndex,
                CommandFlags.None, subCommandFlags, command, 0)
            );
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the qualified name to use when executing the
        /// specified sub-command, combining the parent command name with the
        /// sub-command name as appropriate.
        /// </summary>
        /// <param name="name">
        /// The explicit name to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command being executed.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The name to use when executing the sub-command, or null if no
        /// suitable name can be determined.
        /// </returns>
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

        /// <summary>
        /// This method builds the argument list to use when executing a
        /// sub-command, combining the script command with the cloned tail of
        /// the original argument list.
        /// </summary>
        /// <param name="execute">
        /// The executable entity that will receive the arguments.
        /// </param>
        /// <param name="scriptCommand">
        /// The script command words to prepend to the argument list, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="oldArguments">
        /// The original argument list whose tail is to be appended, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="oldStartIndex">
        /// The index within the original argument list at which to begin
        /// copying arguments.
        /// </param>
        /// <returns>
        /// The newly built argument list.
        /// </returns>
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
        /// <summary>
        /// This method produces a new argument list in which any argument that
        /// names an existing opaque object handle is replaced with the value of
        /// that object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve object handles.
        /// </param>
        /// <param name="oldArguments">
        /// The original argument list to be examined.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newArguments">
        /// Upon return, this parameter will contain the new argument list, or
        /// null if the original argument list was null.
        /// </param>
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

        /// <summary>
        /// This method determines the effective case-insensitivity setting to
        /// use for a sub-command lookup, preferring the explicitly specified
        /// value and otherwise consulting the ensemble.
        /// </summary>
        /// <param name="ensemble">
        /// The ensemble to consult when no explicit value is specified.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// The explicit case-insensitivity value to use, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero if sub-command name matching should be case-insensitive;
        /// otherwise, zero.
        /// </returns>
        private static bool GetNoCase(
            IEnsemble ensemble,
            bool? noCase
            )
        {
            if (noCase != null)
                return (bool)noCase;

            if (ensemble != null)
            {
                IHaveNoCase haveNoCase = ensemble as IHaveNoCase;

                if (haveNoCase != null)
                    return haveNoCase.NoCase;
            }

            return SubCommandNoCase;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble.  This overload uses the default policy filter callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="subCommand">
        /// Upon success, this parameter will contain the matched sub-command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,    /* in */
            IEnsemble ensemble,         /* in: OPTIONAL */
            string type,                /* in */
            bool strict,                /* in */
            bool? noCase,               /* in */
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

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble using the specified filter callback.  This overload
        /// discards any error message and matched sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to filter the matched sub-commands.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool? noCase,                      /* in */
            ref string name                    /* in, out */
            )
        {
            Result error = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, callback, type, strict,
                noCase, ref name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble using the specified filter callback.  This overload
        /// discards the matched sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to filter the matched sub-commands.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool? noCase,                      /* in */
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

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble using the specified filter callback, against the unsafe
        /// list of sub-commands obtained from the ensemble.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to filter the matched sub-commands.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="subCommand">
        /// Upon success, this parameter will contain the matched sub-command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool? noCase,                      /* in */
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

        /// <summary>
        /// This method resolves a sub-command name against the specified
        /// dictionary of sub-commands.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of sub-commands to search.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,        /* in */
            EnsembleDictionary subCommands, /* in */
            string type,                    /* in */
            bool strict,                    /* in */
            bool? noCase,                   /* in */
            ref string name,                /* in, out */
            ref Result error                /* out */
            )
        {
            return SubCommandFromEnsemble(
                interpreter, null, subCommands, null, type,
                strict, noCase, ref name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble and dictionary of sub-commands using the specified filter
        /// callback.  This overload discards the matched sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to which the sub-commands belong.  This parameter may
        /// be null.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of sub-commands to search.
        /// </param>
        /// <param name="callback">
        /// The callback used to filter the matched sub-commands.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in */
            string type,                       /* in */
            bool strict,                       /* in */
            bool? noCase,                      /* in */
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

        /// <summary>
        /// This method resolves a sub-command name within the specified
        /// ensemble and dictionary of sub-commands, optionally filtering the
        /// matches with the specified callback.  This is the core
        /// implementation to which the other overloads delegate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to which the sub-commands belong.  This parameter may
        /// be null.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of sub-commands to search.
        /// </param>
        /// <param name="callback">
        /// The optional callback used to filter the matched sub-commands.  This
        /// parameter may be null.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="subCommand">
        /// Upon success, this parameter will contain the matched sub-command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in: OPTIONAL */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in: OPTIONAL */
            string type,                       /* in */
            bool strict,                       /* in */
            bool? noCase,                      /* in */
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
            bool localNoCase = GetNoCase(ensemble, noCase);

            StringComparison comparisonType =
                SharedStringOps.GetSystemComparisonType(localNoCase);

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
                        exact = !localNoCase;

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

        /// <summary>
        /// This method executes the specified sub-command, enforcing a limit on
        /// the sub-command nesting level to avoid exhausting the native stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to execute the sub-command.
        /// </param>
        /// <param name="name">
        /// The name to use when executing the sub-command.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command to execute.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the sub-command.
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the sub-command.
        /// </param>
        /// <param name="tried">
        /// Upon return, this parameter is non-zero if the sub-command was
        /// actually dispatched.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will contain the result or error message
        /// produced by the sub-command.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method attempts to retrieve a previously cached sub-command from
        /// the second argument in the specified argument list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to query the cache.  This parameter may
        /// be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list whose second argument may carry the cached
        /// sub-command.  This parameter may be null.
        /// </param>
        /// <param name="viaArgument">
        /// Upon return, this parameter is non-zero if the interpreter permits
        /// caching sub-commands via the argument.
        /// </param>
        /// <param name="secondArgument">
        /// Upon return, this parameter will contain the second argument, if
        /// any, or null.
        /// </param>
        /// <param name="subCommand">
        /// Upon a successful lookup, this parameter will contain the cached
        /// sub-command.
        /// </param>
        /// <returns>
        /// True if a cached sub-command was retrieved; otherwise, false.
        /// </returns>
        private static bool MaybeGetISubCommandViaArgument(
            Interpreter interpreter,     /* in */
            ArgumentList arguments,      /* in */
            out bool viaArgument,        /* out */
            out Argument secondArgument, /* out */
            ref ISubCommand subCommand   /* in, out */
            )
        {
            viaArgument = (interpreter != null) ?
                interpreter.HasCacheViaArgument() : false;

            secondArgument = null;

            if (viaArgument &&
                (arguments != null) && (arguments.Count >= 2))
            {
                secondArgument = arguments[1];

                if (secondArgument != null)
                {
                    ISubCommand localSubCommand =
                        secondArgument.GetCacheValue(
                            interpreter, false) as ISubCommand;

                    if (localSubCommand != null)
                    {
                        subCommand = localSubCommand;
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to store the specified sub-command as a cached
        /// value on the specified argument.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to update the cache.
        /// </param>
        /// <param name="argument">
        /// The argument on which to cache the sub-command.  This parameter may
        /// be null.
        /// </param>
        /// <param name="viaArgument">
        /// Non-zero if caching sub-commands via the argument is permitted.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command to cache.
        /// </param>
        /// <returns>
        /// True if the sub-command was cached; otherwise, false.
        /// </returns>
        private static bool MaybeCacheISubCommandViaArgument(
            Interpreter interpreter, /* in */
            Argument argument,       /* in */
            bool viaArgument,        /* in */
            ISubCommand subCommand   /* in */
            )
        {
            if (viaArgument && (argument != null))
            {
                return argument.SetCacheValue(
                    interpreter, subCommand, false);
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves and executes a sub-command from the specified
        /// ensemble in a single step.  This overload does not expose the
        /// matched sub-command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the sub-command.
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the sub-command.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="tried">
        /// Upon return, this parameter is non-zero if the sub-command was
        /// actually dispatched.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will contain the result or error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter, /* in */
            IEnsemble ensemble,      /* in: OPTIONAL */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            bool strict,             /* in */
            bool? noCase,            /* in: OPTIONAL */
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

        /// <summary>
        /// This method resolves and executes a sub-command from the specified
        /// ensemble, using a cached sub-command from the arguments when
        /// available.  This is the core implementation to which the other
        /// overload delegates.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble to search.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the sub-command.
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the sub-command.
        /// </param>
        /// <param name="type">
        /// The descriptive type name used when building error messages.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require that a matching sub-command be found.
        /// </param>
        /// <param name="noCase">
        /// The optional case-insensitivity value to use.  This parameter may be
        /// null.
        /// </param>
        /// <param name="name">
        /// The sub-command name to resolve.  Upon a successful partial match,
        /// this parameter is updated to the matched sub-command name.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command to use, if already known; otherwise, upon success
        /// this parameter will contain the matched sub-command.
        /// </param>
        /// <param name="tried">
        /// Upon return, this parameter is non-zero if the sub-command was
        /// actually dispatched.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will contain the result or error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,    /* in */
            IEnsemble ensemble,         /* in: OPTIONAL */
            IClientData clientData,     /* in */
            ArgumentList arguments,     /* in */
            string type,                /* in */
            bool strict,                /* in */
            bool? noCase,               /* in: OPTIONAL */
            ref string name,            /* in, out */
            ref ISubCommand subCommand, /* in, out */
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
            // HACK: Cheat, attempt to read the cached sub-command
            //       from the Argument object itself.
            //
            bool viaArgument;
            Argument secondArgument;

            if (MaybeGetISubCommandViaArgument(
                    interpreter, arguments, out viaArgument,
                    out secondArgument, ref subCommand))
            {
                goto execute;
            }

            //
            // NOTE: Attempt to lookup the sub-command based on the
            //       name and the parent ensemble.
            //
            if (SubCommandFromEnsemble(interpreter,
                    ensemble, PolicyOps.OnlyAllowedSubCommands,
                    type, strict, noCase, ref name, ref subCommand,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Update the cached sub-command within the second
            //       argument, if needed.
            //
            /* IGNORED */
            MaybeCacheISubCommandViaArgument(
                interpreter, secondArgument, viaArgument, subCommand);

        execute:

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
        /// <summary>
        /// This method wraps an existing delegate in a new delegate triplet,
        /// capturing its target method and the specified delegate flags.
        /// </summary>
        /// <param name="innerDelegate">
        /// The delegate to be wrapped.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the delegate is treated.
        /// </param>
        /// <param name="outerDelegate">
        /// Upon success, this contains the newly created delegate triplet.
        /// </param>
        private static void NewOuterDelegate(
            Delegate innerDelegate,
            DelegateFlags delegateFlags,
            out DelegateTriplet outerDelegate
            )
        {
            outerDelegate = new DelegateTriplet(
                true, (innerDelegate != null) ?
                    innerDelegate.Method : null,
                innerDelegate, delegateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method executes or invokes a single delegate, building a
        /// temporary delegate list and forwarding to the list-based overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="delegate">
        /// The delegate to be executed or invoked.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to pass to the delegate.
        /// </param>
        /// <param name="allowOptions">
        /// Non-zero if leading options are permitted within the arguments.
        /// </param>
        /// <param name="nameCount">
        /// The number of leading arguments that represent the command name.
        /// </param>
        /// <param name="nameIndex">
        /// The index of the first argument that represents the command name.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the delegate is executed or invoked.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of executing or invoking the
        /// delegate; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ExecuteOrInvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            ArgumentList arguments,
            bool allowOptions,
            int nameCount,
            int nameIndex,
            DelegateFlags delegateFlags,
            ref Result result
            )
        {
            DelegateList delegates = new DelegateList();
            DelegateTriplet outerDelegate;

            NewOuterDelegate(
                @delegate, delegateFlags, out outerDelegate);

            delegates.Add(outerDelegate);

            Delegate localDelegate = null;

            return ExecuteOrInvokeDelegate(
                interpreter, delegates, arguments,
                allowOptions, nameCount, nameIndex,
                delegateFlags, ref localDelegate,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method executes or invokes one of the delegates in the specified
        /// list, using either the engine or the object subsystem depending on the
        /// delegate flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="delegates">
        /// The list of delegates to choose from when executing or invoking.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to pass to the delegate.
        /// </param>
        /// <param name="allowOptions">
        /// Non-zero if leading options are permitted within the arguments.
        /// </param>
        /// <param name="nameCount">
        /// The number of leading arguments that represent the command name.
        /// </param>
        /// <param name="nameIndex">
        /// The index of the first argument that represents the command name.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the delegate is executed or invoked.
        /// </param>
        /// <param name="delegate">
        /// Upon success, this contains the delegate that was actually executed or
        /// invoked.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of executing or invoking the
        /// delegate; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ExecuteOrInvokeDelegate(
            Interpreter interpreter,
            DelegateList delegates,
            ArgumentList arguments,
            bool allowOptions,
            int nameCount,
            int nameIndex,
            DelegateFlags delegateFlags,
            ref Delegate @delegate,
            ref Result result
            )
        {
            if (FlagOps.HasFlags(
                    delegateFlags, DelegateFlags.UseEngine, true))
            {
                if ((delegates == null) ||
                    (delegates.Count == 0) || (delegates[0] == null))
                {
                    result = "cannot execute delegate, bad delegates";
                    return ReturnCode.Error;
                }

                @delegate = delegates[0].Y;

                return Engine.ExecuteDelegate(
                    @delegate, arguments, ref result);
            }
            else
            {
                return ObjectOps.InvokeDelegate(
                    interpreter, delegates, arguments,
                    allowOptions, nameCount, nameIndex,
                    ref @delegate, ref result);
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method transforms the raw return value produced by a delegate
        /// into a result, honoring the delegate flags that control how the value
        /// is wrapped or converted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="delegate">
        /// The delegate that produced the return value.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags that control how the return value is wrapped or converted.
        /// </param>
        /// <param name="returnValue">
        /// The raw return value produced by the delegate.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the transformed result; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode HandleDelegateResult(
            Interpreter interpreter,     /* in */
            Delegate @delegate,          /* in */
            DelegateFlags delegateFlags, /* in */
            Result returnValue,          /* in */
            ref Result result            /* out */
            )
        {
            Type returnType = null;

            if (DelegateOps.NeedReturnType(@delegate, ref returnType))
            {
                if ((returnValue == null) || Result.IsSupported(returnType))
                {
                    result = returnValue;
                }
                else
                {
                    object innerReturnValue = returnValue.Value;

                    if (FlagOps.HasFlags(delegateFlags,
                            DelegateFlags.MakeIntoObject, true))
                    {
                        if (MarshalOps.FixupReturnValue(
                                interpreter, delegateFlags,
                                innerReturnValue, false,
                                false, false,
                                ref result) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }
                    else if (FlagOps.HasFlags(delegateFlags,
                            DelegateFlags.WrapReturnType, true))
                    {
                        result = Result.FromObject(
                            innerReturnValue, false, false, false);
                    }
                    else
                    {
                        result = StringOps.GetStringFromObject(
                            innerReturnValue);
                    }
                }
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Message Support Methods
        /// <summary>
        /// This method returns the base (root) exception associated with the
        /// specified exception, if any.
        /// </summary>
        /// <param name="exception">
        /// The exception to examine.
        /// </param>
        /// <returns>
        /// The base exception of <paramref name="exception" />, the exception
        /// itself when it has no base exception, or null when it is null.
        /// </returns>
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
        /// <summary>
        /// This method gets the inner exception of the specified exception, if
        /// any.
        /// </summary>
        /// <param name="exception">
        /// The exception whose inner exception is to be returned.
        /// </param>
        /// <returns>
        /// The inner exception, the original exception when it has no inner
        /// exception, or null when the specified exception is null.
        /// </returns>
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

        /// <summary>
        /// This method builds a standard error message describing a bad value,
        /// optionally listing the set of acceptable values.
        /// </summary>
        /// <param name="adjective">
        /// The adjective describing the problem with the value, or null to use a
        /// default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the value, or null to use a
        /// default.
        /// </param>
        /// <param name="value">
        /// The offending value.
        /// </param>
        /// <param name="values">
        /// The set of acceptable values, if any.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable value, if any.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable values, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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
                        values, ", ", Characters.SpaceString,
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

        /// <summary>
        /// This method builds a standard error message describing a bad value,
        /// optionally listing the set of acceptable values.
        /// </summary>
        /// <param name="adjective">
        /// The adjective describing the problem with the value, or null to use a
        /// default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the value, or null to use a
        /// default.
        /// </param>
        /// <param name="value">
        /// The offending value.
        /// </param>
        /// <param name="values">
        /// The set of acceptable values, if any.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable value, if any.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable values, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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

        /// <summary>
        /// This method builds a standard error message describing a bad
        /// sub-command, listing the sub-commands supported by the specified
        /// ensemble.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="adjective">
        /// The adjective describing the problem with the sub-command, or null to
        /// use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the sub-command, or null to use a
        /// default.
        /// </param>
        /// <param name="subCommand">
        /// The offending sub-command name.
        /// </param>
        /// <param name="ensemble">
        /// The ensemble whose sub-commands are acceptable.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable sub-command, if any.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable sub-commands, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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

        /// <summary>
        /// This method builds a standard error message describing a bad
        /// sub-command, listing the supported sub-commands and distinguishing an
        /// unsupported sub-command from an entirely unknown one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="adjective">
        /// The adjective describing the problem with the sub-command, or null to
        /// use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the sub-command, or null to use a
        /// default.
        /// </param>
        /// <param name="subCommand">
        /// The offending sub-command name.
        /// </param>
        /// <param name="subCommands">
        /// The dictionary of acceptable sub-commands.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable sub-command, if any.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable sub-commands, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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

        /// <summary>
        /// This method builds a standard error message describing a bad
        /// sub-command, listing the acceptable sub-commands drawn from the
        /// specified collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="adjective">
        /// The adjective describing the problem with the sub-command, or null to
        /// use a default.
        /// </param>
        /// <param name="type">
        /// The name of the type or category of the sub-command, or null to use a
        /// default.
        /// </param>
        /// <param name="subCommand">
        /// The offending sub-command name.
        /// </param>
        /// <param name="subCommands">
        /// The collection of acceptable sub-commands.
        /// </param>
        /// <param name="prefix">
        /// The text to place before each acceptable sub-command, if any.
        /// </param>
        /// <param name="suffix">
        /// The text to place after the list of acceptable sub-commands, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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

        /// <summary>
        /// This method builds a standard "wrong # args" error message based on
        /// the specified arguments.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier associated with the command, if any.
        /// </param>
        /// <param name="count">
        /// The number of leading arguments to include in the message.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments used to construct the message.
        /// </param>
        /// <param name="suffix">
        /// The text to append to the argument summary, if any.
        /// </param>
        /// <returns>
        /// The constructed error message.
        /// </returns>
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
                    suffix) ? Characters.SpaceString : null, suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return "wrong # args";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Support Methods
        /// <summary>
        /// This method extracts and converts the value associated with an option
        /// from the specified list, advancing the current index as needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="list">
        /// The list of strings from which to extract the option value.
        /// </param>
        /// <param name="type">
        /// The type that the option value should be converted to, if any.
        /// </param>
        /// <param name="optionFlags">
        /// The flags that control how the option value is processed.
        /// </param>
        /// <param name="force">
        /// Non-zero to require that a value be present even when the option flags
        /// do not otherwise require one.
        /// </param>
        /// <param name="allowInteger">
        /// Non-zero to allow an enumerated value to be specified as an integer.
        /// </param>
        /// <param name="strict">
        /// Non-zero to enable strict parsing of enumerated values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case when parsing enumerated values.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for parsing, if any.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the extracted option value.
        /// </param>
        /// <param name="nextIndex">
        /// Upon success, this contains the index of the next unprocessed element
        /// in the list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
            ref IVariant value,
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
        /// <summary>
        /// This method writes the specified value to a channel by locating and
        /// executing the appropriate command via the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to use for writing, or null to use the
        /// default.
        /// </param>
        /// <param name="channelId">
        /// The identifier of the channel to write to, or null to use the standard
        /// output channel.
        /// </param>
        /// <param name="value">
        /// The value to be written.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the write operation; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
                commandName = TypeNameToEntityName(typeof(_Commands.Puts));

            if (channelId == null)
                channelId = StandardChannel.Output;

            ReturnCode code;
            IExecute execute = null;

            code = interpreter.InternalGetIExecuteViaResolvers(
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
        /// <summary>
        /// This method verifies that a variable satisfies the constraints
        /// requested via the supplied variable flags (e.g. defined versus
        /// undefined, array versus scalar, virtual, and link index status).
        /// </summary>
        /// <param name="varName">
        /// The name of the variable being checked, used when formatting any
        /// error message.
        /// </param>
        /// <param name="localVariable">
        /// The variable to check, or null if no variable was found.
        /// </param>
        /// <param name="variableFlags">
        /// The flags describing the constraints to enforce.  Upon return, this
        /// may be augmented with status flags reflecting why the check failed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// variable did not satisfy the requested constraints.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the variable satisfies all of the
        /// requested constraints; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method maps a pair of array-iteration flags to the appropriate
        /// breakpoint type for variable array tracing.
        /// </summary>
        /// <param name="names">
        /// Non-zero if array element names are being requested.
        /// </param>
        /// <param name="values">
        /// Non-zero if array element values are being requested.
        /// </param>
        /// <returns>
        /// The breakpoint type that corresponds to the supplied flags.
        /// </returns>
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

        /// <summary>
        /// This method returns the default value associated with the specified
        /// variable breakpoint type.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type whose default value is being requested.
        /// </param>
        /// <returns>
        /// The default value that corresponds to the specified breakpoint
        /// type.
        /// </returns>
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

        /// <summary>
        /// This method returns the supplied default value if it is non-null;
        /// otherwise, it returns the default value associated with the
        /// specified variable breakpoint type.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type whose default value is being requested when no
        /// explicit default is supplied.
        /// </param>
        /// <param name="default">
        /// The explicit default value to use, or null to use the default value
        /// associated with the breakpoint type.
        /// </param>
        /// <returns>
        /// The supplied default value if it is non-null; otherwise, the
        /// default value associated with the specified breakpoint type.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified breakpoint type
        /// corresponds to a variable operation that writes a value.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type to examine.
        /// </param>
        /// <returns>
        /// True if the breakpoint type represents a value-writing variable
        /// operation; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a value (old or new) is required for
        /// the specified variable breakpoint type.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type to examine.
        /// </param>
        /// <param name="old">
        /// Non-zero to test whether the old value is required; zero to test
        /// whether the new value is required.
        /// </param>
        /// <returns>
        /// True if the requested value is required for the specified breakpoint
        /// type; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gathers the non-null variable name, index, value, and
        /// array values into a flat list of strings suitable for use with a
        /// trace callback.
        /// </summary>
        /// <param name="varName">
        /// The variable name to include, or null to omit it.
        /// </param>
        /// <param name="varIndex">
        /// The array element index to include, or null to omit it.
        /// </param>
        /// <param name="value">
        /// The scalar variable value to include, or null to omit it.
        /// </param>
        /// <param name="arrayValue">
        /// The array values (keys and element values) to include, or null to
        /// omit them.
        /// </param>
        /// <param name="values">
        /// Upon return, receives the gathered values, with a new list being
        /// created if necessary.
        /// </param>
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

        /// <summary>
        /// This method releases a reference from each old opaque object wrapper
        /// associated with a variable trace, removing (and possibly disposing)
        /// any object that has no remaining references.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter from which opaque object handles should be removed,
        /// or null if opaque object handles cannot be removed.
        /// </param>
        /// <param name="oldObjects">
        /// The list of old object wrappers to process, or null if there are no
        /// old objects.
        /// </param>
        /// <param name="code">
        /// Upon failure, set to <see cref="ReturnCode.Error" /> if any object
        /// could not be removed.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives the errors that occurred while removing any
        /// of the opaque object handles, with a new list being created if
        /// necessary.
        /// </param>
        public static void ProcessOldObjectsForTrace(
            Interpreter interpreter,         /* in */
            IList<ObjectWrapper> oldObjects, /* in */
            ref ReturnCode code,             /* out */
            ref ResultList errors            /* out */
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
            foreach (ObjectWrapper oldWrapper in oldObjects)
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
                //       underlying object, dispose and remove it now.
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

                removeCode = interpreter.InternalRemoveObject(
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

        /// <summary>
        /// This method adds a reference to each new opaque object wrapper
        /// associated with a variable trace, skipping any wrapper that is
        /// flagged as locked.
        /// </summary>
        /// <param name="newObjects">
        /// The list of new object wrappers to process, or null if there are no
        /// new objects.
        /// </param>
        public static void ProcessNewObjectsForTrace(
            IList<ObjectWrapper> newObjects /* in */
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
            foreach (ObjectWrapper newWrapper in newObjects)
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

        /// <summary>
        /// This method adds the specified old array values into the old values
        /// collection of a trace information object, creating that collection
        /// if necessary.
        /// </summary>
        /// <param name="variableEvent">
        /// The event used to synchronize access to the array values collection
        /// when a new collection must be created.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information object whose old values collection should be
        /// augmented, or null to do nothing.
        /// </param>
        /// <param name="oldValues">
        /// The old array values to add, or null to do nothing.
        /// </param>
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
        /// <summary>
        /// This method determines whether the first trace list contains all of
        /// the trace callbacks present in the second trace list.  The caller
        /// should not pass null for either parameter, as the results are
        /// undefined in that case.
        /// </summary>
        /// <param name="traces1">
        /// The trace list that is checked for containing all of the callbacks
        /// in <paramref name="traces2" />.
        /// </param>
        /// <param name="traces2">
        /// The trace list whose callbacks must all be present in
        /// <paramref name="traces1" />.
        /// </param>
        /// <returns>
        /// True if the first trace list contains all of the trace callbacks
        /// present in the second trace list; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method creates a new core trace object that wraps the specified
        /// trace callback delegate.
        /// </summary>
        /// <param name="callback">
        /// The trace callback delegate to wrap.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new trace, or null if there is
        /// none.
        /// </param>
        /// <param name="traceFlags">
        /// The trace flags to associate with the new trace.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the new trace, or null if there is
        /// none.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the trace
        /// could not be created.
        /// </param>
        /// <returns>
        /// The newly created trace object upon success; otherwise, null.
        /// </returns>
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
                            FormatOps.TraceDelegateName(callback), null, null,
                            clientData, type.FullName, type, methodInfo.Name,
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

        /// <summary>
        /// This method returns a trace information object describing a pending
        /// variable operation.  When possible, the per-thread trace information
        /// object cached on the interpreter is updated and reused in order to
        /// avoid creating redundant objects on the heap.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose cached trace information object may be reused,
        /// or null to always create a new object.
        /// </param>
        /// <param name="trace">
        /// The trace being fired, or null if there is none.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type that describes the variable operation.
        /// </param>
        /// <param name="frame">
        /// The call frame associated with the variable, or null if there is
        /// none.
        /// </param>
        /// <param name="variable">
        /// The variable associated with the operation, or null if there is
        /// none.
        /// </param>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="index">
        /// The array element index of the variable, or null if there is none.
        /// </param>
        /// <param name="flags">
        /// The variable flags associated with the operation.
        /// </param>
        /// <param name="oldValue">
        /// The old value of the variable, or null if there is none.
        /// </param>
        /// <param name="newValue">
        /// The new value of the variable, or null if there is none.
        /// </param>
        /// <param name="oldValues">
        /// The old array values of the variable, or null if there are none.
        /// </param>
        /// <param name="newValues">
        /// The new array values of the variable, or null if there are none.
        /// </param>
        /// <param name="list">
        /// The list of gathered trace values, or null if there is none.
        /// </param>
        /// <param name="force">
        /// Non-zero to always create a new trace information object instead of
        /// reusing the one cached on the interpreter.
        /// </param>
        /// <param name="cancel">
        /// Non-zero if the variable operation has been canceled.
        /// </param>
        /// <param name="postProcess">
        /// Non-zero if the variable operation requires post-processing.
        /// </param>
        /// <param name="returnCode">
        /// The return code associated with the variable operation.
        /// </param>
        /// <returns>
        /// A trace information object describing the variable operation.
        /// </returns>
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
        /// <summary>
        /// This method fires the traces for a variable operation that cannot
        /// return a value (e.g. "set", "unset", "reset", or "add").  Do not
        /// call this overload from "get" operation traces.
        /// </summary>
        /// <param name="variable">
        /// The variable whose traces should be fired.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type that describes the variable operation.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the variable operation, or null if
        /// there is none.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information object describing the variable operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// traces could not be fired successfully.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> upon success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method fires the traces for a variable operation, optionally
        /// returning a value produced by a trace callback that canceled a "get"
        /// operation.
        /// </summary>
        /// <param name="variable">
        /// The variable whose traces should be fired.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type that describes the variable operation.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the variable operation, or null if
        /// there is none.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information object describing the variable operation.
        /// </param>
        /// <param name="value">
        /// Upon success, may receive the value produced by a trace callback
        /// that canceled an otherwise successful "get" operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the
        /// traces could not be fired successfully.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> upon success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method retrieves the next item value from an enumerable variable,
        /// advancing its underlying enumerator.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being performed, used when formatting any error
        /// message.
        /// </param>
        /// <param name="variable">
        /// The variable associated with the enumerable value, used when formatting
        /// any error message.
        /// </param>
        /// <param name="name">
        /// The name of the variable, used when formatting any error message.
        /// </param>
        /// <param name="index">
        /// The array element index, if any, used when formatting any error message.
        /// </param>
        /// <param name="value">
        /// The underlying value of the variable, which must be an enumerable
        /// triplet.
        /// </param>
        /// <param name="itemValue">
        /// Upon success, receives the value of the next item in the enumeration.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method retrieves the member value for a linked variable, tracing
        /// any error that is encountered.
        /// </summary>
        /// <param name="value">
        /// The underlying value of the linked variable, which must be a member and
        /// object pair.
        /// </param>
        /// <param name="memberValue">
        /// Upon success, receives the value of the linked member.
        /// </param>
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

        /// <summary>
        /// This method retrieves the member information, declaring type, target
        /// object, and value for a linked variable.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being performed, used when formatting any error
        /// message.
        /// </param>
        /// <param name="variable">
        /// The variable associated with the linked value, used when formatting any
        /// error message.
        /// </param>
        /// <param name="name">
        /// The name of the variable, used when formatting any error message.
        /// </param>
        /// <param name="index">
        /// The array element index, if any, used when formatting any error message.
        /// </param>
        /// <param name="value">
        /// The underlying value of the variable, which must be a member and object
        /// pair.
        /// </param>
        /// <param name="memberInfo">
        /// Upon success, receives the member information for the linked member.
        /// </param>
        /// <param name="type">
        /// Upon success, receives the type of the linked member.
        /// </param>
        /// <param name="object">
        /// Upon success, receives the object instance that contains the linked
        /// member.
        /// </param>
        /// <param name="memberValue">
        /// Upon success, receives the value of the linked member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the member values for all elements of a linked
        /// array variable.
        /// </summary>
        /// <param name="variableEvent">
        /// The event used to synchronize access to the resulting element
        /// collection.
        /// </param>
        /// <param name="arrayValue">
        /// The array of linked elements whose member values are to be retrieved.
        /// </param>
        /// <param name="values">
        /// Upon success, receives the resolved member values for the array
        /// elements, created if necessary.
        /// </param>
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

        /// <summary>
        /// This method sets the member value for a linked variable.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being performed, used when formatting any error
        /// message.
        /// </param>
        /// <param name="variable">
        /// The variable associated with the linked value, used when formatting any
        /// error message.
        /// </param>
        /// <param name="name">
        /// The name of the variable, used when formatting any error message.
        /// </param>
        /// <param name="index">
        /// The array element index, if any, used when formatting any error message.
        /// </param>
        /// <param name="memberInfo">
        /// The member information for the linked member to be set.
        /// </param>
        /// <param name="object">
        /// The object instance that contains the linked member.
        /// </param>
        /// <param name="memberValue">
        /// The new value to assign to the linked member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method conditionally adds the user-interface flag to the specified
        /// event wait flags when running on a single-threaded apartment thread.
        /// </summary>
        /// <param name="eventWaitFlags">
        /// The event wait flags to examine and possibly modify.
        /// </param>
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
        /// <summary>
        /// This method splits a variable name into its base name and array element
        /// index components.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="flags">
        /// The variable flags that control how the name is parsed.
        /// </param>
        /// <param name="name">
        /// The variable name to be split.
        /// </param>
        /// <param name="varName">
        /// Upon success, receives the base name of the variable.
        /// </param>
        /// <param name="varIndex">
        /// Upon success, receives the array element index of the variable, or null
        /// when the name does not refer to an array element.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        #region Variable Value Support Methods
        /// <summary>
        /// This method resolves the effective value of a variable, following any
        /// variable links and unwrapping any value-bearing wrapper.
        /// </summary>
        /// <param name="default">
        /// The value to use when the underlying variable value is null.
        /// </param>
        /// <param name="variableFlags">
        /// The variable flags that control how links are followed; may be updated
        /// by this method.
        /// </param>
        /// <param name="variable">
        /// The variable whose value is to be resolved; may be updated to the final
        /// linked variable.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the resolved value of the variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ResolveVariableValue(
            object @default,                 /* in */
            ref VariableFlags variableFlags, /* in, out */
            ref IVariable variable,          /* in, out */
            ref object value,                /* out */
            ref Result error                 /* out */
            )
        {
            //
            // NOTE: Follow variable links (i.e. via [variable] command,
            //       etc).
            //
            variable = EntityOps.FollowLinks(
                variable, variableFlags, ref error);

            if (variable == null)
                return ReturnCode.Error;

            //
            // NOTE: Grab the underlying value of the script variable.
            //
            object localValue = variable.Value;

        retry:

            //
            // NOTE: Check if underlying variable value is explicitly null.
            //       We cannot do much else with a null value; however, it
            //       is technically legal.
            //
            if (localValue == null)
            {
                value = @default;
                return ReturnCode.Ok;
            }

            //
            // NOTE: If the underlying variable value has a "simple" type,
            //       just return it verbatim.
            //
            if ((localValue is string) || (localValue is ValueType))
            {
                value = localValue;
                return ReturnCode.Ok;
            }

            //
            // NOTE: If the underlying variable value refers to an IGetValue
            //       instance, grab the wrapped value and try our type checks
            //       again.
            //
            if (localValue is IGetValue)
            {
                localValue = ((IGetValue)localValue).Value;
                goto retry;
            }

            //
            // NOTE: At this point, just return the current value (which may
            //       have been an IGetValue wrapped value) and return it.
            //
            value = localValue;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Frame Support Methods
        /// <summary>
        /// This method creates a variable link (i.e. an upvar) from a variable in
        /// one call frame to a variable in another call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="localFrame">
        /// The call frame that will contain the local (linking) variable.
        /// </param>
        /// <param name="localName">
        /// The name of the local (linking) variable.
        /// </param>
        /// <param name="otherFrame">
        /// The call frame that contains the other (target) variable.
        /// </param>
        /// <param name="otherName">
        /// The name of the other (target) variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes the failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
                bool useNamespaces = interpreter.InternalAreNamespacesEnabled();

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
                // NOTE: *NAMESPACES* Need to make sure the correct frame
                //       is being used if the other frame is marked to use
                //       the associated namespace.
                //
                // BUGFIX: This was broken for namespace prefixed "other"
                //         variable names (see test "namespace-99.2025").
                //
                if (useNamespaces)
                {
                    INamespace otherNamespace;

                    if (NamespaceOps.IsQualifiedName(otherVarName))
                    {
                        otherNamespace = NamespaceOps.LookupParent(
                            interpreter, otherVarName, false, false,
                            false, ref error);

                        if (otherNamespace == null)
                            return ReturnCode.Error;

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
                    else
                    {
                        otherFrame = CallFrameOps.FollowNext(otherFrame);

                        if (CallFrameOps.IsUseNamespace(otherFrame))
                        {
                            otherNamespace = NamespaceOps.GetCurrent(
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
                        localFrame, localVarName /* FULL NAME */,
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
                    // NOTE: If local variable has been flagged as undefined
                    //       then go ahead and allow them to use it (it was
                    //       not purged?).
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
                        otherFrame, otherVarName /* FULL NAME */,
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
                    string targetVarIndex = otherVarIndex;

                    if (EntityOps.IsLink(targetVariable))
                    {
                        targetVariable = EntityOps.FollowLinks(
                            otherVariable, VariableFlags.None,
                            0, ref targetVarIndex, ref error);

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
                    // BUGFIX: The final target for the link must be used to
                    //         create the link, e.g. since the link will not
                    //         be correctly used by [info exists], et al.
                    //
                    otherVariable = targetVariable;
                    otherVarIndex = targetVarIndex;

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

                        interpreter.MaybeSetQualifiedName(otherVariable);

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
        /// <summary>
        /// This method determines whether the specified variable name is
        /// permitted within a "safe" interpreter.
        /// </summary>
        /// <param name="name">
        /// The variable name to check.
        /// </param>
        /// <returns>
        /// True if the specified variable name is safe; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified Tcl platform array
        /// element name is permitted within a "safe" interpreter.
        /// </summary>
        /// <param name="name">
        /// The array element name to check.
        /// </param>
        /// <returns>
        /// True if the specified element name is safe; otherwise, false.
        /// </returns>
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
                        TclVars.Platform.AlternateDirectorySeparator,
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

        /// <summary>
        /// This method determines whether the specified Eagle platform array
        /// element name is permitted within a "safe" interpreter.
        /// </summary>
        /// <param name="name">
        /// The array element name to check.
        /// </param>
        /// <returns>
        /// True if the specified element name is safe; otherwise, false.
        /// </returns>
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
                        Vars.Platform.RuntimeName,
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
        /// <summary>
        /// This method converts the name of the specified type into its
        /// corresponding core entity name, using case-insensitive handling.
        /// </summary>
        /// <param name="type">
        /// The type whose name should be converted.
        /// </param>
        /// <returns>
        /// The core entity name, or null if the specified type is null.
        /// </returns>
        public static string TypeNameToEntityName(
            Type type
            )
        {
            return TypeNameToEntityName(type, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the name of the specified type into its
        /// corresponding core entity name.
        /// </summary>
        /// <param name="type">
        /// The type whose name should be converted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to convert the entire name to lowercase; otherwise, only
        /// the first letter is made lowercase.
        /// </param>
        /// <returns>
        /// The core entity name, or null if the specified type is null.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified member name into its
        /// corresponding core entity name.
        /// </summary>
        /// <param name="name">
        /// The member name to convert.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to convert the entire name to lowercase; otherwise, only
        /// the first letter is made lowercase.
        /// </param>
        /// <returns>
        /// The core entity name, or null if the specified name is null.
        /// </returns>
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
                    result = StringOps.ToLowerInitial(result, null, true);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute/IVariable Naming Support Methods
        /// <summary>
        /// This method produces a command prefix from the specified name by
        /// trimming any leading namespace qualifiers.
        /// </summary>
        /// <param name="name">
        /// The name to convert into a command prefix.
        /// </param>
        /// <returns>
        /// The resulting command prefix.
        /// </returns>
        public static string MakeCommandPrefix(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a command name from the specified name by
        /// trimming any leading namespace qualifiers.
        /// </summary>
        /// <param name="name">
        /// The name to convert into a command name.
        /// </param>
        /// <returns>
        /// The resulting command name.
        /// </returns>
        public static string MakeCommandName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a command pattern from the specified name by
        /// trimming all namespace qualifiers.
        /// </summary>
        /// <param name="name">
        /// The name to convert into a command pattern.
        /// </param>
        /// <returns>
        /// The resulting command pattern.
        /// </returns>
        public static string MakeCommandPattern(
            string name
            )
        {
            return NamespaceOps.TrimAll(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a variable name from the specified name by
        /// trimming any leading namespace qualifiers.
        /// </summary>
        /// <param name="name">
        /// The name to convert into a variable name.
        /// </param>
        /// <returns>
        /// The resulting variable name.
        /// </returns>
        public static string MakeVariableName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Script Class Support Methods
        /// <summary>
        /// This method attempts to extract an embedded script identifier from
        /// the specified text.
        /// </summary>
        /// <param name="text">
        /// The text to search for an embedded identifier.
        /// </param>
        /// <param name="id">
        /// Upon success, this contains the extracted identifier.
        /// </param>
        /// <returns>
        /// True if an identifier was successfully extracted; otherwise, false.
        /// </returns>
        public static bool ExtractId(
            string text, /* in */
            ref Guid id  /* in, out */
            )
        {
            if (String.IsNullOrEmpty(text))
                return false;

            Regex regEx = EmbeddedIdRegEx;

            if (regEx == null)
                return false;

            string value = RegExOps.GetMatchValue(
                regEx.Match(text), 1);

            if (String.IsNullOrEmpty(value))
                return false;

            if (Value.GetGuid(
                    value, null, ref id) != ReturnCode.Ok)
            {
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Command Support Methods
        /// <summary>
        /// This method creates a new stub command, which serves as a
        /// placeholder for a command that has not yet been fully created.
        /// </summary>
        /// <param name="name">
        /// The name for the new command, or null to use the default name.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new command, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the new command, if any.
        /// </param>
        /// <param name="useSubCommands">
        /// Non-zero to enable sub-command support for the new command.
        /// </param>
        /// <returns>
        /// The newly created command.
        /// </returns>
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
                localName = TypeNameToEntityName(type);

            ICommand command = new _Commands.Stub(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            command.SubCommands = useSubCommands ?
                new EnsembleDictionary() : null;

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new ensemble command, which dispatches to a
        /// collection of named sub-commands.
        /// </summary>
        /// <param name="name">
        /// The name for the new command, or null to use the default name.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new command, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the new command, if any.
        /// </param>
        /// <returns>
        /// The newly created command.
        /// </returns>
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
                localName = TypeNameToEntityName(type);

            ICommand command = new _Commands.Ensemble(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method creates a new sub-delegate command, which dispatches to
        /// a collection of delegate-backed sub-commands.
        /// </summary>
        /// <param name="name">
        /// The name for the new command, or null to use the default name.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new command, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the new command, if any.
        /// </param>
        /// <returns>
        /// The newly created command.
        /// </returns>
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
                localName = TypeNameToEntityName(type);

            ICommand command = new _Commands.SubDelegate(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0));

            return command;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new automatic command, which exposes the
        /// members of an object instance as delegate-backed sub-commands.
        /// </summary>
        /// <param name="name">
        /// The name for the new command, or null to use the default name.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new command, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the new command, if any.
        /// </param>
        /// <param name="typedInstance">
        /// The typed object instance whose members are to be exposed.
        /// </param>
        /// <param name="mapper">
        /// The delegate mapper used to map members to sub-commands.
        /// </param>
        /// <param name="safe">
        /// Non-zero to create a command that is safe for use within a "safe"
        /// interpreter, zero to create an unsafe command, or null to use the
        /// default behavior.
        /// </param>
        /// <returns>
        /// The newly created command.
        /// </returns>
        public static ICommand NewAutomaticCommand(
            string name,
            IClientData clientData,
            IPlugin plugin,
            TypedInstance typedInstance,
            IDelegateMapper mapper,
            bool? safe
            )
        {
            Type type = typeof(_Commands.Automatic);
            CommandFlags flags = AttributeOps.GetCommandFlags(type);

            string localName = name;

            if (localName == null)
                localName = AttributeOps.GetObjectName(type);

            if (localName == null)
                localName = TypeNameToEntityName(type);

            ICommand command = new _Commands.Automatic(new CommandData(
                localName, null, null, clientData, (type != null) ?
                type.FullName : null, flags, plugin, 0), typedInstance,
                mapper, Defaults.DelegateFlags, safe);

            return command;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new command using the command creation
        /// callback configured on the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="name">
        /// The name for the new command, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new command, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the new command, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created command, or null if it could not be created.
        /// </returns>
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

        /// <summary>
        /// This method removes duplicate entries from the specified collection
        /// of typed instances, treating two entries as duplicates when they
        /// refer to the same underlying object or type.
        /// </summary>
        /// <param name="typedInstances">
        /// The collection of typed instances to filter.
        /// </param>
        /// <returns>
        /// A new collection containing the unique typed instances, or null if
        /// the specified collection is null.
        /// </returns>
        public static IEnumerable<TypedInstance> GetUnique(
            IEnumerable<TypedInstance> typedInstances /* in */
            )
        {
            if (typedInstances == null)
                return null;

            Dictionary<object, TypedInstance> dictionary =
                new Dictionary<object, TypedInstance>();

            foreach (TypedInstance typedInstance in typedInstances)
            {
                if (typedInstance == null)
                    continue;

                object @object = typedInstance.Object;

                if (@object == null)
                    @object = typedInstance.Type;

                if (@object == null)
                    continue;

                dictionary[@object] = typedInstance;
            }

            return new List<TypedInstance>(dictionary.Values);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the core looping logic shared by the
        /// [foreach] command and its collecting variants, iterating over one
        /// or more value lists and evaluating a body script for each set of
        /// loop variable assignments.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier naming the command being implemented, used when
        /// formatting error messages; null to use the default name.
        /// </param>
        /// <param name="collect">
        /// Non-zero to collect the result of each iteration into a list that
        /// becomes the overall result.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments to the command.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the collected list or an empty result;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

            int iterationLimit = interpreter.InternalIterationLimit;
            int iterationCount = 0;

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
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} loop variable \"{2}\")",
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

                    if (interpreter.ExitNoThrow)
                        goto done;
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
                        /* IGNORED */
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

                if ((iterationLimit != Limits.Unlimited) &&
                    (++iterationCount > iterationLimit))
                {
                    localResult = String.Format(
                        "iteration limit {0} exceeded",
                        iterationLimit);

                    result = localResult;
                    code = ReturnCode.Error;

                    break;
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

        /// <summary>
        /// This method implements the core looping logic shared by the
        /// [array foreach] sub-command and its collecting variants, iterating
        /// over the element names of one or more arrays and evaluating a body
        /// script for each set of loop variable assignments.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier naming the command being implemented, used when
        /// formatting error messages; null to use the default name.
        /// </param>
        /// <param name="subCommandName">
        /// The name of the sub-command being implemented, used when formatting
        /// error messages; null to use the default name.
        /// </param>
        /// <param name="collect">
        /// Non-zero to collect the result of each iteration into a list that
        /// becomes the overall result.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments to the command.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the collected list or an empty result;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

            int iterationLimit = interpreter.InternalIterationLimit;
            int iterationCount = 0;

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
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} {2} loop variable \"{3}\")",
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
                        /* IGNORED */
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

                if ((iterationLimit != Limits.Unlimited) &&
                    (++iterationCount > iterationLimit))
                {
                    localResult = String.Format(
                        "iteration limit {0} exceeded",
                        iterationLimit);

                    result = localResult;
                    code = ReturnCode.Error;

                    break;
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

        /// <summary>
        /// This method implements the core looping logic shared by the
        /// [array for] sub-command and its collecting variants, iterating over
        /// the element names and values of an array and evaluating a body
        /// script for each one.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier naming the command being implemented, used when
        /// formatting error messages; null to use the default name.
        /// </param>
        /// <param name="subCommandName">
        /// The name of the sub-command being implemented, used when formatting
        /// error messages; null to use the default name.
        /// </param>
        /// <param name="collect">
        /// Non-zero to collect the result of each iteration into a list that
        /// becomes the overall result.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments to the command.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the collected list or an empty result;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

            int iterationLimit = interpreter.InternalIterationLimit;
            int iterationCount = 0;

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
                    /* IGNORED */
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (getting {1} {2} loop variable \"{3}\")",
                            Environment.NewLine, commandName, subCommandName,
                            FormatOps.VariableName(varName, varIndex)));

                    goto done;
                }

                code = interpreter.SetVariableValue(
                    VariableFlags.None, variableList[0], varIndex, null,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    /* IGNORED */
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (setting {1} {2} loop name variable \"{3}\")",
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
                        /* IGNORED */
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format(
                                "{0}    (setting {1} {2} loop value variable \"{3}\")",
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
                        /* IGNORED */
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

                if ((iterationLimit != Limits.Unlimited) &&
                    (++iterationCount > iterationLimit))
                {
                    localResult = String.Format(
                        "iteration limit {0} exceeded",
                        iterationLimit);

                    result = localResult;
                    code = ReturnCode.Error;

                    break;
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
        /// <summary>
        /// This method determines whether the specified interpreter flags are
        /// present on the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The interpreter flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if all of the specified flags must be present; otherwise,
        /// the presence of any of the specified flags is sufficient.
        /// </param>
        /// <returns>
        /// True if the specified flags are present; otherwise, false.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter test flags
        /// are present on the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The interpreter test flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if all of the specified flags must be present; otherwise,
        /// the presence of any of the specified flags is sufficient.
        /// </param>
        /// <returns>
        /// True if the specified flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            Interpreter interpreter,
            InterpreterTestFlags hasFlags,
            bool all
            )
        {
            if (interpreter == null)
                return false;

            return FlagOps.HasFlags( /* EXEMPT */
                interpreter.InterpreterTestFlags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Evaluation Support Methods
        /// <summary>
        /// This method extracts the individual boolean trust settings encoded
        /// within the specified trust flags.
        /// </summary>
        /// <param name="trustFlags">
        /// The trust flags to extract the individual settings from.
        /// </param>
        /// <param name="exclusive">
        /// Upon return, non-zero if the trusted evaluation should be performed
        /// exclusively (that is, not shared).
        /// </param>
        /// <param name="withEvents">
        /// Upon return, non-zero if events should be processed during the
        /// trusted evaluation.
        /// </param>
        /// <param name="markTrusted">
        /// Upon return, non-zero if the interpreter should be marked as trusted
        /// during the evaluation.
        /// </param>
        /// <param name="allowUnsafe">
        /// Upon return, non-zero if unsafe operations should be allowed during
        /// the trusted evaluation.
        /// </param>
        /// <param name="ignoreHidden">
        /// Upon return, non-zero if hidden commands should be ignored during the
        /// trusted evaluation.
        /// </param>
        /// <param name="useSecurityLevels">
        /// Upon return, non-zero if security levels should be used during the
        /// trusted evaluation.
        /// </param>
        /// <param name="pushScriptLocation">
        /// Upon return, non-zero if the script location should be pushed during
        /// the trusted evaluation.
        /// </param>
        /// <param name="noIsolatedPlugins">
        /// Upon return, non-zero if isolated plugins should be disabled during
        /// the trusted evaluation.
        /// </param>
        /// <param name="withScopeFrame">
        /// Upon return, non-zero if a scope frame should be used during the
        /// trusted evaluation.
        /// </param>
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
#if THREADING
        /// <summary>
        /// This method calculates the effective timeout, in milliseconds, to use
        /// when waiting for an interpreter health check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the default timeout.  This parameter may
        /// be null.
        /// </param>
        /// <param name="timeout">
        /// The requested timeout, in milliseconds, or null to use the default
        /// timeout.
        /// </param>
        /// <param name="effectiveTimeout">
        /// Upon return, the effective timeout, in milliseconds, to use.
        /// </param>
        public static void GetHealthWaitTimeout(
            Interpreter interpreter,
            int? timeout,
            out int effectiveTimeout
            )
        {
            //
            // HACK: Use the default event timeout since waiting
            //       forever in this method is somewhat useless.
            //
            effectiveTimeout = ThreadOps.GetDefaultTimeout(
                interpreter, TimeoutType.Health);

            if (timeout != null)
                effectiveTimeout = (int)timeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire a lock on the specified interpreter
        /// for the purpose of performing a health check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to lock.  This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to wait for the lock, or null to use a
        /// soft (non-blocking) lock attempt.
        /// </param>
        /// <param name="locked">
        /// Upon success, non-zero if the interpreter lock was acquired.  The
        /// caller is responsible for releasing the lock.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives any error messages.  This parameter may be
        /// null, in which case a new list is created as needed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode TryLockForHealth(
            Interpreter interpreter,
            int? timeout,
            ref bool locked,
            ref ResultList errors
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("tryLock: invalid interpreter");
                return ReturnCode.Error;
            }

            IProfilerState profiler = null;

            try
            {
                profiler = ProfilerState.Create();

                if (profiler != null)
                    profiler.Start();

                if (timeout != null)
                {
                    interpreter.InternalTryLock(
                        (int)timeout, ref locked);
                }
                else
                {
                    interpreter.InternalSoftTryLock(
                        ref locked);
                }

                if (locked)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(
                        "tryLock: unable to acquire interpreter lock");

                    return ReturnCode.Error;
                }
            }
            finally
            {
                if (profiler != null)
                {
                    profiler.Stop();

                    long? milliseconds = profiler.GetMilliseconds();

                    if (milliseconds != null)
                        interpreter.TrackHealthRunTime((long)milliseconds);

                    ObjectOps.TryDisposeOrComplain<IProfilerState>(
                        interpreter, ref profiler);

                    profiler = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the built-in health check script using the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate the health check script.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the evaluation; upon failure,
        /// receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode EvaluateForHealth(
            Interpreter interpreter,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "evaluate: invalid interpreter";
                return ReturnCode.Error;
            }

            IProfilerState profiler = null;

            try
            {
                profiler = ProfilerState.Create();

                if (profiler != null)
                    profiler.Start();

                return interpreter.EvaluateScript(
                    HealthScript, ref result);
            }
            finally
            {
                if (profiler != null)
                {
                    profiler.Stop();

                    long? milliseconds = profiler.GetMilliseconds();

                    if (milliseconds != null)
                        interpreter.TrackHealthRunTime((long)milliseconds);

                    /* NO RESULT */
                    interpreter.UpdateHealthPerformance(profiler.ToString());

                    ObjectOps.TryDisposeOrComplain<IProfilerState>(
                        interpreter, ref profiler);

                    profiler = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies the outcome of a health check evaluation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that performed the health check.  This parameter may
        /// be null.
        /// </param>
        /// <param name="code">
        /// The return code produced by the health check evaluation.
        /// </param>
        /// <param name="result">
        /// The result produced by the health check evaluation.
        /// </param>
        /// <param name="resetOk">
        /// Non-zero to mark the interpreter as healthy upon successful
        /// verification.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives any error messages.  This parameter may be
        /// null, in which case a new list is created as needed.
        /// </param>
        /// <returns>
        /// True if the health check succeeded; otherwise, false.
        /// </returns>
        public static bool VerifyForHealth(
            Interpreter interpreter,
            ReturnCode code,
            Result result,
            bool resetOk,
            ref ResultList errors
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("verify: invalid interpreter");
                return false;
            }

            if (code != ReturnCode.Ok)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("verify: return code is not ok");
                return false;
            }

            if (!SharedStringOps.SystemEquals(result, HealthResult))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("verify: result has unexpected value");
                return false;
            }

            if (resetOk)
            {
                /* NO RESULT */
                interpreter.TouchHealthOk();
            }

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Remote Time Support Methods
#if NETWORK
        /// <summary>
        /// This method queries the current time from a remote, well-known
        /// auxiliary resource.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the query.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the query.  This parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the downloaded data to a string, or null
        /// to use the default encoding for remote URIs.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to use for the query, or null for no
        /// timeout.
        /// </param>
        /// <param name="response">
        /// Upon success, receives the remote time response.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode QueryRemoteTime(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Encoding encoding,       /* in: OPTIONAL */
            int? timeout,            /* in */
            ref string response,     /* in */
            ref Result error         /* out */
            )
        {
            string resourceName = TimeResourceName;

            Uri uri = PathOps.BuildAuxiliaryUri(
                ref resourceName, ref error);

            if (uri == null)
                return ReturnCode.Error;

#if TEST
            if (WebOps.SetSecurityProtocol(
                    false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            byte[] bytes = null;

            if (WebOps.DownloadData(
                    interpreter, clientData, uri,
                    null, timeout, null, ref bytes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (encoding == null)
            {
                encoding = StringOps.GetEncoding(
                    EncodingType.RemoteUri);
            }

            if (encoding == null)
            {
                error = "invalid encoding";
                return ReturnCode.Error;
            }

            response = encoding.GetString(bytes);
            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Zip Archive Support Methods
#if NETWORK
        /// <summary>
        /// This method extracts the contents of a zip archive into the specified
        /// directory using the external "unzip" command line tool.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the extraction.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the extraction.  This parameter may be
        /// null.
        /// </param>
        /// <param name="downloadDirectory">
        /// The directory used to download the command line tool, when running on
        /// Windows.  This parameter may be null on non-Windows operating systems.
        /// </param>
        /// <param name="downloadFileName">
        /// The file name of the zip archive to extract.
        /// </param>
        /// <param name="extractDirectory">
        /// The directory into which the zip archive is extracted.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use when executing the command line tool, or null
        /// to use the default event flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractZipFileToDirectory(
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

            bool deleteUnzipFileName = false;
            string unzipFileName = null;

            try
            {
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

                    if (!File.Exists(unzipFileName))
                    {
#if TEST
                        if (WebOps.SetSecurityProtocol(
                                false, false, ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
#endif

                        string resourceName = UnzipResourceName;

                        Uri uri = PathOps.BuildAuxiliaryUri(
                            ref resourceName, ref error);

                        if (uri == null)
                            return ReturnCode.Error;

                        deleteUnzipFileName = true;

                        if (WebOps.DownloadFile(
                                interpreter, clientData, uri,
                                unzipFileName, null, null, null,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: The downloaded command line tool
                    //       must be "trusted" by Windows in
                    //       order to be used; either way, it
                    //       will deleted at the end of this
                    //       method.
                    //
                    if (!RuntimeOps.IsFileTrusted(
                            interpreter, null, unzipFileName,
                            IntPtr.Zero))
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
                string unzipArguments;
                bool done = false;

                unzipArguments = RuntimeOps.BuildCommandLine(
                    interpreter, new string[] {
                        "-n", downloadFileName, "-d", extractDirectory
                    }, null, false, false, false, ref done, ref error);

                if (done)
                    return ReturnCode.Ok;

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
                Result localError = null;

                if (ProcessOps.ExecuteProcess(
                        interpreter, unzipFileName, unzipArguments,
                        localEventFlags, ref exitCode, ref result,
                        ref localError) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (exitCode != ResultOps.SuccessExitCode())
                {
                    ResultList errors = null;

                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    if (result != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(result);
                    }

                    error = String.Format(
                        "command line tool {0} {1} failed: {2}",
                        FormatOps.WrapOrNull(unzipFileName),
                        FormatOps.WrapOrNull(unzipArguments),
                        FormatOps.WrapOrNull(errors));

                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
            finally
            {
                if (deleteUnzipFileName &&
                    (unzipFileName != null) && File.Exists(unzipFileName))
                {
                    if (UnzipDeleteDelayMilliseconds >= 0)
                    {
                        HostOps.ThreadSleep(
                            UnzipDeleteDelayMilliseconds);
                    }

                    File.Delete(unzipFileName);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the contents of a zip archive into the specified
        /// directory, optionally falling back to the external "unzip" command
        /// line tool when the managed extraction is unavailable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the extraction.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the extraction.  This parameter may be
        /// null.
        /// </param>
        /// <param name="downloadDirectory">
        /// The directory used to download the command line tool, when running on
        /// Windows.  This parameter may be null on non-Windows operating systems.
        /// </param>
        /// <param name="downloadFileName">
        /// The file name of the zip archive to extract.
        /// </param>
        /// <param name="extractDirectory">
        /// The directory into which the zip archive is extracted.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use when executing the command line tool, or null
        /// to use the default event flags.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to force use of the command line tool fallback, zero to use
        /// only the managed extraction, or null to use the managed extraction
        /// and fall back to the command line tool if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExtractZipFileToDirectory(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            string downloadDirectory, /* in: OPTIONAL (Windows) */
            string downloadFileName,  /* in */
            string extractDirectory,  /* in */
            EventFlags? eventFlags,   /* in */
            bool? useFallback,        /* in */
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
                error = "download file name does not exist";
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

            ///////////////////////////////////////////////////////////////////

#if COMPRESSION
            if ((useFallback == null) || !(bool)useFallback)
            {
                try
                {
                    ZipFile.ExtractToDirectory(
                        downloadFileName, extractDirectory);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Attempt to fallback to using the "unzip" command
            //       line tool, which will be downloaded via auxiliary
            //       base URI for this assembly.  It must be digitally
            //       signed with a trusted certificate in order to be
            //       used.
            //
            if ((useFallback == null) || (bool)useFallback)
            {
                if (ExtractZipFileToDirectory(
                        interpreter, clientData, downloadDirectory,
                        downloadFileName, extractDirectory, eventFlags,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            error = "cannot extract zip file";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method downloads a zip archive from a well-known auxiliary
        /// resource and extracts its contents into the specified directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the download and extraction.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the download and extraction.  This
        /// parameter may be null.
        /// </param>
        /// <param name="extractDirectory">
        /// The directory into which the zip archive is extracted.
        /// </param>
        /// <param name="resourceName">
        /// The name of the auxiliary resource to download.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to force use of the command line tool fallback, zero to use
        /// only the managed extraction, or null to use the managed extraction
        /// and fall back to the command line tool if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
                        false, false, ref error) != ReturnCode.Ok)
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
                        downloadFileName, null, null, null,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return ExtractZipFileToDirectory(
                    interpreter, clientData, downloadDirectory,
                    downloadFileName, extractDirectory, null,
                    useFallback, ref error);
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

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Remote Script Library Initialization Support Methods
#if NETWORK && DATA && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// This method sets up the trace priorities used when initializing the
        /// script library via a trusted remote URI.
        /// </summary>
        /// <param name="debugPriority">
        /// Upon return, the trace priority to use for debug messages.
        /// </param>
        /// <param name="errorPriority">
        /// Upon return, the trace priority to use for error messages.
        /// </param>
        private static void SetupTracePrioritiesForTrustedRemote(
            out TracePriority debugPriority, /* out */
            out TracePriority errorPriority  /* out */
            )
        {
            //
            // HACK: Make 100% sure that we *always* see these trace
            //       messages because they are quite important from
            //       a centralized enterprise management perspective.
            //
            TracePriority basePriority =
                TracePriority.Always | TracePriority.NoLimits;

            debugPriority = basePriority | TracePriority.ScriptDebug3;
            errorPriority = basePriority | TracePriority.ScriptError3;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the data and signature URIs used to download a
        /// trusted remote script bundle.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used when combining the URIs.  This parameter may be
        /// null.
        /// </param>
        /// <param name="baseUri">
        /// The base URI from which the data and signature URIs are derived.
        /// </param>
        /// <param name="dataUri">
        /// Upon success, receives the URI of the trusted remote data.
        /// </param>
        /// <param name="signatureUri">
        /// Upon success, receives the URI of the trusted remote signature.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the URIs were built successfully; otherwise, false.
        /// </returns>
        private static bool BuildTrustedRemoteUris(
            Encoding encoding,    /* in */
            Uri baseUri,          /* in */
            out Uri dataUri,      /* out */
            out Uri signatureUri, /* out */
            ref Result error      /* out */
            )
        {
            dataUri = null;
            signatureUri = null;

            if (baseUri == null)
            {
                error = "invalid trusted remote base URI";
                return false;
            }

            string relativeUri = FileExtension.Signature;

            signatureUri = PathOps.TryCombineUris(
                baseUri, relativeUri, encoding, UriComponents.AbsoluteUri,
                UriFormat.Unescaped, UriFlags.NoSeparators, ref error);

            if (signatureUri == null) /* e.g. "https://urn.to/r/auto.harpy" */
                return false;

            dataUri = baseUri; /* e.g. "https://urn.to/r/auto" */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method downloads the data and signature for a trusted remote
        /// script bundle, while holding the interpreter lock.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to lock during the download.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use for the download.  This parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use for the download.  This parameter may be null.
        /// </param>
        /// <param name="dataUri">
        /// The URI from which the trusted remote data is downloaded.
        /// </param>
        /// <param name="signatureUri">
        /// The URI from which the trusted remote signature is downloaded.
        /// </param>
        /// <param name="debugPriority">
        /// The trace priority to use for debug messages.
        /// </param>
        /// <param name="errorPriority">
        /// The trace priority to use for error messages.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the downloaded trusted remote data.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the downloaded trusted remote signature.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode DownloadFromTrustedRemoteUri(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            Encoding encoding,           /* in */
            Uri dataUri,                 /* in */
            Uri signatureUri,            /* in */
            TracePriority debugPriority, /* in */
            TracePriority errorPriority, /* in */
            ref byte[] data,             /* out */
            ref byte[] signature,        /* out */
            ref Result error             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool locked = false;

            try
            {
                interpreter.InternalHardTryLock(ref locked);

                if (locked)
                {
                    if (interpreter.Disposed)
                    {
                        error = "interpreter is disposed";
                        return ReturnCode.Error;
                    }

                    Result localError; /* REUSED */

                    data = null;
                    localError = null;

                    if (WebOps.DownloadData(
                            interpreter, clientData, dataUri, null, null,
                            null, ref data, ref localError) != ReturnCode.Ok)
                    {
                        error = localError;
                        return ReturnCode.Error;
                    }

                    signature = null;
                    localError = null;

                    if (WebOps.DownloadData(
                            interpreter, clientData, signatureUri, null, null,
                            null, ref signature, ref error) != ReturnCode.Ok)
                    {
                        error = localError;
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unable to acquire lock";

                    TraceOps.LockTrace(
                        "DownloadFromTrustedRemoteUri",
                        typeof(Interpreter).Name, false,
                        TracePriority.LockError2,
                        interpreter.MaybeWhoHasLock());

                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                interpreter.InternalExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally enables security on the specified
        /// interpreter in preparation for a trusted remote initialization.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on which to enable security.  This parameter may be
        /// null.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if this method is running asynchronously with respect to the
        /// primary interpreter thread.
        /// </param>
        /// <param name="keepSecurity">
        /// Non-zero if security should be kept enabled after the trusted remote
        /// initialization.
        /// </param>
        /// <param name="wasEnabled">
        /// Upon return, non-zero if security was already enabled prior to this
        /// method being called.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode MaybeEnableOrDisableSecurity(
            Interpreter interpreter, /* in */
            bool asynchronous,       /* in */
            bool keepSecurity,       /* in */
            ref bool? wasEnabled,    /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool locked = false;

            try
            {
                interpreter.InternalHardTryLock(ref locked);

                if (locked)
                {
                    if (interpreter.Disposed)
                    {
                        error = "interpreter is disposed";
                        return ReturnCode.Error;
                    }

                    PluginFlags savedPluginFlags;

                    interpreter.BeginLoadOnAnyThread(
                        out savedPluginFlags);

                    if (!AreTypicalPluginFlagsInUse(
                            savedPluginFlags, false))
                    {
                        error = String.Format(
                            "interpreter has atypical plugin flags: {0}",
                            savedPluginFlags);

                        return ReturnCode.Error;
                    }

                    wasEnabled = interpreter.SecurityWasEnabled();

                    if ((bool)wasEnabled)
                        return ReturnCode.Ok;

                    //
                    // NOTE: If this method is running asynchronously
                    //       with respect to the primary interpreter
                    //       thread -AND- we are not allowed to keep
                    //       security enabled, then we cannot enable
                    //       security now.  This is because we would
                    //       need to (eventually) disable it at some
                    //       "unpredictable" point-in-time from the
                    //       perspective of the primary interpreter
                    //       thread and that would be a bad design.
                    //
                    if (asynchronous && !keepSecurity)
                    {
                        error = "security must be enabled already";
                        return ReturnCode.Error;
                    }

                    if (EnableOrDisableSecurity(
                            interpreter, true, true,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unable to acquire lock";

                    TraceOps.LockTrace(
                        "MaybeEnableOrDisableSecurity",
                        typeof(Interpreter).Name, false,
                        TracePriority.LockError3,
                        interpreter.MaybeWhoHasLock());

                    return ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the password used to decrypt a trusted remote script
        /// bundle from the global configuration.
        /// </summary>
        /// <returns>
        /// The password bytes, or null if no password is configured or it could
        /// not be decoded.
        /// </returns>
        public static byte[] GetPasswordForTrustedRemoteUri()
        {
            string value = GlobalConfiguration.GetValue(
                EnvVars.TrustedBundlePassword,
                ConfigurationFlags.Interpreter);

            if (value == null)
                return null;

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ScriptOps).Name,
                    TracePriority.ScriptError3);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the script library of the specified
        /// interpreter by downloading and evaluating a trusted remote script
        /// bundle.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to initialize.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to use during initialization.  This parameter may be
        /// null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when downloading the trusted remote bundle.  This
        /// parameter may be null.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags associated with the initialization.  This parameter
        /// is not used.
        /// </param>
        /// <param name="password">
        /// The password used to decrypt the trusted remote bundle, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero if this method is running asynchronously with respect to the
        /// primary interpreter thread.
        /// </param>
        /// <param name="keepSecurity">
        /// Non-zero if security should be kept enabled after the initialization.
        /// </param>
        public static void InitializeViaTrustedRemoteUri(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Encoding encoding,       /* in */
            ScriptFlags scriptFlags, /* in: NOT USED */
            byte[] password,         /* in: OPTIONAL */
            bool asynchronous,       /* in */
            bool keepSecurity        /* in */
            )
        {
            TracePriority debugPriority;
            TracePriority errorPriority;

            SetupTracePrioritiesForTrustedRemote(
                out debugPriority, out errorPriority);

            if (interpreter == null)
            {
                TraceOps.DebugTrace(
                    "InitializeViaTrustedRemoteUri: Invalid interpreter.",
                    typeof(ScriptOps).Name, errorPriority);

                return;
            }

            if (encoding == null)
            {
                TraceOps.DebugTrace(
                    "InitializeViaTrustedRemoteUri: Invalid encoding.",
                    typeof(ScriptOps).Name, errorPriority);

                return;
            }

            if (interpreter.IsTrustedRemoteOk())
            {
                TraceOps.DebugTrace(
                    "InitializeViaTrustedRemoteUri: Already completed.",
                    typeof(ScriptOps).Name, debugPriority);

                return;
            }

            Uri baseUri = SharedAttributeOps.GetAssemblyTrustedRemoteUri(
                GlobalState.GetAssembly());

            if (baseUri == null)
            {
                TraceOps.DebugTrace(
                    "InitializeViaTrustedRemoteUri: No trusted remote URI.",
                    typeof(ScriptOps).Name, debugPriority);

                return;
            }

            Result error = null; /* REUSED */

            if (CanEnableSecurity(
                    interpreter, true, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Security unavailable for " +
                        "trusted remote URI {0}: {1}",
                    FormatOps.WrapOrNull(baseUri),
                    FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name,
                    debugPriority);
            }

            Uri dataUri;
            Uri signatureUri;

            error = null;

            if (!BuildTrustedRemoteUris(
                    encoding, baseUri, out dataUri,
                    out signatureUri, ref error))
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Could not build trusted " +
                        "remote with URI {0}: {1}",
                    FormatOps.WrapOrNull(baseUri),
                    FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name,
                    errorPriority);

                return;
            }

#if TEST
            error = null;

            if (WebOps.SetSecurityProtocol(
                    false, false, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Lacking SSL/TLS protocols for " +
                        "trusted remote URI {0}: {1}",
                    FormatOps.WrapOrNull(baseUri),
                    FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name,
                    TracePriority.ScriptError);

                return;
            }
#endif

            byte[] data = null;
            byte[] signature = null;

            error = null;

            if (DownloadFromTrustedRemoteUri(
                    interpreter, clientData, encoding, dataUri,
                    signatureUri, debugPriority, errorPriority,
                    ref data, ref signature, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Could not download {0} or {1}: {2}",
                    FormatOps.WrapOrNull(dataUri),
                    FormatOps.WrapOrNull(signatureUri),
                    FormatOps.WrapOrNull(error)),
                    typeof(ScriptOps).Name,
                    errorPriority);

                return;
            }

            string bundleFileName = PathOps.GetTempFileName(
                interpreter, "etru_"); /* Eagle Trusted Remote Uri */

            if (String.IsNullOrEmpty(bundleFileName) ||
                File.Exists(bundleFileName))
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Bundle data file name error: {0}",
                    FormatOps.WrapOrNull(bundleFileName)),
                    typeof(ScriptOps).Name,
                    errorPriority);

                return;
            }

            string signatureFileName = String.Format(
                "{0}{1}", bundleFileName, FileExtension.Signature);

            if (String.IsNullOrEmpty(signatureFileName) ||
                File.Exists(signatureFileName))
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeViaTrustedRemoteUri: " +
                        "Bundle signature file name error: {0}",
                    FormatOps.WrapOrNull(signatureFileName)),
                    typeof(ScriptOps).Name,
                    errorPriority);

                return;
            }

            bool locked = false;

            try
            {
                if (asynchronous)
                    interpreter.InternalHardTryLock(ref locked);

                if (!asynchronous || locked)
                {
                    bool? wasEnabled = null;

                    try
                    {
                        if (MaybeEnableOrDisableSecurity(
                                interpreter, asynchronous, keepSecurity,
                                ref wasEnabled, ref error) != ReturnCode.Ok)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "InitializeViaTrustedRemoteUri: " +
                                    "Could not enable security: {0}",
                                FormatOps.WrapOrNull(error)),
                                typeof(ScriptOps).Name,
                                errorPriority);

                            return;
                        }

                        try
                        {
                            File.WriteAllBytes(bundleFileName, data);
                            File.WriteAllBytes(signatureFileName, signature);

                            //
                            // TODO: Re-evaluate if these are the best flags to
                            //       use here.
                            //
                            BundleFlags bundleFlags =
                                BundleFlags.Default | BundleFlags.StopOnError |
                                BundleFlags.RequireKeyRing;

                            Result result = null;

                            if (interpreter.EvaluateBundleFile(
                                    bundleFileName, password, bundleFlags,
                                    ref clientData, ref result) == ReturnCode.Ok)
                            {
                                interpreter.MarkAsTrustedRemoteOk();

                                TraceOps.DebugTrace(String.Format(
                                    "InitializeViaTrustedRemoteUri: " +
                                        "Bundle {0} evaluation success: {1}",
                                    FormatOps.WrapOrNull(bundleFileName),
                                    FormatOps.WrapOrNull(result)),
                                    typeof(ScriptOps).Name,
                                    debugPriority);
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "InitializeViaTrustedRemoteUri: " +
                                        "Bundle {0} evaluation error: {1}",
                                    FormatOps.WrapOrNull(bundleFileName),
                                    FormatOps.WrapOrNull(result)),
                                    typeof(ScriptOps).Name,
                                    errorPriority);
                            }
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                errorPriority);
                        }
                        finally
                        {
                            try
                            {
                                if (File.Exists(bundleFileName))
                                    File.Delete(bundleFileName);
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(ScriptOps).Name,
                                    errorPriority);
                            }

                            try
                            {
                                if (File.Exists(signatureFileName))
                                    File.Delete(signatureFileName);
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(ScriptOps).Name,
                                    errorPriority);
                            }
                        }
                    }
                    finally
                    {
                        //
                        // TODO: If the caller (e.g. PrivateInitializeLibrary)
                        //       wanted to evaluate the trusted remote script
                        //       bundle asynchronously, we have no reliable
                        //       way to disable security at some well-defined
                        //       point that we know will not potentially cause
                        //       any problems for the remaining script library
                        //       initialization process.
                        //
                        // NOTE: Above note still applies; however, we provide
                        //       an override flag to (forcibly) keep security
                        //       enabled.
                        //
                        if (!keepSecurity &&
                            (wasEnabled != null) && !(bool)wasEnabled)
                        {
                            error = null;

                            if (EnableOrDisableSecurity(
                                    interpreter, false, false,
                                    ref error) != ReturnCode.Ok)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "InitializeViaTrustedRemoteUri: " +
                                        "Could not disable security: {0}",
                                    FormatOps.WrapOrNull(error)),
                                    typeof(ScriptOps).Name,
                                    errorPriority);
                            }
                        }
                    }
                }
                else
                {
                    error = "unable to acquire lock";

                    TraceOps.LockTrace(
                        "InitializeViaTrustedRemoteUri",
                        typeof(Interpreter).Name, false,
                        TracePriority.LockError3,
                        interpreter.MaybeWhoHasLock());

                    return;
                }
            }
            finally
            {
                if (asynchronous)
                    interpreter.InternalExitLock(ref locked);
            }
        }
#endif
        #endregion
    }
}
