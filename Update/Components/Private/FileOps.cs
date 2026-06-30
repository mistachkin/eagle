/*
 * FileOps.cs --
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods used by the Eagle update
    /// component to manipulate files and directories on disk, including
    /// enumerating, filtering, hashing, backing up, copying, moving, and
    /// deleting files, as well as managing files that are currently "in-use"
    /// by the running process.
    /// </summary>
    [Guid("d27f282b-b6bd-45f0-b89a-cfcbeec2675c")]
    internal static class FileOps
    {
        #region Private Constants
        /// <summary>
        /// The trace category used when logging diagnostic messages emitted by
        /// this class.
        /// </summary>
        private static readonly string TraceCategory = typeof(FileOps).Name;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wildcard pattern that matches all file names within a directory.
        /// </summary>
        private const string AllPattern = "*";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name suffix appended to backup copies of files.
        /// </summary>
        private const string BackupSuffix = ".old";
        /// <summary>
        /// The file name suffix appended to a file that is currently in-use by
        /// the running process and is awaiting deletion.
        /// </summary>
        private const string InUseSuffix = ".in-use";
        /// <summary>
        /// The file name suffix used for log files.
        /// </summary>
        private const string LogSuffix = ".log";
        /// <summary>
        /// The file name suffix used for executable files.
        /// </summary>
        private const string ExeSuffix = ".exe";
        /// <summary>
        /// The file name suffix used for dynamic-link library files.
        /// </summary>
        private const string DllSuffix = ".dll";
        /// <summary>
        /// The file name suffix used for batch files.
        /// </summary>
        private const string BatSuffix = ".bat";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the environment variable that contains the path to the
        /// command interpreter (i.e. "cmd.exe") on Windows.
        /// </summary>
        private const string ComSpecEnvVarName = "ComSpec";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The template used to build the contents of the temporary batch file
        /// that waits and then deletes the in-use file (and itself).  The first
        /// format placeholder is the ping count used to introduce a delay and
        /// the second is the name of the in-use file to delete.
        /// </summary>
        private static readonly string DeleteInUseTemplate =
            "ping.exe -n {0} 127.0.0.1 >NUL" + Environment.NewLine +
            "IF EXIST \"{1}\" DEL /F \"{1}\"" + Environment.NewLine +
            "IF EXIST \"%~f0\" DEL \"%~f0\"" + Environment.NewLine;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The template used to build the command line arguments passed to the
        /// command interpreter.  The single format placeholder is the name of
        /// the batch file to execute.
        /// </summary>
        private const string ComSpecArgumentTemplate = "/C \"{0}\"";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: When NOT running on Windows, it is possible that neither
        //       of the directory separator character values will be the
        //       backslash character.  Therefore, use our fixed character
        //       values instead, because various methods in this class
        //       depend on these two character values being different.
        //
        /// <summary>
        /// The set of directory separator characters (backslash and slash)
        /// recognized by this class, regardless of the current platform.
        /// </summary>
        private static readonly char[] DirectoryChars = {
            Characters.Backslash, Characters.Slash
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A read-only list wrapper around the directory separator characters,
        /// used for convenient membership tests.
        /// </summary>
        private static readonly IList<char> DirectoryCharsList =
            new List<char>(DirectoryChars);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of seconds to wait before attempting
        //       to delete a locked (i.e. "in-use") file.
        //
        /// <summary>
        /// The number of seconds to wait before attempting to delete a locked
        /// (i.e. "in-use") file.
        /// </summary>
        private const int LockingRetrySeconds = 3;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds in a single second.
        //
        /// <summary>
        /// The number of milliseconds in a single second.
        /// </summary>
        private const int MillisecondsPerSecond = 1000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data (Read-Only)
        /// <summary>
        /// The comparer used to compare and order file names in a manner
        /// appropriate for the current platform.
        /// </summary>
        private static IAnyComparer<string> fileNameComparer =
            new _Comparers.FileName();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region File Support Methods
        #region Public Methods
        /// <summary>
        /// Gets the amount of time, in milliseconds, to wait before attempting
        /// to delete a locked (i.e. "in-use") file.
        /// </summary>
        /// <returns>
        /// The locking delay, in milliseconds.
        /// </returns>
        public static int GetLockingDelay()
        {
            return LockingRetrySeconds * MillisecondsPerSecond;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, unique temporary log file name decorated
        /// with the specified prefix and the log file suffix.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="prefix">
        /// The optional prefix to prepend to the generated log file name.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The created log file name, or null if it could not be created.
        /// </returns>
        public static string GetLogName(
            Configuration configuration,
            string prefix
            )
        {
            try
            {
                //
                // NOTE: Create a new unique temporary file for logging.
                //
                string temporaryFileName = Path.GetTempFileName(); /* throw */

                //
                // NOTE: Split the temporary file name into a directory and
                //       file name.
                //
                string directory = null;
                string fileName = null;

                if (SplitName(
                        configuration, temporaryFileName, ref directory,
                        ref fileName))
                {
                    //
                    // NOTE: Use the temporary file name to build a more
                    //       user-friendly "decorated" log file name.
                    //
                    string logFileName = Path.Combine(directory,
                        (!String.IsNullOrEmpty(prefix) ?
                            prefix : String.Empty) + fileName + LogSuffix);

                    //
                    // NOTE: Rename the temporary file name to the newly
                    //       "decorated" name.  This is used to make sure
                    //       that the log file name does not already exist.
                    //
                    File.Move(temporaryFileName, logFileName); /* throw */

                    return logFileName;
                }
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the file version associated with the specified
        /// file.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to query the version information for.
        /// </param>
        /// <param name="default">
        /// The version to return if the file version cannot be determined.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The version of the specified file, or <paramref name="default" /> if
        /// it could not be determined.
        /// </returns>
        public static Version GetVersion(
            Configuration configuration,
            string fileName,
            Version @default
            )
        {
            try
            {
                if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    FileVersionInfo fileVersionInfo =
                        FileVersionInfo.GetVersionInfo(fileName);

                    return new Version(fileVersionInfo.FileVersion);
                }
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file name ends with the
        /// specified suffix, using the comparison appropriate for the current
        /// platform.
        /// </summary>
        /// <param name="fileName">
        /// The file name to check.
        /// </param>
        /// <param name="suffix">
        /// The suffix to look for at the end of the file name.
        /// </param>
        /// <returns>
        /// True if the file name ends with the suffix; otherwise, false.
        /// </returns>
        public static bool MatchSuffix(
            string fileName,
            string suffix
            )
        {
            if (String.IsNullOrEmpty(fileName) || (suffix == null))
                return false;

            if (suffix.Length > fileName.Length)
                return false;

            return fileName.EndsWith(suffix, GetComparisonType());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the first file name matching the specified search
        /// criteria within the specified directory.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="directory">
        /// The directory to search for matching files.
        /// </param>
        /// <param name="rootDirectory">
        /// The root directory offset to use when the search must be redirected
        /// away from the volume root.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name or pattern to search for.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require that the returned file name refers to a file
        /// that actually exists.
        /// </param>
        /// <param name="noRoot">
        /// Non-zero to forbid the search from using the volume root directory.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to search subdirectories recursively.
        /// </param>
        /// <param name="noDirectory">
        /// Non-zero to return only the bare file names, without their directory
        /// prefix.
        /// </param>
        /// <returns>
        /// The first matching file name, or null if none was found.
        /// </returns>
        public static string GetFirstName(
            Configuration configuration,
            string directory,
            string rootDirectory, /* OFFSET ONLY */
            string fileName,
            bool exists,
            bool noRoot,
            bool recursive,
            bool noDirectory
            )
        {
            try
            {
                IList<string> fileNames = GetNames(
                    configuration, directory, rootDirectory, fileName,
                    noRoot, recursive, noDirectory);

                if ((fileNames != null) && (fileNames.Count > 0))
                {
                    fileName = fileNames[0];

                    if (!exists || File.Exists(fileName))
                        return fileName;
                }
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the bare file name of the currently executing
        /// assembly.
        /// </summary>
        /// <returns>
        /// The file name of the currently executing assembly, or null if it
        /// could not be determined.
        /// </returns>
        public static string GetExecutingFileName()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if (assembly == null)
                return null;

            string fileName = Path.GetFileName(assembly.Location);

            if (String.IsNullOrEmpty(fileName))
                return null;

            return fileName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the "in-use" file associated with the
        /// currently executing assembly, based on the specified configuration.
        /// </summary>
        /// <param name="configuration">
        /// The configuration providing the core directory and used for
        /// diagnostic tracing.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The name of the "in-use" file, or null if it could not be
        /// determined.
        /// </returns>
        public static string GetInUseFileName(
            Configuration configuration
            )
        {
            try
            {
                if (configuration == null)
                    return null;

                string fileName = GetExecutingFileName();

                if (fileName == null)
                    return null;

                string directory = configuration.CoreDirectory;

                if (String.IsNullOrEmpty(directory))
                    return null;

                return Path.Combine(directory, fileName) + InUseSuffix;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the names of all files within the specified
        /// directory.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="directory">
        /// The directory to enumerate files from.
        /// </param>
        /// <param name="rootDirectiory">
        /// The root directory offset to use when enumeration must be redirected
        /// away from the volume root.  This parameter may be null.
        /// </param>
        /// <param name="noRoot">
        /// Non-zero to forbid the enumeration from using the volume root
        /// directory.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to enumerate subdirectories recursively.
        /// </param>
        /// <param name="noDirectory">
        /// Non-zero to return only the bare file names, without their directory
        /// prefix.
        /// </param>
        /// <returns>
        /// The list of file names, or null if they could not be enumerated.
        /// </returns>
        public static IList<string> GetAllNames(
            Configuration configuration,
            string directory,
            string rootDirectiory,
            bool noRoot,
            bool recursive,
            bool noDirectory
            )
        {
            return GetNames(
                configuration, directory, rootDirectiory, AllPattern,
                noRoot, recursive, noDirectory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method arranges for the "in-use" file associated with the
        /// currently executing assembly to be deleted.  On Windows, this is
        /// accomplished by writing and launching a temporary batch file that
        /// waits and then deletes the file (and itself) after this process has
        /// exited.
        /// </summary>
        /// <param name="configuration">
        /// The configuration providing the in-use file name and used for
        /// diagnostic tracing.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the in-use file
        /// could not be deleted.
        /// </param>
        /// <returns>
        /// True if the deletion was successfully arranged (or there was nothing
        /// to do); otherwise, false.
        /// </returns>
        public static bool DeleteInUse(
            Configuration configuration,
            ref string error
            )
        {
            string inUseFileName = null;

            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                //
                // NOTE: Query the name of the "in use" file.  If its name
                //       cannot be determined -OR- it does not exist, we
                //       have "succeeded" and there is nothing else to do.
                //
                inUseFileName = GetInUseFileName(configuration);

                if ((inUseFileName == null) || !File.Exists(inUseFileName))
                {
                    TraceOps.Trace(configuration, String.Format(
                        "In-use file \"{0}\" does not exist.",
                        inUseFileName), TraceCategory);

                    return true;
                }

                TraceOps.Trace(configuration, String.Format(
                    "In-use file \"{0}\" exists.", inUseFileName),
                    TraceCategory);

                if (!VersionOps.IsWindowsOperatingSystem())
                {
                    TraceOps.Trace(configuration,
                        "Nothing can be done, not running on Windows.",
                        TraceCategory);

                    return true;
                }

                string fileName = Environment.GetEnvironmentVariable(
                    ComSpecEnvVarName);

                if ((fileName == null) || !File.Exists(fileName))
                {
                    error = String.Format(
                        "Environment variable \"{0}\" is missing.",
                        ComSpecEnvVarName);

                    return false;
                }

                //
                // NOTE: Create a new unique temporary file to write the
                //       batch file commands that will be used to delete
                //       the "in-use" file.
                //
                string temporaryFileName = Path.GetTempFileName(); /* throw */

                //
                // NOTE: Using the unique temporary file name as the basis,
                //       create the file name for the batch file.
                //
                string batchFileName = Path.Combine(Path.GetDirectoryName(
                    temporaryFileName), Path.GetFileNameWithoutExtension(
                    temporaryFileName) + BatSuffix);

                //
                // NOTE: Attempt to move the created temporary file to the
                //       final batch file name.
                //
                File.Move(temporaryFileName, batchFileName);

                //
                // NOTE: Write the commands into the final batch file, using
                //       the configured delay, in seconds, as the amount of
                //       time to wait before the batch file tries to actually
                //       delete in the "in-use" file.
                //
                string contents = String.Format(DeleteInUseTemplate,
                    LockingRetrySeconds + 1, inUseFileName);

                File.WriteAllText(batchFileName, contents);

                TraceOps.Trace(configuration, String.Format(
                    "Wrote temporary batch file \"{0}\" with contents:" +
                    "{1}{1}{2}{1}", batchFileName, Environment.NewLine,
                    contents), TraceCategory);

                //
                // NOTE: Start a child "cmd.exe" (ComSpec) process and then
                //       exit this process as soon as possible.  Hopefully,
                //       this will allow the "in-use" file to [eventually]
                //       be deleted by the batch file.
                //
                /* IGNORED */
                Process.Start(fileName, String.Format(
                    ComSpecArgumentTemplate, batchFileName));

                //
                // NOTE: If we get to this point, we have "succeeded" to
                //       the maximum extent that we can actually determine
                //       at this point.  The actual "in-use" file will not
                //       be deleted until the batch file completes.  Also,
                //       it may not be deleted (i.e. if there is an error)
                //       and there will be no way to be notified of that
                //       error because this process will already be gone.
                //
                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to delete in-use file \"{0}\".",
                    inUseFileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the hash of the specified file using the
        /// specified hash algorithm.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to compute the hash for.
        /// </param>
        /// <param name="hash">
        /// Upon success, receives the computed hash of the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be hashed.
        /// </param>
        /// <returns>
        /// True if the file was successfully hashed; otherwise, false.
        /// </returns>
        public static bool Hash(
            Configuration configuration,
            string hashAlgorithmName,
            string fileName,
            ref byte[] hash,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(hashAlgorithmName))
                {
                    error = "Invalid hash algorithm.";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "Invalid file name.";
                    return false;
                }

                using (HashAlgorithm hashAlgorithm =
                        HashAlgorithm.Create(hashAlgorithmName))
                {
                    if (hashAlgorithm != null)
                    {
                        using (FileStream stream = new FileStream(
                                fileName, FileMode.Open, FileAccess.Read))
                        {
                            hashAlgorithm.Initialize();

                            hash = hashAlgorithm.ComputeHash(stream);

                            if (configuration.Verbose)
                            {
                                TraceOps.Trace(configuration, String.Format(
                                    "File \"{0}\" {1} hashed to: {2}.",
                                    fileName, hashAlgorithmName.ToUpperInvariant(),
                                    FormatOps.ToHexString(hash)),
                                    TraceCategory);
                            }

                            return true;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "Unsupported hash algorithm \"{0}\".", hashAlgorithmName);
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to hash file \"{0}\".", fileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method trims trailing directory separator characters from the
        /// specified path, appending a backslash when the result is left as a
        /// bare drive letter and colon so that it does not refer to the current
        /// directory of that volume.
        /// </summary>
        /// <param name="path">
        /// The path to normalize.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The normalized path.  This will be null or empty only if
        /// <paramref name="path" /> was null or empty.
        /// </returns>
        public static string CannotBeDriveLetterAndColon(
            string path
            ) /* CANNOT RETURN NULL/EMPTY UNLESS PATH IS NULL/EMPTY */
        {
            if (String.IsNullOrEmpty(path))
                return path; /* NOTE: Garbage in, garbage out. */

            //
            // NOTE: Trim off all backslash and slash characters from the
            //       end of the path.
            //
            string result = path.TrimEnd(DirectoryChars);

            //
            // NOTE: If the path was only backslash and slash characters,
            //       just return the original (i.e. which was garbage).
            //
            if (String.IsNullOrEmpty(result))
                return path;

            //
            // NOTE: If the path is now ONLY a drive letter and colon, add
            //       a backslash to the end to facilitate the caller being
            //       able to searching for files [within it] without using
            //       the current directory for the volume.
            //
            if ((result.Length == 2) && (result[1] == Characters.Colon))
                result += Characters.Backslash;

            //
            // NOTE: Return the backslash/slash trimmed (and then possibly
            //       backslash appended) resulting path to the caller.
            //
            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the first (base) directory component from the
        /// specified relative path offset.
        /// </summary>
        /// <param name="pathOffset">
        /// The relative path offset to extract the base component from.  This
        /// parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The base directory component of the path offset.
        /// </returns>
        public static string GetBasePathFromOffset(
            string pathOffset /* NOTE: For "Eagle\bin", returns "Eagle". */
            )
        {
            if (String.IsNullOrEmpty(pathOffset))
                return pathOffset; /* NOTE: Garbage in, garbage out. */

            if ((pathOffset.Length >= 2) &&
                (pathOffset[1] == Characters.Colon))
            {
                //
                // NOTE: We do not handle arbitrary [qualified] paths that
                //       include a drive letter and colon.
                //
                return pathOffset;
            }

            string[] parts = pathOffset.Trim(DirectoryChars).Split(
                DirectoryChars, StringSplitOptions.RemoveEmptyEntries);

            if ((parts == null) || (parts.Length == 0))
                return pathOffset;

            return parts[0];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes (i.e. checks and, optionally, copies) all
        /// files from the source directory to the target directory, backing up
        /// the existing target files and verifying file hashes during the copy.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="sourceDirectory">
        /// The directory containing the source files.
        /// </param>
        /// <param name="targetDirectory">
        /// The directory containing the target files.
        /// </param>
        /// <param name="rootDirectory">
        /// The root directory offset to use when enumerating the target files
        /// must be redirected away from the volume root.  This parameter may be
        /// null.
        /// </param>
        /// <param name="copy">
        /// Non-zero to actually copy the files; zero to only check them.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to allow existing target files to be overwritten.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the files could not
        /// be processed.
        /// </param>
        /// <returns>
        /// True if all files were successfully processed; otherwise, false.
        /// </returns>
        public static bool ProcessAll(
            Configuration configuration,
            string sourceDirectory,
            string targetDirectory,
            string rootDirectory, /* OFFSET ONLY */
            bool copy,
            bool overwrite,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(sourceDirectory))
                {
                    error = "Invalid source directory.";
                    return false;
                }

                if (!Directory.Exists(sourceDirectory))
                {
                    error = String.Format(
                        "Source directory \"{0}\" does not exist.",
                        sourceDirectory);

                    return false;
                }

                if (String.IsNullOrEmpty(targetDirectory))
                {
                    error = "Invalid target directory.";
                    return false;
                }

                if (!Directory.Exists(targetDirectory))
                {
                    error = String.Format(
                        "Target directory \"{0}\" does not exist.",
                        targetDirectory);

                    return false;
                }

                if (configuration.WhatIf)
                {
                    TraceOps.Trace(configuration,
                        "Processing files in \"what-if\" mode, no changes " +
                        "will be made.", TraceCategory);
                }

                List<string> sourceFileNames = GetAllNames(configuration,
                    sourceDirectory, null, false, true, false) as List<string>;

                if ((sourceFileNames == null) || (sourceFileNames.Count == 0))
                {
                    error = String.Format(
                        "List of file names for source directory \"{0}\" " +
                        "is invalid or empty.", sourceDirectory);

                    return false;
                }

                List<string> targetFileNames = FilterSuffix(configuration,
                    GetAllNames(configuration, targetDirectory, rootDirectory,
                    true, true, false), InUseSuffix) as List<string>;

                if ((targetFileNames == null) || (targetFileNames.Count == 0))
                {
                    error = String.Format(
                        "List of file names for target directory \"{0}\" " +
                        "is invalid or empty.", targetDirectory);

                    return false;
                }

                //
                // NOTE: We need to filter the list of target file names based
                //       on the available source file names.  Also, need to
                //       guarantee the same relative ordering of the file names
                //       prior to the main processing loop (below).
                //
                if (!SynchronizeNameLists(
                        configuration, sourceDirectory, targetDirectory,
                        sourceFileNames, targetFileNames, ref error))
                {
                    return false;
                }

                //
                // BUGBUG: Rethink this in the future and make it more
                //         fine-grained?
                //
                if (sourceFileNames.Count != targetFileNames.Count)
                {
                    error = String.Format(
                        "Source file name count ({0}) does not match target " +
                        "file name count ({1}).", sourceFileNames.Count,
                        targetFileNames.Count);

                    return false;
                }

                //
                // NOTE: If there are no source files to copy, we must have
                //       failed.
                //
                if (sourceFileNames.Count == 0)
                {
                    error = "Source file name count is zero.";
                    return false;
                }

                //
                // NOTE: If there are no target files to copy, we must have
                //       failed.
                //
                if (targetFileNames.Count == 0)
                {
                    error = "Target file name count is zero.";
                    return false;
                }

                //
                // NOTE: *PHASE #1* If copying, backup all the target files
                //       now.
                //
                if (copy)
                {
                    foreach (string targetFileName in targetFileNames)
                    {
                        if (!Backup(
                                configuration, targetFileName, true, false,
                                ref error))
                        {
                            return false;
                        }
                    }

                    TraceOps.Trace(configuration,
                        "All existing target files backup up.", TraceCategory);
                }

                //
                // NOTE: Grab an IComparer instance capable of comparing file
                //       names properly for this platform.
                //
                IComparer<string> comparer = fileNameComparer;

                if (comparer == null)
                {
                    error = "Invalid file name comparer.";
                    return false;
                }

                //
                // BUGFIX: Account for the target directory being at the root
                //         of the volume.
                //
                string newTargetDirectory = MightBeDriveLetterAndColon(
                    targetDirectory);

                if (String.IsNullOrEmpty(newTargetDirectory))
                {
                    error = "Invalid new target directory.";
                    return false;
                }

                //
                // NOTE: *PHASE #2* Check and possibly copy the source files
                //       to the target files.
                //
                for (int index = 0; index < sourceFileNames.Count; index++)
                {
                    //
                    // NOTE: Grab the source and target file names from their
                    //       respective lists.  The lists are supposed to
                    //       represent the exact same files residing in two
                    //       different directories.  The lists have the same
                    //       number of items and have been sorted using the
                    //       same comparer.  Therefore, the source and target
                    //       file name should match exactly (excluding the
                    //       directory prefix).
                    //
                    string sourceFileName = sourceFileNames[index];
                    string targetFileName = targetFileNames[index];

                    //
                    // NOTE: Obviously, the directory names are not going to be
                    //       the same; therefore, strip them off for comparison
                    //       purposes.
                    //
                    string sourceFileNameOffset = sourceFileName.Substring(
                        sourceDirectory.Length);

                    string targetFileNameOffset = targetFileName.Substring(
                        newTargetDirectory.Length);

                    if (comparer.Compare(
                            sourceFileNameOffset, targetFileNameOffset) != 0)
                    {
                        error = String.Format(
                            "Source file name \"{0}\" does not match target " +
                            "file name \"{1}\".", sourceFileNameOffset,
                            targetFileNameOffset);

                        return false;
                    }

                    if (copy && !Copy(
                            configuration, sourceFileName, targetFileName,
                            false, overwrite, ref error))
                    {
                        return false;
                    }
                }

                //
                // NOTE: *PHASE #3* If we copied files in the processing loop,
                //       delete the backup files now that we know it was a
                //       complete success.
                //
                if (copy)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "All {0} files copied.", sourceFileNames.Count),
                        TraceCategory);

                    IList<string> backupFileNames;

                    if (!configuration.WhatIf)
                    {
                        backupFileNames = GetNames(
                            configuration, targetDirectory, rootDirectory,
                            AllPattern + BackupSuffix, true, true, false);
                    }
                    else
                    {
                        backupFileNames = AppendSuffix(
                            targetFileNames, BackupSuffix);
                    }

                    foreach (string backupFileName in backupFileNames)
                    {
                        bool readOnly = false;

                        if (!IsReadOnly(
                                configuration, backupFileName, ref readOnly,
                                ref error) || (readOnly &&
                            !SetReadOnly(
                                configuration, backupFileName, false,
                                ref error)))
                        {
                            return false;
                        }

                        if (!Delete(
                                configuration, backupFileName,
                                !configuration.WhatIf, ref error))
                        {
                            return false;
                        }
                    }

                    TraceOps.Trace(configuration,
                        "All existing backup files deleted.", TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to process all files in {0} mode from source " +
                    "directory \"{1}\" and target directory \"{2}\".", copy ?
                    (overwrite ? "copy with overwrite" : "copy") : "check",
                    sourceDirectory, targetDirectory);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method reduces a path of the form "X:\" to just the drive
        /// letter and colon "X:", leaving all other paths unchanged.
        /// </summary>
        /// <param name="path">
        /// The path to reduce.  This parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The reduced path.  This will be null or empty only if
        /// <paramref name="path" /> was null or empty.
        /// </returns>
        private static string MightBeDriveLetterAndColon(
            string path
            ) /* CANNOT RETURN NULL/EMPTY UNLESS PATH IS NULL/EMPTY */
        {
            if (String.IsNullOrEmpty(path))
                return path; /* NOTE: Garbage in, garbage out. */

            if (path.Length != 3)
                return path; /* NOTE: Wrong length for "X:\". */

            if (path[1] != Characters.Colon)
                return path; /* NOTE: Does not start with "X:". */

            //
            // NOTE: Make sure it ends with a backslash or slash.  If not,
            //       just return the original path verbatim.
            //
            if (!DirectoryCharsList.Contains(path[2]))
                return path;

            //
            // NOTE: Everything is good, return just the drive letter and
            //       colon.
            //
            return path.Substring(0, 2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified path into its directory and file
        /// name components.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="path">
        /// The path to split.
        /// </param>
        /// <param name="directory">
        /// Upon success, receives the directory component of the path.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name component of the path.
        /// </param>
        /// <returns>
        /// True if the path was successfully split; otherwise, false.
        /// </returns>
        private static bool SplitName(
            Configuration configuration,
            string path,
            ref string directory,
            ref string fileName
            )
        {
            try
            {
                directory = Path.GetDirectoryName(path);
                fileName = Path.GetFileName(path);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path refers to the root
        /// directory of its volume.
        /// </summary>
        /// <param name="path">
        /// The path to check.
        /// </param>
        /// <returns>
        /// True if the path refers to a volume root directory; otherwise,
        /// false.
        /// </returns>
        private static bool IsRoot(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            IComparer<string> comparer = fileNameComparer;

            if (comparer == null)
                return false;

            string pathRoot = Path.GetPathRoot(path);

            if (String.IsNullOrEmpty(pathRoot))
                return false;

            return (comparer.Compare(path, pathRoot) == 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the names of the files matching the specified
        /// pattern within the specified directory.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="directory">
        /// The directory to enumerate files from.
        /// </param>
        /// <param name="rootDirectory">
        /// The root directory offset to use when enumeration must be redirected
        /// away from the volume root.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The search pattern used to match file names.
        /// </param>
        /// <param name="noRoot">
        /// Non-zero to forbid the enumeration from using the volume root
        /// directory.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to enumerate subdirectories recursively.
        /// </param>
        /// <param name="noDirectory">
        /// Non-zero to return only the bare file names, without their directory
        /// prefix.
        /// </param>
        /// <returns>
        /// The list of matching file names, or null if they could not be
        /// enumerated.
        /// </returns>
        private static IList<string> GetNames(
            Configuration configuration,
            string directory,
            string rootDirectory, /* OFFSET ONLY */
            string pattern,
            bool noRoot,
            bool recursive,
            bool noDirectory
            )
        {
            try
            {
                if (String.IsNullOrEmpty(directory))
                    return null;

                if (!Directory.Exists(directory))
                    return null;

                //
                // BUGFIX: Do not allow the file search to use the root
                //         directory.
                //
                if (noRoot && IsRoot(directory))
                {
                    //
                    // HACK: Hard-code the directory from the root for now.
                    //
                    directory = Path.Combine(directory, rootDirectory);
                }

                string[] fileNames = Directory.GetFiles(directory,
                    pattern, recursive ? SearchOption.AllDirectories :
                    SearchOption.TopDirectoryOnly);

                if (fileNames == null)
                    return null;

                Array.Sort(fileNames); /* O(N) */

                if (noDirectory)
                {
                    IList<string> result = new List<string>();

                    foreach (string fileName in fileNames)
                        result.Add(Path.GetFileName(fileName));

                    return result;
                }
                else
                {
                    return new List<string>(fileNames);
                }
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the specified list of file names with
        /// any file names matching the specified suffix removed.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileNames">
        /// The list of file names to filter.  This parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix used to identify the file names to remove.  This
        /// parameter may be null or empty.
        /// </param>
        /// <returns>
        /// The filtered list of file names, or null if filtering failed.
        /// </returns>
        private static IList<string> FilterSuffix(
            Configuration configuration,
            IList<string> fileNames,
            string suffix
            )
        {
            try
            {
                if (fileNames == null)
                    return null;

                if (String.IsNullOrEmpty(suffix))
                    return new List<string>(fileNames);

                IList<string> result = new List<string>();

                foreach (string fileName in fileNames)
                {
                    if (!String.IsNullOrEmpty(fileName) &&
                        !MatchSuffix(fileName, suffix))
                    {
                        result.Add(fileName);
                    }
                    else
                    {
                        TraceOps.Trace(configuration, String.Format(
                            "Filtered out file name \"{0}\" based on " +
                            "matching suffix \"{1}\".", fileName, suffix),
                            TraceCategory);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file is a managed
        /// assembly.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check.
        /// </param>
        /// <returns>
        /// True if the file is a managed assembly; otherwise, false.
        /// </returns>
        private static bool IsAssembly(
            Configuration configuration,
            string fileName
            )
        {
            byte[] publicKeyToken = null;
            string error = null;

            return IsAssembly(
                configuration, fileName, ref publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file is a managed
        /// assembly and, if so, obtains its public key token.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, receives the public key token of the assembly, which
        /// may be null when the assembly is not strong-name signed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file is not a
        /// managed assembly.
        /// </param>
        /// <returns>
        /// True if the file is a managed assembly; otherwise, false.
        /// </returns>
        private static bool IsAssembly(
            Configuration configuration,
            string fileName,
            ref byte[] publicKeyToken,
            ref string error
            )
        {
            try
            {
                //
                // HACK: Forbid an assembly file from having any extension
                //       other than ".dll" or ".exe".
                //
                if (!IsExecutable(fileName))
                {
                    error = String.Format(
                        "File \"{0}\" is not an executable.", fileName);

                    return false;
                }

                AssemblyName assemblyName = AssemblyName.GetAssemblyName(
                    fileName);

                if (assemblyName == null)
                {
                    error = String.Format(
                        "No assembly name from file \"{0}\".", fileName);

                    return false;
                }

                publicKeyToken = assemblyName.GetPublicKeyToken();
                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to get assembly name from file \"{0}\".",
                    fileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file name has an
        /// executable suffix (i.e. ".exe" or ".dll").
        /// </summary>
        /// <param name="fileName">
        /// The file name to check.
        /// </param>
        /// <returns>
        /// True if the file name has an executable suffix; otherwise, false.
        /// </returns>
        private static bool IsExecutable(
            string fileName
            )
        {
            return MatchSuffix(fileName, ExeSuffix) ||
                MatchSuffix(fileName, DllSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a copy of the specified list of file names with
        /// the specified suffix appended to each file name.
        /// </summary>
        /// <param name="fileNames">
        /// The list of file names to append the suffix to.  This parameter may
        /// be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to each file name.  This parameter may be null
        /// or empty.
        /// </param>
        /// <returns>
        /// The list of file names with the suffix appended, or null if
        /// <paramref name="fileNames" /> was null.
        /// </returns>
        private static IList<string> AppendSuffix(
            IList<string> fileNames,
            string suffix
            )
        {
            if (fileNames == null)
                return null;

            if (String.IsNullOrEmpty(suffix))
                return new List<string>(fileNames);

            IList<string> result = new List<string>();

            foreach (string fileName in fileNames)
                if (!String.IsNullOrEmpty(fileName))
                    result.Add(fileName + suffix);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method synchronizes the target file name list with the source
        /// file name list, adding entries for source files that have no target
        /// counterpart and removing target entries that have no source
        /// counterpart, and finally sorts both lists.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="sourceDirectory">
        /// The directory containing the source files.
        /// </param>
        /// <param name="targetDirectory">
        /// The directory containing the target files.
        /// </param>
        /// <param name="sourceFileNames">
        /// The list of source file names.
        /// </param>
        /// <param name="targetFileNames">
        /// The list of target file names, which is modified in place to match
        /// the source file names.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the lists could not
        /// be synchronized.
        /// </param>
        /// <returns>
        /// True if the lists were successfully synchronized; otherwise, false.
        /// </returns>
        private static bool SynchronizeNameLists(
            Configuration configuration,
            string sourceDirectory,
            string targetDirectory,
            List<string> sourceFileNames,
            List<string> targetFileNames,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(sourceDirectory))
                {
                    error = "Invalid source directory.";
                    return false;
                }

                if (!Directory.Exists(sourceDirectory))
                {
                    error = String.Format(
                        "Source directory \"{0}\" does not exist.",
                        sourceDirectory);

                    return false;
                }

                if (String.IsNullOrEmpty(targetDirectory))
                {
                    error = "Invalid target directory.";
                    return false;
                }

                if (!Directory.Exists(targetDirectory))
                {
                    error = String.Format(
                        "Target directory \"{0}\" does not exist.",
                        targetDirectory);

                    return false;
                }

                //
                // BUGFIX: Account for the target directory being at the root
                //         of the volume.
                //
                targetDirectory = MightBeDriveLetterAndColon(targetDirectory);

                if (String.IsNullOrEmpty(targetDirectory))
                {
                    error = "Invalid new target directory for file name lists.";
                    return false;
                }

                if (sourceFileNames == null)
                {
                    error = "Invalid source file name list.";
                    return false;
                }

                if (targetFileNames == null)
                {
                    error = "Invalid target file name list.";
                    return false;
                }

                IAnyComparer<string> comparer = fileNameComparer;

                if (comparer == null)
                {
                    error = "Invalid file name comparer.";
                    return false;
                }

                //
                // NOTE: Store the relative file name (i.e. without the source
                //       directory prefix) for each source file name along with
                //       the full file name and the index into the source file
                //       name list.
                //
                Dictionary<string, AnyPair<int, string>> sourceFileNamesOnly =
                    new Dictionary<string, AnyPair<int, string>>(comparer);

                /* O(N) */
                for (int index = 0; index < sourceFileNames.Count; index++)
                {
                    string sourceFileName = sourceFileNames[index];

                    string sourceFileNameOffset =
                        sourceFileName.Substring(sourceDirectory.Length);

                    sourceFileNamesOnly.Add(sourceFileNameOffset,
                        new AnyPair<int, string>(index, sourceFileName));
                }

                //
                // NOTE: Store the relative file name (i.e. without the target
                //       directory prefix) for each target file name along with
                //       the full file name and the index into the target file
                //       name list.
                //
                Dictionary<string, AnyPair<int, string>> targetFileNamesOnly =
                    new Dictionary<string, AnyPair<int, string>>(comparer);

                /* O(N) */
                for (int index = 0; index < targetFileNames.Count; index++)
                {
                    string targetFileName = targetFileNames[index];

                    string targetFileNameOffset =
                        targetFileName.Substring(targetDirectory.Length);

                    targetFileNamesOnly.Add(targetFileNameOffset,
                        new AnyPair<int, string>(index, targetFileName));
                }

                //
                // NOTE: For each source file name, check if a corresponding
                //       target file name exists.  If not, add that file name
                //       to the list of target file names.
                //
                /* O(N) */
                for (int index = 0; index < sourceFileNames.Count; index++)
                {
                    string sourceFileName = sourceFileNames[index];

                    string sourceFileNameOffset =
                        sourceFileName.Substring(sourceDirectory.Length);

                    if (!targetFileNamesOnly.ContainsKey(sourceFileNameOffset))
                    {
                        //
                        // NOTE: Attempt to remove the source file name from the
                        //       list because it does not exist in the target.
                        //
                        AnyPair<int, string> sourceAnyPair;

                        if (sourceFileNamesOnly.TryGetValue(
                                sourceFileNameOffset, out sourceAnyPair))
                        {
                            targetFileNames.Add(
                                targetDirectory + sourceFileNameOffset);

                            TraceOps.Trace(configuration, String.Format(
                                "Added source file name \"{0}\" with full " +
                                "path \"{1}\" from source list at index " +
                                "{2} ==> {3} to target list (no matching " +
                                "target file name).", sourceFileNameOffset,
                                sourceAnyPair.Y, index, sourceAnyPair.X),
                                TraceCategory);
                        }
                    }
                }

                //
                // NOTE: For each target file name, check if a corresponding
                //       source file name exists.  If not, remove that file
                //       name from the list of target file names.
                //
                /* O(N) */
                for (int index = targetFileNames.Count - 1; index >= 0; index--)
                {
                    string targetFileName = targetFileNames[index];

                    string targetFileNameOffset =
                        targetFileName.Substring(targetDirectory.Length);

                    if (!sourceFileNamesOnly.ContainsKey(targetFileNameOffset))
                    {
                        //
                        // NOTE: Attempt to remove the target file name from the
                        //       list because it does not exist in the source.
                        //
                        AnyPair<int, string> targetAnyPair;

                        if (targetFileNamesOnly.TryGetValue(
                                targetFileNameOffset, out targetAnyPair))
                        {
                            targetFileNames.RemoveAt(index);

                            TraceOps.Trace(configuration, String.Format(
                                "Removed target file name \"{0}\" with full " +
                                "path \"{1}\" from target list at index " +
                                "{2} ==> {3} (no matching source file name).",
                                targetFileNameOffset, targetAnyPair.Y, index,
                                targetAnyPair.X), TraceCategory);
                        }
                    }
                }

                //
                // NOTE: Sort the lists of source and target file names into
                //       ascending order.
                //
                sourceFileNames.Sort(comparer); /* O(N) */
                targetFileNames.Sort(comparer); /* O(N) */

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = "Failed to synchronize source and target file name lists.";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the two specified file names are
        /// equal, using the comparison appropriate for the current platform.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileName1">
        /// The first file name to compare.
        /// </param>
        /// <param name="fileName2">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// True if the two file names are considered equal; otherwise, false.
        /// </returns>
        public static bool MatchFileName(
            Configuration configuration,
            string fileName1,
            string fileName2
            )
        {
            try
            {
                if (fileNameComparer == null)
                    return false;

                return fileNameComparer.Equals(fileName1, fileName2);
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the string comparison type appropriate for
        /// comparing file names on the current platform.
        /// </summary>
        /// <returns>
        /// <see cref="StringComparison.OrdinalIgnoreCase" /> on Windows;
        /// otherwise, <see cref="StringComparison.Ordinal" />.
        /// </returns>
        public static StringComparison GetComparisonType()
        {
            return VersionOps.IsWindowsOperatingSystem() ?
                StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file name refers to the
        /// location of the specified assembly.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.  This parameter may
        /// be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose location is compared to the file name.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The file name to compare to the assembly location.
        /// </param>
        /// <returns>
        /// True if the file name refers to the assembly location; otherwise,
        /// false.
        /// </returns>
        private static bool MatchAssembly(
            Configuration configuration,
            Assembly assembly,
            string fileName
            )
        {
            if (assembly == null)
                return false;

            return MatchFileName(configuration, fileName, assembly.Location);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file has the read-only
        /// attribute set.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check.
        /// </param>
        /// <param name="readOnly">
        /// Upon success, receives non-zero if the file is read-only.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be checked.
        /// </param>
        /// <returns>
        /// True if the read-only status was successfully determined; otherwise,
        /// false.
        /// </returns>
        private static bool IsReadOnly(
            Configuration configuration,
            string fileName,
            ref bool readOnly,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "Invalid file name.";
                    return false;
                }

                FileAttributes fileAttributes;

                if (!configuration.WhatIf)
                {
                    fileAttributes = File.GetAttributes(fileName);
                }
                else
                {
                    fileAttributes = File.Exists(fileName) ?
                        File.GetAttributes(fileName) : (FileAttributes)0;
                }

                readOnly = ((fileAttributes & FileAttributes.ReadOnly) ==
                    FileAttributes.ReadOnly);

                if (configuration.Verbose)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "File \"{0}\" is {1}.", fileName, readOnly ?
                            "read-only" : "read-write"), TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to check if file \"{0}\" is read-only.",
                    fileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the read-only attribute on the specified
        /// file.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to modify.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to set the read-only attribute; zero to clear it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be modified.
        /// </param>
        /// <returns>
        /// True if the read-only attribute was successfully set; otherwise,
        /// false.
        /// </returns>
        private static bool SetReadOnly(
            Configuration configuration,
            string fileName,
            bool readOnly,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "Invalid file name.";
                    return false;
                }

                FileAttributes fileAttributes;

                if (!configuration.WhatIf)
                {
                    fileAttributes = File.GetAttributes(fileName);
                }
                else
                {
                    fileAttributes = File.Exists(fileName) ?
                        File.GetAttributes(fileName) : (FileAttributes)0;
                }

                if (readOnly)
                    fileAttributes |= FileAttributes.ReadOnly;
                else
                    fileAttributes &= ~FileAttributes.ReadOnly;

                if (!configuration.WhatIf)
                    File.SetAttributes(fileName, fileAttributes);

                if (configuration.Verbose)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "Set file \"{0}\" {1}.", fileName, readOnly ?
                            "read-only" : "read-write"), TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to set file \"{0}\" {1}.", fileName,
                    readOnly ? "read-only" : "read-write");
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method backs up the specified file by copying or moving it to a
        /// file with the backup suffix appended.  When the file is the currently
        /// executing assembly, it is handled specially using an "in-use" file.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to back up.
        /// </param>
        /// <param name="move">
        /// Non-zero to move the file to the backup; zero to copy it.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a missing source file as a failure; zero to treat
        /// it as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be backed up.
        /// </param>
        /// <returns>
        /// True if the file was successfully backed up; otherwise, false.
        /// </returns>
        private static bool Backup(
            Configuration configuration,
            string fileName,
            bool move,
            bool strict,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "Cannot backup, file name is invalid.";
                    return false;
                }

                if (!File.Exists(fileName))
                {
                    if (strict)
                    {
                        error = String.Format(
                            "Cannot backup, file \"{0}\" does not exist.",
                            fileName);

                        return false;
                    }
                    else
                    {
                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "Cannot backup, file \"{0}\" does not exist.",
                                fileName), TraceCategory);
                        }

                        return true;
                    }
                }

                string backupFileName = fileName + BackupSuffix;

                if (File.Exists(backupFileName))
                {
                    error = String.Format(
                        "Cannot backup, file \"{0}\" already exists.",
                        backupFileName);

                    return false;
                }

                //
                // HACK: If the target file name is the currently executing
                //       assembly, handle it specially.
                //
                if (MatchAssembly(
                        configuration, Assembly.GetExecutingAssembly(),
                        fileName))
                {
                    string inUseFileName = fileName + InUseSuffix;

                    if (!configuration.WhatIf)
                        File.Move(fileName, inUseFileName); /* throw */

                    if (configuration.Verbose)
                    {
                        TraceOps.Trace(configuration, String.Format(
                            "File \"{0}\" moved to \"{1}\".",
                            fileName, inUseFileName), TraceCategory);
                    }

                    if (!configuration.WhatIf)
                        File.Copy(inUseFileName, backupFileName); /* throw */

                    if (configuration.Verbose)
                    {
                        TraceOps.Trace(configuration, String.Format(
                            "File \"{0}\" backed up to \"{1}\".",
                            inUseFileName, backupFileName), TraceCategory);
                    }
                }
                else
                {
                    if (!configuration.WhatIf)
                    {
                        if (move)
                            File.Move(fileName, backupFileName); /* throw */
                        else
                            File.Copy(fileName, backupFileName); /* throw */
                    }

                    if (configuration.Verbose)
                    {
                        TraceOps.Trace(configuration, String.Format(
                            "File \"{0}\" backed up to \"{1}\".", fileName,
                            backupFileName), TraceCategory);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to backup file \"{0}\".",
                    fileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies or moves the source file to the target file,
        /// verifying that the file hashes match after the operation completes.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="sourceFileName">
        /// The name of the source file.
        /// </param>
        /// <param name="targetFileName">
        /// The name of the target file.
        /// </param>
        /// <param name="move">
        /// Non-zero to move the source file; zero to copy it.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to allow an existing target file to be overwritten.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be copied or moved.
        /// </param>
        /// <returns>
        /// True if the file was successfully copied or moved and verified;
        /// otherwise, false.
        /// </returns>
        private static bool CopyOrMoveWithHash(
            Configuration configuration,
            string sourceFileName,
            string targetFileName,
            bool move,
            bool overwrite,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(sourceFileName))
                {
                    error = String.Format(
                        "Cannot {0}, source file name is invalid.",
                        move ? "move" : "copy");

                    return false;
                }

                if (!File.Exists(sourceFileName))
                {
                    error = String.Format(
                        "Cannot {0}, source file \"{1}\" does not exist.",
                        move ? "move" : "copy", sourceFileName);

                    return false;
                }

                if (String.IsNullOrEmpty(targetFileName))
                {
                    error = String.Format(
                        "Cannot {0}, target file name is invalid.",
                        move ? "move" : "copy");

                    return false;
                }

                if ((move || !overwrite) && File.Exists(targetFileName))
                {
                    error = String.Format(
                        "Cannot {0}, target file \"{1}\" already exists.",
                        move ? "move" : "copy", targetFileName);

                    return false;
                }

                string hashAlgorithmName = configuration.HashAlgorithmName;
                byte[] sourceHash = null;

                if (!Hash(configuration, hashAlgorithmName, sourceFileName,
                        ref sourceHash, ref error))
                {
                    return false;
                }

                if (!configuration.WhatIf)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(
                        targetFileName)); /* throw */

                    if (move)
                        File.Move(sourceFileName, targetFileName); /* throw */
                    else
                        File.Copy(sourceFileName, targetFileName, overwrite); /* throw */

                    byte[] targetHash = null;

                    if (!Hash(configuration, hashAlgorithmName, targetFileName,
                            ref targetHash, ref error))
                    {
                        return false;
                    }

                    if (!GenericOps<byte>.Equals(sourceHash, targetHash))
                    {
                        error = String.Format(
                            "Source file \"{0}\" and {1} target file \"{2}\" {3} " +
                            "hash mismatch.", sourceFileName, move ? "moved" : "copied",
                            targetFileName, hashAlgorithmName.ToUpperInvariant());

                        return false;
                    }
                }

                if (configuration.Verbose)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "File \"{0}\" {1} to \"{2}\".", sourceFileName,
                        move ? "moved" : "copied", targetFileName),
                        TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to {0} file from \"{1}\" to \"{2}\".",
                    move ? "move" : overwrite ? "copy with overwrite" : "copy",
                    sourceFileName, targetFileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the source file to the target file, optionally
        /// backing up an existing target file first and verifying any required
        /// strong name and Authenticode signatures on the source file.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="sourceFileName">
        /// The name of the source file.
        /// </param>
        /// <param name="targetFileName">
        /// The name of the target file.
        /// </param>
        /// <param name="backup">
        /// Non-zero to back up an existing target file before copying.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to allow an existing target file to be overwritten.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be copied.
        /// </param>
        /// <returns>
        /// True if the file was successfully copied; otherwise, false.
        /// </returns>
        public static bool Copy(
            Configuration configuration,
            string sourceFileName,
            string targetFileName,
            bool backup,
            bool overwrite,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(sourceFileName))
                {
                    error = "Cannot copy, source file name is invalid.";
                    return false;
                }

                if (!File.Exists(sourceFileName))
                {
                    error = String.Format(
                        "Cannot copy, source file \"{0}\" does not exist.",
                        sourceFileName);

                    return false;
                }

                if (String.IsNullOrEmpty(targetFileName))
                {
                    error = "Cannot copy, target file name is invalid.";
                    return false;
                }

                if (!overwrite && File.Exists(targetFileName))
                {
                    error = String.Format(
                        "Cannot copy, target file \"{0}\" already exists.",
                        targetFileName);

                    return false;
                }

                if (backup && File.Exists(targetFileName))
                {
                    string backupFileName = targetFileName + BackupSuffix;

                    if (File.Exists(backupFileName))
                    {
                        error = String.Format(
                            "Cannot backup, file \"{0}\" already exists.",
                            backupFileName);

                        return false;
                    }

                    //
                    // HACK: If the target file name is the currently executing
                    //       assembly, handle it specially.
                    //
                    if (MatchAssembly(
                            configuration, Assembly.GetExecutingAssembly(),
                            targetFileName))
                    {
                        string inUseFileName = targetFileName + InUseSuffix;

                        if (!configuration.WhatIf)
                        {
                            if (!CopyOrMoveWithHash(configuration, targetFileName,
                                    inUseFileName, true, false, ref error))
                            {
                                return false;
                            }
                        }

                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "File \"{0}\" moved to \"{1}\".",
                                targetFileName, inUseFileName), TraceCategory);
                        }

                        if (!configuration.WhatIf)
                        {
                            if (!CopyOrMoveWithHash(configuration, inUseFileName,
                                    backupFileName, false, false, ref error))
                            {
                                return false;
                            }
                        }

                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "File \"{0}\" backed up to \"{1}\".",
                                inUseFileName, backupFileName), TraceCategory);
                        }
                    }
                    else
                    {
                        if (!configuration.WhatIf)
                            File.Move(targetFileName, backupFileName); /* throw */

                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "File \"{0}\" backed up to \"{1}\".",
                                targetFileName, backupFileName), TraceCategory);
                        }
                    }
                }

                //
                // NOTE: Are we configured to check for StrongName signatures
                //       on the source file (if it's an assembly file).
                //
                if (configuration.HasFlags(StrongNameExFlags.Other, true) &&
                    IsAssembly(configuration, sourceFileName))
                {
#if NATIVE && WINDOWS
                    if (VersionOps.IsWindowsOperatingSystem() &&
                        !StrongNameEx.IsStrongNameSigned(
                            configuration, sourceFileName, true,
                            ref error))
                    {
                        return false;
                    }
#endif
                }

                //
                // NOTE: Are we configured to check for Authenticode signatures
                //       on the source file (if it's an executable file).
                //
                if (configuration.HasFlags(SignatureFlags.Other, true) &&
                    IsExecutable(sourceFileName))
                {
                    X509Certificate2 certificate2 = null;
                    string localError = null;

                    if (!configuration.VerifyFileCertificate(
                            sourceFileName, false, false, ref certificate2,
                            ref localError))
                    {
                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "File \"{0}\" signature error: {1}",
                                sourceFileName, localError), TraceCategory);
                        }

                        error = String.Format(
                            "File \"{0}\" signature is missing, invalid, or untrusted.",
                            sourceFileName);

                        return false;
                    }
                    else
                    {
                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "File \"{0}\" verified, signed by \"{1}\".",
                                sourceFileName, FormatOps.CertificateToString(
                                    certificate2, false)), TraceCategory);
                        }
                    }
                }

                //
                // NOTE: Attempt to copy the source file to the target file.
                //
                if (!CopyOrMoveWithHash(configuration, sourceFileName,
                        targetFileName, false, overwrite, ref error))
                {
                    return false;
                }

                if (configuration.Verbose)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "File \"{0}\" copied to \"{1}\".",
                        sourceFileName, targetFileName), TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to {0} file from \"{1}\" to \"{2}\".",
                    overwrite ? "copy with overwrite" : "copy",
                    sourceFileName, targetFileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method deletes the specified file.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the operation and used for diagnostic
        /// tracing.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to delete.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat a missing file as a failure; zero to treat it as
        /// success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives a message describing why the file could not
        /// be deleted.
        /// </param>
        /// <returns>
        /// True if the file was successfully deleted; otherwise, false.
        /// </returns>
        private static bool Delete(
            Configuration configuration,
            string fileName,
            bool strict,
            ref string error
            )
        {
            try
            {
                if (configuration == null)
                {
                    error = "Invalid configuration.";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "Cannot delete, file name is invalid.";
                    return false;
                }

                if (!File.Exists(fileName))
                {
                    if (strict)
                    {
                        error = String.Format(
                            "Cannot delete, file \"{0}\" does not exist.",
                            fileName);

                        return false;
                    }
                    else
                    {
                        if (configuration.Verbose)
                        {
                            TraceOps.Trace(configuration, String.Format(
                                "Cannot delete, file \"{0}\" does not exist.",
                                fileName), TraceCategory);
                        }

                        return true;
                    }
                }

                if (!configuration.WhatIf)
                    File.Delete(fileName); /* throw */

                if (configuration.Verbose)
                {
                    TraceOps.Trace(configuration, String.Format(
                        "File \"{0}\" deleted.",
                        fileName), TraceCategory);
                }

                return true;
            }
            catch (Exception e)
            {
                TraceOps.Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to delete file \"{0}\".",
                    fileName);
            }

            return false;
        }
        #endregion
        #endregion
    }
}
