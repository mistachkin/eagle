/*
 * PackageOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if DATA
using BundlePair = System.Collections.Generic.KeyValuePair<string, byte[]>;
using BundleDictionary = System.Collections.Generic.Dictionary<string, byte[]>;
#endif

using PathList = System.Collections.Generic.IEnumerable<string>;
using SearchDictionary = Eagle._Containers.Public.PathDictionary<object>;

using AssemblyPluginPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Containers.Public.StringList>;

using AssemblyFilePluginNames = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

using PackageFileNameTriplet = Eagle._Components.Public.AnyTriplet<
    Eagle._Components.Public.PackageType, string, string>;

using PackageFileNameList = System.Collections.Generic.List<
    Eagle._Components.Public.AnyTriplet<Eagle._Components.Public.PackageType,
    string, string>>;

using PackageIndexPair = System.Collections.Generic.KeyValuePair<string,
    Eagle._Components.Public.MutableAnyPair<string,
        Eagle._Components.Public.PackageIndexFlags>>;

using PackageIndexAnyPair = Eagle._Components.Public.MutableAnyPair<
    string, Eagle._Components.Public.PackageIndexFlags>;

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
using PluginPair = System.Collections.Generic.KeyValuePair<string, byte[]>;
using PluginDictionary = System.Collections.Generic.Dictionary<string, byte[]>;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper methods used to discover,
    /// create, evaluate, and manage package index files (e.g.
    /// "pkgIndex.eagle") and the associated "package ifneeded" scripts for
    /// an interpreter, including support for host-provided, file system, and
    /// plugin assembly package indexes.
    /// </summary>
    [ObjectId("e6e6c799-cbfd-4aa3-9017-c9944322a81c")]
    internal static class PackageOps
    {
        #region Private Constants
        //
        // NOTE: These are the ScriptFlags that are *always* used when trying
        //       to fetch the "pkgIndex.eagle" file via the interpreter host.
        //
        /// <summary>
        /// The script flags that are always used when attempting to fetch the
        /// "pkgIndex.eagle" file via the interpreter host.
        /// </summary>
        private static readonly ScriptFlags IndexScriptFlags =
            ScriptFlags.PackageLibraryOptionalFile;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name of the command used to lazily create the "package
        /// ifneeded" command (and any associated procedures) when it is
        /// needed.
        /// </summary>
        private static string loaderCommand =
            "::maybeCreatePackageIfNeededCommand";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The name of the core command used to evaluate a script file (i.e.
        /// the "source" command).
        /// </summary>
        private static string sourceCommand = "::source";

        /// <summary>
        /// The name of the wrapper command that forwards to the core "source"
        /// command while tracking additional information about the operation.
        /// </summary>
        private static string sourceWithInfoCommand = "::sourceWithInfo";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name of the host-provided script that contains the list of
        /// host package index file names.
        /// </summary>
        private static string HostListFileName = "hostPackageIndexes";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a string value appears to be a
        //       public key token.
        //
        /// <summary>
        /// The regular expression used to check whether a string value appears
        /// to be a public key token (an optional "0x" prefix followed by 16
        /// hexadecimal digits).
        /// </summary>
        private static readonly Regex PublicKeyTokenRegEx = RegExOps.Create(
            "^(?:0x)?([0-9a-f]{16})$");

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // NOTE: This pattern ends up being "*/pkgIndex.eagle".  This is
        //       specifically designed for use with bundled scripts.
        //
        /// <summary>
        /// The pattern, which ends up being "*/pkgIndex.eagle", used to match
        /// package index files within bundled scripts.
        /// </summary>
        private static readonly string BundleFileNamePattern = "*/{0}";
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This pattern ends up being "pkgIndex_*.eagle".  This is
        //       specifically designed to exclude "pkgIndex.eagle" because
        //       that is handled separately.
        //
        /// <summary>
        /// The pattern, which ends up being "pkgIndex_*.eagle", used to match
        /// tagged package index files while excluding the plain
        /// "pkgIndex.eagle" file (which is handled separately).
        /// </summary>
        private static readonly string IndexFileNamePattern = "{0}_*{1}";

        //
        // NOTE: This further restricts the above pattern to enforce the
        //       requirement that all tagged package index file names must
        //       contain the 16 digit hexadecimal number.
        //
        /// <summary>
        /// The regular expression that further restricts the tagged package
        /// index file name pattern, enforcing the requirement that all tagged
        /// package index file names contain the 16 digit hexadecimal number.
        /// </summary>
        private static readonly Regex IndexFileNameRegEx = RegExOps.Create(
            "^" + ScriptTypes.PackageIndex + "_([0-9a-f]{16})\\" +
            FileExtension.Script + "$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Checking Methods
        /// <summary>
        /// This method compares two version numbers, treating a null version
        /// as less than any non-null version.
        /// </summary>
        /// <param name="version1">
        /// The first version to compare.  This parameter may be null.
        /// </param>
        /// <param name="version2">
        /// The second version to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the two versions are equal, a negative number if
        /// <paramref name="version1" /> is less than
        /// <paramref name="version2" />, or a positive number if
        /// <paramref name="version1" /> is greater than
        /// <paramref name="version2" />.
        /// </returns>
        public static int VersionCompare(
            Version version1,
            Version version2
            ) /* ENTRY-POINT */
        {
            if ((version1 != null) && (version2 != null))
                return version1.CompareTo(version2);
            else if ((version1 == null) && (version2 == null))
                return 0;        // x (null) is equal to y (null)
            else if (version1 == null)
                return -1;       // x (null) is less than y (non-null)
            else
                return 1;        // x (non-null) is greater than y (null)
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether one version satisfies another,
        /// either requiring an exact match or allowing the first version to be
        /// greater than or equal to the second.
        /// </summary>
        /// <param name="version1">
        /// The version being tested.  This parameter may be null.
        /// </param>
        /// <param name="version2">
        /// The version that must be satisfied.  This parameter may be null.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require that the two versions be exactly equal;
        /// otherwise, the first version must be greater than or equal to the
        /// second.
        /// </param>
        /// <returns>
        /// True if the version requirement is satisfied; otherwise, false.
        /// </returns>
        public static bool VersionSatisfies(
            Version version1,
            Version version2,
            bool exact
            ) /* ENTRY-POINT */
        {
            if (exact)
                return (VersionCompare(version1, version2) == 0);
            else
                return (VersionCompare(version1, version2) >= 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method swaps the two specified versions, if necessary, so that
        /// the first version is less than or equal to the second version.
        /// </summary>
        /// <param name="version1">
        /// The first version.  Upon return, this contains the lesser of the
        /// two versions.  This parameter may be null.
        /// </param>
        /// <param name="version2">
        /// The second version.  Upon return, this contains the greater of the
        /// two versions.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two versions were swapped; otherwise, false.
        /// </returns>
        public static bool MaybeSwapVersion(
            ref Version version1,
            ref Version version2
            ) /* ENTRY-POINT */
        {
            if (VersionCompare(version1, version2) == 1)
            {
                Version temporary = version1;

                version1 = version2;
                version2 = temporary;

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        /// <summary>
        /// This method creates a new core package object using the specified
        /// package metadata.
        /// </summary>
        /// <param name="name">
        /// The name of the package.
        /// </param>
        /// <param name="group">
        /// The group the package belongs to, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="description">
        /// The human-readable description of the package, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the package, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="indexFileName">
        /// The name of the package index file associated with the package, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="provideFileName">
        /// The name of the file that provided the package, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags for the package.
        /// </param>
        /// <param name="loaded">
        /// The version of the package that is currently loaded, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ifNeeded">
        /// The collection that maps each available version to its associated
        /// "package ifneeded" script.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created package object.
        /// </returns>
        public static IPackage NewCore(
            string name,
            string group,
            string description,
            IClientData clientData,
            string indexFileName,
            string provideFileName,
            PackageFlags flags,
            Version loaded,
            VersionStringDictionary ifNeeded
            ) /* ENTRY-POINT */
        {
            return new _Packages.Core(new PackageData(
                name, group, description, clientData, indexFileName,
                provideFileName, flags, loaded, ifNeeded, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Support Methods
        /// <summary>
        /// This method returns the absolute name of the core "load" command.
        /// </summary>
        /// <returns>
        /// The absolute name of the core "load" command.
        /// </returns>
        private static string GetLoadCommandName()
        {
            return NamespaceOps.MakeAbsoluteName(
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Load)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the absolute name of the core "package"
        /// command.
        /// </summary>
        /// <returns>
        /// The absolute name of the core "package" command.
        /// </returns>
        private static string GetCommandName()
        {
            return NamespaceOps.MakeAbsoluteName(
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Package)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of the specified version,
        /// falling back to the two-part assembly version and then to a default
        /// version value when no version is supplied.
        /// </summary>
        /// <param name="version">
        /// The version to convert to a string, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The string form of the resolved version.
        /// </returns>
        private static string GetVersionString(
            Version version /* in: OPTIONAL */
            )
        {
            if (version != null)
                return version.ToString();

            version = GlobalState.GetTwoPartAssemblyVersion();

            if (version != null)
                return version.ToString();

            return Vars.Version.DefaultValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified directory should be
        /// treated as a package directory, i.e. one that contains a package
        /// index file or a candidate script or assembly file, subject to the
        /// supplied flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when checking whether candidate
        /// assembly files are trusted.  This parameter may be null.
        /// </param>
        /// <param name="directory">
        /// The directory to examine.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the directory is examined.
        /// </param>
        /// <returns>
        /// True if the directory qualifies as a package directory; otherwise,
        /// false.
        /// </returns>
        private static bool IsDirectory(
            Interpreter interpreter,   /* in: OPTIONAL */
            string directory,          /* in */
            PackageIfNeededFlags flags /* in */
            )
        {
            if (String.IsNullOrEmpty(directory))
                return false;

            if (!Directory.Exists(directory))
                return false;

            bool ignoreDisabled = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.IgnoreDisabled, true);

            if (!ignoreDisabled && IsDisabled(directory))
                return false;

            string indexFileName = PathOps.CombinePath(
                null, directory, FileNameOnly.PackageIndex);

            if (String.IsNullOrEmpty(indexFileName))
                return false;

            if ((ignoreDisabled || !IsDisabled(indexFileName)) &&
                File.Exists(indexFileName))
            {
                return true;
            }

            if (!FlagOps.HasFlags(
                    flags, PackageIfNeededFlags.IgnoreFileName, true))
            {
                string[] fileNames = null;

                try
                {
                    fileNames = Directory.GetFiles(
                        directory, Characters.Asterisk.ToString(),
                        SearchOption.TopDirectoryOnly);
                }
                catch (Exception e)
                {
                    if (!FlagOps.HasFlags(
                            flags, PackageIfNeededFlags.Silent, true))
                    {
                        TraceOps.DebugTrace(
                            e, typeof(PackageOps).Name,
                            TracePriority.FileSystemError);
                    }
                }

                if (fileNames != null)
                {
                    bool mustHaveAssembly = FlagOps.HasFlags(
                        flags, PackageIfNeededFlags.MustHaveAssembly, true);

                    bool noTrusted = FlagOps.HasFlags(
                        flags, PackageIfNeededFlags.NoTrusted, true);

                    bool noVerified = FlagOps.HasFlags(
                        flags, PackageIfNeededFlags.NoVerified, true);

                    Array.Sort(fileNames); /* O(N) */

                    foreach (string fileName in fileNames)
                    {
                        if (!ignoreDisabled && IsDisabled(fileName))
                            continue;

                        if (PathOps.MatchExtension(
                                fileName, FileExtension.Script) ||
                            PathOps.MatchExtension(
                                fileName, FileExtension.EncryptedScript))
                        {
                            return true;
                        }

                        if (PathOps.MatchExtension(
                                fileName, FileExtension.Library) ||
                            PathOps.MatchExtension(
                                fileName, FileExtension.Executable))
                        {
                            if (GlobalState.IsAssemblyLocation(fileName))
                                continue;

                            if (mustHaveAssembly &&
                                !RuntimeOps.IsManagedAssembly(fileName))
                            {
                                continue;
                            }

                            if (!noTrusted &&
                                !RuntimeOps.IsFileTrusted(
                                    interpreter, null, fileName, IntPtr.Zero))
                            {
                                continue;
                            }

                            if (!noVerified &&
                                !RuntimeOps.IsStrongNameVerified(fileName, true))
                            {
                                continue;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method cannot (currently) fail.  The error parameter
        //       is here just in case this needs to change in the future.
        //
        /// <summary>
        /// This method builds the text of a "load" command used to load the
        /// plugin with the specified type from the specified file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to emit instead of the default "load"
        /// command name, if any.  This parameter may be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that the assembly must have, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the plugin to load.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to load.
        /// </param>
        /// <param name="anyThread">
        /// Non-zero to permit the plugin to be loaded on any thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this would contain an appropriate error message.
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// The text of the constructed "load" command.
        /// </returns>
        private static string GetLoadCommand(
            Interpreter interpreter, /* in: NOT USED */
            string commandName,      /* in: OPTIONAL */
            byte[] publicKeyToken,   /* in: OPTIONAL */
            string fileName,         /* in */
            string typeName,         /* in */
            bool anyThread,          /* in */
            ref Result error         /* out: NOT USED */
            )
        {
            StringList list = new StringList();

            if (commandName != null)
                list.Add(commandName);
            else
                list.Add(GetLoadCommandName());

            if (publicKeyToken != null)
            {
                list.Add("-publickeytoken");

                list.Add(String.Format("0x{0}",
                    ArrayOps.ToHexadecimalString(publicKeyToken)));
            }

            if (anyThread)
                list.Add("-anythread");

#if NATIVE
            list.Add("-maybeverifiedonly");
            list.Add("-maybetrustedonly");
#endif

            list.Add(Option.EndOfOptions);
            list.Add(fileName);
            list.Add(typeName);

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method cannot (currently) fail.  The error parameter
        //       is here just in case this needs to change in the future.
        //
        /// <summary>
        /// This method builds the text of a "package ifneeded" command used to
        /// register the script that should be evaluated to provide the
        /// specified package version.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to emit instead of the default "package"
        /// command name, if any.  This parameter may be null.
        /// </param>
        /// <param name="packageName">
        /// The name of the package.  This parameter may be null.
        /// </param>
        /// <param name="version">
        /// The version of the package, if any.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The script text to evaluate when the package is needed, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="locked">
        /// Non-zero to mark the package as locked.
        /// </param>
        /// <param name="error">
        /// Upon failure, this would contain an appropriate error message.
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// The text of the constructed "package ifneeded" command.
        /// </returns>
        private static string GetIfNeededCommand(
            Interpreter interpreter, /* in: NOT USED */
            string commandName,      /* in: OPTIONAL */
            string packageName,      /* in: OPTIONAL */
            Version version,         /* in: OPTIONAL */
            string text,             /* in: OPTIONAL */
            bool locked,             /* in: OPTIONAL */
            ref Result error         /* out: NOT USED */
            )
        {
            StringList list = new StringList();

            if (commandName != null)
                list.Add(commandName);
            else
                list.Add(GetCommandName());

            list.Add("ifneeded");
            list.Add(packageName);
            list.Add(GetVersionString(version));
            list.Add(text);

            if (locked)
            {
                list.Add(String.Format(
                    "{0}{1}", AttributeFlags.AddCharacter,
                    PackageFlags.Locked));
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified directories, in order, for the
        /// first one that contains a file with the specified file name only.
        /// </summary>
        /// <param name="directories">
        /// The directories to search.  This parameter may be null.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name (without any directory information) to search for.
        /// </param>
        /// <returns>
        /// The full path of the first matching file, or null if no matching
        /// file is found.
        /// </returns>
        private static string FindFileNameOnly(
            PathList directories, /* in */
            string fileNameOnly   /* in */
            )
        {
            if ((directories == null) ||
                String.IsNullOrEmpty(fileNameOnly))
            {
                return null;
            }

            foreach (string directory in directories)
            {
                if (String.IsNullOrEmpty(directory))
                    continue;

                if (!Directory.Exists(directory))
                    continue;

                string fileName = PathOps.CombinePath(
                    null, directory, fileNameOnly);

                if (!File.Exists(fileName))
                    continue;

                return fileName;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the text of a "package ifneeded" command whose
        /// body is a "load" command for the specified plugin type and file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the plugin to load.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to load; this also serves as the
        /// package name.
        /// </param>
        /// <param name="version">
        /// The version of the package.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that the assembly must have, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="anyThread">
        /// Non-zero to permit the plugin to be loaded on any thread.
        /// </param>
        /// <param name="locked">
        /// Non-zero to mark the package as locked.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The text of the constructed "package ifneeded" command, or null on
        /// failure.
        /// </returns>
        private static string GetIfNeededScript(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string typeName,         /* in */
            Version version,         /* in */
            byte[] publicKeyToken,   /* in: OPTIONAL */
            bool anyThread,          /* in */
            bool locked,             /* in */
            ref Result error         /* out */
            )
        {
            string text = GetLoadCommand(
                interpreter, null, publicKeyToken,
                fileName, typeName, anyThread,
                ref error);

            if (text == null)
                return null;

            return GetIfNeededCommand(
                interpreter, null, typeName,
                version, text, locked, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified directory to the supplied collection
        /// if it qualifies as a package directory and has not already been
        /// added.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when validating the directory.  This
        /// parameter may be null.
        /// </param>
        /// <param name="directory">
        /// The directory to add.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the directory is validated.
        /// </param>
        /// <param name="directories">
        /// The collection of directories to add to.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <returns>
        /// True if the directory was added; otherwise, false.
        /// </returns>
        private static bool MaybeAddDirectory(
            Interpreter interpreter,         /* in */
            string directory,                /* in */
            PackageIfNeededFlags flags,      /* in */
            ref SearchDictionary directories /* out */
            )
        {
            if ((directory == null) ||
                !IsDirectory(interpreter, directory, flags))
            {
                return false;
            }

            if (directories == null)
                directories = new SearchDictionary();

            if (directories.ContainsKey(directory))
                return false;

            directories.Add(directory, null);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns all directories under the specified path,
        /// optionally recursing into sub-directories.
        /// </summary>
        /// <param name="path">
        /// The root path to enumerate.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the enumeration (e.g. whether to recurse and
        /// whether to trace errors).
        /// </param>
        /// <returns>
        /// The list of directories, in order, or null if the path is invalid
        /// or the enumeration fails.
        /// </returns>
        private static PathList GetAllDirectories(
            string path,               /* in */
            PackageIfNeededFlags flags /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return null;

            bool recursive = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.AllDirectories, true);

            SearchDictionary paths = null;

            try
            {
                paths = SearchDictionary.ForAllDirectories(
                    path, recursive);
            }
            catch (Exception e)
            {
                if (!FlagOps.HasFlags(
                        flags, PackageIfNeededFlags.Silent, true))
                {
                    TraceOps.DebugTrace(
                        e, typeof(PackageOps).Name,
                        TracePriority.FileSystemError);
                }
            }

            if (paths == null)
                return null;

            return paths.GetKeysInOrder(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all qualifying directories under the specified
        /// path to the supplied collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when validating each directory.
        /// This parameter may be null.
        /// </param>
        /// <param name="path">
        /// The root path to enumerate.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the enumeration and validation.
        /// </param>
        /// <param name="paths">
        /// The collection of directories to add to.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <returns>
        /// The number of directories that were added.
        /// </returns>
        private static long MaybeAddAllDirectories(
            Interpreter interpreter,    /* in */
            string path,                /* in */
            PackageIfNeededFlags flags, /* in */
            ref SearchDictionary paths  /* out */
            )
        {
            int count = 0;

            if (String.IsNullOrEmpty(path))
                return count;

            PathList directories = GetAllDirectories(path, flags);

            if (directories == null)
                return count;

            foreach (string directory in directories)
            {
                if (MaybeAddDirectory(
                        interpreter, directory, flags, ref paths))
                {
                    count++;
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the ordered list of directories to search for
        /// package files, combining the supplied path, the binary path, the
        /// parent path, and the base path according to the specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when validating directories.
        /// </param>
        /// <param name="path">
        /// The primary path to consider, if any.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which directory sources are considered.
        /// </param>
        /// <returns>
        /// The ordered list of directories to search, or null if none were
        /// found.
        /// </returns>
        private static PathList GetDirectories(
            Interpreter interpreter,   /* in */
            string path,               /* in */
            PackageIfNeededFlags flags /* in */
            )
        {
            SearchDictionary subResult1 = null;

            if (FlagOps.HasFlags(
                    flags, PackageIfNeededFlags.UseThePath, true))
            {
                /* IGNORED */
                MaybeAddDirectory(
                    interpreter, path, flags, ref subResult1);
            }

            ///////////////////////////////////////////////////////////////////

            SearchDictionary subResult2 = null;

            if (FlagOps.HasFlags(
                    flags, PackageIfNeededFlags.UseBinaryPath, true))
            {
                /* IGNORED */
                MaybeAddDirectory(interpreter,
                    GlobalState.InitializeOrGetBinaryPath(true),
                    flags, ref subResult2);
            }

            ///////////////////////////////////////////////////////////////////

            SearchDictionary subResult3 = null;

            if (FlagOps.HasFlags(
                    flags, PackageIfNeededFlags.UseParentPath, true) &&
                (path != null))
            {
                string parentPath = null;

                try
                {
                    parentPath = Path.GetDirectoryName(path);
                }
                catch (Exception e)
                {
                    if (!FlagOps.HasFlags(
                            flags, PackageIfNeededFlags.Silent, true))
                    {
                        TraceOps.DebugTrace(
                            e, typeof(PackageOps).Name,
                            TracePriority.FileSystemError);
                    }
                }

                /* IGNORED */
                MaybeAddAllDirectories(
                    interpreter, parentPath, flags, ref subResult3);
            }

            ///////////////////////////////////////////////////////////////////

            SearchDictionary subResult4 = null;

            if (FlagOps.HasFlags(
                    flags, PackageIfNeededFlags.UseBasePath, true))
            {
                /* IGNORED */
                MaybeAddAllDirectories(
                    interpreter, GlobalState.GetBasePath(), flags,
                    ref subResult4);
            }

            ///////////////////////////////////////////////////////////////////

            StringList result = null;

            if ((subResult1 != null) || (subResult2 != null) ||
                (subResult3 != null) || (subResult4 != null))
            {
                result = new StringList();

                if (subResult1 != null)
                    result.AddRange(subResult1.GetKeysInOrder(false));

                if (subResult2 != null)
                    result.AddRange(subResult2.GetKeysInOrder(false));

                if (subResult3 != null)
                    result.AddRange(subResult3.GetKeysInOrder(false));

                if (subResult4 != null)
                    result.AddRange(subResult4.GetKeysInOrder(false));
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to extract a public key token from the
        /// specified string value, when that value matches the public key
        /// token format.
        /// </summary>
        /// <param name="value">
        /// The string value to examine.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting the hexadecimal string to bytes.
        /// </param>
        /// <param name="flags">
        /// The flags controlling whether errors are traced.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, this contains the extracted public key token bytes;
        /// otherwise, it is left unchanged.
        /// </param>
        /// <returns>
        /// True if a public key token was extracted; otherwise, false.
        /// </returns>
        private static bool ExtractPublicKeyToken(
            string value,               /* in */
            CultureInfo cultureInfo,    /* in */
            PackageIfNeededFlags flags, /* in */
            ref byte[] publicKeyToken   /* in, out */
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            Regex regEx = PublicKeyTokenRegEx;

            if (regEx == null)
                return false;

            Match match = regEx.Match(value);

            if ((match == null) || !match.Success)
                return false;

            byte[] localPublicKeyToken = null;
            Result error = null;

            if (ArrayOps.GetBytesFromHexadecimalString(
                    RegExOps.GetMatchValue(match, 1),
                    cultureInfo, ref localPublicKeyToken,
                    ref error) != ReturnCode.Ok)
            {
                if (!FlagOps.HasFlags(
                        flags, PackageIfNeededFlags.Silent, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "ExtractPublicKeyToken: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(PackageOps).Name,
                        TracePriority.InputError);
                }

                return false;
            }

            publicKeyToken = localPublicKeyToken;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and evaluates the "package ifneeded" scripts
        /// for each assembly-to-plugin-names mapping, using the directories
        /// derived from the specified path and flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="mappings">
        /// The dictionary that maps each assembly file name only to its list
        /// of plugin type names.
        /// </param>
        /// <param name="path">
        /// The primary path used to derive the directories to search, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="version">
        /// The version to use for the packages, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that the assemblies must have, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting public key tokens, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the script creation and evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of scripts that were created;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public static ReturnCode CreateAndEvaluateIfNeededScripts(
            Interpreter interpreter,          /* in */
            AssemblyFilePluginNames mappings, /* in */
            string path,                      /* in: OPTIONAL */
            Version version,                  /* in: OPTIONAL */
            byte[] publicKeyToken,            /* in: OPTIONAL */
            CultureInfo cultureInfo,          /* in: OPTIONAL */
            PackageIfNeededFlags flags,       /* in */
            ref Result result                 /* out */
            ) /* ENTRY-POINT */
        {
            if (mappings == null)
            {
                result = "invalid mappings dictionary";
                return ReturnCode.Error;
            }

            StringList list = null;

            PathList directories = GetDirectories(
                interpreter, path, flags);

            foreach (AssemblyPluginPair pair in mappings)
            {
                if (CreateAndEvaluateIfNeededScript(
                        interpreter, pair.Key, pair.Value,
                        directories, version, publicKeyToken,
                        cultureInfo, flags, ref list,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            result = list;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and (unless suppressed) evaluates the "package
        /// ifneeded" scripts for a single assembly file and its associated
        /// plugin type names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name only of the assembly to locate within the supplied
        /// directories.
        /// </param>
        /// <param name="typeNames">
        /// The list of plugin type names, optionally interleaved with public
        /// key tokens, for which to create scripts.
        /// </param>
        /// <param name="directories">
        /// The directories to search for the assembly file.
        /// </param>
        /// <param name="version">
        /// The version to use for the packages, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The initial public key token that the assembly must have, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting public key tokens, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the script creation and evaluation.
        /// </param>
        /// <param name="list">
        /// The list of created scripts to append to.  Upon return, this may
        /// contain a newly created list if one was not already supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode CreateAndEvaluateIfNeededScript(
            Interpreter interpreter,    /* in */
            string fileNameOnly,        /* in */
            StringList typeNames,       /* in */
            PathList directories,       /* in */
            Version version,            /* in: OPTIONAL */
            byte[] publicKeyToken,      /* in: OPTIONAL */
            CultureInfo cultureInfo,    /* in: OPTIONAL */
            PackageIfNeededFlags flags, /* in */
            ref StringList list,        /* in, out */
            ref Result error            /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(fileNameOnly))
            {
                error = "invalid file name only";
                return ReturnCode.Error;
            }

            if (typeNames == null)
            {
                error = "invalid type names";
                return ReturnCode.Error;
            }

            bool anyThread = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.AnyThread, true);

            bool locked = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.Locked, true);

            bool whatIf = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.WhatIf, true);

            bool errorOnNotFound = FlagOps.HasFlags(
                flags, PackageIfNeededFlags.ErrorOnNotFound, true);

            string fileName = FindFileNameOnly(
                directories, fileNameOnly);

            if (fileName == null)
            {
                if (errorOnNotFound)
                {
                    error = String.Format(
                        "could not find file name {0} in {1}",
                        FormatOps.WrapOrNull(fileNameOnly),
                        FormatOps.WrapOrNull(directories));

                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            byte[] localPublicKeyToken = publicKeyToken;

            foreach (string typeName in typeNames)
            {
                if (typeName == null)
                    continue;

                //
                // HACK: *WARNING* This is somewhat tricky.
                //       If the first type name appears to
                //       be a public key token, attempt to
                //       extract and use it with subsequent
                //       type names until a new public key
                //       token is found.  This is necessary
                //       because the underlying lists of
                //       public key tokens and plugin names
                //       are actually contained within one
                //       list, in a specific order.
                //
                if (ExtractPublicKeyToken(
                        typeName, cultureInfo, flags,
                        ref localPublicKeyToken))
                {
                    continue;
                }

                string text = GetIfNeededScript(
                    interpreter, fileName, typeName,
                    version, localPublicKeyToken,
                    anyThread, locked, ref error);

                if (text == null)
                    return ReturnCode.Error;

                if (!whatIf)
                {
                    Result result = null;

                    if (interpreter.EvaluateScript(text,
                            ref result) != ReturnCode.Ok)
                    {
                        error = result;
                        return ReturnCode.Error;
                    }
                }

                if (list == null)
                    list = new StringList();

                list.Add(text);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method cannot (currently) fail.  The error parameter
        //       is here just in case this needs to change in the future.
        //
        /// <summary>
        /// This method builds the text of a "package scan" command used to
        /// rescan the specified paths for new package indexes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, used to determine whether plugin probing
        /// is enabled.  This parameter may be null.
        /// </param>
        /// <param name="commandName">
        /// The name of the command to emit instead of the default "package"
        /// command name, if any.  This parameter may be null.
        /// </param>
        /// <param name="paths">
        /// The paths to scan, if any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this would contain an appropriate error message.
        /// This parameter is not used.
        /// </param>
        /// <returns>
        /// The text of the constructed "package scan" command.
        /// </returns>
        public static string GetScanCommand(
            Interpreter interpreter, /* in: OPTIONAL */
            string commandName,      /* in: OPTIONAL */
            PathList paths,          /* in: OPTIONAL */
            ref Result error         /* out: NOT USED */
            ) /* ENTRY-POINT */
        {
            //
            // TODO: This method contains several hard-coded option names
            //       for the [package scan] sub-command and must be kept
            //       manually synchronized if those options change.
            //
            StringList list = new StringList();

            if (commandName != null)
                list.Add(commandName);
            else
                list.Add(GetCommandName());

            list.Add("scan");
            list.Add("-host");
            list.Add("-bundle");

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            //
            // BUGFIX: When scanning for new package indexes, be sure to
            //         include probing of plugin assemblies whenever the
            //         associated interpreter has that option enabled.
            //
            if ((interpreter != null) &&
                interpreter.HasProbePlugins())
            {
                list.Add("-plugin");
            }
#endif

            list.Add("-normal");
            list.Add("-primary");
            list.Add("-tagged");
            list.Add("-refresh");

#if !NATIVE || DEBUG
            //
            // BUGFIX: For debug builds (or builds without NATIVE code
            //         support), trusted status of assemblies cannot be
            //         checked, due to lack of build signing; therefore,
            //         just skip trust checking in those cases.
            //
            list.Add("-notrusted");
#endif

            list.Add(Option.EndOfOptions);

            if (paths != null)
                list.AddRange(paths);

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the name of the specified script file relative
        /// to the package index directory that contains it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="name">
        /// The script file name to make relative.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package index
        /// directories.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to trace details when the relative file name cannot be
        /// determined.
        /// </param>
        /// <returns>
        /// The relative file name, or null if it could not be determined.
        /// </returns>
        public static string GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            bool verbose                           /* in */
            ) /* ENTRY-POINT */
        {
            string fileName = null;
            Result error = null;

            if (GetRelativeFileName(
                    interpreter, name, pathComparisonType,
                    ref fileName, ref error) == ReturnCode.Ok)
            {
                return fileName;
            }
            else if (verbose)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetRelativeFileName: interpreter = {0}, " +
                    "name = {1}, pathComparisonType = {2}, " +
                    "verbose = {3}, fileName = {4}, error = {5}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(pathComparisonType), verbose,
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(error)), typeof(PackageOps).Name,
                    TracePriority.PathDebug);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the name of the specified script file relative
        /// to the package index directory that contains it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="name">
        /// The script file name to make relative.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package index
        /// directories.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this contains the computed relative file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            ref string fileName,                   /* out */
            ref Result error                       /* out */
            ) /* ENTRY-POINT */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(name))
            {
                error = "invalid script name";
                return ReturnCode.Error;
            }

            StringList packageIndexFileNames;

            PackageIndexDictionary packageIndexes =
                interpreter.CopyPackageIndexes();

            if (packageIndexes == null)
            {
                error = "package indexes not available";
                return ReturnCode.Error;
            }

            //
            // NOTE: Sort the package index file names in order so
            //       that the deepest directories are listed first.
            //
            packageIndexFileNames = packageIndexes.GetKeysInOrder(false);

            if (packageIndexFileNames == null)
            {
                error = "failed to reorder file names for searching";
                return ReturnCode.Error;
            }

            if (pathComparisonType == PathComparisonType.BuiltIn)
            {
                packageIndexFileNames.Sort();
            }
            else
            {
                packageIndexFileNames.Sort(_Comparers.StringFileName.Create(
                    pathComparisonType));
            }

            string localFileName = PathOps.ResolveFullPath(interpreter, name);

            if (localFileName == null)
            {
                error = String.Format(
                    "failed to resolve full path of {0}",
                    FormatOps.WrapOrNull(name));

                return ReturnCode.Error;
            }

            string directory = PathOps.GetDirectoryName(localFileName);

            if (directory == null)
            {
                error = String.Format(
                    "failed to get directory name for {0}",
                    FormatOps.WrapOrNull(localFileName));

                return ReturnCode.Error;
            }

            directory = PathOps.AppendSeparator(directory);

            foreach (string packageIndexFileName in packageIndexFileNames)
            {
                string packageDirectory = PathOps.GetDirectoryName(
                    packageIndexFileName);

                if (String.IsNullOrEmpty(packageDirectory))
                    continue;

#if MONO || MONO_HACKS
                //
                // HACK: *MONO* The Mono call to Path.GetDirectoryName does not
                //       appear to convert the forward slashes in the directory
                //       name to backslashes as the .NET does; therefore, force
                //       a conversion by fully resolving the directory name, but
                //       only when running on Mono.
                //
                if (CommonOps.Runtime.IsMono())
                {
                    packageDirectory = PathOps.ResolveFullPath(interpreter,
                        packageDirectory);
                }
#endif

                packageDirectory = PathOps.AppendSeparator(packageDirectory);

                if (PathOps.IsEqualFileName(
                        packageDirectory, directory, packageDirectory.Length))
                {
                    fileName = PathOps.GetUnixPath(localFileName.Substring(
                        packageDirectory.Length));

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "package index matching directory {0} not found",
                FormatOps.WrapOrNull(directory));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the specified file name by prefixing it with
        /// the file name extracted from the supplied client data, when that
        /// client data carries the expected plugin or resource manager
        /// information.
        /// </summary>
        /// <param name="clientData">
        /// The client data that may carry a prefix file name.  This parameter
        /// may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name to adjust.  Upon success, this contains the file name
        /// combined with the prefix file name.
        /// </param>
        /// <param name="prefixFileName">
        /// Upon success, this contains the prefix file name that was applied;
        /// otherwise, it is set to null.
        /// </param>
        /// <returns>
        /// True if the file name was adjusted; otherwise, false.
        /// </returns>
        private static bool AdjustFileName(
            IClientData clientData,   /* in */
            ref string fileName,      /* in, out */
            out string prefixFileName /* out */
            )
        {
            prefixFileName = null;

            if (clientData == null)
                return false;

            //
            // TODO: Adjust this type check if the GetData() method for the
            //       "File" (i.e. Eagle._Hosts.File) host changes what could
            //       be provided in the associated IClientData.
            //
            IAnyTriplet<IPluginData, string, string> anyTriplet1 =
                clientData.Data as IAnyTriplet<IPluginData, string, string>;

            IAnyTriplet<IAnyPair<string, ResourceManager>, string, string>
                anyTriplet2 = clientData.Data as IAnyTriplet<IAnyPair<string,
                ResourceManager>, string, string>;

            if ((anyTriplet1 == null) && (anyTriplet2 == null))
            {
                return false;
            }
            else if (anyTriplet1 != null)
            {
                IPluginData pluginData = anyTriplet1.X;

                if (pluginData == null)
                    return false;

                prefixFileName = pluginData.FileName;
            }
            else
            {
                IAnyPair<string, ResourceManager> anyPair = anyTriplet2.X;

                if (anyPair == null)
                    return false;

                prefixFileName = anyPair.X;
            }

            if (String.IsNullOrEmpty(prefixFileName))
                return false;

            if (!String.IsNullOrEmpty(fileName))
            {
                fileName = PathOps.GetUnixPath(PathOps.CombinePath(
                    null, prefixFileName, fileName));
            }
            else
            {
                fileName = prefixFileName;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified package index
        /// collection contains either the relative file name or the full file
        /// name.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to search.
        /// </param>
        /// <param name="relativeFileName">
        /// The relative file name to look for.
        /// </param>
        /// <param name="fileName">
        /// The full file name to look for.
        /// </param>
        /// <returns>
        /// True if either file name is present; otherwise, false.
        /// </returns>
        private static bool ContainsFileName(
            PackageIndexDictionary packageIndexes,
            string relativeFileName,
            string fileName
            )
        {
            if (ContainsFileName(packageIndexes, relativeFileName))
                return true; /* IMPOSSIBLE? */

            if (ContainsFileName(packageIndexes, fileName))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified package index
        /// collection contains the specified file name.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to search.  This parameter may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The file name to look for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the file name is present; otherwise, false.
        /// </returns>
        private static bool ContainsFileName(
            PackageIndexDictionary packageIndexes,
            string fileName
            )
        {
            if ((packageIndexes == null) || (fileName == null))
                return false;

            return packageIndexes.ContainsKey(fileName); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether package indexing has been explicitly
        /// disabled for the specified path, by checking for the presence of a
        /// companion ".noPkgIndex" file or directory.
        /// </summary>
        /// <param name="path">
        /// The file or directory path to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if package indexing is disabled for the path; otherwise,
        /// false.
        /// </returns>
        private static bool IsDisabled(
            string path
            ) /* RECURSIVE */
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string newPath; /* REUSED */

            if (File.Exists(path))
            {
                //
                // NOTE: The path is a file and indexing of it
                //       can be prevented by creating another
                //       file within the same directory, with
                //       (almost) exactly the same name, i.e.
                //       just append the suffix ".noPkgIndex"
                //       to its name.
                //
                newPath = String.Format(
                    "{0}{1}", path, FileExtension.NoPkgIndex);

                if (File.Exists(newPath) ||
                    Directory.Exists(newPath))
                {
                    return true;
                }

                if (!PathOps.HasDirectory(newPath))
                    return false;

                return IsDisabled(Path.GetDirectoryName(
                    newPath)); /* RECURSIVE */
            }
            else if (Directory.Exists(path))
            {
                //
                // NOTE: In this case, the path is a directory
                //       and indexing within it can be stopped
                //       by creating a file or directory named
                //       ".noPkgIndex" within it.
                //
                newPath = Path.Combine(
                    path, FileExtension.NoPkgIndex);

                if (File.Exists(newPath) ||
                    Directory.Exists(newPath))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified file name to the package index
        /// collection, or updates its prefix file name and flags if it is
        /// already present.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  This parameter may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The file name to add or update.  This parameter may be null.
        /// </param>
        /// <param name="prefixFileName">
        /// The prefix file name to associate with the file name, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="addFlags">
        /// The package index flags to add for the file name.
        /// </param>
        private static void AddFileNameWithFlags(
            PackageIndexDictionary packageIndexes,
            string fileName,
            string prefixFileName,
            PackageIndexFlags addFlags
            )
        {
            if ((packageIndexes == null) || (fileName == null))
                return;

            PackageIndexAnyPair anyPair;

            if (packageIndexes.TryGetValue(fileName, out anyPair))
            {
                if (anyPair != null)
                {
                    if (prefixFileName != null)
                        anyPair.X = prefixFileName;

                    anyPair.Y |= addFlags;
                    return;
                }
            }

            packageIndexes[fileName] = new PackageIndexAnyPair(
                true, prefixFileName, addFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified package index callback for a
        /// single package index file, managing the interpreter state and
        /// recording the resulting file name and flags as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke.  This parameter may be null, in which case
        /// nothing is done.
        /// </param>
        /// <param name="path">
        /// The directory path associated with the package index, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name of the package index.
        /// </param>
        /// <param name="tag">
        /// The tag associated with the package index, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="packageType">
        /// The type of the package index being processed.
        /// </param>
        /// <param name="initialFlags">
        /// The initial package index flags supplied to the callback.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update when the callback
        /// indicates the index was evaluated.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data used when operating in "what if"
        /// mode.
        /// </param>
        /// <param name="addFlags">
        /// The package index flags to add for the file name when it is
        /// recorded.
        /// </param>
        /// <param name="purge">
        /// Upon return, this is set to non-zero if the package index should be
        /// forcibly purged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode InvokeCallback(
            Interpreter interpreter,                 /* in */
            PackageIndexCallback callback,           /* in */
            string path,                             /* in */
            string fileName,                         /* in */
            string tag,                              /* in */
            PackageType packageType,                 /* in */
            PackageIndexFlags initialFlags,          /* in */
            PackageIndexDictionary packageIndexes,   /* in */
            PackageContextClientData packageContext, /* in */
            PackageIndexFlags addFlags,              /* in */
            ref bool purge,                          /* out */
            ref Result error                         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (callback == null)
                return ReturnCode.Ok;

            bool whatIf = FlagOps.HasFlags(
                initialFlags, PackageIndexFlags.WhatIf, true);

            IClientData savedPackageContext = null;

            if (whatIf)
            {
                if (packageContext.ChangeIndexFileName(
                        fileName, false, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                savedPackageContext = interpreter.ContextClientData;
                interpreter.ContextClientData = packageContext;
            }

            try
            {
                InterpreterStateFlags savedInterpreterStateFlags;

                interpreter.BeginPendingPackageIndexes(
                    out savedInterpreterStateFlags);

                try
                {
                    PackageIndexFlags flags = initialFlags;

                    bool temporaryPackages = FlagOps.HasFlags(
                        flags, PackageIndexFlags.Temporary, true);

                    try
                    {
                        if (temporaryPackages)
                            interpreter.SetTemporaryPackages();

                        IClientData clientData = ClientData.Empty;
                        Result result = null;

                        if (callback(
                                interpreter, path, fileName, tag,
                                packageType, ref flags, ref clientData,
                                ref result) != ReturnCode.Ok)
                        {
                            error = result;
                            return ReturnCode.Error;
                        }

                        if (FlagOps.HasFlags(flags,
                                PackageIndexFlags.Evaluated,
                                true))
                        {
                            string newFileName = fileName;
                            string prefixFileName;

                            if (AdjustFileName(
                                    clientData, ref newFileName,
                                    out prefixFileName))
                            {
                                AddFileNameWithFlags(
                                    packageIndexes, newFileName,
                                    prefixFileName, addFlags);

                                purge = true;
                            }
                        }
                        else
                        {
                            purge = true;
                        }
                    }
                    finally
                    {
                        if (temporaryPackages)
                            interpreter.UnsetTemporaryPackages();
                    }
                }
                finally
                {
                    interpreter.EndPendingPackageIndexes(
                        ref savedInterpreterStateFlags);
                }
            }
            finally
            {
                if (whatIf)
                {
                    interpreter.ContextClientData = savedPackageContext;
                    savedPackageContext = null;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the host-provided package indexes (including any
        /// bundled package indexes), invoking the supplied callback for each
        /// and purging any that are no longer present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.  This parameter is not used.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke for each package index found.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data used when operating in "what if"
        /// mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode FindHost(
            Interpreter interpreter,                     /* in */
            StringList paths,                            /* in */ /* NOT USED */
            PackageIndexCallback callback,               /* in */
            PackageIndexFlags packageIndexFlags,         /* in */
            PathComparisonType pathComparisonType,       /* in */
            ref PackageIndexDictionary packageIndexes,   /* in, out */
            ref PackageContextClientData packageContext, /* in, out */
            ref Result error                             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.HasPendingPackageIndexes())
                return ReturnCode.Ok;

            if (paths == null) /* NOT USED */
            {
                error = "invalid paths";
                return ReturnCode.Error;
            }

            //
            // NOTE: Grab the full list of host package index file
            //       names, including their associated package types.
            //       Normally, this will only be the built-in package
            //       types, which will correspond to the core script
            //       library and test library packages.
            //
            PackageFileNameList fileNames = GetIndexFileNames(
                interpreter, interpreter.InternalCultureInfo,
                StringOps.GetEncoding(EncodingType.Script),
                packageIndexFlags);

            if (fileNames == null)
            {
                error = "host package file names not available";
                return ReturnCode.Error;
            }

            //
            // NOTE: Initialize the package index collection if
            //       necessary.
            //
            if (packageIndexes == null)
                packageIndexes = new PackageIndexDictionary();

            //
            // NOTE: Initially mark all package indexes as "not found".
            //       After the main search loop (below), any remaining
            //       package indexes that are still marked "not found"
            //       will be purged.
            //
            if (MarkIndexes(
                    packageIndexes, fileNames, PackageIndexFlags.HostMask,
                    PackageIndexFlags.NonHostMask, PackageIndexFlags.Found,
                    false, false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            packageIndexFlags &= ~PackageIndexFlags.NonHostMask;
            packageIndexFlags |= PackageIndexFlags.HostMask;

            //
            // NOTE: If we are refreshing package indexes or we have
            //       never seen this package index before, notify the
            //       caller.
            //
            bool refresh = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Refresh, true);

            bool temporary = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Temporary, true);

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Found;

            if (temporary)
                addFlags |= PackageIndexFlags.Temporary;

            //
            // NOTE: For each package index file, notify the callback
            //       if it is new and/or add it to the resulting
            //       collection.
            //
            foreach (PackageFileNameTriplet anyTriplet in fileNames)
            {
                //
                // NOTE: First, grab the pair of strings that hold the
                //       non-full and full file names for this package
                //       type.
                //
                PackageType fileType = anyTriplet.X;
                string relativeFileName = anyTriplet.Y;
                string fileName = anyTriplet.Z;

                //
                // NOTE: Setup the package index files for this file,
                //       starting with the common package index flags
                //       for all package types and then adding those
                //       specific to this package type.
                //
                PackageIndexFlags fileFlags = addFlags;

                if (fileType == PackageType.Bundle)
                    fileFlags |= PackageIndexFlags.Bundle;
                else
                    fileFlags |= PackageIndexFlags.Host;

                //
                // HACK: Have we seen this package index before?  This
                //       is important because it designed to prevent a
                //       [package ifneeded] script for a package from
                //       being evaluated more than once (e.g. once via
                //       the file system and once via the host).
                //
                bool exists = ContainsFileName(
                    packageIndexes, relativeFileName, fileName);

                //
                // NOTE: When set to non-zero, forcibly purge the
                //       package index, due to changes in the file
                //       name.
                //
                bool purge = false;

                //
                // NOTE: If we are refreshing package indexes or
                //       we have never seen this package index
                //       before, notify the caller.
                //
                if (refresh || !exists)
                {
                    if (InvokeCallback(
                            interpreter, callback, null,
                            relativeFileName, null, fileType,
                            packageIndexFlags, packageIndexes,
                            packageContext, fileFlags, ref purge,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                //
                // NOTE: If we have not seen this package index
                //       before add it to the resulting
                //       collection now; otherwise, mark it as
                //       "found" so that it will not be purged.
                //
                if (!purge)
                {
                    AddFileNameWithFlags(
                        packageIndexes, fileName, null, fileFlags);
                }
            }

            //
            // NOTE: Purge any package indexes from the list that are
            //       still marked as "not found".
            //
            if (PurgeIndexes(
                    packageIndexes, fileNames, PackageIndexFlags.HostMask,
                    PackageIndexFlags.NonHostMask | PackageIndexFlags.Found,
                    false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method finds the package indexes embedded within plugin
        /// assemblies in the specified paths, invoking the supplied callback
        /// for each and purging any that are no longer present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke for each package index found.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data used when operating in "what if"
        /// mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode FindPlugin(
            Interpreter interpreter,                     /* in */
            StringList paths,                            /* in */
            PackageIndexCallback callback,               /* in */
            PackageIndexFlags packageIndexFlags,         /* in */
            PathComparisonType pathComparisonType,       /* in */
            ref PackageIndexDictionary packageIndexes,   /* in, out */
            ref PackageContextClientData packageContext, /* in, out */
            ref Result error                             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.HasPendingPackageIndexes())
                return ReturnCode.Ok;

            if (paths == null)
            {
                error = "invalid paths";
                return ReturnCode.Error;
            }

            //
            // NOTE: Initialize the package index collection if
            //       necessary.
            //
            if (packageIndexes == null)
                packageIndexes = new PackageIndexDictionary();

            //
            // NOTE: Initially mark all package indexes as "not found".
            //       After the main search loop (below), any remaining
            //       package indexes that are still marked "not found"
            //       will be purged.
            //
            if (MarkIndexes(
                    packageIndexes, PackageIndexFlags.Plugin,
                    PackageIndexFlags.NonPluginMask,
                    PackageIndexFlags.Found, false, false,
                    false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            packageIndexFlags &= ~PackageIndexFlags.NonPluginMask;
            packageIndexFlags |= PackageIndexFlags.Plugin;

            //
            // NOTE: Find all the package index files in the specified
            //       paths, optionally looking in all sub-directories.
            //
            bool recursive = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Recursive, true);

            bool refresh = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Refresh, true);

            bool noFileError = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoFileError, true);

            bool trace = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Trace, true);

            bool verbose = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Verbose, true);

            bool noTrusted = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoTrusted, true);

            bool noVerified = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoVerified, true);

            bool noSort = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoSort, true);

            bool allowDuplicate = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.AllowDuplicateDirectory, true);

            bool temporary = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Temporary, true);

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Plugin |
                PackageIndexFlags.Found;

            if (temporary)
                addFlags |= PackageIndexFlags.Temporary;

            //
            // NOTE: Create a string comparer for file names, used to
            //       sort them.
            //
            IComparer<string> comparer = null;

            if (!noSort &&
                (pathComparisonType != PathComparisonType.BuiltIn))
            {
                comparer = _Comparers.StringFileName.Create(
                    pathComparisonType);
            }

            SearchOption searchOption = FileOps.GetSearchOption(recursive);

            foreach (string path in paths)
            {
                //
                // NOTE: Normalize the path prior to using it or adding
                //       it to the dictionary.
                //
                string newPath = PathOps.ResolveFullPath(interpreter,
                    path);

                //
                // HACK: If path has been explicitly disabled, skip it.
                //
                if (IsDisabled(newPath))
                    continue;

                //
                // HACK: If duplicate paths are not allowed -AND- this
                //       is a duplicate path, skip it.
                //
                if (!allowDuplicate)
                {
                    int oldCount = 0;

                    if (SearchIndexes(
                            interpreter, packageIndexes, path,
                            ref oldCount, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (oldCount > 0)
                        continue;
                }

                //
                // NOTE: Make sure the directory exists prior to
                //       attempting to find any files in it; otherwise,
                //       we just ignore it to reduce the burden on the
                //       caller to validate that a given path actually
                //       exists (which could be especially burdensome
                //       if it is constructed dynamically from
                //       environment variables, etc).
                //
                if (!String.IsNullOrEmpty(newPath) &&
                    Directory.Exists(newPath))
                {
                    //
                    // NOTE: Find all plugin files in the specified
                    //       directory.
                    //
                    StringList patterns = GetPluginPatterns(
                        interpreter, verbose);

                    if (patterns != null)
                    {
                        foreach (string pattern in patterns)
                        {
                            StringList fileNames = null;
                            Result localError = null;

                            try
                            {
                                fileNames = new StringList(
                                    Directory.GetFiles(newPath,
                                        PathOps.ScriptFileNameOnly(
                                            pattern) /* PATTERN */,
                                        searchOption));
                            }
                            catch (Exception e)
                            {
                                if (trace && verbose)
                                {
                                    TraceOps.DebugTrace(
                                        e, typeof(PackageOps).Name,
                                        TracePriority.FileSystemError);
                                }

                                localError = e;
                            }

                            //
                            // NOTE: If the list of file names is null,
                            //       then the GetFiles method threw an
                            //       exception.  In that case, either
                            //       stop now or skip this directory.
                            //
                            if (fileNames == null)
                            {
                                if (noFileError)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (localError != null)
                                        error = localError;
                                    else
                                        error = "no plugin package index files found";

                                    return ReturnCode.Error;
                                }
                            }

                            //
                            // HACK: This is somewhat bad.  This list
                            //       does not have to be sorted;
                            //       however, it is nice to know that
                            //       package index script files will
                            //       be evaluated in a deterministic
                            //       order.
                            //
                            if (!noSort)
                            {
                                if (comparer != null)
                                    fileNames.Sort(comparer);
                                else
                                    fileNames.Sort();
                            }

                            //
                            // NOTE: For each package index file,
                            //       notify the callback if it is new
                            //       and/or add it to the resulting
                            //       collection.
                            //
                            foreach (string fileName in fileNames)
                            {
                                //
                                // HACK: Skip over any obviously invalid
                                //       names.
                                //
                                if (String.IsNullOrEmpty(fileName))
                                    continue;

                                //
                                // HACK: If this name has been explicitly
                                //       disabled, skip it.
                                //
                                if (IsDisabled(fileName))
                                    continue;

                                //
                                // HACK: Always skip the core library
                                //       assembly itself as any package
                                //       indexes embedded within it
                                //       should be handled via "host"
                                //       processing.
                                //
                                // WARNING: The entry assembly location
                                //          is purposely NOT skipped
                                //          because it may contain some
                                //          value-added package indexes
                                //          supplied by its creator.
                                //
                                if (GlobalState.IsAssemblyLocation(fileName))
                                {
                                    if (trace && verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "FindPlugin: SKIPPING " +
                                            "SELF, interpreter = {0}, " +
                                            "fileName = {1}, flags = {2}",
                                            FormatOps.InterpreterNoThrow(
                                            interpreter), FormatOps.WrapOrNull(
                                            fileName), FormatOps.WrapOrNull(
                                                packageIndexFlags)),
                                            typeof(PackageOps).Name,
                                            TracePriority.PackageDebug3);
                                    }

                                    continue;
                                }

                                //
                                // HACK: Skip over any non-assembly DLL
                                //       files that happen to be present.
                                //
                                if (!RuntimeOps.IsManagedAssembly(fileName))
                                {
                                    if (trace && verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "FindPlugin: SKIPPING " +
                                            "UNMANAGED, interpreter = {0}, " +
                                            "fileName = {1}, flags = {2}",
                                            FormatOps.InterpreterNoThrow(
                                            interpreter), FormatOps.WrapOrNull(
                                            fileName), FormatOps.WrapOrNull(
                                                packageIndexFlags)),
                                            typeof(PackageOps).Name,
                                            TracePriority.PackageDebug3);
                                    }

                                    continue;
                                }

                                //
                                // NOTE: Unless forbidden, check that
                                //       the candidate plugin assembly
                                //       is signed using Authenticode.
                                //
                                if (!noTrusted &&
                                    !RuntimeOps.IsFileTrusted(
                                        interpreter, null, fileName, IntPtr.Zero))
                                {
                                    if (trace && verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "FindPlugin: SKIPPING " +
                                            "UNTRUSTED, interpreter = {0}, " +
                                            "fileName = {1}, flags = {2}",
                                            FormatOps.InterpreterNoThrow(
                                            interpreter), FormatOps.WrapOrNull(
                                            fileName), FormatOps.WrapOrNull(
                                                packageIndexFlags)),
                                            typeof(PackageOps).Name,
                                            TracePriority.PackageDebug3);
                                    }

                                    continue;
                                }

                                //
                                // NOTE: Unless forbidden, check that
                                //       the candidate plugin assembly
                                //       is signed with a StrongName
                                //       key pair.
                                //
                                if (!noVerified &&
                                    !RuntimeOps.IsStrongNameVerified(fileName, true))
                                {
                                    if (trace && verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "FindPlugin: SKIPPING " +
                                            "UNVERIFIED, interpreter = {0}, " +
                                            "fileName = {1}, flags = {2}",
                                            FormatOps.InterpreterNoThrow(
                                            interpreter), FormatOps.WrapOrNull(
                                            fileName), FormatOps.WrapOrNull(
                                                packageIndexFlags)),
                                            typeof(PackageOps).Name,
                                            TracePriority.PackageDebug3);
                                    }

                                    continue;
                                }

                                //
                                // NOTE: Have we seen this package index
                                //       before?
                                //
                                bool exists = ContainsFileName(
                                    packageIndexes, fileName);

                                //
                                // NOTE: When set to non-zero, forcibly
                                //       purge the package index, due to
                                //       changes in the file name.
                                //
                                bool purge = false;

                                //
                                // NOTE: If we are refreshing package
                                //       indexes or we have never seen
                                //       this package index before,
                                //       notify the caller.
                                //
                                if (refresh || !exists)
                                {
                                    if (InvokeCallback(
                                            interpreter, callback, newPath,
                                            fileName, null, PackageType.None,
                                            packageIndexFlags, packageIndexes,
                                            packageContext, addFlags, ref purge,
                                            ref error) != ReturnCode.Ok)
                                    {
                                        return ReturnCode.Error;
                                    }
                                }

                                //
                                // NOTE: If we have not seen this package
                                //       index before add it to the resulting
                                //       collection now; otherwise, mark it as
                                //       "found" so that it will not be purged.
                                //
                                if (!purge)
                                {
                                    AddFileNameWithFlags(
                                        packageIndexes, fileName, null, addFlags);
                                }
                            }
                        }
                    }
                }
            }

            //
            // NOTE: Purge any package indexes from the list that are
            //       still marked as "not found".
            //
            if (PurgeIndexes(
                    packageIndexes, PackageIndexFlags.Plugin,
                    PackageIndexFlags.NonPluginMask |
                    PackageIndexFlags.Found, false, false,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the package index files on the file system in the
        /// specified paths (both primary and tagged), invoking the supplied
        /// callback for each and purging any that are no longer present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke for each package index found.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data used when operating in "what if"
        /// mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode FindFile(
            Interpreter interpreter,                     /* in */
            StringList paths,                            /* in */
            PackageIndexCallback callback,               /* in */
            PackageIndexFlags packageIndexFlags,         /* in */
            PathComparisonType pathComparisonType,       /* in */
            ref PackageIndexDictionary packageIndexes,   /* in, out */
            ref PackageContextClientData packageContext, /* in, out */
            ref Result error                             /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.HasPendingPackageIndexes())
                return ReturnCode.Ok;

            if (paths == null)
            {
                error = "invalid paths";
                return ReturnCode.Error;
            }

            //
            // NOTE: Initialize the package index collection if
            //       necessary.
            //
            if (packageIndexes == null)
                packageIndexes = new PackageIndexDictionary();

            //
            // NOTE: Initially mark all package indexes as "not found".
            //       After the main search loop (below), any remaining
            //       package indexes that are still marked "not found"
            //       will be purged.
            //
            if (MarkIndexes(
                    packageIndexes, PackageIndexFlags.Normal,
                    PackageIndexFlags.NonNormalMask,
                    PackageIndexFlags.Found, false, false,
                    false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            packageIndexFlags &= ~PackageIndexFlags.NonNormalMask;
            packageIndexFlags |= PackageIndexFlags.Normal;

            //
            // NOTE: Find all the package index files in the specified
            //       paths, optionally looking in all sub-directories.
            //
            bool recursive = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Recursive, true);

            bool refresh = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Refresh, true);

            bool noFileError = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoFileError, true);

            bool trace = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Trace, true);

            bool verbose = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Verbose, true);

            bool noSort = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoSort, true);

            bool allowDuplicate = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.AllowDuplicateDirectory, true);

            bool temporary = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Temporary, true);

            bool primary = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Primary, true);

            bool tagged = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Tagged, true);

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Normal |
                PackageIndexFlags.Found;

            if (temporary)
                addFlags |= PackageIndexFlags.Temporary;

            //
            // NOTE: Create a string comparer for file names, used to
            //       sort them.
            //
            IComparer<string> comparer = null;

            if (!noSort &&
                (pathComparisonType != PathComparisonType.BuiltIn))
            {
                comparer = _Comparers.StringFileName.Create(
                    pathComparisonType);
            }

            SearchOption searchOption = FileOps.GetSearchOption(recursive);

            foreach (string path in paths)
            {
                //
                // NOTE: Normalize the path prior to using it or adding
                //       it to the dictionary.
                //
                string newPath = PathOps.ResolveFullPath(interpreter,
                    path);

                //
                // HACK: If path has been explicitly disabled, skip it.
                //
                if (IsDisabled(newPath))
                    continue;

                //
                // HACK: If duplicate paths are not allowed -AND- this
                //       is a duplicate path, skip it.
                //
                if (!allowDuplicate)
                {
                    int oldCount = 0;

                    if (SearchIndexes(
                            interpreter, packageIndexes, path,
                            ref oldCount, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (oldCount > 0)
                        continue;
                }

                //
                // NOTE: Make sure the directory exists prior to
                //       attempting to find any files in it; otherwise,
                //       we just ignore it to reduce the burden on the
                //       caller to validate that a given path actually
                //       exists (which could be especially burdensome
                //       if it is constructed dynamically from
                //       environment variables, etc).
                //
                if (!String.IsNullOrEmpty(newPath) &&
                    Directory.Exists(newPath))
                {
                    //
                    // NOTE: Find all package index files in the
                    //       specified directory.
                    //
                    string searchPattern; /* REUSED */
                    StringList fileNames = null;
                    StringDictionary tags = null;
                    Result localError = null;

                    if (primary)
                    {
                        searchPattern = GetIndexFilePattern(
                            interpreter, PackageType.None, false,
                            false);

                        if (fileNames == null)
                            fileNames = new StringList();

                        try
                        {
                            fileNames.AddRange(
                                Directory.GetFiles(
                                    newPath, searchPattern,
                                searchOption));
                        }
                        catch (Exception e)
                        {
                            if (trace && verbose)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(PackageOps).Name,
                                    TracePriority.FileSystemError);
                            }

                            localError = e;
                        }
                    }

                    if (tagged)
                    {
                        searchPattern = GetIndexFilePattern(
                            interpreter, PackageType.None, true,
                            false);

                        if (fileNames == null)
                            fileNames = new StringList();

                        string[] taggedFileNames = null;

                        try
                        {
                            taggedFileNames = Directory.GetFiles(
                                newPath, searchPattern,
                                searchOption);
                        }
                        catch (Exception e)
                        {
                            if (trace && verbose)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(PackageOps).Name,
                                    TracePriority.FileSystemError);
                            }

                            localError = e;
                        }

                        if (taggedFileNames == null)
                        {
                            if (noFileError)
                            {
                                continue;
                            }
                            else
                            {
                                if (localError != null)
                                    error = localError;
                                else
                                    error = "no tagged package index files found";

                                return ReturnCode.Error;
                            }
                        }

                        Regex regEx = IndexFileNameRegEx;

                        if (regEx != null)
                        {
                            tags = new StringDictionary();

                            foreach (string fileName in taggedFileNames)
                            {
                                if (String.IsNullOrEmpty(fileName))
                                    continue;

                                string fileNameOnly = Path.GetFileName(
                                    fileName);

                                if (String.IsNullOrEmpty(fileNameOnly))
                                    continue;

                                Match match = regEx.Match(fileNameOnly);

                                if ((match == null) || !match.Success)
                                    continue;

                                string tag = RegExOps.GetMatchValue(
                                    match, 1);

                                if (String.IsNullOrEmpty(tag))
                                    continue;

                                fileNames.Add(fileName);
                                tags.Add(fileName, tag);
                            }
                        }
                        else
                        {
                            fileNames.AddRange(taggedFileNames);
                        }
                    }

                    //
                    // NOTE: If the list of file names is null, then the
                    //       GetFiles method threw an exception.  In that
                    //       case, either stop now or skip this directory.
                    //
                    if (fileNames == null)
                    {
                        if (noFileError)
                        {
                            continue;
                        }
                        else
                        {
                            if (localError != null)
                                error = localError;
                            else
                                error = "no package index files found";

                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: For each package index file, notify the
                    //       callback if it is new and/or add it to the
                    //       resulting collection.
                    //
                    string basePath = GlobalState.GetBasePath();

                    //
                    // HACK: This is somewhat bad.  This list does not
                    //       have to be sorted; however, it is nice to
                    //       know that package index script files will
                    //       be evaluated in a deterministic order.
                    //
                    if (!noSort)
                    {
                        if (comparer != null)
                            fileNames.Sort(comparer);
                        else
                            fileNames.Sort();
                    }

                    foreach (string fileName in fileNames)
                    {
                        //
                        // HACK: Skip over any obviously invalid names.
                        //
                        if (String.IsNullOrEmpty(fileName))
                            continue;

                        //
                        // HACK: If this name has been explicitly
                        //       disabled, skip it.
                        //
                        if (IsDisabled(fileName))
                            continue;

                        //
                        // NOTE: Figure out the relative file name that
                        //       would correspond to the full file name
                        //       of the package index.
                        //
                        string relativeFileName = PathOps.GetUnixPath(
                            PathOps.MaybeRemoveBase(fileName, basePath,
                            null, true));

                        //
                        // NOTE: Have we seen this package index before?
                        //
                        bool exists = ContainsFileName(
                            packageIndexes, relativeFileName, fileName);

                        //
                        // NOTE: When set to non-zero, forcibly purge the
                        //       package index, due to changes in the file
                        //       name.
                        //
                        bool purge = false;

                        //
                        // NOTE: If we are refreshing package indexes or
                        //       we have never seen this package index
                        //       before, notify the caller.
                        //
                        if (refresh || !exists)
                        {
                            string tag = null;

                            if ((tags != null) &&
                                tags.TryGetValue(fileName, out tag) &&
                                String.IsNullOrEmpty(tag))
                            {
                                tag = null;
                            }

                            if (InvokeCallback(
                                    interpreter, callback, newPath,
                                    fileName, tag, PackageType.None,
                                    packageIndexFlags, packageIndexes,
                                    packageContext, addFlags, ref purge,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: If we have not seen this package index
                        //       before add it to the resulting
                        //       collection now; otherwise, mark it as
                        //       "found" so that it will not be purged.
                        //
                        if (!purge)
                        {
                            AddFileNameWithFlags(
                                packageIndexes, fileName, null, addFlags);
                        }
                    }
                }
            }

            //
            // NOTE: Purge any package indexes from the list that are
            //       still marked as "not found".
            //
            if (PurgeIndexes(
                    packageIndexes, PackageIndexFlags.Normal,
                    PackageIndexFlags.NonNormalMask |
                    PackageIndexFlags.Found, false, false,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the file system should be searched
        /// for package indexes before the interpreter host, based on the
        /// supplied flags and the default index script flags.
        /// </summary>
        /// <param name="packageIndexFlags">
        /// The flags that may explicitly prefer the file system or the host.
        /// </param>
        /// <returns>
        /// True if the file system should be preferred; otherwise, false.
        /// </returns>
        private static bool ShouldPreferFileSystem(
            PackageIndexFlags packageIndexFlags
            )
        {
            if (FlagOps.HasFlags(packageIndexFlags,
                    PackageIndexFlags.PreferFileSystem, true))
            {
                return true;
            }

            if (FlagOps.HasFlags(packageIndexFlags,
                    PackageIndexFlags.PreferHost, true))
            {
                return false;
            }

            if (FlagOps.HasFlags(IndexScriptFlags,
                    ScriptFlags.PreferFileSystem, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the package index file name associated with the
        /// specified package type, optionally fully qualified with the
        /// interpreter library path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve the library path when a
        /// fully qualified file name is requested.
        /// </param>
        /// <param name="packageType">
        /// The package type whose package index file name is required.
        /// </param>
        /// <param name="full">
        /// Non-zero to return a fully qualified file name; otherwise, a
        /// relative file name is returned.
        /// </param>
        /// <returns>
        /// The package index file name, or null if the package type is not
        /// recognized.
        /// </returns>
        private static string GetIndexFileName(
            Interpreter interpreter,
            PackageType packageType,
            bool full
            )
        {
            string fileName;

            switch (packageType)
            {
                case PackageType.None:
                case PackageType.Host:
                case PackageType.Bundle:
                    {
                        fileName = FileNameOnly.PackageIndex;
                        break;
                    }
                case PackageType.Loader:
                    {
                        fileName = FileName.LoaderPackageIndex;
                        break;
                    }
                case PackageType.Library:
                    {
                        fileName = FileName.LibraryPackageIndex;
                        break;
                    }
                case PackageType.Test:
                    {
                        fileName = FileName.TestPackageIndex;
                        break;
                    }
                case PackageType.Kit:
                    {
                        fileName = FileName.KitPackageIndex;
                        break;
                    }
                default:
                    {
                        fileName = null;
                        break;
                    }
            }

            if (full && (fileName != null))
            {
                //
                // NOTE: First, fetch library path for the interpreter.
                //       If this is null or an empty string, it will
                //       simply be ignored.
                //
                string[] directories = {
                    //
                    // NOTE: This is the interpreter library path, if
                    //       any.
                    //
                    PathOps.GetUnixPath(
                        GlobalState.GetLibraryPath(
                            interpreter, false, false)),

                    //
                    // NOTE: This may be used to store the directory
                    //       name portion for the library package index
                    //       file name.  Will be set below if necessary.
                    //
                    null
                };

                if (!String.IsNullOrEmpty(directories[0]))
                {
                    //
                    // NOTE: Check if library path for the interpreter
                    //       ends with the directory name portion of
                    //       the library package index file name.
                    //
                    directories[1] = PathOps.GetUnixPath(
                        PathOps.GetDirectoryName(fileName));

                    if (directories[0].EndsWith(
                            directories[1], PathOps.ComparisonType))
                    {
                        //
                        // NOTE: Yes.  In this case, append just the
                        //       file name portion of the library
                        //       package index file name.
                        //
                        fileName = PathOps.GetUnixPath(
                            PathOps.CombinePath(null, directories[0],
                                Path.GetFileName(fileName)));
                    }
                    else
                    {
                        //
                        // NOTE: No.  Append the library package index
                        //       file name verbatim, including the
                        //       directory name portion.
                        //
                        fileName = PathOps.GetUnixPath(
                            PathOps.CombinePath(null, directories[0],
                                fileName));
                    }
                }
            }

            return fileName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the full list of host package index file name
        /// triplets, including the built-in package types, any bundled package
        /// indexes, and any host-provided package indexes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when gathering bundled scripts.
        /// </param>
        /// <param name="encoding">
        /// The encoding used when gathering bundled scripts.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling which package index sources are included and
        /// whether errors are traced.
        /// </param>
        /// <returns>
        /// The list of package index file name triplets, each pairing a
        /// package type with its relative and full file names.
        /// </returns>
        private static PackageFileNameList GetIndexFileNames(
            Interpreter interpreter,            /* in */
            CultureInfo cultureInfo,            /* in */
            Encoding encoding,                  /* in */
            PackageIndexFlags packageIndexFlags /* in */
            )
        {
            PackageFileNameList fileNames = new PackageFileNameList();

            foreach (PackageType packageType in new PackageType[] {
                    PackageType.Loader, PackageType.Library,
                    PackageType.Test, PackageType.Kit })
            {
                //
                // NOTE: For each package type, the "non-full" file name
                //       is something like "lib/<pkg>/pkgIndex.eagle",
                //       where <pkg> is "Eagle1.0", "Test1.0", etc.  Also,
                //       for each package type, the "full" file name is
                //       something like "<dir>/lib/<pkg>/pkgIndex.eagle".
                //
                fileNames.Add(new PackageFileNameTriplet(packageType,
                    GetIndexFileName(interpreter, packageType, false),
                    GetIndexFileName(interpreter, packageType, true)));
            }

            if (interpreter != null)
            {
#if DATA
                if (FlagOps.HasFlags(
                        packageIndexFlags, PackageIndexFlags.Bundle, true))
                {
                    IBundleManager bundleManager = interpreter.BundleManager;

                    if (bundleManager != null)
                    {
                        BundleDictionary dictionary =
                            bundleManager.FileNames as BundleDictionary;

                        if (dictionary != null)
                        {
                            foreach (BundlePair pair in dictionary)
                            {
                                string fileName = pair.Key;

                                if (String.IsNullOrEmpty(fileName))
                                    continue;

                                byte[] password = pair.Value;
                                List<Script> scripts = null;

                                if (DataOps.GatherBundleScripts(
                                        interpreter, cultureInfo, null,
                                        null, encoding, fileName, password,
                                        String.Format(BundleFileNamePattern,
                                        GetIndexFileName(interpreter,
                                        PackageType.Bundle, false)), false,
                                        true, ref scripts) != ReturnCode.Ok)
                                {
                                    continue;
                                }

                                foreach (Script script in scripts)
                                {
                                    IBundleData bundleData = script.BundleData;

                                    if (bundleData == null)
                                        continue;

                                    string fullName = bundleData.FullName;

                                    string path = DataOps.BuildBundlePath(
                                        fileName, fullName, true);

                                    if (path == null)
                                        continue;

                                    fileNames.Add(new PackageFileNameTriplet(
                                        PackageType.Bundle, path, null));
                                }
                            }
                        }
                    }
                }
#endif

                ScriptFlags scriptFlags = ScriptOps.GetFlags(
                    interpreter, IndexScriptFlags, PackageType.Host,
                    false, true);

                scriptFlags &= ~ScriptFlags.AutomaticPackage;

                IClientData clientData = ClientData.Empty;

                string text = interpreter.GetScript(
                    HostListFileName, ref scriptFlags, ref clientData);

                if (text != null)
                {
                    StringList list = null;
                    Result error = null;

                    if (ParserOps<string>.SplitList(
                            interpreter, text, 0, Length.Invalid, true,
                            ref list, ref error) == ReturnCode.Ok)
                    {
                        string directory = PathOps.GetUnixPath(
                            GlobalState.GetLibraryPath(
                                interpreter, false, false));

                        foreach (string element in list)
                        {
                            if (String.IsNullOrEmpty(element))
                                continue;

                            if (!String.IsNullOrEmpty(directory))
                            {
                                fileNames.Add(new PackageFileNameTriplet(
                                    PackageType.Host, element,
                                    PathOps.CombinePath(null, directory,
                                    element)));
                            }
                            else
                            {
                                fileNames.Add(new PackageFileNameTriplet(
                                    PackageType.Host, element, null));
                            }
                        }
                    }
                    else if (FlagOps.HasFlags(
                            packageIndexFlags, PackageIndexFlags.Trace, true))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetIndexFileNames: error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(PackageOps).Name,
                            TracePriority.PackageError);
                    }
                }
            }

            return fileNames;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the search pattern used to locate package index
        /// files, either the tagged package index pattern or the plain package
        /// index file name for the specified package type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve the package index file name
        /// when the tagged pattern is not used.
        /// </param>
        /// <param name="packageType">
        /// The package type whose package index pattern is required.
        /// </param>
        /// <param name="tagged">
        /// Non-zero to return the pattern that matches tagged package index
        /// files.
        /// </param>
        /// <param name="full">
        /// Non-zero to return a fully qualified file name pattern; otherwise, a
        /// relative file name pattern is returned.
        /// </param>
        /// <returns>
        /// The package index search pattern.
        /// </returns>
        public static string GetIndexFilePattern(
            Interpreter interpreter,
            PackageType packageType,
            bool tagged,
            bool full
            )
        {
            string searchPattern = null;

            if (tagged)
            {
                searchPattern = IndexFileNamePattern;

                if (searchPattern != null)
                {
                    searchPattern = String.Format(
                        searchPattern, ScriptTypes.PackageIndex,
                        FileExtension.Script);
                }
            }

            if (searchPattern == null)
            {
                searchPattern = GetIndexFileName(
                    interpreter, packageType, full);
            }

            return searchPattern;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method returns the list of file name patterns used to find
        /// candidate plugin assembly files, falling back to a default pattern
        /// when none are configured.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to use verbose configuration lookup behavior.
        /// </param>
        /// <returns>
        /// The list of plugin file name patterns.
        /// </returns>
        private static StringList GetPluginPatterns(
            Interpreter interpreter,
            bool verbose
            )
        {
            StringList patterns = StringList.FromString(
                GlobalConfiguration.GetValue(EnvVars.PluginPatterns,
                GlobalConfiguration.GetFlags(
                    ConfigurationFlags.PackageOpsNoPrefix |
                    ConfigurationFlags.PatternListValue, verbose)));

            if (patterns == null)
            {
                patterns = new StringList();
                patterns.Add(Characters.Asterisk + FileExtension.Library);
            }

            return patterns;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the bare and fully qualified patterns used to
        /// find package index files (e.g. "pkgIndex.eagle" and
        /// "*/pkgIndex.eagle") within plugin assembly resources.
        /// </summary>
        /// <returns>
        /// The list of package index resource name patterns.
        /// </returns>
        private static StringList GetIndexPatterns()
        {
            //
            // NOTE: Return the bare and fully qualified patterns used
            //       to find package index files, e.g. "pkgIndex.eagle"
            //       and "*/pkgIndex.eagle".
            //
            StringList list = new StringList();

            list.Add(FileNameOnly.PackageIndex);

            list.Add(PathOps.GetUnixPath(PathOps.CombinePath(
                null, Characters.Asterisk.ToString(),
                FileNameOnly.PackageIndex)));

            return list;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the relative, absolute, and prefixed forms of
        /// the specified package index file name, using the prefix file name
        /// from the supplied package index pair, if any.
        /// </summary>
        /// <param name="indexFileName">
        /// The package index file name to expand.
        /// </param>
        /// <param name="anyPair">
        /// The package index pair that may supply a prefix file name.  This
        /// parameter may be null.
        /// </param>
        /// <param name="relativeFileName">
        /// Upon return, this contains the relative form of the file name, or
        /// null if it could not be computed.
        /// </param>
        /// <param name="absoluteFileName">
        /// Upon return, this contains the absolute form of the file name, or
        /// null if it could not be computed.
        /// </param>
        /// <param name="prefixedFileName">
        /// Upon return, this contains the prefixed form of the file name, or
        /// null if no prefix file name was supplied.
        /// </param>
        private static void GetAllFileNames(
            string indexFileName,
            PackageIndexAnyPair anyPair,
            out string relativeFileName,
            out string absoluteFileName,
            out string prefixedFileName
            )
        {
            relativeFileName = null;
            absoluteFileName = null;
            prefixedFileName = null;

            string basePath = GlobalState.GetBasePath();

            string prefixFileName = (anyPair != null) ?
                anyPair.X : null;

            if (PathOps.GetPathType(
                    indexFileName) == PathType.Relative)
            {
                absoluteFileName = PathOps.GetUnixPath(
                    PathOps.CombinePath(null, basePath,
                    indexFileName));

                relativeFileName = indexFileName;
            }
            else
            {
                if (prefixFileName != null)
                {
                    relativeFileName = PathOps.GetUnixPath(
                        PathOps.MaybeRemoveBase(indexFileName,
                        prefixFileName, null, true));

                    absoluteFileName = PathOps.GetUnixPath(
                        PathOps.CombinePath(null, basePath,
                        relativeFileName));
                }
                else
                {
                    absoluteFileName = indexFileName;

                    relativeFileName = PathOps.GetUnixPath(
                        PathOps.MaybeRemoveBase(indexFileName,
                        basePath, null, true));
                }
            }

            if (prefixFileName != null)
            {
                prefixedFileName = PathOps.GetUnixPath(
                    PathOps.CombinePath(null, prefixFileName,
                    relativeFileName));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes logically duplicate package indexes from the
        /// specified collection, treating the relative, absolute, and prefixed
        /// forms of a file name as the same package index and merging their
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to deduplicate.  Upon success,
        /// this is replaced with a new collection containing no logical
        /// duplicates.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode RemoveLogicalDuplicates(
            Interpreter interpreter,                   /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            StringList fileNames = packageIndexes.GetKeysInOrder(false);

            if (fileNames == null)
            {
                error = "failed to reorder file names for removing";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            PackageIndexDictionary newPackageIndexes =
                new PackageIndexDictionary();

            foreach (string fileName in fileNames)
            {
                PackageIndexAnyPair oldAnyPair;

                if (!packageIndexes.TryGetValue(
                        fileName, out oldAnyPair))
                {
                    continue;
                }

                string relativeFileName;
                string absoluteFileName;
                string prefixedFileName;

                GetAllFileNames(
                    fileName, oldAnyPair, out relativeFileName,
                    out absoluteFileName, out prefixedFileName);

                PackageIndexFlags flags = (oldAnyPair != null) ?
                    oldAnyPair.Y : PackageIndexFlags.None;

                int count = 0;
                PackageIndexAnyPair newAnyPair; /* REUSED */

                if ((relativeFileName != null) &&
                    newPackageIndexes.TryGetValue(relativeFileName,
                        out newAnyPair))
                {
                    if (newAnyPair != null)
                        newAnyPair.Y |= flags;

                    count++;
                }

                if ((absoluteFileName != null) &&
                    newPackageIndexes.TryGetValue(absoluteFileName,
                        out newAnyPair))
                {
                    if (newAnyPair != null)
                        newAnyPair.Y |= flags;

                    count++;
                }

                if ((prefixedFileName != null) &&
                    newPackageIndexes.TryGetValue(prefixedFileName,
                        out newAnyPair))
                {
                    if (newAnyPair != null)
                        newAnyPair.Y |= flags;

                    count++;
                }

                if (count > 0)
                    continue;

                newPackageIndexes.Add(fileName, new PackageIndexAnyPair(
                    true, (oldAnyPair != null) ? oldAnyPair.X : null,
                    flags));
            }

            packageIndexes = newPackageIndexes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a human-readable list describing the contents of
        /// the specified package index collection, one element per package
        /// index.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to describe.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The list describing the package indexes, or null if the collection
        /// is null or its keys could not be ordered.
        /// </returns>
        private static StringList ToList(
            PackageIndexDictionary packageIndexes /* in */
            )
        {
            if (packageIndexes == null)
                return null;

            StringList fileNames = packageIndexes.GetKeysInOrder(false);

            if (fileNames == null)
                return null;

            StringList list = new StringList();

            foreach (string fileName in fileNames)
            {
                StringList subList;
                PackageIndexAnyPair anyPair;

                if (!packageIndexes.TryGetValue(fileName, out anyPair))
                {
                    subList = new StringList(
                        "MISSING", "fileName", fileName);

                    list.Add(subList.ToString());
                    continue;
                }

                if (anyPair == null)
                {
                    subList = new StringList(
                        "INVALID", "fileName", fileName);

                    list.Add(subList.ToString());
                    continue;
                }

                subList = new StringList(
                    "fileName", fileName, "prefixFileName", anyPair.X,
                    "flags", anyPair.Y.ToString());

                list.Add(subList.ToString());
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method formats the specified package index dictionary as a
        /// string, with one entry per line.
        /// </summary>
        /// <param name="packageIndexes">
        /// The package index dictionary to format.
        /// </param>
        /// <returns>
        /// The formatted string, or null if the dictionary could not be
        /// formatted.
        /// </returns>
        private static string ToString( /* NOT USED */
            PackageIndexDictionary packageIndexes /* in */
            )
        {
            StringList list = ToList(packageIndexes);

            if (list == null)
                return null;

            return String.Join(Environment.NewLine, list.ToArray());
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a diagnostic trace describing the inputs and
        /// results of a package index discovery operation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths that were searched.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags that controlled the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting that was used.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes that was produced.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data that was used.
        /// </param>
        /// <param name="returnCode">
        /// The return code of the discovery operation.
        /// </param>
        /// <param name="error">
        /// The error message produced by the discovery operation, if any.
        /// </param>
        public static void FindAllDump(
            Interpreter interpreter,                 /* in */
            StringList paths,                        /* in */
            PackageIndexFlags packageIndexFlags,     /* in */
            PathComparisonType pathComparisonType,   /* in */
            PackageIndexDictionary packageIndexes,   /* in */
            PackageContextClientData packageContext, /* in */
            ReturnCode returnCode,                   /* in */
            Result error                             /* in */
            )
        {
            TraceOps.DebugTrace(
                "FindAllDump", null, typeof(PackageOps).Name,
                TracePriority.PackageDebug5, false, "interpreter",
                interpreter, "paths", paths, "packageIndexFlags",
                packageIndexFlags, "pathComparisonType",
                pathComparisonType, "packageIndexes",
                ToList(packageIndexes), "packageContext",
                packageContext, "returnCode", returnCode,
                "error", error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers all package indexes in the specified paths,
        /// using a new internal package index collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode FindAll(
            Interpreter interpreter,               /* in */
            StringList paths,                      /* in */
            PackageIndexFlags packageIndexFlags,   /* in */
            PathComparisonType pathComparisonType, /* in */
            ref Result error                       /* out */
            ) /* ENTRY-POINT */
        {
            PackageIndexDictionary packageIndexes = null;

            return FindAll(
                interpreter, paths, packageIndexFlags,
                pathComparisonType, ref packageIndexes,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers all package indexes in the specified paths,
        /// updating the supplied package index collection, using a new
        /// internal package context.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode FindAll(
            Interpreter interpreter,                   /* in */
            StringList paths,                          /* in */
            PackageIndexFlags packageIndexFlags,       /* in */
            PathComparisonType pathComparisonType,     /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            ) /* ENTRY-POINT */
        {
            PackageContextClientData packageContext = null;

            return FindAll(
                interpreter, paths, packageIndexFlags, pathComparisonType,
                ref packageIndexes, ref packageContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers all package indexes in the specified paths,
        /// searching the host, plugin assemblies, and file system in the order
        /// dictated by the supplied flags, and then removing any logical
        /// duplicates.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="paths">
        /// The paths to search.
        /// </param>
        /// <param name="packageIndexFlags">
        /// The flags controlling the search.
        /// </param>
        /// <param name="pathComparisonType">
        /// The type of comparison and sorting used to order the package
        /// indexes.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.  Upon return, this may
        /// contain a newly created collection if one was not already supplied.
        /// </param>
        /// <param name="packageContext">
        /// The package context client data used when operating in "what if"
        /// mode.  Upon return, this may contain a newly created context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode FindAll(
            Interpreter interpreter,                     /* in */
            StringList paths,                            /* in */
            PackageIndexFlags packageIndexFlags,         /* in */
            PathComparisonType pathComparisonType,       /* in */
            ref PackageIndexDictionary packageIndexes,   /* in, out */
            ref PackageContextClientData packageContext, /* in, out */
            ref Result error                             /* out */
            ) /* ENTRY-POINT */
        {
            bool host = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.HostMask, false);

            bool normal = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Normal, true);

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            bool plugin = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Plugin, true);
#endif

            bool dump = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Dump, true);

            StringList localPaths = ListOps.GetUniqueElements(paths);

            if (ShouldPreferFileSystem(packageIndexFlags))
            {
                if ((!normal || (FindFile(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)) &&
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                    (!plugin || (FindPlugin(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)) &&
#endif
                    (!host || (FindHost(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)))
                {
                    if (!FlagOps.HasFlags(packageIndexFlags,
                            PackageIndexFlags.AllowDuplicateFile,
                            true) &&
                        (RemoveLogicalDuplicates(
                            interpreter, ref packageIndexes,
                            ref error) != ReturnCode.Ok))
                    {
                        if (dump)
                        {
                            FindAllDump(
                                interpreter, localPaths,
                                packageIndexFlags,
                                pathComparisonType,
                                packageIndexes,
                                packageContext,
                                ReturnCode.Error, error);
                        }

                        return ReturnCode.Error;
                    }

                    if (dump)
                    {
                        FindAllDump(
                            interpreter, localPaths,
                            packageIndexFlags,
                            pathComparisonType,
                            packageIndexes,
                            packageContext,
                            ReturnCode.Ok, error);
                    }

                    return ReturnCode.Ok;
                }
            }
            else
            {
                if ((!host || (FindHost(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)) &&
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                    (!plugin || (FindPlugin(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)) &&
#endif
                    (!normal || (FindFile(
                        interpreter, localPaths, IndexCallback,
                        packageIndexFlags, pathComparisonType,
                        ref packageIndexes, ref packageContext,
                        ref error) == ReturnCode.Ok)))
                {
                    if (!FlagOps.HasFlags(packageIndexFlags,
                            PackageIndexFlags.AllowDuplicateFile,
                            true) &&
                        (RemoveLogicalDuplicates(
                            interpreter, ref packageIndexes,
                            ref error) != ReturnCode.Ok))
                    {
                        if (dump)
                        {
                            FindAllDump(
                                interpreter, localPaths,
                                packageIndexFlags,
                                pathComparisonType,
                                packageIndexes,
                                packageContext,
                                ReturnCode.Error, error);
                        }

                        return ReturnCode.Error;
                    }

                    if (dump)
                    {
                        FindAllDump(
                            interpreter, localPaths,
                            packageIndexFlags,
                            pathComparisonType,
                            packageIndexes,
                            packageContext,
                            ReturnCode.Ok, error);
                    }

                    return ReturnCode.Ok;
                }
            }

            if (dump)
            {
                FindAllDump(
                    interpreter, localPaths,
                    packageIndexFlags,
                    pathComparisonType,
                    packageIndexes,
                    packageContext,
                    ReturnCode.Error, error);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified package index flags
        /// have the required flags and do not have the forbidden flags.
        /// </summary>
        /// <param name="flags">
        /// The package index flags to test.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that must be present, or none to skip this check.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that must be absent, or none to skip this check.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the required flags be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the forbidden flags be present
        /// before the check fails; otherwise, any one of them causes the check
        /// to fail.
        /// </param>
        /// <returns>
        /// True if the flags satisfy the required and forbidden conditions;
        /// otherwise, false.
        /// </returns>
        private static bool MatchFlags(
            PackageIndexFlags flags,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
        {
            if (((hasFlags == PackageIndexFlags.None) ||
                    FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                ((notHasFlags == PackageIndexFlags.None) ||
                    !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the specified mark flags on each package
        /// index, named by the supplied file name triplets, whose flags match
        /// the required and forbidden conditions.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.
        /// </param>
        /// <param name="fileNames">
        /// The list of package index file name triplets identifying which
        /// package indexes to consider.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a package index must have to be marked, or none to
        /// skip this check.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a package index must not have to be marked, or none
        /// to skip this check.
        /// </param>
        /// <param name="markFlags">
        /// The flags to set or clear on each matching package index.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the required flags be present.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the forbidden flags be present
        /// before excluding a package index.
        /// </param>
        /// <param name="mark">
        /// Non-zero to set the mark flags; otherwise, the mark flags are
        /// cleared.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode MarkIndexes(
            PackageIndexDictionary packageIndexes,
            PackageFileNameList fileNames,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            PackageIndexFlags markFlags,
            bool hasAll,
            bool notHasAll,
            bool mark,
            ref Result error
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            if (fileNames == null)
            {
                error = "invalid file names";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            string fileName; /* REUSED */
            PackageIndexAnyPair anyPair; /* REUSED */
            PackageIndexFlags flags; /* REUSED */

            foreach (PackageFileNameTriplet anyTriplet in fileNames)
            {
                fileName = anyTriplet.Y;

                if ((fileName != null) &&
                    packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                             hasAll, notHasAll))
                    {
                        if (mark)
                            flags |= markFlags;
                        else
                            flags &= ~markFlags;

                        anyPair.Y = flags;
                    }
                }

                fileName = anyTriplet.Z;

                if ((fileName != null) &&
                    packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                            hasAll, notHasAll))
                    {
                        if (mark)
                            flags |= markFlags;
                        else
                            flags &= ~markFlags;

                        anyPair.Y = flags;
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the specified mark flags on each package
        /// index in the collection whose flags match the required and
        /// forbidden conditions.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a package index must have to be marked, or none to
        /// skip this check.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a package index must not have to be marked, or none
        /// to skip this check.
        /// </param>
        /// <param name="markFlags">
        /// The flags to set or clear on each matching package index.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the required flags be present.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the forbidden flags be present
        /// before excluding a package index.
        /// </param>
        /// <param name="mark">
        /// Non-zero to set the mark flags; otherwise, the mark flags are
        /// cleared.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode MarkIndexes(
            PackageIndexDictionary packageIndexes,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            PackageIndexFlags markFlags,
            bool hasAll,
            bool notHasAll,
            bool mark,
            ref Result error
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            StringList fileNames = packageIndexes.GetKeysInOrder(false);

            if (fileNames == null)
            {
                error = "failed to reorder file names for marking";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            foreach (string fileName in fileNames)
            {
                if (fileName == null)
                    continue;

                PackageIndexAnyPair anyPair;

                if (packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    PackageIndexFlags flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                            hasAll, notHasAll))
                    {
                        if (mark)
                            flags |= markFlags;
                        else
                            flags &= ~markFlags;

                        anyPair.Y = flags;
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the collection each package index, named
        /// by the supplied file name triplets, whose flags match the required
        /// and forbidden conditions.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.
        /// </param>
        /// <param name="fileNames">
        /// The list of package index file name triplets identifying which
        /// package indexes to consider.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a package index must have to be removed, or none to
        /// skip this check.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a package index must not have to be removed, or none
        /// to skip this check.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the required flags be present.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the forbidden flags be present
        /// before excluding a package index.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode PurgeIndexes(
            PackageIndexDictionary packageIndexes,
            PackageFileNameList fileNames,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref Result error
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            if (fileNames == null)
            {
                error = "invalid file names";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            fileNames = new PackageFileNameList(fileNames);
            fileNames.Reverse(); /* O(N) */

            string fileName; /* REUSED */
            PackageIndexAnyPair anyPair; /* REUSED */
            PackageIndexFlags flags; /* REUSED */

            foreach (PackageFileNameTriplet anyTriplet in fileNames)
            {
                fileName = anyTriplet.Y;

                if ((fileName != null) &&
                    packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                            hasAll, notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }

                fileName = anyTriplet.Z;

                if ((fileName != null) &&
                    packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                            hasAll, notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the collection each package index whose
        /// flags match the required and forbidden conditions.
        /// </summary>
        /// <param name="packageIndexes">
        /// The collection of package indexes to update.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that a package index must have to be removed, or none to
        /// skip this check.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that a package index must not have to be removed, or none
        /// to skip this check.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the required flags be present.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the forbidden flags be present
        /// before excluding a package index.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode PurgeIndexes(
            PackageIndexDictionary packageIndexes,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref Result error
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            StringList fileNames = packageIndexes.GetKeysInOrder(false);

            if (fileNames == null)
            {
                error = "failed to reorder file names for purging";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            fileNames.Reverse(); /* O(N) */

            foreach (string fileName in fileNames)
            {
                if (fileName == null)
                    continue;

                PackageIndexAnyPair anyPair;

                if (packageIndexes.TryGetValue(
                        fileName, out anyPair) &&
                    (anyPair != null))
                {
                    PackageIndexFlags flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags,
                            hasAll, notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the package indexes in the collection whose file
        /// name or containing directory refers to the same file as the
        /// specified path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when comparing files.
        /// </param>
        /// <param name="packageIndexes">
        /// The collection of package indexes to search.
        /// </param>
        /// <param name="path">
        /// The path to match against each package index file name and
        /// directory.
        /// </param>
        /// <param name="count">
        /// On input, the running count of matches; on output, this is
        /// increased by the number of matching package indexes found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode SearchIndexes(
            Interpreter interpreter,               /* in */
            PackageIndexDictionary packageIndexes, /* in */
            string path,                           /* in */
            ref int count,                         /* in, out */
            ref Result error                       /* out */
            )
        {
            if (packageIndexes == null)
            {
                error = "invalid package indexes";
                return ReturnCode.Error;
            }

            StringList fileNames = packageIndexes.GetKeysInOrder(false);

            if (fileNames == null)
            {
                error = "failed to reorder file names for marking";
                return ReturnCode.Error;
            }

            if (fileNames.Count == 0)
                return ReturnCode.Ok;

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                if (PathOps.IsSameFile(interpreter, fileName, path))
                {
                    count++;
                    continue;
                }

                string directory = PathOps.GetDirectoryName(fileName);

                if (String.IsNullOrEmpty(directory))
                    continue;

                if (PathOps.IsSameFile(interpreter, directory, path))
                {
                    count++;
                    // continue; /* REDUNDANT */
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores or unsets the directory and tag variables that
        /// were set up for a package index callback, complaining about any
        /// errors that occur.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="dirVarName">
        /// The name of the directory variable.
        /// </param>
        /// <param name="tagVarName">
        /// The name of the tag variable.
        /// </param>
        /// <param name="savedDirVarValue">
        /// The saved value of the directory variable.  Upon successful restore,
        /// this is set to null.
        /// </param>
        /// <param name="savedTagVarValue">
        /// The saved value of the tag variable.  Upon successful restore, this
        /// is set to null.
        /// </param>
        /// <param name="setDirectory">
        /// Non-zero if the directory variable was set and needs to be restored
        /// or unset.  Upon successful handling, this is set to zero.
        /// </param>
        /// <param name="setTag">
        /// Non-zero if the tag variable was set and needs to be restored or
        /// unset.  Upon successful handling, this is set to zero.
        /// </param>
        private static void UnsetIndexCallbackVariables(
            Interpreter interpreter,     /* in */
            string dirVarName,           /* in */
            string tagVarName,           /* in */
            ref string savedDirVarValue, /* in, out */
            ref string savedTagVarValue, /* in, out */
            ref bool setDirectory,       /* in, out */
            ref bool setTag              /* in, out */
            )
        {
            ResultList errors = null;

            try
            {
                Result error; /* REUSED */

                if (interpreter == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid interpreter");
                    return;
                }

                if (setDirectory)
                {
                    if (savedDirVarValue != null)
                    {
                        error = null;

                        if (interpreter.SetVariableValue( /* EXEMPT */
                                VariableFlags.None, dirVarName,
                                savedDirVarValue,
                                ref error) == ReturnCode.Ok)
                        {
                            savedDirVarValue = null;
                            setDirectory = false;
                        }
                        else if (error != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(error);
                        }
                    }
                    else
                    {
                        error = null;

                        if (interpreter.UnsetVariable( /* EXEMPT */
                                VariableFlags.None, dirVarName,
                                ref error) == ReturnCode.Ok)
                        {
                            setDirectory = false;
                        }
                        else if (error != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(error);
                        }
                    }
                }

                if (setTag)
                {
                    if (savedTagVarValue != null)
                    {
                        error = null;

                        if (interpreter.SetVariableValue( /* EXEMPT */
                                VariableFlags.None, tagVarName,
                                savedTagVarValue,
                                ref error) == ReturnCode.Ok)
                        {
                            savedTagVarValue = null;
                            setTag = false;
                        }
                        else if (error != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(error);
                        }
                    }
                    else
                    {
                        error = null;

                        if (interpreter.UnsetVariable( /* EXEMPT */
                                VariableFlags.None, tagVarName,
                                ref error) == ReturnCode.Ok)
                        {
                            setTag = false;
                        }
                        else if (error != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(error);
                        }
                    }
                }
            }
            finally
            {
                if (errors != null)
                {
                    DebugOps.Complain(
                        interpreter, ReturnCode.Error, errors);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the directory and tag variables used by a
        /// package index callback, saving the previous values of any variables
        /// that already existed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="dirVarName">
        /// The name of the directory variable.
        /// </param>
        /// <param name="tagVarName">
        /// The name of the tag variable.
        /// </param>
        /// <param name="fileName">
        /// The file name whose directory is used to set the directory
        /// variable, if any.  This parameter may be null.
        /// </param>
        /// <param name="tag">
        /// The tag value used to set the tag variable, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="savedDirVarValue">
        /// Upon return, this contains the saved value of the directory
        /// variable, if it existed.
        /// </param>
        /// <param name="savedTagVarValue">
        /// Upon return, this contains the saved value of the tag variable, if
        /// it existed.
        /// </param>
        /// <param name="setDirectory">
        /// Upon return, this is set to non-zero if the directory variable was
        /// set and should later be restored or unset.
        /// </param>
        /// <param name="setTag">
        /// Upon return, this is set to non-zero if the tag variable was set
        /// and should later be restored or unset.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode SetIndexCallbackVariables(
            Interpreter interpreter,     /* in */
            string dirVarName,           /* in */
            string tagVarName,           /* in */
            string fileName,             /* in */
            string tag,                  /* in */
            ref string savedDirVarValue, /* in, out */
            ref string savedTagVarValue, /* in, out */
            ref bool setDirectory,       /* out */
            ref bool setTag,             /* out */
            ref Result result            /* out */
            )
        {
            ResultList errors = null;
            Result error; /* REUSED */

            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (fileName != null)
            {
                bool? hadDirectory = null;

                if (savedDirVarValue == null)
                {
                    Result dirVarValue = null;

                    if (interpreter.GetVariableValue(
                            VariableFlags.None, dirVarName,
                            ref dirVarValue) == ReturnCode.Ok)
                    {
                        savedDirVarValue = dirVarValue;
                        hadDirectory = true;
                    }
                    else
                    {
                        hadDirectory = false;
                    }
                }

                string directory = PathOps.GetUnixPath(
                    PathOps.GetDirectoryName(fileName));

                error = null;

                if (interpreter.SetVariableValue( /* EXEMPT */
                        VariableFlags.None, dirVarName,
                        directory, ref error) == ReturnCode.Ok)
                {
                    //
                    // BUGFIX: Do not allow the "dir" variable
                    //         to be unset if it existed prior
                    //         to us changing its value.
                    //
                    if (hadDirectory != null)
                        setDirectory = true;
                }
                else if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }
            }

            if (tag != null)
            {
                bool? hadTag = null;

                if (savedTagVarValue == null)
                {
                    Result tagVarValue = null;

                    if (interpreter.GetVariableValue(
                            VariableFlags.None, tagVarName,
                            ref tagVarValue) == ReturnCode.Ok)
                    {
                        savedTagVarValue = tagVarValue;
                        hadTag = true;
                    }
                    else
                    {
                        hadTag = false;
                    }
                }

                error = null;

                if (interpreter.SetVariableValue( /* EXEMPT */
                        VariableFlags.None, tagVarName,
                        tag, ref error) == ReturnCode.Ok)
                {
                    //
                    // BUGFIX: Do not allow the "tag" variable
                    //         to be unset if it existed prior
                    //         to us changing its value.
                    //
                    if (hadTag != null)
                        setTag = true;
                }
                else if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }
            }

            if (errors != null)
            {
                result = errors;
                return ReturnCode.Error;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds, if permitted by the supplied rule set, the loader
        /// command used to lazily create the "package ifneeded" command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="plugin">
        /// The plugin that will own the added command.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to decide whether the command should be added, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode MaybeAddLoaderCommand(
            Interpreter interpreter,
            IPlugin plugin,
            IRuleSet ruleSet,
            ref Result error
            ) /* ENTRY-POINT */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            if ((ruleSet != null) && !ruleSet.ApplyRules(interpreter,
                    IdentifierKind.Command, MatchMode.IncludeRuleSetMask,
                    ScriptOps.MakeCommandName(loaderCommand)))
            {
                return ReturnCode.Ok;
            }

            //
            // HACK: Both the [maybeCreatePackageIfNeededCommand] command
            //       provided by this class and its associated procedures
            //       defined by the core loader script package are "safe"
            //       because they only construct [load] commands and/or
            //       [package ifneeded] commands to be evaluated later.
            //
            long token = 0; /* NOT USED */

            return interpreter.AddExecuteCallback(
                loaderCommand, LoaderCommandCallback, null, plugin,
                CommandFlags.Safe, ref token, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /* Eagle._Components.Public.Delegates.ExecuteCallback */
        /// <summary>
        /// This method is the command callback that bootstraps and forwards to
        /// the loader command, ensuring the package loader has been
        /// initialized before delegating to the real loader command
        /// implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when the command was invoked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list supplied to the command.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of the forwarded command, or
        /// an appropriate error message on failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode LoaderCommandCallback(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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

            int argumentCount = arguments.Count;

            if (argumentCount < 1)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} name dir " +
                    "fileNamesOnly tagVarName ?version?\"",
                    NamespaceOps.TrimLeading(loaderCommand));

                return ReturnCode.Error;
            }

            IExecute oldExecute = null;

            if (interpreter.InternalGetIExecuteViaResolvers(
                    interpreter.GetResolveEngineFlagsNoLock(true),
                    arguments[0], arguments, LookupFlags.Default,
                    ref oldExecute, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (interpreter.InternalInitializeLoader(
                    false, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            ArgumentList newArguments = new ArgumentList();

            newArguments.Add(loaderCommand);
            newArguments.AddRange(ArgumentList.GetRange(arguments, 1));

            IExecute newExecute = null;

            if (interpreter.InternalGetIExecuteViaResolvers(
                    interpreter.GetResolveEngineFlagsNoLock(true),
                    newArguments[0], newArguments, LookupFlags.Default,
                    ref newExecute, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (Object.ReferenceEquals(newExecute, oldExecute))
            {
                result = "loader command bootstrap failed?";
                return ReturnCode.Error;
            }

#if ARGUMENT_CACHE
            CacheFlags savedCacheFlags;

            interpreter.BeginNoArgumentCache(out savedCacheFlags);

            try
            {
#endif
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                InterpreterStateFlags savedInterpreterStateFlags;

                interpreter.BeginArgumentLocation(
                    null, out savedInterpreterStateFlags);

                try
                {
#endif
                    return newExecute.Execute(
                        interpreter, clientData, newArguments,
                        ref result);
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                }
                finally
                {
                    interpreter.EndArgumentLocation(
                        ref savedInterpreterStateFlags);
                }
#endif
#if ARGUMENT_CACHE
            }
            finally
            {
                interpreter.EndNoArgumentCache(ref savedCacheFlags);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds, if not already present and if permitted by the
        /// supplied rule set, the wrapper command that forwards to the core
        /// "source" command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="plugin">
        /// The plugin that will own the added command.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to decide whether the command should be added, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode MaybeAddSourceWithInfoCommand(
            Interpreter interpreter,
            IPlugin plugin,
            IRuleSet ruleSet,
            ref Result error
            ) /* ENTRY-POINT */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            if (interpreter.InternalDoesIExecuteExistViaResolvers(
                    sourceWithInfoCommand) == ReturnCode.Ok)
            {
                return ReturnCode.Ok;
            }

            if ((ruleSet != null) && !ruleSet.ApplyRules(interpreter,
                    IdentifierKind.Command, MatchMode.IncludeRuleSetMask,
                    ScriptOps.MakeCommandName(sourceWithInfoCommand)))
            {
                return ReturnCode.Ok;
            }

            long token = 0; /* NOT USED */

            return interpreter.AddExecuteCallback(
                sourceWithInfoCommand, SourceWithInfoCallback,
                null, plugin, ref token, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /* Eagle._Components.Public.Delegates.ExecuteCallback */
        /// <summary>
        /// This method is the command callback that forwards the
        /// "sourceWithInfo" command to the core "source" command, preserving
        /// any supplied options and the file name argument.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when the command was invoked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The argument list supplied to the command.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of the forwarded command, or
        /// an appropriate error message on failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode SourceWithInfoCallback(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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

            int argumentCount = arguments.Count;

            if (argumentCount < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} ?options? fileName\"",
                    NamespaceOps.TrimLeading(sourceWithInfoCommand));

                return ReturnCode.Error;
            }

            ArgumentList newArguments = new ArgumentList(argumentCount);

            newArguments.Add(sourceCommand);
            newArguments.AddRange(ArgumentList.GetRange(arguments, 1));

            IExecute newExecute = null;

            if (interpreter.InternalGetIExecuteViaResolvers(
                    interpreter.GetResolveEngineFlagsNoLock(true),
                    newArguments[0], newArguments, LookupFlags.Default,
                    ref newExecute, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

#if ARGUMENT_CACHE
            CacheFlags savedCacheFlags;

            interpreter.BeginNoArgumentCache(out savedCacheFlags);

            try
            {
#endif
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                InterpreterStateFlags savedInterpreterStateFlags;

                interpreter.BeginArgumentLocation(
                    null, out savedInterpreterStateFlags);

                try
                {
#endif
                    return newExecute.Execute(
                        interpreter, clientData, newArguments,
                        ref result);
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                }
                finally
                {
                    interpreter.EndArgumentLocation(
                        ref savedInterpreterStateFlags);
                }
#endif
#if ARGUMENT_CACHE
            }
            finally
            {
                interpreter.EndNoArgumentCache(ref savedCacheFlags);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the default package index callback, which evaluates
        /// the package index for a host-provided, plugin assembly, or file
        /// system source, setting up the directory and tag variables as
        /// necessary and recording that the index was evaluated.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.
        /// </param>
        /// <param name="path">
        /// The directory path associated with the package index, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name of the package index to evaluate.
        /// </param>
        /// <param name="tag">
        /// The tag associated with the package index, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="packageType">
        /// The type of the package index being processed.
        /// </param>
        /// <param name="flags">
        /// The package index flags controlling how the index is evaluated.
        /// Upon return, this may have the evaluated flag added.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with the package index, if any.  Upon
        /// return, this may contain updated client data.  This parameter may
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode IndexCallback( /* PackageIndexCallback */
            Interpreter interpreter,     /* in */
            string path,                 /* in */
            string fileName,             /* in */
            string tag,                  /* in */
            PackageType packageType,     /* in */
            ref PackageIndexFlags flags, /* in, out */
            ref IClientData clientData,  /* in, out */
            ref Result result            /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;
            string savedDirVarValue = null;
            string savedTagVarValue = null;
            bool setDirectory = false;
            bool setTag = false;

            try
            {
                bool host = FlagOps.HasFlags(
                    flags, PackageIndexFlags.HostMask, false);

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                bool plugin = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Plugin, true);
#else
                bool plugin = false;
#endif

                bool noNormal = FlagOps.HasFlags(
                    flags, PackageIndexFlags.NoNormal, true);

                bool refresh = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Refresh, true);

                bool resolve = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Resolve, true);

                bool trace = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Trace, true);

                bool verbose = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Verbose, true);

                bool safe = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Safe, true);

                bool noComplain = FlagOps.HasFlags(
                    flags, PackageIndexFlags.NoComplain, true);

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                bool stopOnError = FlagOps.HasFlags(
                    flags, PackageIndexFlags.StopOnError, true);
#endif

                if (host)
                {
                    //
                    // NOTE: It is important to note here that currently
                    //       there may only be a maximum of ONE package
                    //       index file provided by the host.
                    //
                    ScriptFlags scriptFlags = ScriptOps.GetFlags(
                        interpreter, IndexScriptFlags, packageType,
                        false, noNormal);

                    //
                    // BUGFIX: This should not be hard-coded to use the
                    //         "pkgIndex.eagle" file name.  Instead, it
                    //         should use the file name provided by the
                    //         caller (which is still "pkgIndex.eagle").
                    //
                    string text = interpreter.GetScript(
                        fileName, ref scriptFlags, ref clientData);

                    if (!String.IsNullOrEmpty(text))
                    {
                        if (FlagOps.HasFlags(
                                scriptFlags, ScriptFlags.File, true))
                        {
                            bool remoteUri = PathOps.IsRemoteUri(text);

                            if (remoteUri || File.Exists(text))
                            {
                                string newText = text;

                                if (resolve && !remoteUri)
                                {
                                    //
                                    // NOTE: Attempt to resolve the file
                                    //       name to a fully qualified
                                    //       one.
                                    //
                                    newText = PathOps.ResolveFullPath(
                                        interpreter, newText);

                                    //
                                    // NOTE: Failing that, fallback to
                                    //       the original file name which
                                    //       has already been "validated".
                                    //
                                    if (String.IsNullOrEmpty(newText))
                                        newText = text;
                                }

                                //
                                // NOTE: The host for the interpreter seems
                                //       to indicate we should be able to
                                //       find the package index on the native
                                //       file system?  Ok, fine.  Setup the
                                //       directory variable properly.
                                //
                                code = SetIndexCallbackVariables(
                                    interpreter, TclVars.Core.Directory,
                                    TclVars.Core.Tag, newText, tag,
                                    ref savedDirVarValue, ref savedTagVarValue,
                                    ref setDirectory, ref setTag, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    /* IGNORED */
                                    interpreter.EnterPackageIndexLevel();

                                    try
                                    {
                                        if (safe && !interpreter.InternalIsSafe())
                                        {
                                            code = interpreter.EvaluateSafeFile(
                                                null, newText, ref result);
                                        }
                                        else
                                        {
                                            code = interpreter.EvaluateFile(
                                                newText, ref result);
                                        }
                                    }
                                    finally
                                    {
                                        /* IGNORED */
                                        interpreter.ExitPackageIndexLevel();
                                    }

                                    flags |= PackageIndexFlags.Evaluated;

                                    if (trace)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "IndexCallback: interpreter = {0}, " +
                                            "path = {1}, fileName = {2}, " +
                                            "flags = {3}, host = {4}, " +
                                            "plugin = {5}, noNormal = {6}, " +
                                            "refresh = {7}, resolve = {8}, " +
                                            "trace = {9}, verbose = {10}, " +
                                            "newText = {11}, code = {12}, " +
                                            "result = {13}",
                                            FormatOps.InterpreterNoThrow(
                                                interpreter),
                                            FormatOps.WrapOrNull(path),
                                            FormatOps.WrapOrNull(fileName),
                                            FormatOps.WrapOrNull(flags),
                                            host, plugin, noNormal, refresh,
                                            resolve, trace, verbose,
                                            FormatOps.WrapOrNull(newText),
                                            code, FormatOps.WrapOrNull(
                                                true, true, result)),
                                            typeof(PackageOps).Name,
                                            TracePriority.PackageDebug);
                                    }

                                    if (noComplain && (code != ReturnCode.Ok))
                                        code = ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "provided {0} script file {1} is not " +
                                    "a valid remote uri and does not exist " +
                                    "locally", FormatOps.WrapOrNull(
                                        ScriptTypes.PackageIndex),
                                    FormatOps.WrapOrNull(text));

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            code = SetIndexCallbackVariables(
                                interpreter, TclVars.Core.Directory,
                                TclVars.Core.Tag, fileName, tag,
                                ref savedDirVarValue, ref savedTagVarValue,
                                ref setDirectory, ref setTag, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                /* IGNORED */
                                interpreter.EnterPackageIndexLevel();

                                try
                                {
                                    //
                                    // BUGFIX: Use the original script [file?]
                                    //         name, exactly as specified, for
                                    //         any contained [info script] calls.
                                    //
                                    bool pushed = false;

                                    interpreter.PushScriptLocation(
                                        fileName, true, ref pushed);

                                    try
                                    {
                                        if (safe && !interpreter.InternalIsSafe())
                                        {
                                            code = interpreter.EvaluateSafeScript(
                                                text, ref result); /* EXEMPT */
                                        }
                                        else
                                        {
                                            code = interpreter.EvaluateScript(
                                                text, ref result); /* EXEMPT */
                                        }
                                    }
                                    finally
                                    {
                                        interpreter.PopScriptLocation(
                                            true, ref pushed);
                                    }
                                }
                                finally
                                {
                                    /* IGNORED */
                                    interpreter.ExitPackageIndexLevel();
                                }

                                flags |= PackageIndexFlags.Evaluated;

                                if (trace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IndexCallback: interpreter = {0}, " +
                                        "path = {1}, fileName = {2}, " +
                                        "flags = {3}, host = {4}, " +
                                        "plugin = {5}, noNormal = {6}, " +
                                        "refresh = {7}, resolve = {8}, " +
                                        "trace = {9}, verbose = {10}, " +
                                        "text = {11}, code = {12}, " +
                                        "result = {13}",
                                        FormatOps.InterpreterNoThrow(
                                            interpreter),
                                        FormatOps.WrapOrNull(path),
                                        FormatOps.WrapOrNull(fileName),
                                        FormatOps.WrapOrNull(flags),
                                        host, plugin, noNormal, refresh,
                                        resolve, trace, verbose,
                                        FormatOps.WrapOrNull(text),
                                        code, FormatOps.WrapOrNull(
                                            true, true, result)),
                                        typeof(PackageOps).Name,
                                        TracePriority.PackageDebug);
                                }

                                if (noComplain && (code != ReturnCode.Ok))
                                    code = ReturnCode.Ok;
                            }
                        }
                    }
                    else
                    {
                        //
                        // NOTE: This is optional; therefore, success.
                        //
                        code = ReturnCode.Ok;
                    }
                }
                else if (plugin)
                {
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        if (File.Exists(fileName))
                        {
                            string newFileName = fileName;

                            if (resolve)
                            {
                                //
                                // NOTE: Attempt to resolve the file name
                                //       to a fully qualified one.
                                //
                                newFileName = PathOps.ResolveFullPath(
                                    interpreter, newFileName);

                                //
                                // NOTE: Failing that, fallback to the
                                //       original file name which has
                                //       already been "validated".
                                //
                                if (String.IsNullOrEmpty(newFileName))
                                    newFileName = fileName;
                            }

                            code = SetIndexCallbackVariables(
                                interpreter, TclVars.Core.Directory,
                                TclVars.Core.Tag, newFileName, tag,
                                ref savedDirVarValue, ref savedTagVarValue,
                                ref setDirectory, ref setTag, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Load assembly file for reflection
                                //       use via RuntimeOps, grab resource
                                //       names matching "*/pkgIndex.eagle",
                                //       and evaluate them all.
                                //
                                int count = 0;
                                PluginDictionary resources = null;

                                code = RuntimeOps.PreviewPluginResources(
                                    interpreter, newFileName, GetIndexPatterns(),
                                    interpreter.PluginFlags, verbose, ref resources,
                                    ref result);

                                if ((code == ReturnCode.Ok) &&
                                    (resources != null))
                                {
                                    foreach (PluginPair pair in resources)
                                    {
                                        byte[] bytes = pair.Value;

                                        if (bytes == null)
                                            continue;

                                        ReturnCode localCode; /* REUSED */
                                        Result localResult; /* REUSED */
                                        string text = null;

                                        localResult = null;

                                        localCode = Engine.ReadScriptBytes(
                                            interpreter, pair.Key, bytes,
                                            ref text, ref localResult);

                                        if (localCode != ReturnCode.Ok)
                                        {
                                            if (trace && verbose)
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "IndexCallback: plugin resource " +
                                                    "string error, localCode = {0}, " +
                                                    "localResult = {1}", localCode,
                                                    FormatOps.WrapOrNull(localResult)),
                                                    typeof(PackageOps).Name,
                                                    TracePriority.PackageError);
                                            }

                                            if (stopOnError)
                                            {
                                                result = localResult;
                                                code = localCode;

                                                break;
                                            }

                                            continue;
                                        }

                                        /* IGNORED */
                                        interpreter.EnterPackageIndexLevel();

                                        try
                                        {
                                            localResult = null;

                                            if (safe && !interpreter.InternalIsSafe())
                                            {
                                                localCode = interpreter.EvaluateSafeScript(
                                                    text, ref localResult);
                                            }
                                            else
                                            {
                                                localCode = interpreter.EvaluateScript(
                                                    text, ref localResult);
                                            }
                                        }
                                        finally
                                        {
                                            /* IGNORED */
                                            interpreter.ExitPackageIndexLevel();
                                        }

                                        if (trace && verbose)
                                        {
                                            TraceOps.DebugTrace(String.Format(
                                                "IndexCallback: interpreter = {0}, " +
                                                "path = {1}, fileName = {2}, " +
                                                "flags = {3}, host = {4}, " +
                                                "plugin = {5}, noNormal = {6}, " +
                                                "refresh = {7}, resolve = {8}, " +
                                                "trace = {9}, verbose = {10}, " +
                                                "text = {11}, code = {12}, " +
                                                "result = {13}",
                                                FormatOps.InterpreterNoThrow(
                                                    interpreter),
                                                FormatOps.WrapOrNull(pair.Key),
                                                FormatOps.WrapOrNull(flags),
                                                host, plugin, noNormal, refresh,
                                                resolve, trace, verbose,
                                                FormatOps.WrapOrNull(text),
                                                localCode, FormatOps.WrapOrNull(
                                                    true, true, localResult)),
                                                typeof(PackageOps).Name,
                                                TracePriority.PackageDebug);
                                        }

                                        if (localCode != ReturnCode.Ok)
                                        {
                                            if (trace && verbose)
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "IndexCallback: plugin resource " +
                                                    "script error, localCode = {0}, " +
                                                    "localResult = {1}", localCode,
                                                    FormatOps.WrapOrNull(localResult)),
                                                    typeof(PackageOps).Name,
                                                    TracePriority.PackageError);
                                            }

                                            if (stopOnError)
                                            {
                                                result = localResult;
                                                code = localCode;

                                                break;
                                            }
                                        }

                                        count++;
                                    }
                                }

                                if (count > 0)
                                    flags |= PackageIndexFlags.Evaluated;

                                if (trace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IndexCallback: interpreter = {0}, " +
                                        "path = {1}, fileName = {2}, " +
                                        "flags = {3}, host = {4}, " +
                                        "plugin = {5}, noNormal = {6}, " +
                                        "refresh = {7}, resolve = {8}, " +
                                        "trace = {9}, verbose = {10}, " +
                                        "newFileName = {11}, code = {12}, " +
                                        "result = {13}",
                                        FormatOps.InterpreterNoThrow(
                                            interpreter),
                                        FormatOps.WrapOrNull(path),
                                        FormatOps.WrapOrNull(fileName),
                                        FormatOps.WrapOrNull(flags),
                                        host, plugin, noNormal, refresh,
                                        resolve, trace, verbose,
                                        FormatOps.WrapOrNull(newFileName),
                                        code, FormatOps.WrapOrNull(
                                            true, true, result)),
                                        typeof(PackageOps).Name,
                                        TracePriority.PackageDebug);
                                }

                                if (noComplain && (code != ReturnCode.Ok))
                                    code = ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "provided {0} plugin file {1} does not " +
                                "exist locally", FormatOps.WrapOrNull(
                                    ScriptTypes.PackageIndex),
                                FormatOps.WrapOrNull(fileName));

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "provided {0} plugin file is invalid",
                            FormatOps.WrapOrNull(
                                ScriptTypes.PackageIndex));

                        code = ReturnCode.Error;
                    }
#else
                    result = "not implemented";
                    code = ReturnCode.Error;
#endif
                }
                else
                {
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        bool remoteUri = PathOps.IsRemoteUri(fileName);

                        if (remoteUri || File.Exists(fileName))
                        {
                            string newFileName = fileName;

                            if (resolve && !remoteUri)
                            {
                                //
                                // NOTE: Attempt to resolve the file name
                                //       to a fully qualified one.
                                //
                                newFileName = PathOps.ResolveFullPath(
                                    interpreter, newFileName);

                                //
                                // NOTE: Failing that, fallback to the
                                //       original file name which has
                                //       already been "validated".
                                //
                                if (String.IsNullOrEmpty(newFileName))
                                    newFileName = fileName;
                            }

                            code = SetIndexCallbackVariables(
                                interpreter, TclVars.Core.Directory,
                                TclVars.Core.Tag, newFileName, tag,
                                ref savedDirVarValue, ref savedTagVarValue,
                                ref setDirectory, ref setTag, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                /* IGNORED */
                                interpreter.EnterPackageIndexLevel();

                                try
                                {
                                    if (safe && !interpreter.InternalIsSafe())
                                    {
                                        code = interpreter.EvaluateSafeFile(
                                            null, newFileName, ref result);
                                    }
                                    else
                                    {
                                        code = interpreter.EvaluateFile(
                                            newFileName, ref result);
                                    }
                                }
                                finally
                                {
                                    /* IGNORED */
                                    interpreter.ExitPackageIndexLevel();
                                }

                                flags |= PackageIndexFlags.Evaluated;

                                if (trace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IndexCallback: interpreter = {0}, " +
                                        "path = {1}, fileName = {2}, " +
                                        "flags = {3}, host = {4}, " +
                                        "plugin = {5}, noNormal = {6}, " +
                                        "refresh = {7}, resolve = {8}, " +
                                        "trace = {9}, verbose = {10}, " +
                                        "newFileName = {11}, code = {12}, " +
                                        "result = {13}",
                                        FormatOps.InterpreterNoThrow(
                                            interpreter),
                                        FormatOps.WrapOrNull(path),
                                        FormatOps.WrapOrNull(fileName),
                                        FormatOps.WrapOrNull(flags),
                                        host, plugin, noNormal, refresh,
                                        resolve, trace, verbose,
                                        FormatOps.WrapOrNull(newFileName),
                                        code, FormatOps.WrapOrNull(
                                            true, true, result)),
                                        typeof(PackageOps).Name,
                                        TracePriority.PackageDebug);
                                }

                                if (noComplain && (code != ReturnCode.Ok))
                                    code = ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "provided {0} script file {1} is not " +
                                "a valid remote uri and does not exist " +
                                "locally", FormatOps.WrapOrNull(
                                    ScriptTypes.PackageIndex),
                                FormatOps.WrapOrNull(fileName));

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "provided {0} script file is invalid",
                            FormatOps.WrapOrNull(
                                ScriptTypes.PackageIndex));

                        code = ReturnCode.Error;
                    }
                }
            }
            catch (Exception e)
            {
                result = String.Format(
                    "caught exception while sourcing package index: {0}",
                    e);

                code = ReturnCode.Error;
            }
            finally
            {
                UnsetIndexCallbackVariables(
                    interpreter, TclVars.Core.Directory,
                    TclVars.Core.Tag, ref savedDirVarValue,
                    ref savedTagVarValue, ref setDirectory,
                    ref setTag);
            }

            return code;
        }
        #endregion
    }
}
