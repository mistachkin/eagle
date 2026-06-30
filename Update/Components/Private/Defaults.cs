/*
 * Defaults.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Shared;
using _Private = Eagle._Components.Private;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class contains the default values used by the software updater
    /// (Hippogriff), such as its identity, file names, version, network
    /// endpoints, and behavioral options.
    /// </summary>
    [Guid("4ef23e44-41c2-4ed3-b3b2-1fd6cc9c4dc0")]
    internal static class Defaults
    {
        #region Public Constants
        /// <summary>
        /// The default trace callback used to emit diagnostic trace output.
        /// </summary>
        public static readonly TraceCallback TraceCallback = TraceOps.TraceCore;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default text encoding used by the updater.
        /// </summary>
        public static readonly Encoding Encoding = Encoding.UTF8;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name of the updater executable.
        /// </summary>
        public static readonly string ExecutableName = "Hippogriff";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the updater executable.
        /// </summary>
        public static readonly string ExecutableFileName = ExecutableName +
            ".exe";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the file containing arguments for the updater
        /// executable.
        /// </summary>
        public static readonly string ArgumentsFileName = ExecutableFileName +
            ".args";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The major version number of the updater.
        /// </summary>
        public static readonly int MajorVersion = 1;

        /// <summary>
        /// The minor version number of the updater.
        /// </summary>
        public static readonly int MinorVersion = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the HTTP user agent string.
        /// </summary>
        public static readonly string UserAgentFormat = "{0}/{1}";

        /// <summary>
        /// The product name portion of the HTTP user agent string.
        /// </summary>
        public static readonly string UserAgentName = ExecutableName;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The version portion of the HTTP user agent string.
        /// </summary>
        public static readonly Version UserAgentVersion = new Version(
            MajorVersion, MinorVersion);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the patch level query string.
        /// </summary>
        public static readonly string QueryPatchLevelFormat = "{0}.{1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the URI for the updater to download
        /// a new copy of itself.
        /// </summary>
        public static readonly string SelfUriFormat = "releases/{0}/" +
            ExecutableFileName;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The X509 certificate subject name expected for official builds.  For
        /// official builds, this is the subject name used by the Eagle
        /// Development Team; otherwise, it is null.
        /// </summary>
#if OFFICIAL
        //
        // NOTE: This is the X509 certificate subject name for the official
        //       builds released by the Eagle Development Team.
        //
        public const string SubjectName = "Mistachkin Systems";
#else
        public const string SubjectName = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default assembly to operate on (none).
        /// </summary>
        public const Assembly Assembly = null;

        /// <summary>
        /// The default numeric identifier (none).
        /// </summary>
        public const int Id = -1;

        /// <summary>
        /// The default software update protocol identifier.
        /// </summary>
        public const string ProtocolId = "1";

        /// <summary>
        /// The default public key token expected for the Eagle assembly.
        /// </summary>
        public const string PublicKeyToken = "29c6297630be05eb";

        /// <summary>
        /// The default product name.
        /// </summary>
        public const string Name = "Eagle";

        /// <summary>
        /// The default name of the binary directory.
        /// </summary>
        public const string BinaryDirectory = "bin";

        /// <summary>
        /// The default culture (none).
        /// </summary>
        public const CultureInfo Culture = null;

        /// <summary>
        /// The default patch level (none).
        /// </summary>
        public const Version PatchLevel = null;

        /// <summary>
        /// The default file name of the Eagle core library.
        /// </summary>
        public const string CoreFileName = "Eagle.dll";

        /// <summary>
        /// The default file name of the Eagle shell executable.
        /// </summary>
        public const string ShellFileName = "EagleShell.exe";

        /// <summary>
        /// The default file name of the Eagle tasks library.
        /// </summary>
        public const string TasksFileName = "EagleTasks.dll";

        /// <summary>
        /// The default file name of the Eagle cmdlets library.
        /// </summary>
        public const string CmdletsFileName = "EagleCmdlets.dll";

        /// <summary>
        /// The default name of the hash algorithm used to verify downloads.
        /// </summary>
        public const string HashAlgorithmName = "sha1";

        /// <summary>
        /// The default delay, in milliseconds.
        /// </summary>
        public const int Delay = 0;

        /// <summary>
        /// The default name of the mutex used to coordinate setup.
        /// </summary>
        public const string MutexName = "Global\\Eagle_Setup";

        /// <summary>
        /// The default base URI for software update requests.
        /// </summary>
        public const string BaseUri = "https://update.eagle.to/";

        /// <summary>
        /// The default format string used to build the URI for downloading a
        /// build.
        /// </summary>
        public const string BuildUriFormat = "releases/{0}/Eagle{1}{2}{0}.exe";

        /// <summary>
        /// The default format string used to build the command to run.
        /// </summary>
        public const string CommandFormat = "{0}";

        /// <summary>
        /// The default format string used to build the arguments to pass to the
        /// command.
        /// </summary>
        public const string ArgumentFormat = "\"-d{0}\" -s2";

        /// <summary>
        /// The default build type.
        /// </summary>
        public const BuildType BuildType = Shared.BuildType.Default;

        /// <summary>
        /// The default release type.
        /// </summary>
        public const ReleaseType ReleaseType = Shared.ReleaseType.Default;

        /// <summary>
        /// The default strong name verification flags.
        /// </summary>
        public const StrongNameExFlags StrongNameExFlags = _Private.StrongNameExFlags.Default;

        /// <summary>
        /// The default signature verification flags.
        /// </summary>
        public const SignatureFlags SignatureFlags = _Private.SignatureFlags.Default;

        /// <summary>
        /// The default extra arguments to pass to the shell (none).
        /// </summary>
        public const string[] ShellArgs = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default path and query string used to fetch the current release
        /// tag.  For stable builds, this refers to the stable tag; otherwise,
        /// it refers to the latest tag.
        /// </summary>
#if STABLE
        public const string TagPathAndQuery = "stable.txt?v={0}";
#else
        public const string TagPathAndQuery = "latest.txt?v={0}";
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the Authenticode signature check is skipped.
        /// </summary>
        public const bool NoAuthenticodeSigned = false;

        /// <summary>
        /// When non-zero, the strong name signature check is skipped.
        /// </summary>
        public const bool NoStrongNameSigned = false;

        /// <summary>
        /// When non-zero, the core file is treated as a managed assembly.
        /// </summary>
        public const bool CoreIsAssembly = true;

        /// <summary>
        /// When non-zero, actions are simulated rather than performed
        /// ("What-If" mode).
        /// </summary>
        public const bool WhatIf = false;

        /// <summary>
        /// When non-zero, verbose output is enabled.
        /// </summary>
        public const bool Verbose = false;

        /// <summary>
        /// When non-zero, output is suppressed (silent mode).
        /// </summary>
        public const bool Silent = false;

        /// <summary>
        /// When non-zero, the user interface is hidden (invisible mode).
        /// </summary>
        public const bool Invisible = false;

        /// <summary>
        /// When non-zero, actions are forced regardless of normal checks.
        /// </summary>
        public const bool Force = false;

        /// <summary>
        /// When non-zero, the update check is repeated.
        /// </summary>
        public const bool ReCheck = false;

        /// <summary>
        /// When non-zero, diagnostic tracing is enabled.
        /// </summary>
        public const bool Tracing = false;

        /// <summary>
        /// When non-zero, logging is enabled.
        /// </summary>
        public const bool Logging = true;

        /// <summary>
        /// When non-zero, the shell is launched after updating.
        /// </summary>
        public const bool Shell = false;

        /// <summary>
        /// When non-zero, the user is prompted to confirm actions.
        /// </summary>
        public const bool Confirm = true;
        #endregion
    }
}
