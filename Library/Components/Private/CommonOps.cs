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
    /// <summary>
    /// This class provides a collection of common, low-level
    /// operations used throughout the Eagle core library, including
    /// managed runtime detection, environment variable access, hash
    /// code combination, and bi-directional looping helpers.
    /// </summary>
    [ObjectId("c385e1b9-95b0-4cd5-b0d9-a5fe582d7162")]
    internal static class CommonOps
    {
        #region Runtime Detection Support Class
        /// <summary>
        /// This class provides support for detecting the managed runtime
        /// that the Eagle core library is currently executing within
        /// (e.g. the .NET Framework, Mono, .NET Core, or .NET 5.0 and
        /// higher) as well as its associated version information.
        /// </summary>
        [ObjectId("e9622641-301b-4208-a5cc-3801edf4854e")]
        internal static class Runtime
        {
            #region Public Constants
            /// <summary>
            /// The image runtime version string used by the version 2.0 of
            /// the .NET Framework runtime.
            /// </summary>
            public static readonly string ImageRuntimeVersion2 = "v2.0.50727";
            /// <summary>
            /// The image runtime version string used by the version 4.0 of
            /// the .NET Framework runtime.
            /// </summary>
            public static readonly string ImageRuntimeVersion4 = "v4.0.30319";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constants
            /// <summary>
            /// The fully qualified name of the type that is present only when
            /// running on the Mono runtime.
            /// </summary>
            private static readonly string MonoRuntimeType = "Mono.Runtime";
            /// <summary>
            /// The name of the member used to query the display name (i.e.
            /// the version) of the Mono runtime.
            /// </summary>
            private static readonly string MonoDisplayNameMember = "GetDisplayName";

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework Constants
#if !NET_STANDARD_20 && NATIVE && WINDOWS
            /// <summary>
            /// The version string for the version 2.0 of the .NET Framework
            /// runtime.
            /// </summary>
            private static readonly string FrameworkVersion2 = "v2.0.50727";
            /// <summary>
            /// The version string for the version 4.0 of the .NET Framework
            /// runtime.
            /// </summary>
            private static readonly string FrameworkVersion4 = "v4.0.30319";
#endif

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The candidate native runtime library file names used to detect
            /// a Microsoft .NET Framework runtime.
            /// </summary>
            private static readonly StringList MicrosoftDllFileNames =
                new StringList(new string[] {
                "mscorwks.dll",
                "clr.dll"
            });
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET Core Constants
            /// <summary>
            /// The name of the file, located within the runtime directory,
            /// that may contain the version of the .NET Core runtime.
            /// </summary>
            private static readonly string DotNetCoreVersionFileName =
                ".version"; /* TODO: Is this official and/or documented? */

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The candidate native runtime library file names used to detect
            /// a .NET Core runtime.
            /// </summary>
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
            /// <summary>
            /// The fully qualified name of the type whose presence indicates
            /// the .NET Core 2.x runtime.
            /// </summary>
            private static readonly string DotNetCore2xLibType =
                "System.CoreLib";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Apparently, the "System.CoreLib" static class type is
            //       completely missing from .NET Core 3.x (?).
            //
            /// <summary>
            /// The fully qualified name of the type whose presence indicates
            /// the .NET Core 3.x runtime.
            /// </summary>
            private static readonly string DotNetCore3xLibType =
                "System.Private.CoreLib.Resources.Strings";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Apparently, things have been moved (again) in the .NET
            //       5.x (and later?) runtime (?).
            //
            /// <summary>
            /// The fully qualified name of the type whose presence indicates
            /// the .NET 5.0 (or higher) runtime.
            /// </summary>
            private static readonly string DotNetCore5xLibType =
                "System.Private.CoreLib.Strings";

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: The .NET 7.x (and later) runtime include new properties
            //       in the DateTime class, namely Microsecond, et al.
            //
            /// <summary>
            /// The fully qualified name of the type used, together with a
            /// particular property, to indicate the .NET 7.0 (or higher)
            /// runtime.
            /// </summary>
            private static readonly string DotNetCore7xLibType =
                "System.DateTime";

            /// <summary>
            /// The name of the property whose presence on the associated type
            /// indicates the .NET 7.0 (or higher) runtime.
            /// </summary>
            private static readonly string DotNetCore7xLibProperty =
                "Microsecond";
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Create a list of regular expression patterns to check the
            //       Mono runtime version against.
            //
            /// <summary>
            /// The list of regular expression patterns used to extract the
            /// version number from the Mono runtime display name.
            /// </summary>
            private static readonly RegExList MonoVersionRegExList =
                new RegExList(new Regex[] {
                RegExOps.Create(" (\\d+(?:\\.\\d+)+)$", /* NOTE: Pre-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
                    RegexOptions.Compiled),
                RegExOps.Create("^(\\d+(?:\\.\\d+)+) ", /* NOTE: Post-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
                    RegexOptions.Compiled)
            });

            ///////////////////////////////////////////////////////////////////

            #region Runtime Name Constants
            /// <summary>
            /// The display name used for the .NET Core runtime.
            /// </summary>
            private static readonly string DotNetCoreRuntimeName = ".NET Core";
            /// <summary>
            /// The display name used for the .NET 5.0 (or higher) runtime.
            /// </summary>
            private static readonly string DotNetRuntimeName = ".NET";

            /// <summary>
            /// The display name used for the Mono runtime.
            /// </summary>
            private static readonly string MonoRuntimeName = "Mono";
            /// <summary>
            /// The display name used for the Microsoft .NET Framework
            /// runtime.
            /// </summary>
            private static readonly string MicrosoftRuntimeName = "Microsoft.NET";

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The alternate display name used for the .NET Core (and .NET
            /// 5.0 or higher) runtime.
            /// </summary>
            private static readonly string AltDotNetCoreRuntimeName = "CoreCLR";
            /// <summary>
            /// The alternate display name used for the Microsoft .NET
            /// Framework runtime.
            /// </summary>
            private static readonly string AltMicrosoftRuntimeName = "CLR";
            /// <summary>
            /// The alternate display name used for the Mono runtime.
            /// </summary>
            private static readonly string AltMonoRuntimeName = "Mono";

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The display name used when the managed runtime cannot be
            /// determined.
            /// </summary>
            private static readonly string UnknownRuntimeName = "Unknown";
            #endregion

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            #region Framework Registry Key Name Constants
            /// <summary>
            /// The registry key name used to query the extra version
            /// information for the version 2.0 of the .NET Framework.
            /// </summary>
            private static readonly string FrameworkSetup20KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727";

            /// <summary>
            /// The registry value name used to query the extra version
            /// information for the version 2.0 of the .NET Framework.
            /// </summary>
            private static readonly string FrameworkSetup20ValueName = "Increment";

            /// <summary>
            /// The registry key name used to query the extra version
            /// information for the version 4.0 of the .NET Framework.
            /// </summary>
            private static readonly string FrameworkSetup40KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full";

            /// <summary>
            /// The registry value name used to query the extra version
            /// information for the version 4.0 of the .NET Framework.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.5 is installed.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.5.1 is installed.
            /// </summary>
            private static readonly int FrameworkSetup451Value = 378758; // >= indicates 4.5.1
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.5.1 is installed on Windows 8.1.
            /// </summary>
            private static readonly int FrameworkSetup451OnWindows81Value = 378675; // >= indicates 4.5.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value indicates the .NET Framework 4.5.2.  It was
            //       obtained from MSDN.
            //
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.5.2 is installed.
            /// </summary>
            private static readonly int FrameworkSetup452Value = 379893; // >= indicates 4.5.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.  They were
            //       obtained from MSDN.
            //
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6 is installed.
            /// </summary>
            private static readonly int FrameworkSetup46Value = 393297; // >= indicates 4.6
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6.1 is installed.
            /// </summary>
            private static readonly int FrameworkSetup461Value = 394271; // >= indicates 4.6.1
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6.1 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6.2 is installed.
            /// </summary>
            private static readonly int FrameworkSetup462Value = 394806; // >= indicates 4.6.2
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.6.2 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7 is installed.
            /// </summary>
            private static readonly int FrameworkSetup47Value = 460805; // >= indicates 4.7
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7.1 is installed.
            /// </summary>
            private static readonly int FrameworkSetup471Value = 461310; // >= indicates 4.7.1
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7.1 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7.2 is installed.
            /// </summary>
            private static readonly int FrameworkSetup472Value = 461814; // >= indicates 4.7.2
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.7.2 is installed on Windows 10.
            /// </summary>
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
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.8 is installed.
            /// </summary>
            private static readonly int FrameworkSetup48Value = 528049; // >= indicates 4.8
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.8 is installed on Windows 10.
            /// </summary>
            private static readonly int FrameworkSetup48OnWindows10Value1 = 528040; // >= indicates 4.8
            /// <summary>
            /// An additional minimum registry release value that indicates
            /// the .NET Framework 4.8 is installed on Windows 10.
            /// </summary>
            private static readonly int FrameworkSetup48OnWindows10Value2 = 528372; // >= indicates 4.8
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.8 is installed on Windows 11.
            /// </summary>
            private static readonly int FrameworkSetup48OnWindows11Value = 528449; // >= indicates 4.8

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.8.1.  They were
            //       obtained from MSDN.
            //
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.8.1 is installed.
            /// </summary>
            private static readonly int FrameworkSetup481Value = 533325; // >= indicates 4.8.1
            /// <summary>
            /// The minimum registry release value that indicates the .NET
            /// Framework 4.8.1 is installed on Windows 11.
            /// </summary>
            private static readonly int FrameworkSetup481OnWindows11Value = 533320; // >= indicates 4.8.1
            #endregion
#endif
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            /// <summary>
            /// The object used to synchronize access to the runtime detection
            /// state of this class.
            /// </summary>
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The identifier of the thread that currently holds the
            /// synchronization lock, or zero if none.
            /// </summary>
            private static long lockThreadId = 0;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// When non-null, the cached result indicating whether the
            /// current runtime is the version 2.0 of the .NET Framework.
            /// </summary>
            private static bool? isFramework20 = null;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// When non-null, the cached result indicating whether the
            /// current runtime is the version 4.0 of the .NET Framework.
            /// </summary>
            private static bool? isFramework40 = null;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// When non-null, the cached result indicating whether the
            /// current runtime is the Mono runtime.
            /// </summary>
            private static bool? isMono = null;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// When non-null, the cached result indicating whether the
            /// current runtime is the .NET Core (or .NET 5.0 or higher)
            /// runtime.
            /// </summary>
            private static bool? isDotNetCore = null;

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            /// <summary>
            /// When non-null, the cached extra version information for the
            /// current .NET Framework runtime.
            /// </summary>
            private static string frameworkExtraVersion = null;
#endif

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// When non-null, the cached version of the current .NET
            /// Framework runtime.
            /// </summary>
            private static Version FrameworkVersion = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Threading Cooperative Locking Diagnostic Methods
            /// <summary>
            /// This method gets the identifier of the thread that currently
            /// holds the synchronization lock, if any.
            /// </summary>
            /// <returns>
            /// The identifier of the thread that currently holds the lock, or
            /// zero if none.
            /// </returns>
            private static long MaybeWhoHasLock()
            {
                return Interlocked.CompareExchange(
                    ref lockThreadId, 0, 0);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method records the current thread as the holder of the
            /// synchronization lock when the lock has been successfully
            /// acquired.
            /// </summary>
            /// <param name="locked">
            /// Non-zero if the synchronization lock has been acquired.
            /// </param>
            private static void MaybeSomebodyHasLock(
                bool locked /* in */
                )
            {
                if (locked)
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(ref lockThreadId,
                        GlobalState.GetCurrentLockThreadId(), 0);
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method clears the recorded holder of the synchronization
            /// lock when the lock is about to be released.
            /// </summary>
            /// <param name="locked">
            /// Non-zero if the synchronization lock is currently held.
            /// </param>
            private static void MaybeNobodyHasLock(
                bool locked /* in */
                )
            {
                if (locked)
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(ref lockThreadId,
                        0, GlobalState.GetCurrentLockThreadId());
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Threading Cooperative Locking Methods
            /// <summary>
            /// This method attempts to acquire the synchronization lock for
            /// this class without blocking.
            /// </summary>
            /// <param name="locked">
            /// Upon return, this is set to non-zero if the synchronization
            /// lock was acquired by this thread; otherwise, it is set to
            /// false.
            /// </param>
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

            /// <summary>
            /// This method releases the synchronization lock for this class
            /// if it is currently held by this thread.
            /// </summary>
            /// <param name="locked">
            /// Upon entry, non-zero if the synchronization lock is held by
            /// this thread; upon return, this is set to false.
            /// </param>
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
            /// <summary>
            /// This method forces the runtime detection state of this class
            /// to be populated, optionally resetting any previously cached
            /// state first.
            /// </summary>
            /// <param name="force">
            /// Non-zero to reset any previously cached detection state before
            /// re-populating it.
            /// </param>
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

            /// <summary>
            /// This method builds a list of name/value pairs describing the
            /// current runtime detection state of this class.
            /// </summary>
            /// <returns>
            /// A list of name/value pairs describing the current detection
            /// state, or null if the synchronization lock could not be
            /// acquired.
            /// </returns>
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

            /// <summary>
            /// This method resets all cached runtime detection state
            /// maintained by this class.
            /// </summary>
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

            /// <summary>
            /// This method manually overrides the cached runtime detection
            /// state so that this class reports the specified managed
            /// runtime.
            /// </summary>
            /// <param name="name">
            /// The runtime to force this class to report, or <see
            /// cref="RuntimeName.None" /> to reset the detection state.
            /// </param>
            /// <param name="result">
            /// Upon success, this contains a message describing the change;
            /// upon failure, this contains an error message.
            /// </param>
            /// <returns>
            /// True if the override was applied successfully; otherwise,
            /// false.
            /// </returns>
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

            /// <summary>
            /// This method determines whether the current managed runtime is
            /// the Mono runtime, caching the result for subsequent calls.
            /// </summary>
            /// <returns>
            /// True if the current runtime is the Mono runtime; otherwise,
            /// false.
            /// </returns>
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

            /// <summary>
            /// This method attempts to query the version of the Mono runtime
            /// via its display name.
            /// </summary>
            /// <returns>
            /// The version of the Mono runtime, or null if it could not be
            /// determined.
            /// </returns>
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

            /// <summary>
            /// This method determines whether the current managed runtime is
            /// the .NET Core (or .NET 5.0 or higher) runtime, caching the
            /// result for subsequent calls.
            /// </summary>
            /// <returns>
            /// True if the current runtime is the .NET Core (or higher)
            /// runtime; otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method determines whether the current managed runtime
            /// appears to be the .NET Core 2.x runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime appears to be the .NET Core 2.x
            /// runtime; otherwise, false.
            /// </returns>
            public static bool IsDotNetCore2x()
            {
                if (DotNetCore2xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore2xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the current managed runtime
            /// appears to be the .NET Core 3.x runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime appears to be the .NET Core 3.x
            /// runtime; otherwise, false.
            /// </returns>
            public static bool IsDotNetCore3x()
            {
                if (DotNetCore3xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore3xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the current managed runtime
            /// appears to be the .NET 5.0 (or higher) runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime appears to be the .NET 5.0 (or
            /// higher) runtime; otherwise, false.
            /// </returns>
            public static bool IsDotNetCore5xOrHigher()
            {
                if (DotNetCore5xLibType == null)
                    return false;

                return (Type.GetType(DotNetCore5xLibType) != null);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the current managed runtime
            /// appears to be the .NET 7.0 (or higher) runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime appears to be the .NET 7.0 (or
            /// higher) runtime; otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method builds the full path to the file that may contain
            /// the version of the .NET Core runtime.
            /// </summary>
            /// <returns>
            /// The full path to the .NET Core version file, or null if it
            /// could not be determined.
            /// </returns>
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

            /// <summary>
            /// This method locates the native runtime library file for the
            /// .NET Core runtime.
            /// </summary>
            /// <returns>
            /// The full path to the .NET Core native runtime library file, or
            /// null if it could not be found.
            /// </returns>
            private static string GetDotNetCoreDllFileName()
            {
                return GetRuntimeDllFileName(DotNetCoreDllFileNames);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method attempts to query the version of the .NET Core
            /// runtime by reading its version file.
            /// </summary>
            /// <returns>
            /// The version of the .NET Core runtime, or null if it could not
            /// be determined.
            /// </returns>
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

            /// <summary>
            /// This method attempts to query the version of the .NET Core
            /// runtime based on the name of the directory containing its
            /// native runtime library.
            /// </summary>
            /// <returns>
            /// The version of the .NET Core runtime, or null if it could not
            /// be determined.
            /// </returns>
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

            /// <summary>
            /// This method attempts to query the version of the .NET Core
            /// runtime using one of several available techniques.
            /// </summary>
            /// <returns>
            /// The version of the .NET Core runtime, or null if it could not
            /// be determined.
            /// </returns>
            private static Version GetDotNetCoreVersion()
            {
                Version version = GetDotNetCoreVersionViaTextFile();

                if (version != null)
                    return version;

                return GetDotNetCoreVersionViaDllDirectory();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method attempts to query the extra (file) version
            /// information for the .NET Core runtime.
            /// </summary>
            /// <returns>
            /// The extra version information for the .NET Core runtime, or
            /// null if it could not be determined.
            /// </returns>
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
            /// <summary>
            /// This method gets the display name of the current managed
            /// runtime.
            /// </summary>
            /// <returns>
            /// The display name of the current managed runtime.
            /// </returns>
            public static string GetRuntimeName()
            {
                return GetRuntimeName(false);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the display name of the current managed
            /// runtime, optionally using the alternate naming scheme.
            /// </summary>
            /// <param name="alternate">
            /// Non-zero to use the alternate runtime naming scheme.
            /// </param>
            /// <returns>
            /// The display name of the current managed runtime.
            /// </returns>
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
            /// <summary>
            /// This method gets the native runtime version string associated
            /// with the current .NET Framework runtime.
            /// </summary>
            /// <returns>
            /// The native runtime version string, or null if it is not
            /// applicable to the current runtime.
            /// </returns>
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
            /// <summary>
            /// This method gets the image runtime version string that
            /// corresponds to the runtime currently executing.
            /// </summary>
            /// <returns>
            /// The image runtime version string for the current runtime.
            /// </returns>
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

            /// <summary>
            /// This method gets the version of the current managed runtime.
            /// </summary>
            /// <returns>
            /// The version of the current managed runtime, or null if it
            /// could not be determined.
            /// </returns>
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

            /// <summary>
            /// This method gets the build (file) version of the current
            /// managed runtime, when available.
            /// </summary>
            /// <returns>
            /// The build version of the current managed runtime, or null if
            /// it could not be determined.
            /// </returns>
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

            /// <summary>
            /// This method gets the extra version information for the current
            /// managed runtime, when available.
            /// </summary>
            /// <returns>
            /// The extra version information for the current managed runtime,
            /// or null if it could not be determined.
            /// </returns>
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

            /// <summary>
            /// This method gets a combined string containing the name and
            /// version information for the current managed runtime.
            /// </summary>
            /// <returns>
            /// A string containing the name and version of the current
            /// managed runtime.
            /// </returns>
            public static string GetRuntimeNameAndVersion()
            {
                return FormatOps.NameAndVersion(
                    GetRuntimeName(), GetRuntimeVersion(),
                    GetRuntimeBuild(), GetRuntimeExtraVersion()
                );
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets a combined string containing the name and
            /// major/minor version of the current managed runtime.
            /// </summary>
            /// <returns>
            /// A string containing the name and major/minor version of the
            /// current managed runtime.
            /// </returns>
            public static string GetRuntimeNameAndVMajorMinor()
            {
                return GetRuntimeNameAndVMajorMinor(true);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets a combined string containing the name and
            /// major/minor version of the current managed runtime, optionally
            /// using the alternate naming scheme.
            /// </summary>
            /// <param name="alternate">
            /// Non-zero to use the alternate runtime naming scheme.
            /// </param>
            /// <returns>
            /// A string containing the name and major/minor version of the
            /// current managed runtime.
            /// </returns>
            public static string GetRuntimeNameAndVMajorMinor(
                bool alternate
                )
            {
                return String.Format("{0} {1}", GetRuntimeName(alternate),
                    FormatOps.VMajorMinorOrNull(GetRuntimeVersion()));
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the directory that contains the core managed
            /// runtime assembly.
            /// </summary>
            /// <returns>
            /// The runtime directory, or null if it could not be determined.
            /// </returns>
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

            /// <summary>
            /// This method locates the first existing native runtime library
            /// file, from the specified candidate file names, within the
            /// runtime directory.
            /// </summary>
            /// <param name="fileNames">
            /// The candidate native runtime library file names to search for.
            /// </param>
            /// <returns>
            /// The full path to the first matching native runtime library
            /// file, or null if none was found.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current managed runtime is
            /// a version 2.x runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime is a version 2.x runtime;
            /// otherwise, false.
            /// </returns>
            public static bool IsRuntime20()
            {
                Version version = GetRuntimeVersion();

                return (version != null) && (version.Major == 2);
            }
#endif

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the Eagle core library was
            /// compiled to target the version 4.0 of the common language
            /// runtime.
            /// </summary>
            /// <returns>
            /// True if the library was built for the CLR version 4.0;
            /// otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method verifies that the specified set of compile-time
            /// define constants is consistent with the managed runtime that
            /// is currently executing.
            /// </summary>
            /// <param name="defines">
            /// The set of compile-time define constants to check.
            /// </param>
            /// <param name="error">
            /// Upon failure, this contains an error message describing the
            /// inconsistency.
            /// </param>
            /// <returns>
            /// <see cref="ReturnCode.Ok" /> if the define constants are
            /// consistent with the current runtime; otherwise, <see
            /// cref="ReturnCode.Error" />.
            /// </returns>
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
            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.5.1 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.5.1.
            /// </returns>
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

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.6 is installed, accounting for
            /// the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework 4.6.
            /// </returns>
            private static int GetFrameworkSetup46Value()
            {
                return PlatformOps.IsWindows10OrHigher() ?
                    FrameworkSetup46OnWindows10Value : FrameworkSetup46Value;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.6.1 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.6.1.
            /// </returns>
            private static int GetFrameworkSetup461Value()
            {
                return PlatformOps.IsWindows10NovemberUpdate() ?
                    FrameworkSetup461OnWindows10Value : FrameworkSetup461Value;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.6.2 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.6.2.
            /// </returns>
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

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.7 is installed, accounting for
            /// the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework 4.7.
            /// </returns>
            private static int GetFrameworkSetup47Value()
            {
                return PlatformOps.IsWindows10CreatorsUpdate() ?
                    FrameworkSetup47OnWindows10Value : FrameworkSetup47Value;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.7.1 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.7.1.
            /// </returns>
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

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.7.2 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.7.2.
            /// </returns>
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

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.8 is installed, accounting for
            /// the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework 4.8.
            /// </returns>
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

            /// <summary>
            /// This method gets the minimum registry release value that
            /// indicates the .NET Framework 4.8.1 is installed, accounting
            /// for the current operating system.
            /// </summary>
            /// <returns>
            /// The minimum registry release value for the .NET Framework
            /// 4.8.1.
            /// </returns>
            private static int GetFrameworkSetup481Value()
            {
                return PlatformOps.IsWindows11September2022Update() ?
                    FrameworkSetup481OnWindows11Value : FrameworkSetup481Value;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the version of the current .NET Framework
            /// runtime, caching the result for subsequent calls.
            /// </summary>
            /// <returns>
            /// The version of the current .NET Framework runtime, or null if
            /// it could not be determined.
            /// </returns>
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
            /// <summary>
            /// This method gets the extra version information for the current
            /// .NET Framework runtime, querying the registry as necessary and
            /// caching the result.
            /// </summary>
            /// <returns>
            /// The extra version information for the current .NET Framework
            /// runtime, or null if it could not be determined.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current managed runtime is
            /// the version 2.0 of the .NET Framework, caching the result for
            /// subsequent calls.
            /// </summary>
            /// <returns>
            /// True if the current runtime is the version 2.0 of the .NET
            /// Framework; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current managed runtime is
            /// the version 4.0 of the .NET Framework, caching the result for
            /// subsequent calls.
            /// </summary>
            /// <returns>
            /// True if the current runtime is the version 4.0 of the .NET
            /// Framework; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current .NET Framework
            /// runtime is a version 2.x runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime is a version 2.x .NET Framework
            /// runtime; otherwise, false.
            /// </returns>
            private static bool IsFramework2x()
            {
                Version version = GetFrameworkVersion();

                return (version != null) && (version.Major == 2);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono and !IsDotNetCore also.
            //
            /// <summary>
            /// This method determines whether the current .NET Framework
            /// runtime is a version 4.x runtime.
            /// </summary>
            /// <returns>
            /// True if the current runtime is a version 4.x .NET Framework
            /// runtime; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current .NET Framework
            /// runtime is version 4.5 or higher.
            /// </summary>
            /// <returns>
            /// True if the current runtime is the .NET Framework 4.5 or
            /// higher; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.5.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.5; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.5.1.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.5.1; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.5.2.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.5.2; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.6.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.6; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.6.1.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.6.1; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.6.2.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.6.2; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.7.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.7; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.7.1.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.7.1; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.7.2.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.7.2; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.8.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.8; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified .NET Framework
            /// version and extra version value indicate the .NET Framework
            /// 4.8.1.
            /// </summary>
            /// <param name="version">
            /// The .NET Framework version to check.
            /// </param>
            /// <param name="extraValue">
            /// The .NET Framework extra version (registry release) value to
            /// check.
            /// </param>
            /// <returns>
            /// True if the specified version information indicates the .NET
            /// Framework 4.8.1; otherwise, false.
            /// </returns>
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
        /// <summary>
        /// This class provides thread-safe support for querying and
        /// manipulating the environment variables belonging to the
        /// current process.
        /// </summary>
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
            /// <summary>
            /// The value returned by the string-returning methods of this
            /// class when an exception is caught.
            /// </summary>
            private static string ExceptionStringValue = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the value to return when an exception is caught
            //       within methods that return an object (of some kind).
            //
            // HACK: This is purposely not read-only.
            //
            /// <summary>
            /// The value returned by the object-returning methods of this
            /// class when an exception is caught.
            /// </summary>
            private static object ExceptionObjectValue = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            //
            // NOTE: This lock is used to protect access to the environment
            //       variables for the current process, when accessed via
            //       this class.
            //
            /// <summary>
            /// The object used to synchronize access to the environment
            /// variables for the current process.
            /// </summary>
            private static readonly object syncRoot = new object();
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Raw Get / Set / Unset (With Exceptions)
            /// <summary>
            /// This method gets the value of the specified environment
            /// variable, allowing any exception to propagate to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to query.
            /// </param>
            /// <returns>
            /// The value of the environment variable, or null if it does not
            /// exist.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable, allowing any exception to propagate to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to set.
            /// </param>
            /// <param name="value">
            /// The value to set the environment variable to.
            /// </param>
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

            /// <summary>
            /// This method removes the specified environment variable,
            /// allowing any exception to propagate to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to remove.
            /// </param>
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

            /// <summary>
            /// This method gets all the environment variables for the current
            /// process, allowing any exception to propagate to the caller.
            /// </summary>
            /// <returns>
            /// A dictionary containing all the environment variables for the
            /// current process.
            /// </returns>
            private static IDictionary GetRawVariablesWithThrow()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return SysEnv.GetEnvironmentVariables(); /* throw */
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets all the environment variables for the current
            /// process as a string dictionary, allowing any exception to
            /// propagate to the caller.
            /// </summary>
            /// <returns>
            /// A string dictionary containing all the environment variables
            /// for the current process, or null if none are available.
            /// </returns>
            private static StringDictionary GetVariablesWithThrow()
            {
                IDictionary dictionary = GetRawVariablesWithThrow(); /* throw */

                if (dictionary == null)
                    return null;

                return new StringDictionary(dictionary);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method expands any environment variable references
            /// contained within the specified string, allowing any exception
            /// to propagate to the caller.
            /// </summary>
            /// <param name="name">
            /// The string that may contain environment variable references to
            /// expand.
            /// </param>
            /// <returns>
            /// The string with any environment variable references expanded.
            /// </returns>
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
            /// <summary>
            /// This method gets the value of the specified environment
            /// variable, returning a fallback value if an exception is
            /// caught.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to query.
            /// </param>
            /// <returns>
            /// The value of the environment variable, or the configured
            /// fallback value if it does not exist or an exception is caught.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable, returning a value indicating whether it succeeded.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to set.
            /// </param>
            /// <param name="value">
            /// The value to set the environment variable to.
            /// </param>
            /// <returns>
            /// True if the environment variable was set successfully;
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method removes the specified environment variable,
            /// returning a value indicating whether it succeeded.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to remove.
            /// </param>
            /// <returns>
            /// True if the environment variable was removed successfully;
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method gets all the environment variables for the current
            /// process, returning a fallback value if an exception is caught.
            /// </summary>
            /// <returns>
            /// A string dictionary containing all the environment variables,
            /// or the configured fallback value if an exception is caught.
            /// </returns>
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

            /// <summary>
            /// This method expands any environment variable references
            /// contained within the specified string, returning a fallback
            /// value if an exception is caught.
            /// </summary>
            /// <param name="name">
            /// The string that may contain environment variable references to
            /// expand.
            /// </param>
            /// <returns>
            /// The string with any environment variable references expanded,
            /// or the configured fallback value if an exception is caught.
            /// </returns>
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
            /// <summary>
            /// This method gets the value of the specified environment
            /// variable and then removes it, allowing any exception to
            /// propagate to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to query and remove.
            /// </param>
            /// <returns>
            /// The original value of the environment variable, or null if it
            /// did not exist.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable and returns its previous value, allowing any
            /// exception to propagate to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to change.
            /// </param>
            /// <param name="value">
            /// The new value to set the environment variable to.
            /// </param>
            /// <returns>
            /// The previous value of the environment variable, or null if it
            /// did not exist.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable to a new value only when its current value matches
            /// the specified old value, allowing any exception to propagate
            /// to the caller.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to change.
            /// </param>
            /// <param name="oldValue">
            /// The value the environment variable must currently have in
            /// order for the change to take place.
            /// </param>
            /// <param name="newValue">
            /// The new value to set the environment variable to when the old
            /// value matches.
            /// </param>
            /// <returns>
            /// The value of the environment variable prior to any change.
            /// </returns>
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
            /// <summary>
            /// This method gets the value of the specified environment
            /// variable and then removes it, returning a fallback value if an
            /// exception is caught.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to query and remove.
            /// </param>
            /// <returns>
            /// The original value of the environment variable, or the
            /// configured fallback value if it did not exist or an exception
            /// is caught.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable and returns its previous value, returning a fallback
            /// value if an exception is caught.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to change.
            /// </param>
            /// <param name="value">
            /// The new value to set the environment variable to.
            /// </param>
            /// <returns>
            /// The previous value of the environment variable, or the
            /// configured fallback value if an exception is caught.
            /// </returns>
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

            /// <summary>
            /// This method sets the value of the specified environment
            /// variable to a new value only when its current value matches
            /// the specified old value, returning a fallback value if an
            /// exception is caught.
            /// </summary>
            /// <param name="variable">
            /// The name of the environment variable to change.
            /// </param>
            /// <param name="oldValue">
            /// The value the environment variable must currently have in
            /// order for the change to take place.
            /// </param>
            /// <param name="newValue">
            /// The new value to set the environment variable to when the old
            /// value matches.
            /// </param>
            /// <returns>
            /// The value of the environment variable prior to any change, or
            /// the configured fallback value if an exception is caught.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists and, if so, captures its value and then
            /// removes it, allowing any exception to propagate to the caller.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check and remove.
            /// </param>
            /// <param name="value">
            /// Upon success, this contains the value of the environment
            /// variable; otherwise, it is set to null.
            /// </param>
            /// <returns>
            /// True if the environment variable existed (and was removed);
            /// otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists and, if so, removes it, returning false if an
            /// exception is caught.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check and remove.
            /// </param>
            /// <returns>
            /// True if the environment variable existed (and was removed);
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists and, if so, captures its value and then
            /// removes it, returning false if an exception is caught.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check and remove.
            /// </param>
            /// <param name="value">
            /// Upon success, this contains the value of the environment
            /// variable; otherwise, it is set to null.
            /// </param>
            /// <returns>
            /// True if the environment variable existed (and was removed);
            /// otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists and captures its value, allowing any exception
            /// to propagate to the caller.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check.
            /// </param>
            /// <param name="value">
            /// Upon return, this contains the value of the environment
            /// variable, or null if it does not exist.
            /// </param>
            /// <returns>
            /// True if the environment variable exists; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists and captures its value, returning false if an
            /// exception is caught.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check.
            /// </param>
            /// <param name="value">
            /// Upon return, this contains the value of the environment
            /// variable, or null if it does not exist.
            /// </param>
            /// <returns>
            /// True if the environment variable exists; otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method determines whether the specified environment
            /// variable exists, returning false if an exception is caught.
            /// </summary>
            /// <param name="name">
            /// The name of the environment variable to check.
            /// </param>
            /// <returns>
            /// True if the environment variable exists; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This method saves the current values of the specified
            /// environment variables into a client data object so they may be
            /// restored later.
            /// </summary>
            /// <param name="names">
            /// The names of the environment variables to save.
            /// </param>
            /// <param name="clientData">
            /// Upon success, this contains the client data object holding the
            /// saved environment variable values.
            /// </param>
            /// <returns>
            /// True if the environment variables were saved successfully;
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method applies the saved values of the specified
            /// environment variables from the specified client data object.
            /// </summary>
            /// <param name="names">
            /// The names of the environment variables to set.
            /// </param>
            /// <param name="clientData">
            /// The client data object holding the saved environment variable
            /// values.
            /// </param>
            /// <returns>
            /// True if the environment variables were set successfully;
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method restores the saved values of the specified
            /// environment variables from the specified client data object.
            /// </summary>
            /// <param name="names">
            /// The names of the environment variables to restore.
            /// </param>
            /// <param name="clientData">
            /// The client data object holding the saved environment variable
            /// values.
            /// </param>
            /// <returns>
            /// True if the environment variables were restored successfully;
            /// otherwise, false.
            /// </returns>
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
        /// <summary>
        /// This class provides support for combining multiple integer
        /// hash codes into a single composite hash code.
        /// </summary>
        [ObjectId("b1bf9f59-a8a5-45b3-b54b-02c3d7107b85")]
        internal static class HashCodes
        {
            /// <summary>
            /// This method combines two integer hash codes into a single
            /// composite hash code.
            /// </summary>
            /// <param name="X">
            /// The first hash code to combine.
            /// </param>
            /// <param name="Y">
            /// The second hash code to combine.
            /// </param>
            /// <returns>
            /// The combined hash code.
            /// </returns>
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

            /// <summary>
            /// This method combines three integer hash codes into a single
            /// composite hash code.
            /// </summary>
            /// <param name="X">
            /// The first hash code to combine.
            /// </param>
            /// <param name="Y">
            /// The second hash code to combine.
            /// </param>
            /// <param name="Z">
            /// The third hash code to combine.
            /// </param>
            /// <returns>
            /// The combined hash code.
            /// </returns>
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
        /// <summary>
        /// This method evaluates the continuation condition for a bi-
        /// directional loop.
        /// </summary>
        /// <param name="increment">
        /// Non-zero if the loop is counting upward; otherwise, the loop
        /// is counting downward.
        /// </param>
        /// <param name="index">
        /// The current loop index.
        /// </param>
        /// <param name="lowerBound">
        /// The lower bound of the loop.
        /// </param>
        /// <param name="upperBound">
        /// The upper bound of the loop.
        /// </param>
        /// <returns>
        /// True if the loop should continue; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method advances the index of a bi-directional loop in the
        /// appropriate direction.
        /// </summary>
        /// <param name="increment">
        /// Non-zero if the loop is counting upward; otherwise, the loop
        /// is counting downward.
        /// </param>
        /// <param name="index">
        /// The loop index to advance.
        /// </param>
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
