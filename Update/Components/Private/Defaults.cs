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
    [Guid("4ef23e44-41c2-4ed3-b3b2-1fd6cc9c4dc0")]
    internal static class Defaults
    {
        #region Public Constants
        public static readonly TraceCallback TraceCallback = TraceOps.TraceCore;

        ///////////////////////////////////////////////////////////////////////

        public static readonly Encoding Encoding = Encoding.UTF8;

        ///////////////////////////////////////////////////////////////////////

        public static readonly string ExecutableName = "Hippogriff";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string ExecutableFileName = ExecutableName +
            ".exe";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string ArgumentsFileName = ExecutableFileName +
            ".args";

        ///////////////////////////////////////////////////////////////////////

        public static readonly int MajorVersion = 1;
        public static readonly int MinorVersion = 0;

        ///////////////////////////////////////////////////////////////////////

        public static readonly string UserAgentFormat = "{0}/{1}";
        public static readonly string UserAgentName = ExecutableName;

        ///////////////////////////////////////////////////////////////////////

        public static readonly Version UserAgentVersion = new Version(
            MajorVersion, MinorVersion);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string QueryPatchLevelFormat = "{0}.{1}";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string SelfUriFormat = "releases/{0}/" +
            ExecutableFileName;

        ///////////////////////////////////////////////////////////////////////

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

        public const Assembly Assembly = null;
        public const int Id = -1;
        public const string ProtocolId = "1";
        public const string PublicKeyToken = "29c6297630be05eb";
        public const string Name = "Eagle";
        public const string BinaryDirectory = "bin";
        public const CultureInfo Culture = null;
        public const Version PatchLevel = null;
        public const string CoreFileName = "Eagle.dll";
        public const string ShellFileName = "EagleShell.exe";
        public const string TasksFileName = "EagleTasks.dll";
        public const string CmdletsFileName = "EagleCmdlets.dll";
        public const string HashAlgorithmName = "sha1";
        public const int Delay = 0;
        public const string MutexName = "Global\\Eagle_Setup";
        public const string BaseUri = "https://update.eagle.to/";
        public const string BuildUriFormat = "releases/{0}/Eagle{1}{2}{0}.exe";
        public const string CommandFormat = "{0}";
        public const string ArgumentFormat = "\"-d{0}\" -s2";
        public const BuildType BuildType = Shared.BuildType.Default;
        public const ReleaseType ReleaseType = Shared.ReleaseType.Default;
        public const StrongNameExFlags StrongNameExFlags = _Private.StrongNameExFlags.Default;
        public const SignatureFlags SignatureFlags = _Private.SignatureFlags.Default;
        public const string[] ShellArgs = null;

        ///////////////////////////////////////////////////////////////////////

#if STABLE
        public const string TagPathAndQuery = "stable.txt?v={0}";
#else
        public const string TagPathAndQuery = "latest.txt?v={0}";
#endif

        ///////////////////////////////////////////////////////////////////////

        public const bool NoAuthenticodeSigned = false;
        public const bool NoStrongNameSigned = false;
        public const bool CoreIsAssembly = true;
        public const bool WhatIf = false;
        public const bool Verbose = false;
        public const bool Silent = false;
        public const bool Invisible = false;
        public const bool Force = false;
        public const bool ReCheck = false;
        public const bool Tracing = false;
        public const bool Logging = true;
        public const bool Shell = false;
        public const bool Confirm = true;
        #endregion
    }
}
