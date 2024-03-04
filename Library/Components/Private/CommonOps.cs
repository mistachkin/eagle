/*
 * CommonOps.cs --
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

#if !NET_STANDARD_20
using Microsoft.Win32;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using DefineConstants = Eagle._Constants.DefineConstants;
using SysEnv = System.Environment;

namespace Eagle._Components.Private
{
    [ObjectId("c385e1b9-95b0-4cd5-b0d9-a5fe582d7162")]
    internal static class CommonOps
    {
        #region Runtime Detection Support Class
        [ObjectId("e9622641-301b-4208-a5cc-3801edf4854e")]
        internal static class Runtime
        {
            #region Public Constants
            public static readonly string ImageRuntimeVersion2 = "v2.0.50727";
            public static readonly string ImageRuntimeVersion4 = "v4.0.30319";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constants
            private static readonly string MonoRuntimeType = "Mono.Runtime";
            private static readonly string MonoDisplayNameMember = "GetDisplayName";

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework Constants
#if !NET_STANDARD_20 && NATIVE && WINDOWS
            private static readonly string FrameworkVersion2 = "v2.0.50727";
            private static readonly string FrameworkVersion4 = "v4.0.30319";
#endif

            ///////////////////////////////////////////////////////////////////

            private static readonly StringList MicrosoftDllFileNames =
                new StringList(new string[] {
                "mscorwks.dll",
                "clr.dll"
            });
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET Core Constants
            private static readonly string DotNetCoreVersionFileName =
                ".version"; /* TODO: Is this official and/or documented? */

            ///////////////////////////////////////////////////////////////////

            private static readonly StringList DotNetCoreDllFileNames =
                new StringList(new string[] {
#if WINDOWS
                "coreclr.dll",
#endif
#if UNIX
                "libcoreclr.so",
                "libcoreclr.dylib",
#endif
            });

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: If this type is present, we are (probably) running on
            //       some variant of the .NET Core 2.x runtime.
            //
            private static readonly string DotNetCore2xLibType =
                "System.CoreLib";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Apparently, the "System.CoreLib" static class type is
            //       completely missing from .NET Core 3.x (?).
            //
            private static readonly string DotNetCore3xLibType =
                "System.Private.CoreLib.Resources.Strings";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Apparently, things have been moved (again) in the .NET
            //       5.x (and later?) runtime (?).
            //
            private static readonly string DotNetCore5xLibType =
                "System.Private.CoreLib.Strings";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: The .NET 7.x (and later) runtime include new properties
            //       in the DateTime class, namely Microsecond, et al.
            //
            private static readonly string DotNetCore7xLibType =
                "System.DateTime";

            private static readonly string DotNetCore7xLibProperty =
                "Microsecond";
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Create a list of regular expression patterns to check the
            //       Mono runtime version against.
            //
            private static readonly RegExList MonoVersionRegExList =
                new RegExList(new Regex[] {
                RegExOps.Create(" (\\d+(?:\\.\\d+)+)$", /* NOTE: Pre-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                RegExOps.Create("^(\\d+(?:\\.\\d+)+) ", /* NOTE: Post-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            });

            ///////////////////////////////////////////////////////////////////

            #region Runtime Name Constants
            private static readonly string DotNetCoreRuntimeName = ".NET Core";
            private static readonly string DotNetRuntimeName = ".NET";

            private static readonly string MonoRuntimeName = "Mono";
            private static readonly string MicrosoftRuntimeName = "Microsoft.NET";

            ///////////////////////////////////////////////////////////////////

            private static readonly string AltDotNetCoreRuntimeName = "CoreCLR";
            private static readonly string AltMicrosoftRuntimeName = "CLR";
            private static readonly string AltMonoRuntimeName = "Mono";

            ///////////////////////////////////////////////////////////////////

            private static readonly string UnknownRuntimeName = "Unknown";
            #endregion

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            #region Framework Registry Key Name Constants
            private static readonly string FrameworkSetup20KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727";

            private static readonly string FrameworkSetup20ValueName = "Increment";

            private static readonly string FrameworkSetup40KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full";

            private static readonly string FrameworkSetup40ValueName = "Release";
            #endregion

            ///////////////////////////////////////////////////////////////////

#if NET_40
            #region Framework Registry Key Value Constants
            //
            // NOTE: These values were verified against those listed on the
            //       MSDN page:
            //
            //       https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
            //
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value indicates the .NET Framework 4.5.  It was
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup45Value = 378389; // >= indicates 4.5

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.5.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 8.1 value only applies to that
            //         exact version, not any higher versions.  This class
            //         obeys this assumption.
            //
            private static readonly int FrameworkSetup451Value = 378758; // >= indicates 4.5.1
            private static readonly int FrameworkSetup451OnWindows81Value = 378675; // >= indicates 4.5.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value indicates the .NET Framework 4.5.2.  It was
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup452Value = 379893; // >= indicates 4.5.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.  They were
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup46Value = 393297; // >= indicates 4.6
            private static readonly int FrameworkSetup46OnWindows10Value = 393295; // >= indicates 4.6

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup461Value = 394271; // >= indicates 4.6.1
            private static readonly int FrameworkSetup461OnWindows10Value = 394254; // >= indicates 4.6.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.2.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup462Value = 394806; // >= indicates 4.6.2
            private static readonly int FrameworkSetup462OnWindows10Value = 394802; // >= indicates 4.6.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.7.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup47Value = 460805; // >= indicates 4.7
            private static readonly int FrameworkSetup47OnWindows10Value = 460798; // >= indicates 4.7

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.7.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup471Value = 461310; // >= indicates 4.7.1
            private static readonly int FrameworkSetup471OnWindows10Value = 461308; // >= indicates 4.7.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.7.2.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup472Value = 461814; // >= indicates 4.7.2
            private static readonly int FrameworkSetup472OnWindows10Value = 461808; // >= indicates 4.7.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.8.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup48Value = 528049; // >= indicates 4.8
            private static readonly int FrameworkSetup48OnWindows10Value1 = 528040; // >= indicates 4.8
            private static readonly int FrameworkSetup48OnWindows10Value2 = 528372; // >= indicates 4.8
            private static readonly int FrameworkSetup48OnWindows11Value = 528449; // >= indicates 4.8

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.8.1.  They were
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup481Value = 533325; // >= indicates 4.8.1
            private static readonly int FrameworkSetup481OnWindows11Value = 533320; // >= indicates 4.8.1
            #endregion
#endif
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////

            private static long lockThreadId = 0;

            ///////////////////////////////////////////////////////////////////

            private static bool? isFramework20 = null;

            ///////////////////////////////////////////////////////////////////

            private static bool? isFramework40 = null;

            ///////////////////////////////////////////////////////////////////

            private static bool? isMono = null;

            ///////////////////////////////////////////////////////////////////

            private static bool? isDotNetCore = null;

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            private static string frameworkExtraVersion = null;
#endif

            ///////////////////////////////////////////////////////////////////

            private static Version FrameworkVersion = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Threading Cooperative Locking Diagnostic Methods
            private static long MaybeWhoHasLock()
            {
                return Interlocked.CompareExchange(
                    ref lockThreadId, 0, 0);
            }

            ///////////////////////////////////////////////////////////////////

            private static void MaybeSomebodyHasLock(
                bool locked /* in */
                )
            {
                if (locked)
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(ref lockThreadId,
                        GlobalState.GetCurrentThreadId(), 0);
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static void MaybeNobodyHasLock(
                bool locked /* in */
                )
            {
                if (locked)
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(ref lockThreadId,
                        0, GlobalState.GetCurrentThreadId());
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Threading Cooperative Locking Methods
            private static void TryLock(
                ref bool locked /* out */
                )
            {
                if (syncRoot == null)
                    return;

                locked = Monitor.TryEnter(syncRoot);
                MaybeSomebodyHasLock(locked);
            }

            ///////////////////////////////////////////////////////////////////

            private static void ExitLock(
                ref bool locked /* in, out */
                )
            {
                if (syncRoot == null)
                    return;

                if (locked)
                {
                    MaybeNobodyHasLock(locked);
                    Monitor.Exit(syncRoot);
                    locked = false;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Runtime Detection Methods
            public static void Initialize(
                bool force
                )
            {
                bool locked = false;

                try
                {
                    TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        #region Forcibly Reset Detection State
                        if (force) ResetState();
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Initialize Detection State
                        /* IGNORED */
                        IsFramework20();

                        /* IGNORED */
                        IsFramework40();

                        /* IGNORED */
                        IsMono();

                        /* IGNORED */
                        IsDotNetCore();

#if !NET_STANDARD_20
                        /* IGNORED */
                        GetFrameworkExtraVersion();
#endif

                        /* IGNORED */
                        GetFrameworkVersion();
                        #endregion
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "Initialize",
                            typeof(CommonOps.Runtime).Name, true,
                            TracePriority.LockWarning,
                            MaybeWhoHasLock());
                    }
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static StringList GetState()
            {
                bool locked = false;

                try
                {
                    TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        return new StringList(
                            "isFramework20",
                            (isFramework20 != null) ?
                                ((bool)isFramework20).ToString() :
                                FormatOps.DisplayNull,
                            "isFramework40",
                            (isFramework40 != null) ?
                                ((bool)isFramework40).ToString() :
                                FormatOps.DisplayNull,
                            "isMono",
                            (isMono != null) ?
                                ((bool)isMono).ToString() :
                                FormatOps.DisplayNull,
                            "isDotNetCore",
                            (isDotNetCore != null) ?
                                ((bool)isDotNetCore).ToString() :
                                FormatOps.DisplayNull,
#if !NET_STANDARD_20
                            "frameworkExtraVersion",
                            (frameworkExtraVersion != null) ?
                                ((string)frameworkExtraVersion).ToString() :
                                FormatOps.DisplayNull,
#endif
                            "frameworkVersion",
                            (FrameworkVersion != null) ?
                                ((Version)FrameworkVersion).ToString() :
                                FormatOps.DisplayNull);
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "GetState",
                            typeof(CommonOps.Runtime).Name, true,
                            TracePriority.LockWarning,
                            MaybeWhoHasLock());
                    }
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static void ResetState()
            {
                bool locked = false;

                try
                {
                    TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        isFramework20 = null;
                        isFramework40 = null;
                        isMono = null;
                        isDotNetCore = null;

#if !NET_STANDARD_20
                        frameworkExtraVersion = null;
#endif

                        FrameworkVersion = null;
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "ResetState",
                            typeof(CommonOps.Runtime).Name, true,
                            TracePriority.LockWarning,
                            MaybeWhoHasLock());
                    }
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool SetManualOverride(
                RuntimeName name,
                ref Result result
                )
            {
                bool locked = false;

                try
                {
                    TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        switch (name)
                        {
                            case RuntimeName.None:
                                {
                                    StringList list = GetState();

                                    isFramework20 = null;
                                    isFramework40 = null;
                                    isMono = null;
                                    isDotNetCore = null;

                                    result = String.Format(
                                        "runtime manually reset from state {0}",
                                        FormatOps.WrapOrNull(list));

                                    return true;
                                }
                            case RuntimeName.NetFx:
                                {
                                    if (IsBuiltForCLRv4())
                                    {
                                        isFramework20 = false;
                                        isFramework40 = true;
                                    }
                                    else
                                    {
                                        isFramework20 = true;
                                        isFramework40 = false;
                                    }

                                    isMono = false;
                                    isDotNetCore = false;

                                    result = String.Format(
                                        "runtime manually overridden to {0}",
                                        FormatOps.WrapOrNull(MicrosoftRuntimeName));

                                    return true;
                                }
                            case RuntimeName.Mono:
                                {
                                    if (IsBuiltForCLRv4())
                                    {
                                        isFramework20 = false;
                                        isFramework40 = true;
                                    }
                                    else
                                    {
                                        isFramework20 = true;
                                        isFramework40 = false;
                                    }

                                    isMono = true;
                                    isDotNetCore = false;

                                    result = String.Format(
                                        "runtime manually overridden to {0}",
                                        FormatOps.WrapOrNull(MonoRuntimeName));

                                    return true;
                                }
                            case RuntimeName.DotNetCore:
                                {
                                    isFramework20 = false;
                                    isFramework40 = true;
                                    isMono = false;
                                    isDotNetCore = true;

                                    result = String.Format(
                                        "runtime manually overridden to {0}",
                                        FormatOps.WrapOrNull(DotNetCoreRuntimeName));

                                    return true;
                                }
                            case RuntimeName.DotNet: /* Mostly same as above. */
                                {
                                    isFramework20 = false;
                                    isFramework40 = true;
                                    isMono = false;
                                    isDotNetCore = true;

                                    result = String.Format(
                                        "runtime manually overridden to {0}",
                                        FormatOps.WrapOrNull(DotNetRuntimeName));

                                    return true;
                                }
                            default:
                                {
                                    result = String.Format(
                                        "unsupported runtime name {0}",
                                        FormatOps.WrapOrNull(name));

                                    break;
                                }
                        }
                    }
                    else
                    {
                        result = "unable to acquire lock";
                    }

                    return false;
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsMono()
            {
                try
                {
                    //
                    // BUGFIX: Without this short-circuit here, we always
                    //         try to grab the lock.  The problem here is
                    //         that this method is (often?) called from
                    //         several places in the hot-path (i.e. -AND-
                    //         deep down the stack) where that is not at
                    //         all desirable, e.g. from EnumOps.ToULong
                    //         and ThreadOps.GetDefaultLockTimeout.
                    //
                    bool? localIsMono = isMono; /* NO-LOCK */

                    if (localIsMono != null)
                        return (bool)localIsMono;

                    bool locked = false;

                    try
                    {
                        TryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            if (isMono == null)
                            {
                                if (Environment.DoesVariableExist(
                                        EnvVars.TreatAsMono))
                                {
                                    isMono = true;
                                }
                                else
                                {
                                    if ((MonoRuntimeType != null) &&
                                        (Type.GetType(
                                            MonoRuntimeType) != null))
                                    {
                                        isMono = true;
                                    }
                                    else
                                    {
                                        isMono = false;
                                    }
                                }
                            }

                            return (bool)isMono;
                        }
                        else
                        {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                            //
                            // BUGBUG: This should not be reached in
                            //         normal library operation.
                            //
                            TraceOps.LockTrace(
                                "IsMono",
                                typeof(CommonOps.Runtime).Name, true,
                                TracePriority.LockWarning,
                                MaybeWhoHasLock());
#endif

                            DebugOps.MaybeBreak(
                                "IsMono: unable to acquire lock");
                        }
                    }
                    finally
                    {
                        ExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            private static Version GetMonoVersion()
            {
                try
                {
                    if (MonoRuntimeType != null)
                    {
                        Type type = Type.GetType(MonoRuntimeType);

                        if ((type != null) && (MonoDisplayNameMember != null))
                        {
                            string displayName = type.InvokeMember(
                                MonoDisplayNameMember, ObjectOps.GetBindingFlags(
                                    MetaBindingFlags.PrivateStaticMethod, true),
                                null, null, null) as string;

                            if (!String.IsNullOrEmpty(displayName))
                            {
                                if (MonoVersionRegExList != null)
                                {
                                    foreach (Regex regEx in MonoVersionRegExList)
                                    {
                                        Match match = regEx.Match(displayName);

                                        if ((match != null) && match.Success)
                                            return new Version(match.Value);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsDotNetCore()
            {
                try
                {
                    bool? localIsDotNetCore = isDotNetCore; /* NO-LOCK */

                    if (localIsDotNetCore != null)
                        return (bool)localIsDotNetCore;

                    bool locked = false;

                    try
                    {
                        TryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            if (isDotNetCore == null)
                            {
                                if (Environment.DoesVariableExist(
                                        EnvVars.TreatAsDotNetCore))
                                {
                                    isDotNetCore = true;
                                }
                                else
                                {
                                    if (IsDotNetCore2x() ||
                                        IsDotNetCore3x() ||
                                        IsDotNetCore5xOrHigher())
                                    {
                                        isDotNetCore = true;
                                    }
                                    else
                                    {
                                        isDotNetCore = false;
                                    }
                                }
                            }

                            return (bool)isDotNetCore;
                        }
                        else
                        {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                            //
                            // BUGBUG: This should not be reached in
                            //         normal library operation.
                            //
                            TraceOps.LockTrace(
                                "IsDotNetCore",
                                typeof(CommonOps.Runtime).Name, true,
                                TracePriority.LockWarning,
                                MaybeWhoHasLock());
#endif

                            DebugOps.MaybeBreak(
                                "IsDotNetCore: unable to acquire lock");
                        }
                    }
                    finally
                    {
                        ExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsDotNetCore2x()
            {
                if (DotNetCore2xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore2xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsDotNetCore3x()
            {
                if (DotNetCore3xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore3xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsDotNetCore5xOrHigher()
            {
                if (DotNetCore5xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore5xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            public static bool IsDotNetCore7xOrHigher()
            {
                if (DotNetCore7xLibType == null)
                    return false;

                Type type = Type.GetType(DotNetCore7xLibType);

                if (type == null)
                    return false;

                return (type.GetProperty(DotNetCore7xLibProperty) != null);
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetDotNetCoreVersionFileName()
            {
                string directory = GetRuntimeDirectory();

                if (directory == null)
                    return null;

                if (DotNetCoreVersionFileName == null)
                    return null;

                return Path.Combine(directory, DotNetCoreVersionFileName);
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetDotNetCoreDllFileName()
            {
                return GetRuntimeDllFileName(DotNetCoreDllFileNames);
            }

            ///////////////////////////////////////////////////////////////////

            private static Version GetDotNetCoreVersionViaTextFile()
            {
                try
                {
                    string fileName = GetDotNetCoreVersionFileName();

                    if (fileName == null)
                        return null;

                    string[] lines = File.ReadAllLines(fileName); /* throw */

                    if (lines == null)
                        return null;

                    int length = lines.Length;

                    if (length == 0)
                        return null;

                    for (int index = length - 1; index >= 0; index--)
                    {
                        string line = lines[index];

                        if (String.IsNullOrEmpty(line))
                            continue;

                        try
                        {
                            return new Version(line); /* e.g. "2.0.6" */
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static Version GetDotNetCoreVersionViaDllDirectory()
            {
                try
                {
                    string fileName = GetDotNetCoreDllFileName();

                    if (fileName == null)
                        return null;

                    string directory = Path.GetDirectoryName(fileName);

                    if (String.IsNullOrEmpty(directory))
                        return null;

                    string fileNameOnly = Path.GetFileName(directory);

                    if (String.IsNullOrEmpty(fileNameOnly))
                        return null;

                    return new Version(fileNameOnly); /* e.g. "2.0.6" */
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static Version GetDotNetCoreVersion()
            {
                Version version = GetDotNetCoreVersionViaTextFile();

                if (version != null)
                    return version;

                return GetDotNetCoreVersionViaDllDirectory();
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetDotNetCoreExtraVersion()
            {
                try
                {
                    string fileName = GetDotNetCoreDllFileName();

                    if (fileName == null)
                        return null;

                    FileVersionInfo version = FileVersionInfo.GetVersionInfo(
                        fileName);

                    if (version == null)
                        return null;

                    return version.FileVersion;
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Runtime Information Methods
            public static string GetRuntimeName()
            {
                return GetRuntimeName(false);
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetRuntimeName(
                bool alternate
                )
            {
                if (IsMono())
                {
                    return alternate ?
                        AltMonoRuntimeName :
                        MonoRuntimeName;
                }

                if (IsDotNetCore())
                {
                    if (IsDotNetCore5xOrHigher())
                    {
                        return alternate ?
                            AltDotNetCoreRuntimeName :
                            DotNetRuntimeName;
                    }

                    return alternate ?
                        AltDotNetCoreRuntimeName :
                        DotNetCoreRuntimeName;
                }

                if (IsFramework20() || IsFramework40())
                {
                    return alternate ?
                        AltMicrosoftRuntimeName :
                        MicrosoftRuntimeName;
                }

                return UnknownRuntimeName;
            }

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && NATIVE && WINDOWS
            public static string GetNativeVersion()
            {
                if (IsMono())
                    return null;

                if (IsDotNetCore())
                    return null;

                if (IsFramework40())
                    return FrameworkVersion4;

                if (IsFramework20())
                    return FrameworkVersion2;

                return null;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This method returns what the image runtime version
            //       should be, not based on any assembly, but based on
            //       the runtime currently running.
            //
            public static string GetImageRuntimeVersion()
            {
                //
                // HACK: This code assumes that Mono and .NET Core
                //       always identify themselves as the CLRv4,
                //       which is true for (almost?) all "modern"
                //       versions of Mono and is always true for
                //       all released versions of .NET Core.
                //
                if (IsMono() || IsDotNetCore() || IsFramework40())
                    return ImageRuntimeVersion4;
                else
                    return ImageRuntimeVersion2;
            }

            ///////////////////////////////////////////////////////////////////

            public static Version GetRuntimeVersion()
            {
                if (IsMono())
                    return GetMonoVersion();

                if (IsDotNetCore())
                    return GetDotNetCoreVersion();

                //
                // HACK: Currently, the runtime version is the same as
                //       the framework version when running on the .NET
                //       Framework.
                //
                if (IsFramework20() || IsFramework40())
                    return GetFrameworkVersion();

                //
                // BUGBUG: No idea?
                //
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeBuild()
            {
                if (IsMono())
                    return null; /* TODO: No build number for Mono? */

                if (IsDotNetCore())
                    return null; /* TODO: No build number for .NET Core? */

                if (!IsFramework20() && !IsFramework40())
                    return null; /* TODO: No build number for unknown? */

                if (MicrosoftDllFileNames == null)
                    return null;

                string fileName = GetRuntimeDllFileName(MicrosoftDllFileNames);

                if (fileName == null)
                    return null;

                try
                {
                    FileVersionInfo version = FileVersionInfo.GetVersionInfo(
                        fileName);

                    if (version == null)
                        return null;

                    return version.FileVersion;
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeExtraVersion()
            {
                if (IsMono())
                    return null; /* TODO: No extra info for Mono? */

                if (IsDotNetCore())
                    return GetDotNetCoreExtraVersion();

#if !NET_STANDARD_20
                //
                // HACK: Currently, the runtime version is the same as
                //       the framework version when not running on Mono.
                //
                return GetFrameworkExtraVersion();
#else
                return null;
#endif
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeNameAndVersion()
            {
                return FormatOps.NameAndVersion(
                    GetRuntimeName(), GetRuntimeVersion(),
                    GetRuntimeBuild(), GetRuntimeExtraVersion()
                );
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeNameAndVMajorMinor()
            {
                return GetRuntimeNameAndVMajorMinor(true);
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeNameAndVMajorMinor(
                bool alternate
                )
            {
                return String.Format("{0} {1}", GetRuntimeName(alternate),
                    FormatOps.VMajorMinorOrNull(GetRuntimeVersion()));
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetRuntimeDirectory()
            {
                try
                {
                    Assembly assembly = typeof(Object).Assembly;

                    if (assembly == null)
                        return null;

                    return Path.GetDirectoryName(assembly.Location);
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

            private static string GetRuntimeDllFileName(
                IEnumerable<string> fileNames
                )
            {
                if (fileNames == null)
                    return null;

                string directory = GetRuntimeDirectory();

                if (directory == null)
                    return null;

                foreach (string fileNameOnly in fileNames)
                {
                    if (String.IsNullOrEmpty(fileNameOnly))
                        continue;

                    string fileName = Path.Combine(
                        directory, fileNameOnly);

                    if (File.Exists(fileName))
                        return fileName;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

#if !DEBUG
            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            // WARNING: For use by the AreSecurityPackagesLikelyBroken
            //          method only.
            //
            public static bool IsRuntime20()
            {
                Version version = GetRuntimeVersion();

                return (version != null) && (version.Major == 2);
            }
#endif

            ///////////////////////////////////////////////////////////////////

            private static bool IsBuiltForCLRv4()
            {
#if NET_40
                return true;
#else
                return false;
#endif
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Runtime Checking Methods
            public static ReturnCode CheckDefineConstants(
                StringList defines, /* in */
                ref Result error    /* out */
                )
            {
                if (defines == null)
                {
                    error = "invalid define constants";
                    return ReturnCode.Error;
                }

                StringDictionary dictionary = new StringDictionary(
                    defines, true, false);

                StringList wantedOptions;
                StringList unwantedOptions;

                if (IsDotNetCore())
                {
                    wantedOptions = DefineConstants.DotNetCore;
                    unwantedOptions = DefineConstants.DotNetFramework;
                }
                else
                {
                    wantedOptions = DefineConstants.DotNetFramework;
                    unwantedOptions = DefineConstants.DotNetCore;
                }

                StringList missingOptions = null;

                if (wantedOptions != null)
                {
                    wantedOptions = new StringList(wantedOptions);

                    foreach (string option in wantedOptions)
                    {
                        if (option == null)
                            continue;

                        if (!dictionary.ContainsKey(option))
                        {
                            if (missingOptions == null)
                                missingOptions = new StringList();

                            missingOptions.Add(option);
                        }
                    }

                    //
                    // HACK: If at least one of the "wanted" option was
                    //       found, we (successfully) found the target
                    //       runtime -AND- this is not an error.
                    //
                    if ((missingOptions != null) &&
                        (missingOptions.Count < wantedOptions.Count))
                    {
                        missingOptions = null;
                    }
                }

                StringList extraOptions = null;

                if (unwantedOptions != null)
                {
                    unwantedOptions = new StringList(unwantedOptions);

                    foreach (string option in unwantedOptions)
                    {
                        if (option == null)
                            continue;

                        if (dictionary.ContainsKey(option))
                        {
                            if (extraOptions == null)
                                extraOptions = new StringList();

                            extraOptions.Add(option);
                        }
                    }
                }

                if ((missingOptions == null) && (extraOptions == null))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "check failed, missing {0}, extra {1}",
                        FormatOps.WrapOrNull(missingOptions),
                        FormatOps.WrapOrNull(extraOptions));

                    return ReturnCode.Error;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Framework Information Methods
#if NET_40 && !NET_STANDARD_20
            private static int GetFrameworkSetup451Value()
            {
                if (PlatformOps.IsWindows81() ||
                    PlatformOps.IsWindowsServer2012R2())
                {
                    return FrameworkSetup451OnWindows81Value;
                }

                return FrameworkSetup451Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup46Value()
            {
                return PlatformOps.IsWindows10OrHigher() ?
                    FrameworkSetup46OnWindows10Value : FrameworkSetup46Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup461Value()
            {
                return PlatformOps.IsWindows10NovemberUpdate() ?
                    FrameworkSetup461OnWindows10Value : FrameworkSetup461Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup462Value()
            {
                if (PlatformOps.IsWindows10AnniversaryUpdate() ||
                    PlatformOps.IsWindowsServer2016())
                {
                    return FrameworkSetup462OnWindows10Value;
                }

                return FrameworkSetup462Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup47Value()
            {
                return PlatformOps.IsWindows10CreatorsUpdate() ?
                    FrameworkSetup47OnWindows10Value : FrameworkSetup47Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup471Value()
            {
                if (PlatformOps.IsWindows10FallCreatorsUpdate() ||
                    PlatformOps.IsWindowsServerVersion1709())
                {
                    return FrameworkSetup471OnWindows10Value;
                }

                return FrameworkSetup471Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup472Value()
            {
                if (PlatformOps.IsWindows10April2018Update() ||
                    PlatformOps.IsWindowsServerVersion1803())
                {
                    return FrameworkSetup472OnWindows10Value;
                }

                return FrameworkSetup472Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup48Value()
            {
                if (PlatformOps.IsWindows11() ||
                    PlatformOps.IsWindowsServer2022())
                {
                    return FrameworkSetup48OnWindows11Value;
                }

                if (PlatformOps.IsWindows10May2019Update() ||
                    PlatformOps.IsWindows10November2019Update())
                {
                    return FrameworkSetup48OnWindows10Value1;
                }

                if (PlatformOps.IsWindows10May2020Update() ||
                    PlatformOps.IsWindows10October2020Update())
                {
                    return FrameworkSetup48OnWindows10Value2;
                }

                return FrameworkSetup48Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup481Value()
            {
                return PlatformOps.IsWindows11September2022Update() ?
                    FrameworkSetup481OnWindows11Value : FrameworkSetup481Value;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            public static Version GetFrameworkVersion()
            {
                Version localFrameworkVersion = FrameworkVersion; /* NO-LOCK */

                if (localFrameworkVersion != null)
                    return localFrameworkVersion;

                bool locked = false;

                try
                {
                    TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        if (FrameworkVersion == null)
                            FrameworkVersion = SysEnv.Version;

                        return FrameworkVersion;
                    }
                    else
                    {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        //
                        // BUGBUG: This should not be reached in
                        //         normal library operation.
                        //
                        TraceOps.LockTrace(
                            "GetFrameworkVersion",
                            typeof(CommonOps.Runtime).Name, true,
                            TracePriority.LockWarning,
                            MaybeWhoHasLock());
#endif

                        DebugOps.MaybeBreak(
                            "GetFrameworkVersion: unable to acquire lock");
                    }
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            public static string GetFrameworkExtraVersion()
            {
                try
                {
                    string localFrameworkExtraVersion =
                        frameworkExtraVersion; /* NO-LOCK */

                    if (localFrameworkExtraVersion != null)
                        return localFrameworkExtraVersion;

                    bool locked = false;

                    try
                    {
                        TryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            if (frameworkExtraVersion == null)
                            {
                                object value = null;

                                if (IsFramework40())
                                {
                                    if ((FrameworkSetup40KeyName != null) &&
                                        (FrameworkSetup40ValueName != null))
                                    {
                                        value = Registry.GetValue(
                                            FrameworkSetup40KeyName,
                                            FrameworkSetup40ValueName, null);
                                    }
                                }
                                else if (IsFramework20())
                                {
                                    if ((FrameworkSetup20KeyName != null) &&
                                        (FrameworkSetup20ValueName != null))
                                    {
                                        value = Registry.GetValue(
                                            FrameworkSetup20KeyName,
                                            FrameworkSetup20ValueName, null);
                                    }
                                }

                                //
                                // NOTE: The value may still be null at this
                                //       point and that means this code may
                                //       be executed again the next time this
                                //       method is called (i.e. we have no
                                //       way of caching the null value).
                                //
                                frameworkExtraVersion = (value != null) ?
                                    value.ToString() : null;
                            }

                            return frameworkExtraVersion;
                        }
                        else
                        {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                            //
                            // BUGBUG: This should not be reached in
                            //         normal library operation.
                            //
                            TraceOps.LockTrace(
                                "GetFrameworkExtraVersion",
                                typeof(CommonOps.Runtime).Name, true,
                                TracePriority.LockWarning,
                                MaybeWhoHasLock());
#endif

                            DebugOps.MaybeBreak(
                                "GetFrameworkExtraVersion: unable to acquire lock");
                        }
                    }
                    finally
                    {
                        ExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Framework Version Detection Methods
            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            public static bool IsFramework20()
            {
                try
                {
                    bool? localIsFramework20 = isFramework20; /* NO-LOCK */

                    if (localIsFramework20 != null)
                        return (bool)localIsFramework20;

                    bool locked = false;

                    try
                    {
                        TryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            if (localIsFramework20 == null)
                            {
                                if (Environment.DoesVariableExist(
                                        EnvVars.TreatAsFramework20))
                                {
                                    isFramework20 = true;
                                }
                                else
                                {
                                    if (IsFramework2x())
                                    {
                                        isFramework20 = true;
                                    }
                                    else
                                    {
                                        isFramework20 = false;
                                    }
                                }
                            }

                            return (bool)isFramework20;
                        }
                        else
                        {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                            //
                            // BUGBUG: This should not be reached in
                            //         normal library operation.
                            //
                            TraceOps.LockTrace(
                                "IsFramework20",
                                typeof(CommonOps.Runtime).Name, true,
                                TracePriority.LockWarning,
                                MaybeWhoHasLock());
#endif

                            DebugOps.MaybeBreak(
                                "IsFramework20: unable to acquire lock");
                        }
                    }
                    finally
                    {
                        ExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            public static bool IsFramework40()
            {
                try
                {
                    bool? localIsFramework40 = isFramework40; /* NO-LOCK */

                    if (localIsFramework40 != null)
                        return (bool)localIsFramework40;

                    bool locked = false;

                    try
                    {
                        TryLock(ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            if (localIsFramework40 == null)
                            {
                                if (Environment.DoesVariableExist(
                                        EnvVars.TreatAsFramework40))
                                {
                                    isFramework40 = true;
                                }
                                else
                                {
                                    if (IsFramework4x())
                                    {
                                        isFramework40 = true;
                                    }
                                    else
                                    {
                                        isFramework40 = false;
                                    }
                                }
                            }

                            return (bool)isFramework40;
                        }
                        else
                        {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                            //
                            // BUGBUG: This should not be reached in
                            //         normal library operation.
                            //
                            TraceOps.LockTrace(
                                "IsFramework40",
                                typeof(CommonOps.Runtime).Name, true,
                                TracePriority.LockWarning,
                                MaybeWhoHasLock());
#endif

                            DebugOps.MaybeBreak(
                                "IsFramework40: unable to acquire lock");
                        }
                    }
                    finally
                    {
                        ExitLock(ref locked); /* TRANSACTIONAL */
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework2x()
            {
                Version version = GetFrameworkVersion();

                return (version != null) && (version.Major == 2);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework4x()
            {
                Version version = GetFrameworkVersion();

                return (version != null) && (version.Major == 4);
            }

            ///////////////////////////////////////////////////////////////////

#if NET_40 && !NET_STANDARD_20
            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            public static bool IsFramework45OrHigher()
            {
                Version version = GetFrameworkVersion();
                int extraValue;

                if (!int.TryParse(
                        GetFrameworkExtraVersion(), out extraValue))
                {
                    return false;
                }

                if (IsFramework45(version, extraValue))
                    return true;

                if (IsFramework451(version, extraValue))
                    return true;

                if (IsFramework452(version, extraValue))
                    return true;

                if (IsFramework46(version, extraValue))
                    return true;

                if (IsFramework461(version, extraValue))
                    return true;

                if (IsFramework462(version, extraValue))
                    return true;

                if (IsFramework47(version, extraValue))
                    return true;

                if (IsFramework471(version, extraValue))
                    return true;

                if (IsFramework472(version, extraValue))
                    return true;

                if (IsFramework48(version, extraValue))
                    return true;

                if (IsFramework481(version, extraValue))
                    return true;

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework 4.5+ "Extra Version" Methods
            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework45(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= FrameworkSetup45Value);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework451(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup451Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework452(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= FrameworkSetup452Value);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework46(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup46Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework461(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup461Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework462(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup462Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework47(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup47Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework471(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup471Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework472(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup472Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework48(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup48Value());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            private static bool IsFramework481(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup481Value());
            }
            #endregion
#endif
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Environment Variable Support Class
        [ObjectId("24328505-60ed-4a79-89dd-41014d024f6d")]
        internal static class Environment
        {
            #region Private Constants
            //
            // NOTE: This is the value to return when an exception is caught
            //       within methods that return a string.
            //
            // HACK: This is purposely not read-only.
            //
            private static string ExceptionStringValue = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the value to return when an exception is caught
            //       within methods that return an object (of some kind).
            //
            // HACK: This is purposely not read-only.
            //
            private static object ExceptionObjectValue = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            //
            // NOTE: This lock is used to protect access to the environment
            //       variables for the current process, when accessed via
            //       this class.
            //
            private static readonly object syncRoot = new object();
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Raw Get / Set / Unset (With Exceptions)
            private static string GetVariableWithThrow(
                string variable /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return SysEnv.GetEnvironmentVariable(
                        variable); /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static void SetVariableWithThrow(
                string variable, /* in */
                string value     /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    SysEnv.SetEnvironmentVariable(
                        variable, value); /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static void UnsetVariableWithThrow(
                string variable /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    SysEnv.SetEnvironmentVariable(
                        variable, null); /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static IDictionary GetRawVariablesWithThrow()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return SysEnv.GetEnvironmentVariables(); /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static StringDictionary GetVariablesWithThrow()
            {
                IDictionary dictionary = GetRawVariablesWithThrow(); /* throw */

                if (dictionary == null)
                    return null;

                return new StringDictionary(dictionary);
            }

            ///////////////////////////////////////////////////////////////////

            private static string ExpandVariablesWithThrow(
                string name /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return SysEnv.ExpandEnvironmentVariables(
                        name); /* throw */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Raw Get / Set / Unset (Without Exceptions)
            public static string GetVariable(
                string variable /* in */
                )
            {
                try
                {
                    return GetVariableWithThrow(variable); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return ExceptionStringValue;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool SetVariable(
                string variable, /* in */
                string value     /* in */
                )
            {
                try
                {
                    SetVariableWithThrow(variable, value); /* throw */
                    return true;
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool UnsetVariable(
                string variable /* in */
                )
            {
                try
                {
                    UnsetVariableWithThrow(variable); /* throw */
                    return true;
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static StringDictionary GetVariables()
            {
                try
                {
                    return GetVariablesWithThrow(); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return (StringDictionary)ExceptionObjectValue;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static string ExpandVariables(
                string name /* in */
                )
            {
                try
                {
                    return ExpandVariablesWithThrow(name); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return ExceptionStringValue;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Composite Get / Set / Unset (With Exceptions)
            private static string GetAndUnsetVariableWithThrow(
                string variable /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    string value = GetVariableWithThrow(
                        variable); /* throw */

                    if (value != null)
                    {
                        /* NO RESULT */
                        UnsetVariableWithThrow(
                            variable); /* throw */
                    }

                    return value;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static string ChangeVariableWithThrow(
                string variable, /* in */
                string value     /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    string oldValue = GetVariableWithThrow(
                        variable); /* throw */

                    /* NO RESULT */
                    SetVariableWithThrow(
                        variable, value); /* throw */

                    return oldValue;
                }
            }

            ///////////////////////////////////////////////////////////////////

            private static string MaybeChangeVariableWithThrow(
                string variable, /* in */
                string oldValue, /* in */
                string newValue  /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    string localOldValue = GetVariableWithThrow(
                        variable); /* throw */

                    if (SharedStringOps.SystemEquals(
                            localOldValue, oldValue))
                    {
                        /* NO RESULT */
                        SetVariableWithThrow(
                            variable, newValue); /* throw */
                    }

                    return localOldValue;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Composite Get / Set / Unset (Without Exceptions)
            public static string GetAndUnsetVariable(
                string variable /* in */
                )
            {
                try
                {
                    return GetAndUnsetVariableWithThrow(
                        variable); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return ExceptionStringValue;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static string ChangeVariable(
                string variable, /* in */
                string value     /* in */
                )
            {
                try
                {
                    return ChangeVariableWithThrow(
                        variable, value); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return ExceptionStringValue;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static string MaybeChangeVariable(
                string variable, /* in */
                string oldValue, /* in */
                string newValue  /* in */
                )
            {
                try
                {
                    return MaybeChangeVariableWithThrow(
                        variable, oldValue, newValue); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return ExceptionStringValue;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Does Exist Once (With Exceptions)
            private static bool DoesVariableExistOnceWithThrow(
                string name,     /* in */
                ref string value /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    value = GetVariableWithThrow(name); /* throw */

                    if (value == null)
                        return false;

                    /* NO RESULT */
                    UnsetVariableWithThrow(name); /* throw */

                    return true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Does Exist Once (Without Exceptions)
            public static bool DoesVariableExistOnce(
                string name /* in */
                )
            {
                try
                {
                    string value = null;

                    return DoesVariableExistOnceWithThrow(
                        name, ref value); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool DoesVariableExistOnce(
                string name,     /* in */
                ref string value /* out */
                )
            {
                try
                {
                    return DoesVariableExistOnceWithThrow(
                        name, ref value); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Does Exist (With Exceptions)
            public static bool DoesVariableExistWithThrow(
                string name,     /* in */
                ref string value /* out */
                )
            {
                value = GetVariableWithThrow(name); /* throw */

                return value != null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Does Exist (Without Exceptions)
            public static bool DoesVariableExist(
                string name,     /* in */
                ref string value /* out */
                )
            {
                try
                {
                    return DoesVariableExistWithThrow(
                        name, ref value); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool DoesVariableExist(
                string name /* in */
                )
            {
                try
                {
                    string value = null;

                    return DoesVariableExistWithThrow(
                        name, ref value); /* throw */
                }
                catch (Exception)
                {
                    //
                    // BUGBUG: This method cannot throw -AND- we cannot
                    //         use TraceOps nor the complaint subsystem
                    //         of the DebugOps class here as they call
                    //         into us.
                    //
                    return false;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Save / Set / Restore
            public static bool SaveVariables(
                IEnumerable<string> names, /* in */
                ref IClientData clientData /* out */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    EnvironmentClientData environmentClientData =
                        new EnvironmentClientData(names);

                    if (environmentClientData.Save(names))
                    {
                        clientData = environmentClientData;
                        return true;
                    }

                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool SetVariables(
                IEnumerable<string> names, /* in */
                IClientData clientData     /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    EnvironmentClientData environmentClientData =
                        clientData as EnvironmentClientData;

                    if (environmentClientData == null)
                        return false;

                    return environmentClientData.SetOrUnset(
                        names, SetDirection.Set);
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool RestoreVariables(
                IEnumerable<string> names, /* in */
                IClientData clientData     /* in */
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    EnvironmentClientData environmentClientData =
                        clientData as EnvironmentClientData;

                    if (environmentClientData == null)
                        return false;

                    return environmentClientData.Restore(names);
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Code Support Class
        [ObjectId("b1bf9f59-a8a5-45b3-b54b-02c3d7107b85")]
        internal static class HashCodes
        {
            public static int Combine(
                int X,
                int Y
                )
            {
                byte[] bytes = new byte[sizeof(int) * 2];

                Array.Copy(BitConverter.GetBytes(X),
                    0, bytes, 0, sizeof(int));

                Array.Copy(BitConverter.GetBytes(Y),
                    0, bytes, sizeof(int), sizeof(int));

                return ConversionOps.ToInt(MathOps.HashFnv1UInt(bytes, true));
            }

            ///////////////////////////////////////////////////////////////////

            public static int Combine(
                int X,
                int Y,
                int Z
                )
            {
                byte[] bytes = new byte[sizeof(int) * 3];

                Array.Copy(BitConverter.GetBytes(X),
                    0, bytes, 0, sizeof(int));

                Array.Copy(BitConverter.GetBytes(Y),
                    0, bytes, sizeof(int), sizeof(int));

                Array.Copy(BitConverter.GetBytes(Z),
                    0, bytes, sizeof(int) * 2, sizeof(int));

                return ConversionOps.ToInt(MathOps.HashFnv1UInt(bytes, true));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Bi-directional Looping Support Methods
        public static bool ForCondition(
            bool increment,
            int index,
            int lowerBound,
            int upperBound
            )
        {
            if (increment)
                return (index <= upperBound);
            else
                return (index >= lowerBound);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ForLoop(
            bool increment,
            ref int index
            )
        {
            if (increment)
                index++;
            else
                index--;
        }
        #endregion
    }
}
