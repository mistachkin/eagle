/*
 * Configuration.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Shared;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class encapsulates the complete set of settings that govern the
    /// Hippogriff updater, including the assembly being checked, its expected
    /// signing information (Authenticode certificate, strong name, and public
    /// key token), the remote update endpoint, the local core directory and
    /// file name, and the various behavioral flags (e.g. "what-if", verbose,
    /// silent, and tracing) that control how an update is detected, verified,
    /// and applied.  It also provides factory methods for building a
    /// configuration from defaults, a command line, or a file.
    /// </summary>
    [Guid("75620dd2-d59d-4cf0-87ff-5ecad2472bd2")]
    internal sealed class Configuration
    {
        #region Private Constants
        //
        // NOTE: This is used as the category name for all trace messages that
        //       will originate in this class.
        //
        /// <summary>
        /// The category name used for all trace messages originating in this
        /// class.
        /// </summary>
        private static readonly string TraceCategory =
            typeof(Configuration).Name;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default version to use when one cannot be queried
        //       from a candidate core file name (i.e. instead of null).  This
        //       value itself MAY be null.
        //
        /// <summary>
        /// The default version to use when one cannot be queried from a
        /// candidate core file name (i.e. instead of null).
        /// </summary>
        private static readonly Version DefaultVersion = new Version();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the fragments to combine with a suitable base
        //       installation directory in order to come up with the final
        //       core directory name.
        //
        /// <summary>
        /// The path fragments to combine with a suitable base installation
        /// directory in order to come up with the final core directory name.
        /// </summary>
        private static readonly string[] DefaultPaths = {
            Defaults.Name, Defaults.BinaryDirectory
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the combination of the fragments above into one
        //       path string, for ease of use.  This should always use
        //       the primary directory separator character, which will be
        //       the one native to this platform.
        //
        /// <summary>
        /// The combination of the default path fragments into a single path
        /// string, using the primary directory separator character native to
        /// this platform, for ease of use.
        /// </summary>
        private static readonly string DefaultPath = String.Join(
            Path.DirectorySeparatorChar.ToString(), DefaultPaths);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // NOTE: These are the lists of required files associated with each
        //       release type.
        //
        /// <summary>
        /// The lists of required files associated with each release type, keyed
        /// by release type.
        /// </summary>
        private static IDictionary<ReleaseType, IList<string>> ReleaseFiles;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// When non-zero, the assembly associated with this configuration has a
        /// trusted Authenticode signature.
        /// </summary>
        private bool isAuthenticodeSigned;
        /// <summary>
        /// When non-zero, the assembly associated with this configuration has a
        /// verified strong name signature.
        /// </summary>
        private bool isStrongNameSigned;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        private Configuration()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified settings.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to be checked and (potentially) updated.
        /// </param>
        /// <param name="subjectName">
        /// The expected subject name of the Authenticode signing certificate.
        /// </param>
        /// <param name="certificate2">
        /// The Authenticode signing certificate of the assembly, if any.
        /// </param>
        /// <param name="id">
        /// The numeric identifier used to select the desired release.
        /// </param>
        /// <param name="protocolId">
        /// The protocol identifier used when communicating with the update
        /// endpoint.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected strong name public key token of the assembly.
        /// </param>
        /// <param name="delay">
        /// The number of milliseconds to wait before attempting to delete or
        /// overwrite a potentially locked file; a negative value means "no
        /// delay".
        /// </param>
        /// <param name="mutexName">
        /// The name of the mutex used to coordinate concurrent updater runs.
        /// </param>
        /// <param name="baseUri">
        /// The base URI of the remote update endpoint.
        /// </param>
        /// <param name="tagPathAndQuery">
        /// The path-and-query format string, combined with the patch level, to
        /// query the update endpoint.
        /// </param>
        /// <param name="uriFormat">
        /// The format string used to build download URIs.
        /// </param>
        /// <param name="name">
        /// The name of the product associated with this configuration.
        /// </param>
        /// <param name="culture">
        /// The culture associated with this configuration.
        /// </param>
        /// <param name="patchLevel">
        /// The patch level (version) of the local core file.
        /// </param>
        /// <param name="buildType">
        /// The build type associated with this configuration.
        /// </param>
        /// <param name="releaseType">
        /// The release type associated with this configuration.
        /// </param>
        /// <param name="strongNameExFlags">
        /// The flags controlling strong name signature verification.
        /// </param>
        /// <param name="signatureFlags">
        /// The flags controlling Authenticode signature verification.
        /// </param>
        /// <param name="coreDirectory">
        /// The local directory containing the core file to be updated.
        /// </param>
        /// <param name="coreFileName">
        /// The file name of the local core file to be updated.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used to verify downloaded files.
        /// </param>
        /// <param name="commandFormat">
        /// The format string used to build the command to run after updating.
        /// </param>
        /// <param name="argumentFormat">
        /// The format string used to build the arguments for the command run
        /// after updating.
        /// </param>
        /// <param name="logFileName">
        /// The file name used for logging output.
        /// </param>
        /// <param name="traceCallback">
        /// The callback used to emit trace messages.
        /// </param>
        /// <param name="shellArgs">
        /// The arguments to pass to the shell when it is invoked.
        /// </param>
        /// <param name="noAuthenticodeSigned">
        /// When non-zero, Authenticode signature checking for the self-check is
        /// disabled.
        /// </param>
        /// <param name="noStrongNameSigned">
        /// When non-zero, strong name signature checking for the self-check is
        /// disabled.
        /// </param>
        /// <param name="coreIsAssembly">
        /// When non-zero, the core file is itself a managed assembly.
        /// </param>
        /// <param name="whatIf">
        /// When non-zero, no actual changes are made to the system.
        /// </param>
        /// <param name="verbose">
        /// When non-zero, more detailed output is produced.
        /// </param>
        /// <param name="silent">
        /// When non-zero, non-critical user prompts are suppressed.
        /// </param>
        /// <param name="invisible">
        /// When non-zero, all user interface elements are suppressed.
        /// </param>
        /// <param name="force">
        /// When non-zero, the update is forced even when it might otherwise be
        /// skipped.
        /// </param>
        /// <param name="reCheck">
        /// When non-zero, the update check is repeated after applying an update.
        /// </param>
        /// <param name="tracing">
        /// When non-zero, trace listening to the console is enabled.
        /// </param>
        /// <param name="logging">
        /// When non-zero, trace listening to the log file is enabled.
        /// </param>
        /// <param name="shell">
        /// When non-zero, the interactive shell may be launched.
        /// </param>
        /// <param name="confirm">
        /// When non-zero, the user is prompted to confirm before proceeding.
        /// </param>
        private Configuration(
            Assembly assembly,
            string subjectName,
            X509Certificate2 certificate2,
            int id,
            string protocolId,
            byte[] publicKeyToken,
            int delay,
            string mutexName,
            Uri baseUri,
            string tagPathAndQuery,
            string uriFormat,
            string name,
            CultureInfo culture,
            Version patchLevel,
            BuildType buildType,
            ReleaseType releaseType,
            StrongNameExFlags strongNameExFlags,
            SignatureFlags signatureFlags,
            string coreDirectory,
            string coreFileName,
            string hashAlgorithmName,
            string commandFormat,
            string argumentFormat,
            string logFileName,
            TraceCallback traceCallback,
            IEnumerable<string> shellArgs,
            bool noAuthenticodeSigned,
            bool noStrongNameSigned,
            bool coreIsAssembly,
            bool whatIf,
            bool verbose,
            bool silent,
            bool invisible,
            bool force,
            bool reCheck,
            bool tracing,
            bool logging,
            bool shell,
            bool confirm
            )
            : this()
        {
            this.assembly = assembly;
            this.subjectName = subjectName;
            this.certificate2 = certificate2;
            this.id = id;
            this.protocolId = protocolId;
            this.publicKeyToken = publicKeyToken;
            this.delay = delay;
            this.mutexName = mutexName;
            this.baseUri = baseUri;
            this.tagPathAndQuery = tagPathAndQuery;
            this.uriFormat = uriFormat;
            this.name = name;
            this.culture = culture;
            this.patchLevel = patchLevel;
            this.buildType = buildType;
            this.releaseType = releaseType;
            this.strongNameExFlags = strongNameExFlags;
            this.signatureFlags = signatureFlags;
            this.coreDirectory = coreDirectory;
            this.coreFileName = coreFileName;
            this.hashAlgorithmName = hashAlgorithmName;
            this.commandFormat = commandFormat;
            this.argumentFormat = argumentFormat;
            this.logFileName = logFileName;
            this.traceCallback = traceCallback;
            this.shellArgs = shellArgs;
            this.noAuthenticodeSigned = noAuthenticodeSigned;
            this.noStrongNameSigned = noStrongNameSigned;
            this.coreIsAssembly = coreIsAssembly;
            this.whatIf = whatIf;
            this.verbose = verbose;
            this.silent = silent;
            this.invisible = invisible;
            this.force = force;
            this.reCheck = reCheck;
            this.tracing = tracing;
            this.logging = logging;
            this.shell = shell;
            this.confirm = confirm;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class by copying the settings from
        /// an existing configuration.
        /// </summary>
        /// <param name="configuration">
        /// The configuration whose settings should be copied.  If this value
        /// is null, no settings are copied.
        /// </param>
        public Configuration(
            Configuration configuration
            )
            : this()
        {
            if (configuration != null)
            {
                this.assembly = configuration.assembly;
                this.subjectName = configuration.subjectName;
                this.certificate2 = configuration.certificate2;
                this.id = configuration.id;
                this.protocolId = configuration.protocolId;
                this.publicKeyToken = configuration.publicKeyToken;
                this.mutexName = configuration.mutexName;
                this.baseUri = configuration.baseUri;
                this.tagPathAndQuery = configuration.tagPathAndQuery;
                this.uriFormat = configuration.uriFormat;
                this.name = configuration.name;
                this.culture = configuration.culture;
                this.patchLevel = configuration.patchLevel;
                this.buildType = configuration.buildType;
                this.releaseType = configuration.releaseType;
                this.strongNameExFlags = configuration.strongNameExFlags;
                this.signatureFlags = configuration.signatureFlags;
                this.coreDirectory = configuration.coreDirectory;
                this.coreFileName = configuration.coreFileName;
                this.hashAlgorithmName = configuration.hashAlgorithmName;
                this.commandFormat = configuration.commandFormat;
                this.argumentFormat = configuration.argumentFormat;
                this.logFileName = configuration.logFileName;
                this.traceCallback = configuration.traceCallback;
                this.shellArgs = configuration.shellArgs;
                this.coreIsAssembly = configuration.coreIsAssembly;
                this.whatIf = configuration.whatIf;
                this.verbose = configuration.verbose;
                this.silent = configuration.silent;
                this.invisible = configuration.invisible;
                this.force = configuration.force;
                this.reCheck = configuration.reCheck;
                this.tracing = configuration.tracing;
                this.logging = configuration.logging;
                this.shell = configuration.shell;
                this.confirm = configuration.confirm;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the assembly to be checked and (potentially) updated.
        /// </summary>
        private Assembly assembly;
        /// <summary>
        /// Gets the assembly to be checked and (potentially) updated.
        /// </summary>
        public Assembly Assembly
        {
            get { return assembly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected subject name of the Authenticode signing
        /// certificate.
        /// </summary>
        private string subjectName;
        /// <summary>
        /// Gets the expected subject name of the Authenticode signing
        /// certificate.
        /// </summary>
        public string SubjectName
        {
            get { return subjectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the Authenticode signing certificate of the assembly, if any.
        /// </summary>
        private X509Certificate2 certificate2;
        /// <summary>
        /// Gets the Authenticode signing certificate of the assembly, if any.
        /// </summary>
        public X509Certificate2 Certificate2
        {
            get { return certificate2; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the numeric identifier used to select the desired release.
        /// </summary>
        private int id;
        /// <summary>
        /// Gets the numeric identifier used to select the desired release.
        /// </summary>
        public int Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the protocol identifier used when communicating with the
        /// update endpoint.
        /// </summary>
        private string protocolId;
        /// <summary>
        /// Gets the protocol identifier used when communicating with the
        /// update endpoint.
        /// </summary>
        public string ProtocolId
        {
            get { return protocolId; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expected strong name public key token of the assembly.
        /// </summary>
        private byte[] publicKeyToken;
        /// <summary>
        /// Gets the expected strong name public key token of the assembly.
        /// </summary>
        public byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of milliseconds to wait before attempting to
        /// delete or overwrite a potentially locked file; a negative value
        /// means "no delay".
        /// </summary>
        private int delay;
        /// <summary>
        /// Gets the number of milliseconds to wait before attempting to delete
        /// or overwrite a potentially locked file; a negative value means "no
        /// delay".
        /// </summary>
        public int Delay
        {
            get { return delay; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the mutex used to coordinate concurrent updater
        /// runs.
        /// </summary>
        private string mutexName;
        /// <summary>
        /// Gets the name of the mutex used to coordinate concurrent updater
        /// runs.
        /// </summary>
        public string MutexName
        {
            get { return mutexName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the base URI of the remote update endpoint.
        /// </summary>
        private Uri baseUri;
        /// <summary>
        /// Gets the base URI of the remote update endpoint.
        /// </summary>
        public Uri BaseUri
        {
            get { return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the path-and-query format string, combined with the patch
        /// level, used to query the update endpoint.
        /// </summary>
        private string tagPathAndQuery;
        /// <summary>
        /// Gets the path-and-query format string, combined with the patch
        /// level, used to query the update endpoint.
        /// </summary>
        public string TagPathAndQuery
        {
            get { return tagPathAndQuery; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the format string used to build download URIs.
        /// </summary>
        private string uriFormat;
        /// <summary>
        /// Gets the format string used to build download URIs.
        /// </summary>
        public string UriFormat
        {
            get { return uriFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the product associated with this configuration.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name of the product associated with this configuration.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the culture associated with this configuration.
        /// </summary>
        private CultureInfo culture;
        /// <summary>
        /// Gets the culture associated with this configuration.
        /// </summary>
        public CultureInfo Culture
        {
            get { return culture; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the patch level (version) of the local core file.
        /// </summary>
        private Version patchLevel;
        /// <summary>
        /// Gets the patch level (version) of the local core file.
        /// </summary>
        public Version PatchLevel
        {
            get { return patchLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the build type associated with this configuration.
        /// </summary>
        private BuildType buildType;
        /// <summary>
        /// Gets the build type associated with this configuration.
        /// </summary>
        public BuildType BuildType
        {
            get { return buildType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the release type associated with this configuration.
        /// </summary>
        private ReleaseType releaseType;
        /// <summary>
        /// Gets the release type associated with this configuration.
        /// </summary>
        public ReleaseType ReleaseType
        {
            get { return releaseType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling strong name signature verification.
        /// </summary>
        private StrongNameExFlags strongNameExFlags;
        /// <summary>
        /// Gets the flags controlling strong name signature verification.
        /// </summary>
        public StrongNameExFlags StrongNameExFlags
        {
            get { return strongNameExFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling Authenticode signature verification.
        /// </summary>
        private SignatureFlags signatureFlags;
        /// <summary>
        /// Gets the flags controlling Authenticode signature verification.
        /// </summary>
        public SignatureFlags SignatureFlags
        {
            get { return signatureFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the local directory containing the core file to be updated.
        /// </summary>
        private string coreDirectory;
        /// <summary>
        /// Gets the local directory containing the core file to be updated.
        /// </summary>
        public string CoreDirectory
        {
            get { return coreDirectory; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the file name of the local core file to be updated.
        /// </summary>
        private string coreFileName;
        /// <summary>
        /// Gets the file name of the local core file to be updated.
        /// </summary>
        public string CoreFileName
        {
            get { return coreFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the hash algorithm used to verify downloaded
        /// files.
        /// </summary>
        private string hashAlgorithmName;
        /// <summary>
        /// Gets the name of the hash algorithm used to verify downloaded
        /// files.
        /// </summary>
        public string HashAlgorithmName
        {
            get { return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the format string used to build the command to run after
        /// updating.
        /// </summary>
        private string commandFormat;
        /// <summary>
        /// Gets the format string used to build the command to run after
        /// updating.
        /// </summary>
        public string CommandFormat
        {
            get { return commandFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the format string used to build the arguments for the
        /// command run after updating.
        /// </summary>
        private string argumentFormat;
        /// <summary>
        /// Gets the format string used to build the arguments for the command
        /// run after updating.
        /// </summary>
        public string ArgumentFormat
        {
            get { return argumentFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the file name used for logging output.
        /// </summary>
        private string logFileName;
        /// <summary>
        /// Gets the file name used for logging output.
        /// </summary>
        public string LogFileName
        {
            get { return logFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to emit trace messages.
        /// </summary>
        private TraceCallback traceCallback;
        /// <summary>
        /// Gets or sets the callback used to emit trace messages.
        /// </summary>
        public TraceCallback TraceCallback
        {
            get { return traceCallback; }
            set { traceCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the arguments to pass to the shell when it is invoked.
        /// </summary>
        private IEnumerable<string> shellArgs;
        /// <summary>
        /// Gets the arguments to pass to the shell when it is invoked.
        /// </summary>
        public IEnumerable<string> ShellArgs
        {
            get { return shellArgs; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, Authenticode signature checking for the self-check
        /// is disabled.
        /// </summary>
        private bool noAuthenticodeSigned;
        /// <summary>
        /// Gets a value indicating whether Authenticode signature checking for
        /// the self-check is disabled.
        /// </summary>
        public bool NoAuthenticodeSigned
        {
            get { return noAuthenticodeSigned; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, strong name signature checking for the self-check is
        /// disabled.
        /// </summary>
        private bool noStrongNameSigned;
        /// <summary>
        /// Gets a value indicating whether strong name signature checking for
        /// the self-check is disabled.
        /// </summary>
        public bool NoStrongNameSigned
        {
            get { return noStrongNameSigned; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the core file is itself a managed assembly.
        /// </summary>
        private bool coreIsAssembly;
        /// <summary>
        /// Gets a value indicating whether the core file is itself a managed
        /// assembly.
        /// </summary>
        public bool CoreIsAssembly
        {
            get { return coreIsAssembly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, no actual changes are made to the system.
        /// </summary>
        private bool whatIf;
        /// <summary>
        /// Gets a value indicating whether no actual changes will be made to
        /// the system ("what-if" mode).
        /// </summary>
        public bool WhatIf
        {
            get { return whatIf; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, more detailed output is produced.
        /// </summary>
        private bool verbose;
        /// <summary>
        /// Gets a value indicating whether more detailed output is produced.
        /// </summary>
        public bool Verbose
        {
            get { return verbose; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, non-critical user prompts are suppressed.
        /// </summary>
        private bool silent;
        /// <summary>
        /// Gets a value indicating whether non-critical user prompts are
        /// suppressed.
        /// </summary>
        public bool Silent
        {
            get { return silent; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, all user interface elements are suppressed.
        /// </summary>
        private bool invisible;
        /// <summary>
        /// Gets a value indicating whether all user interface elements are
        /// suppressed.
        /// </summary>
        public bool Invisible
        {
            get { return invisible; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the update is forced even when it might otherwise be
        /// skipped.
        /// </summary>
        private bool force;
        /// <summary>
        /// Gets a value indicating whether the update is forced even when it
        /// might otherwise be skipped.
        /// </summary>
        public bool Force
        {
            get { return force; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the update check is repeated after applying an
        /// update.
        /// </summary>
        private bool reCheck;
        /// <summary>
        /// Gets a value indicating whether the update check is repeated after
        /// applying an update.
        /// </summary>
        public bool ReCheck
        {
            get { return reCheck; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, trace listening to the console is enabled.
        /// </summary>
        private bool tracing;
        /// <summary>
        /// Gets a value indicating whether trace listening to the console is
        /// enabled.
        /// </summary>
        public bool Tracing
        {
            get { return tracing; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, trace listening to the log file is enabled.
        /// </summary>
        private bool logging;
        /// <summary>
        /// Gets a value indicating whether trace listening to the log file is
        /// enabled.
        /// </summary>
        public bool Logging
        {
            get { return logging; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the interactive shell may be launched.
        /// </summary>
        private bool shell;
        /// <summary>
        /// Gets a value indicating whether the interactive shell may be
        /// launched.
        /// </summary>
        public bool Shell
        {
            get { return shell; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the user is prompted to confirm before proceeding.
        /// </summary>
        private bool confirm;
        /// <summary>
        /// Gets a value indicating whether the user is prompted to confirm
        /// before proceeding.
        /// </summary>
        public bool Confirm
        {
            get { return confirm; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the assembly is acceptably signed,
        /// taking into account both the Authenticode and strong name signature
        /// states as well as any flags that disable those checks.
        /// </summary>
        public bool IsSigned
        {
            get
            {
                if (noAuthenticodeSigned && !isAuthenticodeSigned)
                {
                    Trace(this, "Forced to disable file signature " +
                        "checking for self-check with file signature " +
                        "absent or untrusted.", TraceCategory);
                }

                if (noStrongNameSigned && !isStrongNameSigned)
                {
                    Trace(this, "Forced to disable assembly signature " +
                        "checking for self-check with assembly signature " +
                        "absent or unverified.", TraceCategory);
                }

                return (noAuthenticodeSigned || isAuthenticodeSigned) &&
                       (noStrongNameSigned || isStrongNameSigned);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this configuration is internally
        /// valid (i.e. all required settings have acceptable values).  Any
        /// invalid setting is reported via the trace callback.
        /// </summary>
        public bool IsValid
        {
            get
            {
                try
                {
                    if (assembly == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("Assembly", assembly)),
                            TraceCategory);

                        return false;
                    }

#if OFFICIAL
                    if (String.IsNullOrEmpty(subjectName))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("SubjectName",
                            subjectName)), TraceCategory);

                        return false;
                    }
#endif

#if !DEBUG
                    if (certificate2 == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("Certificate2",
                            certificate2)), TraceCategory);

                        return false;
                    }
#endif

                    if (id == 0)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("Id", id)), TraceCategory);

                        return false;
                    }

                    if (protocolId == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("ProtocolId", protocolId)),
                            TraceCategory);

                        return false;
                    }

                    if (publicKeyToken == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("PublicKeyToken",
                            publicKeyToken)), TraceCategory);

                        return false;
                    }

                    //if (delay < 0) // NOTE: Negative means "no delay".
                    //{
                    //    Trace(this, String.Format("Invalid value: {0}",
                    //        FormatOps.NameAndValue("Delay", delay)),
                    //        TraceCategory);
                    //
                    //    return false;
                    //}

                    if (mutexName == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("MutexName", mutexName)),
                            TraceCategory);

                        return false;
                    }

                    if (baseUri == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("BaseUri", baseUri)),
                            TraceCategory);

                        return false;
                    }

                    if (String.IsNullOrEmpty(tagPathAndQuery))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("TagPathAndQuery",
                            tagPathAndQuery)), TraceCategory);

                        return false;
                    }

                    if (uriFormat == null) // NOTE: Empty allowed.
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("UriFormat", uriFormat)),
                            TraceCategory);

                        return false;
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("Name", name)),
                            TraceCategory);

                        return false;
                    }

                    if (culture == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("Culture", culture)),
                            TraceCategory);

                        return false;
                    }

                    if (patchLevel == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("PatchLevel", patchLevel)),
                            TraceCategory);

                        return false;
                    }

                    if ((buildType == BuildType.None) ||
                        (buildType == BuildType.Invalid))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("BuildType", buildType)),
                            TraceCategory);

                        return false;
                    }

                    if ((releaseType == ReleaseType.None) ||
                        (releaseType == ReleaseType.Invalid))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("ReleaseType",
                            releaseType)), TraceCategory);

                        return false;
                    }

                    //
                    // NOTE: The value "None" is allowed here; however, it is
                    //       not advised.
                    //
                    if (HasFlags(strongNameExFlags,
                            StrongNameExFlags.Invalid, false))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("StrongNameExFlags",
                            strongNameExFlags)), TraceCategory);

                        return false;
                    }

                    //
                    // NOTE: The value "None" is allowed here; however, it is
                    //       not advised.
                    //
                    if (HasFlags(signatureFlags,
                            SignatureFlags.Invalid, false))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("SignatureFlags",
                            signatureFlags)), TraceCategory);

                        return false;
                    }

                    //
                    // NOTE: The core directory no longer needs to exist
                    //       because this tool is now capable of "upgrading"
                    //       from nothing (i.e. installation).
                    //
                    if (String.IsNullOrEmpty(coreDirectory) /* ||
                        !Directory.Exists(coreDirectory) */)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("CoreDirectory",
                            coreDirectory)), TraceCategory);

                        return false;
                    }

                    //
                    // NOTE: The core file name no longer needs to exist
                    //       because this tool is now capable of "upgrading"
                    //       from nothing (i.e. installation).
                    //
                    if (String.IsNullOrEmpty(coreFileName) /* ||
                        !File.Exists(coreFileName) */)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("CoreFileName",
                            coreFileName)), TraceCategory);

                        return false;
                    }

                    if (String.IsNullOrEmpty(hashAlgorithmName))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("HashAlgorithmName",
                            hashAlgorithmName)), TraceCategory);

                        return false;
                    }

                    if (String.IsNullOrEmpty(commandFormat))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("CommandFormat",
                            commandFormat)), TraceCategory);

                        return false;
                    }

                    if (argumentFormat == null) // NOTE: Empty allowed.
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("ArgumentFormat",
                            argumentFormat)), TraceCategory);

                        return false;
                    }

                    if (String.IsNullOrEmpty(logFileName))
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("LogFileName",
                            logFileName)), TraceCategory);

                        return false;
                    }

                    if (traceCallback == null)
                    {
                        Trace(this, String.Format("Invalid value: {0}",
                            FormatOps.NameAndValue("TraceCallback",
                            traceCallback)), TraceCategory);

                        return false;
                    }

                    //if (shellArgs == null) // NOTE: Null allowed.
                    //{
                    //    Trace(this, String.Format("Invalid value: {0}",
                    //        FormatOps.NameAndValue("ShellArgs", shellArgs)),
                    //        TraceCategory);
                    //
                    //    return false;
                    //}

                    return true;
                }
                catch (Exception e)
                {
                    Trace(this, e, TraceCategory);
                }

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // NOTE: *SECURITY* This method checks the file associated with this
        //       assembly to see if it has an Authenticode signature.  If so,
        //       it attempts to verify that the file is actually trusted via
        //       the native WinVerifyTrust Win32 API.  Non-zero will only be
        //       returned if the file is trusted.  Otherwise, zero will be
        //       returned, along with an appropriate error message.
        //
        /// <summary>
        /// This method checks the file associated with this assembly to see if
        /// it has a trusted Authenticode signature.
        /// </summary>
        /// <param name="forceVerify">
        /// When non-zero, signature verification is forced even on platforms
        /// where it might otherwise be skipped.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, receives the Authenticode signing certificate.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the file is trusted; otherwise, false.
        /// </returns>
        private bool VerifyAssemblyCertificate(
            bool forceVerify,
            ref X509Certificate2 certificate2,
            ref string error
            )
        {
            if (assembly == null)
            {
                error = "Invalid assembly.";
                return false;
            }

            bool isWindows = VersionOps.IsWindowsOperatingSystem();

            if (SecurityOps.IsAuthenticodeSigned(
                    assembly, subjectName, forceVerify || !isWindows,
                    ref certificate2, ref error)
#if NATIVE && WINDOWS
                && (!isWindows || WinTrustEx.IsFileTrusted(
                    this, assembly.Location, IntPtr.Zero,
                    /* userInterface */ !invisible,
                    /* userPrompt */ false,
                    /* revocation */ true,
                    /* install */ false,
                    ref error))
#endif
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *SECURITY* This method checks the file associated with this
        //       assembly to see if it has a strong name signature.  If so,
        //       it attempts to verify the strong name signature as good via
        //       the native StrongNameSignatureVerificationEx Win32 API.
        //       Non-zero will only be returned if the strong name signature
        //       is verified.  Otherwise, zero will be returned, along with
        //       an appropriate error message.
        //
        /// <summary>
        /// This method checks the file associated with this assembly to see if
        /// it has a verified strong name signature.
        /// </summary>
        /// <param name="publicKeyToken">
        /// Upon success, receives the strong name public key token of the
        /// assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the strong name signature is verified; otherwise, false.
        /// </returns>
        private bool VerifyAssemblyStrongName(
            ref byte[] publicKeyToken,
            ref string error
            )
        {
            if (assembly == null)
            {
                error = "Invalid assembly.";
                return false;
            }

#if NATIVE && WINDOWS
            bool isWindows = VersionOps.IsWindowsOperatingSystem();
#endif

            if (SecurityOps.IsStrongNameSigned(
                    assembly, ref publicKeyToken, ref error)
#if NATIVE && WINDOWS
                && (!isWindows || StrongNameEx.IsStrongNameSigned(this,
                    assembly.Location, /* force */ true, ref error))
#endif
                )
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
        /// This method combines the specified base installation directory with
        /// the default path fragments to produce the final core directory name,
        /// unless the directory already ends with that path.
        /// </summary>
        /// <param name="directory">
        /// The base installation directory to start from.
        /// </param>
        /// <returns>
        /// The resulting core directory name.
        /// </returns>
        private string BuildCoreDirectory(
            string directory
            )
        {
            string result = directory;

            if (!String.IsNullOrEmpty(result))
            {
                if ((DefaultPath != null) &&
                    FileOps.MatchSuffix(result, DefaultPath))
                {
                    return result;
                }

                if (DefaultPaths == null)
                    return result;

                foreach (string path in DefaultPaths)
                {
                    if (path == null)
                        continue;

                    string fileName = Path.GetFileName(result);

                    if (FileOps.MatchFileName(this, fileName, path))
                        continue;

                    result = Path.Combine(result, path);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the configured core file name exists
        /// within the specified directory.
        /// </summary>
        /// <param name="directory">
        /// The directory in which to look for the core file.
        /// </param>
        /// <returns>
        /// True if the core file exists in the specified directory; otherwise,
        /// false.
        /// </returns>
        private bool DoesCoreFileNameExist(
            string directory
            )
        {
            string fileName = Path.Combine(
                directory, GetCoreFileNameOnly(coreFileName));

            if (String.IsNullOrEmpty(fileName))
                return false;

            return File.Exists(fileName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rebuilds the configured core file name by combining the
        /// base core file name with the (possibly changed) core directory, then
        /// refreshes the patch level accordingly.
        /// </summary>
        private void RefreshCoreFileName()
        {
            //
            // NOTE: Grab the configured core file name, without a directory,
            //       to be combined with the configured core directory name.
            //
            string fileName = GetCoreFileNameOnly(coreFileName);

            //
            // NOTE: *SPECIAL* Rebuild the core file name based on the old
            //       base file name and the new (?) core directory.
            //
            coreFileName = (coreDirectory != null) ?
                Path.Combine(coreDirectory, fileName) : fileName;

            //
            // NOTE: *SPECIAL* Must refresh the patch level here because the
            //       underlying core file name has changed.
            //
            RefreshCorePatchLevel();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the patch level for the configured core file
        /// name.  Upon failure, the value may be null or the default version.
        /// </summary>
        private void RefreshCorePatchLevel()
        {
            //
            // NOTE: Refresh the patch level for the core file name.  If this
            //       fails, the new value may be null -OR- the default value.
            //
            patchLevel = FileOps.GetVersion(
                this, coreFileName, DefaultVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method examines the files present in the core directory and,
        /// if permitted, changes the release type to the "most complete" one
        /// whose required files are all present.
        /// </summary>
        private void RefreshReleaseType()
        {
            //
            // NOTE: There must be a core directory set to continue.
            //
            if (String.IsNullOrEmpty(coreDirectory))
                return;

            //
            // NOTE: Some release types cannot be changed from their original
            //       value.  In that case, just return.
            //
            InitializeReleaseFiles();

            if (!CanChangeReleaseType(releaseType))
                return;

            //
            // NOTE: Grab the complete list of files in the core directory.
            //       Then, examine the list, checking it against the files
            //       required for each supported release type, starting from
            //       "most complete", ending with "least complete", stopping
            //       only when all the criteria are met.  Do nothing if the
            //       criteria are never fully met; otherwise, change the
            //       release type to the one associated with the matched list
            //       of required files.
            //
            int maximumCount = 0;

            IList<string> fileNames = FileOps.GetAllNames(
                this, coreDirectory, null, false, false, true);

            if ((fileNames != null) && (fileNames.Count > 0))
            {
                if (ReleaseFiles == null)
                    return;

                foreach (KeyValuePair<ReleaseType, IList<string>> pair
                        in ReleaseFiles)
                {
                    ReleaseType newReleaseType = pair.Key;
                    IList<string> releaseFileNames = pair.Value;

                    if (releaseFileNames == null)
                        continue;

                    int count = 0;

                    foreach (string fileName in releaseFileNames)
                    {
                        if (fileNames.Contains(fileName))
                        {
                            count++;
                        }
                        else
                        {
                            Trace(this, String.Format(
                                "Release type \"{0}\" cannot be " +
                                "selected due to lack of file \"{1}\".",
                                newReleaseType, fileName), TraceCategory);
                        }
                    }

                    if (count != releaseFileNames.Count)
                        continue;

                    if ((maximumCount == 0) || (count > maximumCount))
                    {
                        maximumCount = count;

                        ReleaseType oldReleaseType = releaseType;

                        if (oldReleaseType == newReleaseType)
                        {
                            Trace(this, String.Format(
                                "Release type is still \"{0}\".",
                                oldReleaseType), TraceCategory);
                        }
                        else
                        {
                            releaseType = newReleaseType;

                            Trace(this, String.Format(
                                "Release type changed from \"{0}\" " +
                                "to \"{1}\".", oldReleaseType,
                                newReleaseType), TraceCategory);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the configured patch level for use in an update
        /// query, using only the build and revision when the major and minor
        /// versions match the defaults, or the full version string otherwise.
        /// </summary>
        /// <returns>
        /// The patch level string to use in an update query, or null if no
        /// patch level is configured.
        /// </returns>
        private string GetQueryPatchLevel()
        {
            if (patchLevel == null)
                return null;

            if ((patchLevel.Major == Defaults.MajorVersion) &&
                (patchLevel.Minor == Defaults.MinorVersion))
            {
                //
                // NOTE: This has a default major and minor version, use
                //       the build and revision only.
                //
                return String.Format(Defaults.QueryPatchLevelFormat,
                    patchLevel.Build, patchLevel.Revision);
            }
            else
            {
                //
                // NOTE: This has a non-default major or minor version,
                //       use the full version string.
                //
                return patchLevel.ToString();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the configured strong name flags
        /// include the specified flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// When non-zero, all of the specified flags must be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        public bool HasFlags(
            StrongNameExFlags hasFlags,
            bool all
            )
        {
            return HasFlags(strongNameExFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the configured signature flags
        /// include the specified flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// When non-zero, all of the specified flags must be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        public bool HasFlags(
            SignatureFlags hasFlags,
            bool all
            )
        {
            return HasFlags(signatureFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the core directory based on the location of the
        /// configured assembly, refreshing the core file name and patch level
        /// as necessary.  If the configured assembly is null, the core
        /// directory will be as well.
        /// </summary>
        public void ResetCoreDirectory()
        {
            //
            // NOTE: Based on the configured assembly, set the core directory
            //       and refresh the core file name and patch level, as
            //       necessary.  If the configured assembly is null, the core
            //       directory will be as well.
            //
            if (assembly != null)
                SetCoreDirectory(Path.GetDirectoryName(assembly.Location));
            else
                SetCoreDirectory(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the core directory, refreshing the core file name,
        /// patch level, and release type as necessary.  If the core file name
        /// exists in the specified location, it is used verbatim; otherwise,
        /// the location is treated as the base installation directory.
        /// </summary>
        /// <param name="directory">
        /// The core directory or base installation directory to use.
        /// </param>
        public void SetCoreDirectory(
            string directory
            )
        {
            //
            // NOTE: If the core file name exists in the specified location,
            //       just use it verbatim; otherwise, assume it is the base
            //       installation directory to use.
            //
            if (DoesCoreFileNameExist(directory))
                coreDirectory = directory;
            else
                coreDirectory = BuildCoreDirectory(directory);

            //
            // NOTE: *SPECIAL* Must refresh the core file name and patch level
            //       here because the underlying core directory has changed.
            //
            RefreshCoreFileName();

            //
            // NOTE: *SPECIAL* Must refresh the release type.  The directory
            //       may not contain any files other than the core file name.
            //       In that case, the release type may need to be changed to
            //       "Core".
            //
            RefreshReleaseType();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the core file name and refreshes the patch level
        /// accordingly.
        /// </summary>
        /// <param name="fileName">
        /// The core file name to use.
        /// </param>
        public void SetCoreFileName(
            string fileName
            )
        {
            //
            // NOTE: *SPECIAL* Must refresh the patch level here because the
            //       underlying core file name has changed.
            //
            coreFileName = fileName; RefreshCorePatchLevel();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the release type to its default value.
        /// </summary>
        public void ResetReleaseType()
        {
            releaseType = ReleaseType.Default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets both the release type and the core directory to
        /// their default values.
        /// </summary>
        public void ResetReleaseTypeAndCoreDirectory()
        {
            ResetReleaseType();
            ResetCoreDirectory();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the path-and-query string for an update query by
        /// combining the configured tag path-and-query format with the query
        /// patch level.
        /// </summary>
        /// <returns>
        /// The formatted path-and-query string.
        /// </returns>
        public string GetPathAndQuery()
        {
            return String.Format(tagPathAndQuery, GetQueryPatchLevel());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *SECURITY* This method checks a file to see if it has an
        //       Authenticode signature.  If so, it attempts to verify that
        //       the file is actually trusted via the native WinVerifyTrust
        //       Win32 API.  Non-zero will only be returned if the file is
        //       trusted.  Otherwise, zero will be returned, along with an
        //       appropriate error message.
        //
        /// <summary>
        /// This method checks the specified file to see if it has a trusted
        /// Authenticode signature.
        /// </summary>
        /// <param name="fileName">
        /// The file name to check.
        /// </param>
        /// <param name="forceVerify">
        /// When non-zero, signature verification is forced even on platforms
        /// where it might otherwise be skipped.
        /// </param>
        /// <param name="userPrompt">
        /// When non-zero, the user may be prompted during trust verification.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, receives the Authenticode signing certificate.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the file is trusted; otherwise, false.
        /// </returns>
        public bool VerifyFileCertificate(
            string fileName,
            bool forceVerify,
            bool userPrompt,
            ref X509Certificate2 certificate2,
            ref string error
            )
        {
            bool isWindows = VersionOps.IsWindowsOperatingSystem();

            if (!SecurityOps.IsAuthenticodeSigned(
                    fileName, subjectName, forceVerify || !isWindows,
                    ref certificate2, ref error)
#if NATIVE && WINDOWS
                || (isWindows && !WinTrustEx.IsFileTrusted(
                    this, fileName, IntPtr.Zero,
                    /* userInterface */ !invisible,
                    userPrompt,
                    /* revocation */ true,
                    /* install */ true,
                    ref error))
#endif
                )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the current values of all configuration settings
        /// via the trace callback, for troubleshooting purposes.
        /// </summary>
        public void Dump()
        {
            Trace(this, FormatOps.NameAndValue("Assembly", assembly),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("SubjectName", subjectName),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Certificate2", certificate2),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Id", id), TraceCategory);

            Trace(this, FormatOps.NameAndValue("ProtocolId", protocolId),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("PublicKeyToken",
                publicKeyToken), TraceCategory);

            Trace(this, FormatOps.NameAndValue("Delay", delay), TraceCategory);

            Trace(this, FormatOps.NameAndValue("MutexName", mutexName),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("BaseUri", baseUri),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("TagPathAndQuery",
                tagPathAndQuery), TraceCategory);

            Trace(this, FormatOps.NameAndValue("UriFormat", uriFormat),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Name", name), TraceCategory);

            Trace(this, FormatOps.NameAndValue("Culture", culture),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("PatchLevel", patchLevel),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("BuildType", buildType),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("ReleaseType", releaseType),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("StrongNameExFlags",
                strongNameExFlags), TraceCategory);

            Trace(this, FormatOps.NameAndValue("SignatureFlags",
                signatureFlags), TraceCategory);

            Trace(this, FormatOps.NameAndValue("CoreDirectory", coreDirectory),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("CoreFileName", coreFileName),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("HashAlgorithmName", hashAlgorithmName),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("CommandFormat", commandFormat),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("ArgumentFormat",
                argumentFormat), TraceCategory);

            Trace(this, FormatOps.NameAndValue("LogFileName", logFileName),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("TraceCallback", traceCallback),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("ShellArgs", shellArgs),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("NoAuthenticodeSigned",
                noAuthenticodeSigned), TraceCategory);

            Trace(this, FormatOps.NameAndValue("NoStrongNameSigned",
                noStrongNameSigned), TraceCategory);

            Trace(this, FormatOps.NameAndValue("CoreIsAssembly",
                coreIsAssembly), TraceCategory);

            Trace(this, FormatOps.NameAndValue("WhatIf", whatIf),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Verbose", verbose),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Silent", silent),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Invisible", invisible),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Force", force), TraceCategory);

            Trace(this, FormatOps.NameAndValue("ReCheck", reCheck),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Tracing", tracing),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Logging", logging),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("Shell", shell), TraceCategory);

            Trace(this, FormatOps.NameAndValue("Confirm", confirm),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("IsAuthenticodeSigned",
                isAuthenticodeSigned), TraceCategory);

            Trace(this, FormatOps.NameAndValue("IsStrongNameSigned",
                isStrongNameSigned), TraceCategory);

            Trace(this, FormatOps.NameAndValue("IsSigned", IsSigned),
                TraceCategory);

            Trace(this, FormatOps.NameAndValue("IsValid", IsValid),
                TraceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the assembly-level attribute values for the
        /// specified assembly via the trace callback, for troubleshooting
        /// purposes.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose attribute values should be emitted.
        /// </param>
        public void Dump(
            Assembly assembly
            )
        {
            string release = AttributeOps.GetAssemblyRelease(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyRelease", release),
                TraceCategory);

            string sourceId = AttributeOps.GetAssemblySourceId(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblySourceId", sourceId),
                TraceCategory);

            string sourceTimeStamp = AttributeOps.GetAssemblySourceTimeStamp(
                assembly);

            Trace(this, FormatOps.NameAndValue("AssemblySourceTimeStamp",
                sourceTimeStamp), TraceCategory);

            string strongNameTag = AttributeOps.GetAssemblyStrongNameTag(
                assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyStrongNameTag",
                strongNameTag), TraceCategory);

            string tag = AttributeOps.GetAssemblyTag(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyTag", tag),
                TraceCategory);

            string text = AttributeOps.GetAssemblyText(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyText", text),
                TraceCategory);

            string title = AttributeOps.GetAssemblyTitle(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyTitle", title),
                TraceCategory);

            Uri uri = AttributeOps.GetAssemblyUri(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyUri", uri),
                TraceCategory);

            uri = AttributeOps.GetAssemblyUpdateBaseUri(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyUpdateBaseUri", uri),
                TraceCategory);

            uri = AttributeOps.GetAssemblyDownloadBaseUri(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyDownloadBaseUri", uri),
                TraceCategory);

            uri = AttributeOps.GetAssemblyScriptBaseUri(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyScriptBaseUri", uri),
                TraceCategory);

            uri = AttributeOps.GetAssemblyAuxiliaryBaseUri(assembly);

            Trace(this, FormatOps.NameAndValue("AssemblyAuxiliaryBaseUri", uri),
                TraceCategory);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method builds the command line arguments used when re-running
        /// the updater as itself, starting from the command line that started
        /// this process and optionally appending the extra arguments required
        /// when the updater has been updated.
        /// </summary>
        /// <param name="update">
        /// When non-zero, the extra arguments needed when re-running an updated
        /// updater are appended.
        /// </param>
        /// <returns>
        /// The resulting command line arguments, with superfluous whitespace
        /// removed.
        /// </returns>
        private static string GetSelfArguments(
            bool update
            )
        {
            //
            // NOTE: Start with the command line that started this process.
            //
            string result = FormatOps.EmptyIfNull(Environment.CommandLine);

            //
            // NOTE: If necessary, add the extra command line arguments that
            //       are required when the updater must be re-run because it
            //       has been updated.
            //
            if (update)
            {
                result = String.Format(
                    "{0} -delay {1}", result, FileOps.GetLockingDelay());
            }

            //
            // NOTE: Remove all superfluous whitespace.
            //
            return result.Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts just the file name portion (without a
        /// directory) from the specified core file name, falling back to the
        /// default core file name when the input is unusable.
        /// </summary>
        /// <param name="fileName">
        /// The core file name to process.
        /// </param>
        /// <returns>
        /// The file name without any directory, or the default core file name.
        /// </returns>
        private static string GetCoreFileNameOnly(
            string fileName
            )
        {
            //
            // NOTE: First, check the provided core file name.  If it looks
            //       valid, remove the directory name from it to obtain just
            //       the file name itself.  Otherwise, use the default core
            //       file name.  Also use the default core file name if the
            //       Path.GetFileName method somehow returns null or an empty
            //       string.
            //
            string result = fileName;

            if (!String.IsNullOrEmpty(result))
                result = Path.GetFileName(result);

            if (String.IsNullOrEmpty(result))
                result = Defaults.CoreFileName;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: This method contains hard-coded information and may need to
        //       be updated later.
        //
        /// <summary>
        /// This method lazily initializes the hard-coded lists of required
        /// files for each supported release type ("Binary", "Runtime", and
        /// "Core").
        /// </summary>
        private static void InitializeReleaseFiles()
        {
            if (ReleaseFiles == null)
            {
                ReleaseFiles = new Dictionary<ReleaseType, IList<string>>();

                //
                // NOTE: First, grab the file name of the executing assembly
                //       because it is used in several of the file lists.
                //
                string executingFileName = FileOps.GetExecutingFileName();

                //
                // HACK: This is the hard-coded list of release files for the
                //       "Binary" release type.  It is the "most complete" of
                //       the supported release types.  It contains the core
                //       library, the shell, this tool, and the various extra
                //       binaries.
                //
                ReleaseFiles.Add(ReleaseType.Binary, new string[] {
                    Defaults.CoreFileName, Defaults.ShellFileName,
                    executingFileName, Defaults.TasksFileName,
                    Defaults.CmdletsFileName
                });

                //
                // HACK: This is the hard-coded list of release files for the
                //       "Runtime" release type.  It contains the core library,
                //       the shell, and this tool.
                //
                ReleaseFiles.Add(ReleaseType.Runtime, new string[] {
                    Defaults.CoreFileName, Defaults.ShellFileName,
                    executingFileName
                });

                //
                // HACK: This is the hard-coded list of release files for the
                //       "Core" release type.  It contains the core library
                //       only.
                //
                ReleaseFiles.Add(ReleaseType.Core, new string[] {
                    Defaults.CoreFileName
                });
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified release type is allowed
        /// to be changed automatically.
        /// </summary>
        /// <param name="releaseType">
        /// The release type to check.
        /// </param>
        /// <returns>
        /// True if the release type may be changed; otherwise, false.
        /// </returns>
        private static bool CanChangeReleaseType(
            ReleaseType releaseType
            )
        {
            return (releaseType == ReleaseType.Automatic);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Flags Support Methods
        /// <summary>
        /// This method determines whether the specified strong name flags
        /// include the given flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to test.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// When non-zero, all of the specified flags must be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        private static bool HasFlags(
            StrongNameExFlags flags,
            StrongNameExFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != StrongNameExFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified signature flags include
        /// the given flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to test.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// When non-zero, all of the specified flags must be present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        private static bool HasFlags(
            SignatureFlags flags,
            SignatureFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SignatureFlags.None);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Support Methods
        /// <summary>
        /// This method emits a trace message describing the specified exception
        /// under the given category.
        /// </summary>
        /// <param name="configuration">
        /// The configuration associated with the trace message, if any.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The category name for the trace message.
        /// </param>
        /// <returns>
        /// The formatted trace message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Trace(
            Configuration configuration,
            Exception exception,
            string category
            )
        {
            return TraceOps.Trace(configuration, exception, category);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified trace message under the given
        /// category.
        /// </summary>
        /// <param name="configuration">
        /// The configuration associated with the trace message, if any.
        /// </param>
        /// <param name="message">
        /// The message to emit.
        /// </param>
        /// <param name="category">
        /// The category name for the trace message.
        /// </param>
        /// <returns>
        /// The formatted trace message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Trace(
            Configuration configuration,
            string message,
            string category
            )
        {
            return TraceOps.Trace(configuration, message, category);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new configuration populated entirely with the
        /// default settings.
        /// </summary>
        /// <returns>
        /// The newly created configuration.
        /// </returns>
        public static Configuration CreateDefault()
        {
            return new Configuration(
                Defaults.Assembly, Defaults.SubjectName, null, Defaults.Id,
                Defaults.ProtocolId, null, Defaults.Delay, Defaults.MutexName,
                null, Defaults.TagPathAndQuery, Defaults.BuildUriFormat,
                Defaults.Name, Defaults.Culture, Defaults.PatchLevel,
                Defaults.BuildType, Defaults.ReleaseType,
                Defaults.StrongNameExFlags, Defaults.SignatureFlags, null,
                Defaults.CoreFileName, Defaults.HashAlgorithmName,
                Defaults.CommandFormat, Defaults.ArgumentFormat, null,
                Defaults.TraceCallback, Defaults.ShellArgs,
                Defaults.NoAuthenticodeSigned, Defaults.NoStrongNameSigned,
                Defaults.CoreIsAssembly, Defaults.WhatIf, Defaults.Verbose,
                Defaults.Silent, Defaults.Invisible, Defaults.Force,
                Defaults.ReCheck, Defaults.Tracing, Defaults.Logging,
                Defaults.Shell, Defaults.Confirm);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified configuration, optionally
        /// overriding its protocol identifier.
        /// </summary>
        /// <param name="configuration">
        /// The configuration to copy.
        /// </param>
        /// <param name="protocolId">
        /// The protocol identifier to use, or null to retain the copied value.
        /// </param>
        /// <returns>
        /// The newly created configuration.
        /// </returns>
        public static Configuration CreateWithProtocol(
            Configuration configuration,
            string protocolId
            )
        {
            Configuration selfConfiguration = new Configuration(configuration);

            if (protocolId != null)
                selfConfiguration.protocolId = protocolId;

            return selfConfiguration;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new configuration based on the settings of the
        /// specified release, falling back to a default configuration when the
        /// release is null.
        /// </summary>
        /// <param name="release">
        /// The release whose settings should populate the configuration.
        /// </param>
        /// <returns>
        /// The newly created configuration.
        /// </returns>
        public static Configuration CreateFrom(
            Release release
            )
        {
            if (release != null)
            {
                string uriFormat = release.UriFormat;

                if (uriFormat == null)
                    uriFormat = Defaults.BuildUriFormat;

                return new Configuration(
                    Defaults.Assembly, Defaults.SubjectName, null,
                    release.Id, release.ProtocolId, release.PublicKeyToken,
                    Defaults.Delay, Defaults.MutexName, release.BaseUri,
                    Defaults.TagPathAndQuery, uriFormat, release.Name,
                    release.Culture, release.PatchLevel,
                    release.BuildTypeOrDefault(), Defaults.ReleaseType,
                    Defaults.StrongNameExFlags, Defaults.SignatureFlags,
                    null, Defaults.CoreFileName, Defaults.HashAlgorithmName,
                    Defaults.CommandFormat, Defaults.ArgumentFormat, null,
                    Defaults.TraceCallback, Defaults.ShellArgs,
                    Defaults.NoAuthenticodeSigned, Defaults.NoStrongNameSigned,
                    Defaults.CoreIsAssembly, Defaults.WhatIf, Defaults.Verbose,
                    Defaults.Silent, Defaults.Invisible, Defaults.Force,
                    Defaults.ReCheck, Defaults.Tracing, Defaults.Logging,
                    Defaults.Shell, Defaults.Confirm);
            }

            return CreateDefault();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to create and initialize a configuration for
        /// the specified assembly, setting up default logging, base URI, public
        /// key token, core directory, build/release type, and "what-if" mode.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which to create the configuration.
        /// </param>
        /// <param name="configuration">
        /// On input, the configuration to use, or null to create a default one;
        /// upon success, receives the initialized configuration.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the configuration was created successfully; otherwise, false.
        /// </returns>
        public static bool TryCreate(
            Assembly assembly,
            ref Configuration configuration,
            ref string error
            )
        {
            try
            {
                #region Parameter Validation
                if (assembly == null)
                {
                    error = "Invalid assembly.";
                    return false;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Create Default Configuration
                if (configuration == null)
                    configuration = CreateDefault();
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Store Assembly
                //
                // NOTE: Store the assembly provided by our caller.  This must
                //       be done prior to calling the ResetCoreDirectory method
                //       (below) or things will not be configured correctly.
                //
                configuration.assembly = assembly;
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default Logging
                configuration.logFileName = FileOps.GetLogName(
                    configuration, String.Format("{0}{1}",
                    VersionOps.GetAssemblyName(assembly),
                    Characters.Period));
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default Base URI
                Uri baseUri = AttributeOps.GetAssemblyUpdateBaseUri(assembly);

                if (baseUri == null)
                    baseUri = new Uri(Defaults.BaseUri);

                configuration.baseUri = baseUri;
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default Public Key Token
                configuration.publicKeyToken =
                    ParseOps.HexString(Defaults.PublicKeyToken);
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default Core Directory / File Name / Version
                //
                // NOTE: Reset to the "default" core directory based on the
                //       location of the configured assembly (i.e. the one
                //       as originally provided by our caller).  But first,
                //       reset the release type.
                //
                configuration.ResetReleaseTypeAndCoreDirectory();
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default Build Type and Release Type
                //
                // NOTE: Grab the "assembly text" for this assembly.
                //       By convention [only], this is assumed to be
                //       the build type [and release type] this tool
                //       was compiled for.
                //
                string text = AttributeOps.GetAssemblyText(assembly);

                if (!String.IsNullOrEmpty(text))
                {
                    if (!ParseOps.BuildTypeAndReleaseType(
                            text, false, true,
                            ref configuration.buildType,
                            ref configuration.releaseType))
                    {
                        Trace(configuration, String.Format(
                            "Invalid assembly build/release type value: {0}",
                            FormatOps.ForDisplay(text)), TraceCategory);

                        return false;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                #region Setup Default What-If Mode
                //
                // NOTE: Never actually modify files when running in the
                //       IDE (or under another debugger).
                //
                if (Debugger.IsAttached)
                {
                    Trace(configuration,
                        "Debugger attached, enabling \"what-if\" mode...",
                        TraceCategory);

                    configuration.whatIf = true;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                return true;
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);

                error = "Failed to create configuration.";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method modifies a configuration based on the command line
        /// options read from the specified file, ignoring blank lines and
        /// comment lines.
        /// </summary>
        /// <param name="fileName">
        /// The file to read, or null to use the default arguments file name.
        /// </param>
        /// <param name="strict">
        /// When non-zero, an invalid or unsupported option causes the operation
        /// to fail.
        /// </param>
        /// <param name="configuration">
        /// On input, the configuration to modify, or null to create a default
        /// one; upon success, receives the modified configuration.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the configuration was modified successfully; otherwise,
        /// false.
        /// </returns>
        public static bool FromFile(
            string fileName,
            bool strict,
            ref Configuration configuration,
            ref string error
            )
        {
            try
            {
                if (fileName == null)
                    fileName = Defaults.ArgumentsFileName;

                if (String.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                    return true;

                if (configuration == null)
                    configuration = CreateDefault();

                using (TextReader textReader = new StreamReader(fileName))
                {
                    List<string> argv = new List<string>();

                    while (true)
                    {
                        string line = textReader.ReadLine();

                        if (line == null) // NOTE: End-of-file?
                            break;

                        string trimLine = line.Trim();

                        if (!String.IsNullOrEmpty(trimLine))
                        {
                            if ((trimLine[0] != Characters.Comment) &&
                                (trimLine[0] != Characters.AltComment))
                            {
                                argv.Add(trimLine);
                            }
                        }
                    }

                    return FromArgs(
                        argv.ToArray(), strict, ref configuration, ref error);
                }
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);

                error = String.Format(
                    "Failed to modify configuration from file \"{0}\".",
                    fileName);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method modifies a configuration based on the specified command
        /// line arguments, interpreting each supported option and its value.
        /// </summary>
        /// <param name="args">
        /// The command line arguments to process, or null to do nothing.
        /// </param>
        /// <param name="strict">
        /// When non-zero, an invalid or unsupported argument causes the
        /// operation to fail.
        /// </param>
        /// <param name="configuration">
        /// On input, the configuration to modify, or null to create a default
        /// one; upon success, receives the modified configuration.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the configuration was modified successfully; otherwise,
        /// false.
        /// </returns>
        public static bool FromArgs(
            string[] args,
            bool strict,
            ref Configuration configuration,
            ref string error
            )
        {
            try
            {
                if (args == null)
                    return true;

                if (configuration == null)
                    configuration = CreateDefault();

                Assembly assembly = configuration.assembly;

                if (assembly == null)
                {
                    error = "Invalid assembly.";
                    return false;
                }

                int length = args.Length;

                for (int index = 0; index < length; index++)
                {
                    string arg = args[index];

                    if (String.IsNullOrEmpty(arg))
                        continue;

                    string newArg = arg;

                    if (ParseOps.CheckOption(ref newArg))
                    {
                        //
                        // NOTE: All the supported command line options must
                        //       have a value; therefore, attempt to advance
                        //       to it now.  If we fail, we are done.
                        //
                        index++;

                        if (index >= length)
                        {
                            error = Trace(
                                configuration, String.Format(
                                "Missing value for option: {0}",
                                FormatOps.ForDisplay(arg)),
                                TraceCategory);

                            if (strict)
                                return false;

                            break;
                        }

                        //
                        // NOTE: Grab the textual value of this command line
                        //       option.
                        //
                        string text = args[index];

                        //
                        // NOTE: Figure out which command line option this is
                        //       (based on a partial name match) and then try
                        //       to interpret the textual value as the correct
                        //       type.
                        //
                        if (ParseOps.MatchOption(newArg, "strict"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            //
                            // NOTE: Allow the command line arguments to override
                            //       the "strictness" setting provided by our caller.
                            //
                            strict = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "subjectName"))
                        {
                            configuration.subjectName = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "id"))
                        {
                            int? value = ParseOps.Integer(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} integer value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.id = (int)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "protocolId"))
                        {
                            configuration.protocolId = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "publicKeyToken"))
                        {
                            byte[] value = ParseOps.HexString(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid public key token value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.publicKeyToken = value;
                        }
                        else if (ParseOps.MatchOption(newArg, "delay"))
                        {
                            int? value = ParseOps.Integer(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} integer value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.delay = (int)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "mutexName"))
                        {
                            configuration.mutexName = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "baseUri"))
                        {
                            Uri value = ParseOps.Uri(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} URI value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.baseUri = value;
                        }
                        else if (ParseOps.MatchOption(newArg, "tagPathAndQuery"))
                        {
                            configuration.tagPathAndQuery = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "uriFormat"))
                        {
                            configuration.uriFormat = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "name"))
                        {
                            configuration.name = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "culture"))
                        {
                            CultureInfo value = ParseOps.Culture(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid culture value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.culture = value;
                        }
                        else if (ParseOps.MatchOption(newArg, "patchLevel"))
                        {
                            Version value = ParseOps.Version(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} version value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.patchLevel = value;
                        }
                        else if (ParseOps.MatchOption(newArg, "buildType"))
                        {
                            object value = ParseOps.Enum(
                                typeof(BuildType), text, true);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid build type value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.buildType = (BuildType)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "releaseType"))
                        {
                            object value = ParseOps.Enum(
                                typeof(ReleaseType), text, true);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid release type value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.releaseType = (ReleaseType)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "strongNameExFlags"))
                        {
                            object value = ParseOps.Enum(
                                typeof(StrongNameExFlags), text, true);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid strong name flags value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.strongNameExFlags = (StrongNameExFlags)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "signatureFlags"))
                        {
                            object value = ParseOps.Enum(
                                typeof(SignatureFlags), text, true);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid signature flags value: {0}",
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.signatureFlags = (SignatureFlags)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "coreDirectory"))
                        {
                            configuration.SetCoreDirectory(text);
                        }
                        else if (ParseOps.MatchOption(newArg, "coreFileName"))
                        {
                            configuration.SetCoreFileName(text);
                        }
                        else if (ParseOps.MatchOption(newArg, "hashAlgorithmName"))
                        {
                            configuration.hashAlgorithmName = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "commandFormat"))
                        {
                            configuration.commandFormat = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "argumentFormat"))
                        {
                            configuration.argumentFormat = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "logFileName"))
                        {
                            configuration.logFileName = text;
                        }
                        else if (ParseOps.MatchOption(newArg, "noAuthenticodeSigned"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noAuthenticodeSigned = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "noStrongNameSigned"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.noStrongNameSigned = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "coreIsAssembly"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.coreIsAssembly = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "whatIf"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.whatIf = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "verbose"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.verbose = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "silent"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.silent = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "invisible"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.invisible = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "force"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.force = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "reCheck"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.reCheck = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "tracing"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.tracing = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "logging"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.logging = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "shell"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.shell = (bool)value;
                        }
                        else if (ParseOps.MatchOption(newArg, "shellArgs"))
                        {
                            configuration.shellArgs = ParseOps.CommandLine(text);
                        }
                        else if (ParseOps.MatchOption(newArg, "confirm"))
                        {
                            bool? value = ParseOps.Boolean(text);

                            if (value == null)
                            {
                                error = Trace(
                                    configuration, String.Format(
                                    "Invalid {0} boolean value: {1}",
                                    FormatOps.ForDisplay(arg),
                                    FormatOps.ForDisplay(text)),
                                    TraceCategory);

                                if (strict)
                                    return false;

                                continue;
                            }

                            configuration.confirm = (bool)value;
                        }
                        else
                        {
                            error = Trace(
                                configuration, String.Format(
                                "Unsupported command line option: {0}",
                                FormatOps.ForDisplay(arg)),
                                TraceCategory);

                            if (strict)
                                return false;
                        }
                    }
                    else
                    {
                        //
                        // HACK: Skip this argument if it is the first one
                        //       -AND- it is the fully qualified file name
                        //       for the currently executing assembly.
                        //
                        if ((index > 0) || !StringOps.SystemNoCaseEquals(
                                arg, assembly.Location))
                        {
                            error = Trace(
                                configuration, String.Format(
                                "Unsupported command line argument: {0}",
                                FormatOps.ForDisplay(arg)),
                                TraceCategory);

                            if (strict)
                                return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);

                error = "Failed to modify configuration from arguments.";
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method starts a new process that re-runs the specified assembly
        /// as the updater itself, using the appropriate command line arguments.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to start, or null to do nothing.
        /// </param>
        /// <param name="update">
        /// When non-zero, the extra arguments needed when re-running an updated
        /// updater are included.
        /// </param>
        /// <returns>
        /// The newly started process, or null if the assembly was null.
        /// </returns>
        public static Process StartAsSelf(
            Assembly assembly,
            bool update
            )
        {
            if (assembly == null)
                return null;

            return System.Diagnostics.Process.Start(
                assembly.Location, GetSelfArguments(update)); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates and deletes the "in-use" file associated with
        /// the specified configuration, honoring "what-if" mode.
        /// </summary>
        /// <param name="configuration">
        /// The configuration whose in-use file should be deleted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public static bool DeleteInUse(
            Configuration configuration,
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

                string inUseFileName = FileOps.GetInUseFileName(configuration);

                Trace(configuration, String.Format(
                    "In-use file is \"{0}\".", inUseFileName), TraceCategory);

                if (!String.IsNullOrEmpty(inUseFileName))
                {
                    if (File.Exists(inUseFileName))
                    {
                        Trace(configuration, String.Format(
                            "In-use file \"{0}\" does exist.", inUseFileName),
                            TraceCategory);

                        if (!configuration.whatIf)
                        {
                            File.Delete(inUseFileName); /* throw */

                            Trace(configuration, String.Format(
                                "In-use file \"{0}\" deleted.", inUseFileName),
                                TraceCategory);
                        }
                        else
                        {
                            Trace(configuration,
                                "Skipped deleting in-use file.", TraceCategory);
                        }
                    }
                    else
                    {
                        Trace(configuration, String.Format(
                            "In-use file \"{0}\" does not exist.",
                            inUseFileName), TraceCategory);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);

                error = "Failed to delete in-use file.";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finalizes processing of the specified configuration by
        /// setting up trace and log listeners, verifying the assembly
        /// signatures, dumping the configuration, optionally delaying, and
        /// deleting the in-use file in preparation for an update.
        /// </summary>
        /// <param name="args">
        /// The original command line arguments, used for diagnostic output.
        /// </param>
        /// <param name="configuration">
        /// The configuration to process.
        /// </param>
        /// <param name="strict">
        /// When non-zero, a signature verification failure causes the operation
        /// to fail.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the configuration was processed successfully; otherwise,
        /// false.
        /// </returns>
        public static bool Process(
            string[] args,
            Configuration configuration,
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

                Assembly assembly = configuration.assembly;

                if (assembly == null)
                {
                    error = "Invalid assembly.";
                    return false;
                }

                //
                // HACK: If tracing has been enabled, add the listener.
                //
                if (configuration.tracing)
                {
                    System.Diagnostics.Trace.Listeners.Add(
                        new ConsoleTraceListener());
                }

                //
                // NOTE: If logging has been enabled, add the listener.
                //
                if (configuration.logging)
                {
                    System.Diagnostics.Trace.Listeners.Add(
                        new TextWriterTraceListener(
                            configuration.logFileName));
                }

                string localError = null;

                //
                // NOTE: Grab the Authenticode signature of the assembly
                //       and use it to populate the configuration.
                //
                if (configuration.HasFlags(SignatureFlags.Self, true))
                {
                    X509Certificate2 certificate2 = null;

                    if (configuration.VerifyAssemblyCertificate(
                            false, ref certificate2, ref localError))
                    {
                        configuration.isAuthenticodeSigned = true;
                    }
                    else if (strict)
                    {
                        error = localError;
                        return false;
                    }
                    else
                    {
                        Trace(configuration, localError, TraceCategory);
                    }

                    configuration.certificate2 = certificate2;
                }

                //
                // NOTE: Grab the public key token for the assembly [and
                //       possibly] use it to populate the configuration.
                //
                byte[] publicKeyToken = null;

                if (configuration.HasFlags(StrongNameExFlags.Self, true))
                {
                    if (configuration.VerifyAssemblyStrongName(
                            ref publicKeyToken, ref localError))
                    {
                        configuration.isStrongNameSigned = true;
                    }
                    else if (strict)
                    {
                        error = localError;
                        return false;
                    }
                    else
                    {
                        Trace(configuration, localError, TraceCategory);
                    }
                }

                //
                // NOTE: If the public key token that the assembly is
                //       signed with differs from our default, use it
                //       unless the configuration has previously been
                //       modified to use a non-default public key token.
                //
                if (!SecurityOps.IsDefaultPublicKeyToken(
                        publicKeyToken) &&
                    SecurityOps.IsDefaultPublicKeyToken(
                        configuration.publicKeyToken))
                {
                    Trace(configuration, String.Format(
                        "Using non-default public key token: \"{0}\"...",
                        FormatOps.ToHexString(publicKeyToken)),
                        TraceCategory);

                    configuration.publicKeyToken = publicKeyToken;
                }

                //
                // NOTE: If the culture is null, use the invariant culture.
                //
                if (configuration.culture == null)
                    configuration.culture = CultureInfo.InvariantCulture;

                //
                // NOTE: Dump the configuration now in case we need to
                //       troubleshoot any issues.
                //
                configuration.Dump(assembly);
                configuration.Dump();

#if NATIVE && WINDOWS
                //
                // NOTE: If we are running on Windows, always try to close
                //       our console window.  If the shell has been enabled,
                //       it will try to re-open the console window later
                //       (i.e. if it is actually invoked).
                //
                if (VersionOps.IsWindowsOperatingSystem() &&
                    !ConsoleEx.TryClose(ref error))
                {
                    return false;
                }
#endif

                //
                // NOTE: Add an entry to the log, if applicable.
                //
                Trace(configuration, String.Format(
                    "Configuration processed for assembly \"{0}\".",
                    assembly), TraceCategory);

                //
                // NOTE: Show where we are running from and how we were
                //       invoked.
                //
                string location = assembly.Location;

                Trace(configuration, String.Format(
                    "Original command line is: {0}", Environment.CommandLine),
                    TraceCategory);

                Trace(configuration, String.Format(
                    "Running as \"{0}\" with arguments \"{1}\".",
                    location, FormatOps.ListToString(args)), TraceCategory);

                Trace(configuration, String.Format(
                    "Compiled with options: {0}", FormatOps.ListToString(
                    DefineConstants.OptionList)), TraceCategory);

                //
                // NOTE: If the debugger is attached and What-If mode is [now]
                //       disabled, issue a warning; otherwise, print a notice
                //       stating that it is enabled.
                //
                if (configuration.whatIf)
                {
                    Trace(configuration,
                        "No actual changes will be made to this system " +
                        "because \"what-if\" mode is enabled.", TraceCategory);
                }
                else if (Debugger.IsAttached)
                {
                    Trace(configuration,
                        "Forced to disable \"what-if\" mode with debugger " +
                        "attached.", TraceCategory);
                }

                //
                // NOTE: If requested, wait a number of milliseconds before
                //       trying to delete or overwrite a potentially locked
                //       file (e.g. the updater itself or an Eagle assembly).
                //
                int delay = configuration.Delay;

                if (delay >= 0)
                {
                    Trace(configuration, String.Format(
                        "Sleeping for {0} milliseconds...", delay),
                        TraceCategory);

                    Thread.Sleep(delay);
                }

                //
                // NOTE: Attempt to locate and delete the "in-use" file for the
                //       currently executing assembly file, if necessary.
                //
                if (!DeleteInUse(configuration, ref error))
                    return false;

                //
                // NOTE: If we get to this point, everything was successful.
                //
                return true;
            }
            catch (Exception e)
            {
                Trace(configuration, e, TraceCategory);

                error = "Failed to process configuration.";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a user prompt should be shown, based
        /// on whether there is an interactive user and the configuration's
        /// invisible, silent, and error-icon settings.
        /// </summary>
        /// <param name="configuration">
        /// The configuration governing prompt behavior, or null to use the
        /// default value.
        /// </param>
        /// <param name="icon">
        /// The icon associated with the prompt; an error icon forces the prompt
        /// to be shown even in silent mode.
        /// </param>
        /// <param name="default">
        /// The value to return when there is no configuration to consult.
        /// </param>
        /// <returns>
        /// True if the prompt should be shown; otherwise, false.
        /// </returns>
        public static bool IsPromptOk(
            Configuration configuration,
            MessageBoxIcon icon,
            bool @default
            )
        {
            //
            // NOTE: If there is no interactive user, all user prompts are
            //       disabled.
            //
            if (!SystemInformation.UserInteractive)
                return false;

            //
            // NOTE: Otherwise, if there is no configuration, then return
            //       the default value specified by the caller (i.e. since
            //       further configuration checks are impossible).
            //
            if (configuration == null)
                return @default;

            //
            // NOTE: Otherwise, all user prompts are disabled in invisible
            //       mode.
            //
            if (configuration.Invisible)
                return false;

            //
            // NOTE: Otherwise, all user prompts are enabled in non-silent
            //       mode.
            //
            if (!configuration.Silent)
                return true;

            //
            // NOTE: Otherwise, if an error is being presented, the prompt
            //       must be shown.
            //
            if (icon == MessageBoxIcon.Error)
                return true;

            //
            // NOTE: Otherwise, we are in silent mode and this is not a
            //       critical error, ignore it.
            //
            return false;
        }
        #endregion
    }
}
