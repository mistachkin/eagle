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
using System.IO;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

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
    [ObjectId("e6e6c799-cbfd-4aa3-9017-c9944322a81c")]
    internal static class PackageOps
    {
        #region Private Constants
        //
        // NOTE: These are the ScriptFlags that are *always* used when trying
        //       to fetch the "pkgIndex.eagle" file via the interpreter host.
        //
        private static readonly ScriptFlags IndexScriptFlags =
            ScriptFlags.PackageLibraryOptionalFile;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string sourceCommand = "::source";
        private static string sourceWithInfoCommand = "::sourceWithInfo";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string HostListFileName = "hostPackageIndexes";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Checking Methods
        public static int VersionCompare(
            Version version1,
            Version version2
            )
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

        public static bool VersionSatisfies(
            Version version1,
            Version version2,
            bool exact
            )
        {
            if (exact)
                return (VersionCompare(version1, version2) == 0);
            else
                return (VersionCompare(version1, version2) >= 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
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
            )
        {
            return new _Packages.Core(new PackageData(
                name, group, description, clientData, indexFileName,
                provideFileName, flags, loaded, ifNeeded, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Support Methods
        public static string GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            bool verbose                           /* in */
            )
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

        public static ReturnCode GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            ref string fileName,                   /* out */
            ref Result error                       /* out */
            )
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
                packageIndexFileNames.Sort(_Comparers.FileName.Create(
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

        private static ReturnCode InvokeCallback(
            Interpreter interpreter,                 /* in */
            PackageIndexCallback callback,           /* in */
            string path,                             /* in */
            string fileName,                         /* in */
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
                interpreter.SetPendingPackageIndexes(true);

                try
                {
                    PackageIndexFlags flags = initialFlags;
                    IClientData clientData = ClientData.Empty;
                    Result result = null;

                    if (callback(
                            interpreter, path, fileName,
                            ref flags, ref clientData,
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
                    interpreter.SetPendingPackageIndexes(false);
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
                interpreter, packageIndexFlags);

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
                    packageIndexes, fileNames, PackageIndexFlags.Host,
                    PackageIndexFlags.NonHostMask,
                    PackageIndexFlags.Found, true, false,
                    false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            packageIndexFlags &= ~PackageIndexFlags.NonHostMask;
            packageIndexFlags |= PackageIndexFlags.Host;

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Host |
                PackageIndexFlags.Found;

            //
            // NOTE: If we are refreshing package indexes or we have
            //       never seen this package index before, notify the
            //       caller.
            //
            bool refresh = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Refresh, true);

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
                string relativeFileName = anyTriplet.Y;
                string fileName = anyTriplet.Z;

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
                            relativeFileName, packageIndexFlags,
                            packageIndexes, packageContext,
                            addFlags, ref purge,
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

            //
            // NOTE: Purge any package indexes from the list that are
            //       still marked as "not found".
            //
            if (PurgeIndexes(
                    packageIndexes, fileNames, PackageIndexFlags.Host,
                    PackageIndexFlags.NonHostMask |
                    PackageIndexFlags.Found, true, false,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
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
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Plugin |
                PackageIndexFlags.Found;

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

#if NATIVE
            bool noTrusted = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoTrusted, true);

            bool noVerified = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoVerified, true);
#endif

            bool noSort = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoSort, true);

            //
            // NOTE: Create a string comparer for file names, used to
            //       sort them.
            //
            IComparer<string> comparer = null;

            if (!noSort &&
                (pathComparisonType != PathComparisonType.BuiltIn))
            {
                comparer = _Comparers.FileName.Create(
                    pathComparisonType);
            }

            foreach (string path in paths)
            {
                //
                // NOTE: Normalize the path prior to using it or adding
                //       it to the dictionary.
                //
                string newPath = PathOps.ResolveFullPath(interpreter,
                    path);

                //
                // HACK: If path has been expicitly disabled, skip it.
                //
                if (IsDisabled(newPath))
                    continue;

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
                                        FileOps.GetSearchOption(
                                            recursive)));
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(PackageOps).Name,
                                    TracePriority.FileSystemError);

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
                                    error = localError;
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
                                // HACK: If this name has been expicitly
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

#if NATIVE
                                //
                                // BUGBUG: This only work on Windows with
                                //         native code enabled.
                                //
                                if (PlatformOps.IsWindowsOperatingSystem())
                                {
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
                                        !RuntimeOps.IsStrongNameVerified(
                                            fileName, true))
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
                                }
#endif

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
                                            fileName, packageIndexFlags,
                                            packageIndexes, packageContext,
                                            addFlags, ref purge,
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
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Normal |
                PackageIndexFlags.Found;

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

            bool verbose = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Verbose, true);

            bool noSort = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.NoSort, true);

            //
            // NOTE: Create a string comparer for file names, used to
            //       sort them.
            //
            IComparer<string> comparer = null;

            if (!noSort &&
                (pathComparisonType != PathComparisonType.BuiltIn))
            {
                comparer = _Comparers.FileName.Create(
                    pathComparisonType);
            }

            foreach (string path in paths)
            {
                //
                // NOTE: Normalize the path prior to using it or adding
                //       it to the dictionary.
                //
                string newPath = PathOps.ResolveFullPath(interpreter,
                    path);

                //
                // HACK: If path has been expicitly disabled, skip it.
                //
                if (IsDisabled(newPath))
                    continue;

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
                    StringList fileNames = null;
                    Result localError = null;

                    try
                    {
                        fileNames = new StringList(
                            Directory.GetFiles(newPath,
                                GetIndexFileName(interpreter,
                                    PackageType.None,
                                    false) /* PATTERN */,
                                FileOps.GetSearchOption(
                                    recursive)));
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(PackageOps).Name,
                            TracePriority.FileSystemError);

                        localError = e;
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
                            error = localError;
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
                        // HACK: If this name has been expicitly
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
                            if (InvokeCallback(
                                    interpreter, callback, newPath,
                                    fileName, packageIndexFlags,
                                    packageIndexes, packageContext,
                                    addFlags, ref purge,
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
                    {
                        fileName = FileNameOnly.PackageIndex;
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

        private static PackageFileNameList GetIndexFileNames(
            Interpreter interpreter,            /* in */
            PackageIndexFlags packageIndexFlags /* in */
            )
        {
            PackageFileNameList fileNames = new PackageFileNameList();

            foreach (PackageType packageType in new PackageType[] {
                    PackageType.Library, PackageType.Test })
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
                ScriptFlags scriptFlags = ScriptOps.GetFlags(
                    interpreter, IndexScriptFlags, false, true);

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

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
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

        private static StringList GetIndexPatterns()
        {
            //
            // NOTE: Return the bare and fully qualified patterns used
            //       to find package index files, e.g. "pkgIndex.eagle"
            //       and "*/pkgIndex.eagle".
            //
            StringList list = new StringList();

            list.Add(FileNameOnly.LibraryPackageIndex);

            list.Add(PathOps.GetUnixPath(PathOps.CombinePath(
                null, Characters.Asterisk.ToString(),
                FileNameOnly.LibraryPackageIndex)));

            return list;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode FindAll(
            Interpreter interpreter,                   /* in */
            StringList paths,                          /* in */
            PackageIndexFlags packageIndexFlags,       /* in */
            PathComparisonType pathComparisonType,     /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            )
        {
            PackageContextClientData packageContext = null;

            return FindAll(
                interpreter, paths, packageIndexFlags, pathComparisonType,
                ref packageIndexes, ref packageContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FindAll(
            Interpreter interpreter,                     /* in */
            StringList paths,                            /* in */
            PackageIndexFlags packageIndexFlags,         /* in */
            PathComparisonType pathComparisonType,       /* in */
            ref PackageIndexDictionary packageIndexes,   /* in, out */
            ref PackageContextClientData packageContext, /* in, out */
            ref Result error                             /* out */
            )
        {
            bool host = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Host, true);

            bool normal = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Normal, true);

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            bool plugin = FlagOps.HasFlags(
                packageIndexFlags, PackageIndexFlags.Plugin, true);
#endif

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
                            PackageIndexFlags.AllowDuplicate, true) &&
                        (RemoveLogicalDuplicates(
                            interpreter, ref packageIndexes,
                            ref error) != ReturnCode.Ok))
                    {
                        return ReturnCode.Error;
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
                            PackageIndexFlags.AllowDuplicate, true) &&
                        (RemoveLogicalDuplicates(
                            interpreter, ref packageIndexes,
                            ref error) != ReturnCode.Ok))
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
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
                    packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
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

                if (packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    PackageIndexFlags flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
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
                    packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }

                fileName = anyTriplet.Z;

                if ((fileName != null) &&
                    packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

                if (packageIndexes.TryGetValue(fileName, out anyPair) &&
                    (anyPair != null))
                {
                    PackageIndexFlags flags = anyPair.Y;

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
                    {
                        packageIndexes.Remove(fileName);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetIndexCallbackDirectory(
            Interpreter interpreter, /* in */
            string varName,          /* in */
            ref bool setDirectory    /* in, out */
            )
        {
            if ((interpreter != null) && setDirectory)
            {
                ReturnCode code;
                Result error = null;

                code = interpreter.UnsetVariable( /* EXEMPT */
                    VariableFlags.None, varName, ref error);

                if (code == ReturnCode.Ok)
                    setDirectory = false;
                else
                    DebugOps.Complain(interpreter, code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetIndexCallbackDirectory(
            Interpreter interpreter, /* in */
            string varName,          /* in */
            string fileName,         /* in */
            ref bool setDirectory,   /* out */
            ref Result result        /* out */
            )
        {
            if (interpreter != null)
            {
                string directory = PathOps.GetUnixPath(
                    PathOps.GetDirectoryName(fileName));

                Result error = null;

                if (interpreter.SetVariableValue( /* EXEMPT */
                        VariableFlags.None, varName, directory,
                        ref error) != ReturnCode.Ok)
                {
                    result = error;
                    return ReturnCode.Error;
                }

                setDirectory = true;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode MaybeAddSourceWithInfoCommand(
            Interpreter interpreter,
            IPlugin plugin,
            IRuleSet ruleSet,
            ref Result error
            )
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

            if (interpreter.DoesIExecuteExistViaResolvers(
                    sourceWithInfoCommand) == ReturnCode.Ok)
            {
                return ReturnCode.Ok;
            }

            if ((ruleSet != null) && !ruleSet.ApplyRules(
                    interpreter, IdentifierKind.Command,
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

            IExecute execute = null;

            if (interpreter.GetIExecuteViaResolvers(
                    interpreter.GetResolveEngineFlagsNoLock(true),
                    newArguments[0], newArguments,
                    LookupFlags.Default, ref execute,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

#if ARGUMENT_CACHE
            CacheFlags savedCacheFlags = CacheFlags.None;

            interpreter.BeginNoArgumentCache(ref savedCacheFlags);

            try
            {
#endif
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                InterpreterStateFlags savedInterpreterStateFlags =
                    InterpreterStateFlags.None;

                interpreter.BeginArgumentLocation(
                    ref savedInterpreterStateFlags);

                try
                {
#endif
                    return execute.Execute(
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

        private static ReturnCode IndexCallback( /* PackageIndexCallback */
            Interpreter interpreter,     /* in */
            string path,                 /* in */
            string fileName,             /* in */
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
            bool setDirectory = false;

            try
            {
                bool host = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Host, true);

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
                        interpreter, IndexScriptFlags, false, noNormal);

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
                                code = SetIndexCallbackDirectory(
                                    interpreter, TclVars.Core.Directory,
                                    newText, ref setDirectory, ref result);

                                if (code == ReturnCode.Ok)
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
                            code = SetIndexCallbackDirectory(
                                interpreter, TclVars.Core.Directory,
                                fileName, ref setDirectory, ref result);

                            if (code == ReturnCode.Ok)
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

                            code = SetIndexCallbackDirectory(
                                interpreter, TclVars.Core.Directory,
                                newFileName, ref setDirectory, ref result);

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
                                    interpreter.PluginFlags, ref resources,
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

                            code = SetIndexCallbackDirectory(
                                interpreter, TclVars.Core.Directory,
                                newFileName, ref setDirectory, ref result);

                            if (code == ReturnCode.Ok)
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
                UnsetIndexCallbackDirectory(
                    interpreter, TclVars.Core.Directory,
                    ref setDirectory);
            }

            return code;
        }
        #endregion
    }
}
