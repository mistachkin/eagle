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
    [Guid("75620dd2-d59d-4cf0-87ff-5ecad2472bd2")]
    internal sealed class Configuration
    {
        #region Private Constants
        //
        // NOTE: This is used as the category name for all trace messages that
        //       will originate in this class.
        //
        private static readonly string TraceCategory =
            typeof(Configuration).Name;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default version to use when one cannot be queried
        //       from a candidate core file name (i.e. instead of null).  This
        //       value itself MAY be null.
        //
        private static readonly Version DefaultVersion = new Version();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the fragments to combine with a suitable base
        //       installation directory in order to come up with the final
        //       core directory name.
        //
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
        private static readonly string DefaultPath = String.Join(
            Path.DirectorySeparatorChar.ToString(), DefaultPaths);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // NOTE: These are the lists of required files associated with each
        //       release type.
        //
        private static IDictionary<ReleaseType, IList<string>> ReleaseFiles;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private bool isAuthenticodeSigned;
        private bool isStrongNameSigned;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Configuration()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private Assembly assembly;
        public Assembly Assembly
        {
            get { return assembly; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string subjectName;
        public string SubjectName
        {
            get { return subjectName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private X509Certificate2 certificate2;
        public X509Certificate2 Certificate2
        {
            get { return certificate2; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int id;
        public int Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string protocolId;
        public string ProtocolId
        {
            get { return protocolId; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] publicKeyToken;
        public byte[] PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int delay;
        public int Delay
        {
            get { return delay; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string mutexName;
        public string MutexName
        {
            get { return mutexName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri baseUri;
        public Uri BaseUri
        {
            get { return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string tagPathAndQuery;
        public string TagPathAndQuery
        {
            get { return tagPathAndQuery; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string uriFormat;
        public string UriFormat
        {
            get { return uriFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CultureInfo culture;
        public CultureInfo Culture
        {
            get { return culture; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Version patchLevel;
        public Version PatchLevel
        {
            get { return patchLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BuildType buildType;
        public BuildType BuildType
        {
            get { return buildType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReleaseType releaseType;
        public ReleaseType ReleaseType
        {
            get { return releaseType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StrongNameExFlags strongNameExFlags;
        public StrongNameExFlags StrongNameExFlags
        {
            get { return strongNameExFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SignatureFlags signatureFlags;
        public SignatureFlags SignatureFlags
        {
            get { return signatureFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string coreDirectory;
        public string CoreDirectory
        {
            get { return coreDirectory; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string coreFileName;
        public string CoreFileName
        {
            get { return coreFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string hashAlgorithmName;
        public string HashAlgorithmName
        {
            get { return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string commandFormat;
        public string CommandFormat
        {
            get { return commandFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string argumentFormat;
        public string ArgumentFormat
        {
            get { return argumentFormat; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string logFileName;
        public string LogFileName
        {
            get { return logFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceCallback traceCallback;
        public TraceCallback TraceCallback
        {
            get { return traceCallback; }
            set { traceCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> shellArgs;
        public IEnumerable<string> ShellArgs
        {
            get { return shellArgs; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noAuthenticodeSigned;
        public bool NoAuthenticodeSigned
        {
            get { return noAuthenticodeSigned; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noStrongNameSigned;
        public bool NoStrongNameSigned
        {
            get { return noStrongNameSigned; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool coreIsAssembly;
        public bool CoreIsAssembly
        {
            get { return coreIsAssembly; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool whatIf;
        public bool WhatIf
        {
            get { return whatIf; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool verbose;
        public bool Verbose
        {
            get { return verbose; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool silent;
        public bool Silent
        {
            get { return silent; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool invisible;
        public bool Invisible
        {
            get { return invisible; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool force;
        public bool Force
        {
            get { return force; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool reCheck;
        public bool ReCheck
        {
            get { return reCheck; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool tracing;
        public bool Tracing
        {
            get { return tracing; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool logging;
        public bool Logging
        {
            get { return logging; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool shell;
        public bool Shell
        {
            get { return shell; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool confirm;
        public bool Confirm
        {
            get { return confirm; }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public bool HasFlags(
            StrongNameExFlags hasFlags,
            bool all
            )
        {
            return HasFlags(strongNameExFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasFlags(
            SignatureFlags hasFlags,
            bool all
            )
        {
            return HasFlags(signatureFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void ResetReleaseType()
        {
            releaseType = ReleaseType.Default;
        }

        ///////////////////////////////////////////////////////////////////////

        public void ResetReleaseTypeAndCoreDirectory()
        {
            ResetReleaseType();
            ResetCoreDirectory();
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static bool CanChangeReleaseType(
            ReleaseType releaseType
            )
        {
            return (releaseType == ReleaseType.Automatic);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Flags Support Methods
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
