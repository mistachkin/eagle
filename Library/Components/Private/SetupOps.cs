/*
 * SetupOps.cs --
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

#if !NET_STANDARD_20
using System.Collections.Generic;
#endif

using System.Runtime.InteropServices;

#if !NET_STANDARD_20
using Microsoft.Win32;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper methods used to query and
    /// manage the per-version setup information for the core library,
    /// including the installation path, application identifier, and the
    /// various security and update related settings stored in the system
    /// registry.  On Windows, it also manages the named mutexes used to
    /// prevent the setup program from running while the library is in use.
    /// </summary>
    [ObjectId("bd1dfe4f-116f-4780-9bbf-4c8c80d94873")]
    internal static class SetupOps
    {
        #region Private Constants
#if NATIVE && WINDOWS
        /// <summary>
        /// The name of the session-local mutex used to indicate that the
        /// library is currently in use, preventing the setup program from
        /// running.
        /// </summary>
        private static readonly string mutexName = GlobalState.GetPackageName(
            PackageType.Default, null, "_Setup", false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the system-global mutex (i.e. the session-local mutex
        /// name prefixed with the "Global\\" namespace) used to indicate that
        /// the library is currently in use across all user sessions.
        /// </summary>
        private static readonly string globalMutexName =
            "Global\\" + mutexName;
#endif

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// The base name of the registry key, relative to a root key, under
        /// which the per-version setup information for the library is stored.
        /// </summary>
        private static readonly string libraryKeyName =
            GlobalState.GetPackageName(PackageType.Default, "Software\\",
                null, false);

        /// <summary>
        /// The suffix appended to a registry key name to select the "low
        /// security" group of settings (i.e. those writable by non-elevated
        /// users).
        /// </summary>
        private static readonly string lowSecurityKeyNameSuffix = "\\Low";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the registry value containing the strong name of the
        /// installed core library assembly.
        /// </summary>
        private const string assemblyValueName = "Assembly";

        /// <summary>
        /// The name of the registry value containing the unique application
        /// identifier assigned to the setup instance.
        /// </summary>
        private const string appIdValueName = "AppId";

        /// <summary>
        /// The name of the registry value containing the compilation
        /// information for the installed core library.
        /// </summary>
        private const string compileInfoValueName = "CompileInfo";

        /// <summary>
        /// The name of the registry value containing the installation path of
        /// the core library.
        /// </summary>
        private const string pathValueName = "Path";

        /// <summary>
        /// The name of the registry value indicating whether the core library
        /// should be checked for being trusted.
        /// </summary>
        private const string checkCoreTrustedValueName = "CheckCoreTrusted";

        /// <summary>
        /// The name of the registry value indicating whether the core library
        /// should be checked for being verified.
        /// </summary>
        private const string checkCoreVerifiedValueName = "CheckCoreVerified";

        /// <summary>
        /// The name of the registry value indicating whether (or when) the
        /// core library should be checked for updates.
        /// </summary>
        private const string checkCoreUpdatesValueName = "CheckCoreUpdates";

        /// <summary>
        /// The name of the registry value indicating whether the core library
        /// should be made "safe".
        /// </summary>
        private const string makeCoreSafeValueName = "MakeCoreSafe";

        /// <summary>
        /// The name of the registry value indicating whether the core library
        /// should be made "secure".
        /// </summary>
        private const string makeCoreSecureValueName = "MakeCoreSecure";

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// The name of the registry value indicating whether the core library
        /// should be made "isolated".
        /// </summary>
        private const string makeCoreIsolatedValueName = "MakeCoreIsolated";
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Settings
#if !NET_STANDARD_20
        //
        // HACK: These are not read-only.
        //
        /// <summary>
        /// The default setting flags used when reading setting values from the
        /// registry.
        /// </summary>
        private static SettingFlags DefaultReadSettingFlags =
            SettingFlags.Default;

        /// <summary>
        /// The default setting flags used when writing setting values to the
        /// registry.
        /// </summary>
        private static SettingFlags DefaultWriteSettingFlags =
            SettingFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        // NOTE: These setting flags were primarily designed to be used with
        //       the methods reading and writing "CheckCoreUpdates" value.  The
        //       intent here is that "normal users" can check for updates using
        //       the "low security" group of settings, only falling back to the
        //       "high security" group of settings if necessary (for read-only
        //       operations) and not impact users with administrator access.
        //       Furthermore, users with administrator access can always check
        //       for updates using the "high security" group of settings, only
        //       falling back to the "low security" group of settings if
        //       necessary.
        //
        // NOTE: It should be noted that "normal users" will not typically be
        //       able to actually install updates; however, that is a totally
        //       separate issue and can vary wildly between deployments.
        //
        /// <summary>
        /// The setting flags used when reading the "CheckCoreUpdates" value;
        /// these enable both the "low security" and "high security" groups of
        /// local machine settings.
        /// </summary>
        private static SettingFlags SpecialReadSettingFlags =
            SettingFlags.LocalMachine | SettingFlags.LowSecurity |
            SettingFlags.HighSecurity;

        /// <summary>
        /// The setting flags used when writing the "CheckCoreUpdates" value;
        /// these enable both the "low security" and "high security" groups of
        /// local machine settings.
        /// </summary>
        private static SettingFlags SpecialWriteSettingFlags =
            SettingFlags.LocalMachine | SettingFlags.LowSecurity |
            SettingFlags.HighSecurity;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        /// <summary>
        /// When non-zero, errors encountered while reading the "Path" setting
        /// value are not reported via the complaint subsystem.
        /// </summary>
        private static bool PathNoComplain = false;

        /// <summary>
        /// When non-zero, errors encountered while reading the
        /// "CheckCoreTrusted" setting value are not reported via the complaint
        /// subsystem.
        /// </summary>
        private static bool CheckCoreTrustedNoComplain = true;

        /// <summary>
        /// When non-zero, errors encountered while reading the
        /// "CheckCoreVerified" setting value are not reported via the
        /// complaint subsystem.
        /// </summary>
        private static bool CheckCoreVerifiedNoComplain = true;

        /// <summary>
        /// When non-zero, errors encountered while reading or writing the
        /// "CheckCoreUpdates" setting value are not reported via the complaint
        /// subsystem.
        /// </summary>
        private static bool CheckCoreUpdatesNoComplain = true;

        /// <summary>
        /// When non-zero, errors encountered while reading the "MakeCoreSafe"
        /// setting value are not reported via the complaint subsystem.
        /// </summary>
        private static bool MakeCoreSafeNoComplain = true;

        /// <summary>
        /// When non-zero, errors encountered while reading the
        /// "MakeCoreSecure" setting value are not reported via the complaint
        /// subsystem.
        /// </summary>
        private static bool MakeCoreSecureNoComplain = true;

#if ISOLATED_PLUGINS
        /// <summary>
        /// When non-zero, errors encountered while reading the
        /// "MakeCoreIsolated" setting value are not reported via the complaint
        /// subsystem.
        /// </summary>
        private static bool MakeCoreIsolatedNoComplain = true;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        /// <summary>
        /// The default value returned for the "CheckCoreTrusted" setting when
        /// it is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultCheckCoreTrusted = true;

        /// <summary>
        /// The default value returned for the "CheckCoreVerified" setting when
        /// it is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultCheckCoreVerified = true;

        /// <summary>
        /// The default value returned for the "CheckCoreUpdates" setting when
        /// it is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultCheckCoreUpdates = false;

        /// <summary>
        /// The default value returned for the "MakeCoreSafe" setting when it
        /// is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultMakeCoreSafe = false;

        /// <summary>
        /// The default value returned for the "MakeCoreSecure" setting when it
        /// is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultMakeCoreSecure = false;

#if ISOLATED_PLUGINS
        /// <summary>
        /// The default value returned for the "MakeCoreIsolated" setting when
        /// it is missing or cannot be parsed.
        /// </summary>
        private static bool DefaultMakeCoreIsolated = false;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is not read-only.
        //
        /// <summary>
        /// The number of ticks that must elapse between successive checks for
        /// updates.  A negative value means "never" and a zero value means
        /// "always".
        /// </summary>
        private static long CheckCoreUpdatesTicks = 15 * TimeSpan.TicksPerDay;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// class across threads.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        //
        // NOTE: *NEVER CLOSED* Prevents our setup from installing.
        //
        /// <summary>
        /// The handle of the system-global mutex held while the library is in
        /// use, preventing the setup program from running across all user
        /// sessions.
        /// </summary>
        private static IntPtr globalMutex = IntPtr.Zero;

        /// <summary>
        /// The handle of the session-local mutex held while the library is in
        /// use, preventing the setup program from running in the current user
        /// session.
        /// </summary>
        private static IntPtr mutex = IntPtr.Zero;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        /// <summary>
        /// This method adds rows describing the current setup information
        /// (e.g. mutex names, registry key names, setting flags, and selected
        /// setting values) to the specified list, for diagnostic purposes.
        /// </summary>
        /// <param name="list">
        /// The list to which the setup information rows are added.  If this
        /// parameter is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included; when empty
        /// content is requested, all available rows are emitted even when
        /// their values are unset.
        /// </param>
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot)
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

#if NATIVE && WINDOWS
                if (empty || !String.IsNullOrEmpty(mutexName))
                    localList.Add("MutexName",
                        FormatOps.DisplayString(mutexName));

                if (empty || (mutex != IntPtr.Zero))
                    localList.Add("Mutex", mutex.ToString());

                if (empty || !String.IsNullOrEmpty(globalMutexName))
                    localList.Add("GlobalMutexName",
                        FormatOps.DisplayString(globalMutexName));

                if (empty || (globalMutex != IntPtr.Zero))
                    localList.Add("GlobalMutex", globalMutex.ToString());
#endif

#if !NET_STANDARD_20
                if (empty || !String.IsNullOrEmpty(libraryKeyName))
                    localList.Add("LibraryKeyName",
                        FormatOps.DisplayString(libraryKeyName));

                if (empty || !String.IsNullOrEmpty(lowSecurityKeyNameSuffix))
                    localList.Add("LowSecurityKeyNameSuffix",
                        FormatOps.DisplayString(lowSecurityKeyNameSuffix));

                if (empty || (DefaultReadSettingFlags != SettingFlags.None))
                    localList.Add("ReadSettingFlags",
                        DefaultReadSettingFlags.ToString());

                if (empty || (DefaultWriteSettingFlags != SettingFlags.None))
                    localList.Add("WriteSettingFlags",
                        DefaultWriteSettingFlags.ToString());

                if (empty || (SpecialReadSettingFlags != SettingFlags.None))
                    localList.Add("SpecialReadSettingFlags",
                        SpecialReadSettingFlags.ToString());

                if (empty || (SpecialWriteSettingFlags != SettingFlags.None))
                    localList.Add("SpecialWriteSettingFlags",
                        SpecialWriteSettingFlags.ToString());

                if (empty || PathNoComplain)
                    localList.Add("PathNoComplain",
                        PathNoComplain.ToString());

                if (empty || CheckCoreTrustedNoComplain)
                    localList.Add("CheckCoreTrustedNoComplain",
                        CheckCoreTrustedNoComplain.ToString());

                if (empty || CheckCoreVerifiedNoComplain)
                    localList.Add("CheckCoreVerifiedNoComplain",
                        CheckCoreVerifiedNoComplain.ToString());

                if (empty || CheckCoreUpdatesNoComplain)
                    localList.Add("CheckCoreUpdatesNoComplain",
                        CheckCoreUpdatesNoComplain.ToString());

                if (empty || DefaultCheckCoreTrusted)
                    localList.Add("DefaultCheckCoreTrusted",
                        DefaultCheckCoreTrusted.ToString());

                if (empty || DefaultCheckCoreVerified)
                    localList.Add("DefaultCheckCoreVerified",
                        DefaultCheckCoreVerified.ToString());

                if (empty || DefaultCheckCoreUpdates)
                    localList.Add("DefaultCheckCoreUpdates",
                        DefaultCheckCoreUpdates.ToString());

                if (empty || (CheckCoreUpdatesTicks > 0))
                    localList.Add("CheckCoreUpdatesTicks",
                        StringList.MakeList(CheckCoreUpdatesTicks.ToString(),
                        FormatOps.AlwaysOrNever(CheckCoreUpdatesTicks)));

                bool shouldCheckCoreTrusted = ShouldCheckCoreTrusted();

                if (empty || shouldCheckCoreTrusted)
                    localList.Add("ShouldCheckCoreTrusted",
                        shouldCheckCoreTrusted.ToString());

                bool shouldCheckCoreVerified = ShouldCheckCoreVerified();

                if (empty || shouldCheckCoreVerified)
                    localList.Add("ShouldCheckCoreVerified",
                        shouldCheckCoreVerified.ToString());

#if !NET_STANDARD_20 && THREADING
                bool shouldCheckCoreUpdates = ShouldCheckCoreUpdates();

                if (empty || shouldCheckCoreUpdates)
                    localList.Add("ShouldCheckCoreUpdates",
                        shouldCheckCoreUpdates.ToString());
#endif

                string appId = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), appIdValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(appId))
                    localList.Add("AppId", FormatOps.DisplayString(appId));

                string path = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), pathValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(path))
                    localList.Add("Path", FormatOps.DisplayString(path));

                string assembly = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), assemblyValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(assembly))
                    localList.Add("Assembly",
                        FormatOps.DisplayString(assembly));

                string compileInfo = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), compileInfoValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(compileInfo))
                    localList.Add("CompileInfo",
                        FormatOps.DisplayString(compileInfo));
#endif

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Setup Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Path Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method queries the installation path for the setup instance
        /// matching the version of the currently executing assembly.
        /// </summary>
        /// <returns>
        /// The installation path, or null if it could not be found.
        /// </returns>
        public static string GetPath()
        {
            return GetPath(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the installation path for the setup instance
        /// matching the specified version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// The installation path, or null if it could not be found.
        /// </returns>
        public static string GetPath(
            Version version
            )
        {
            return GetSettingValue(
                version, pathValueName, DefaultReadSettingFlags,
                PathNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the installation path for the setup instance
        /// matching the specified version, using the specified root registry
        /// key.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to search.
        /// </param>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="path">
        /// Upon success, receives the installation path.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the installation path was found; otherwise, false.
        /// </returns>
        private static bool GetPath(
            RegistryKey rootKey,
            Version version,
            ref string path,
            ref Result error
            )
        {
            return GetSettingValue(
                rootKey, version, pathValueName, DefaultReadSettingFlags,
                ref path, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Enumeration Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method enumerates all setup instances stored under the local
        /// machine root registry key.
        /// </summary>
        /// <param name="result">
        /// Upon success, receives the list of setup instances; upon failure,
        /// receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetInstances(
            ref Result result
            )
        {
            return GetInstances(Registry.LocalMachine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enumerates all setup instances stored under the
        /// specified root registry key.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to search.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the list of setup instances; upon failure,
        /// receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode GetInstances(
            RegistryKey rootKey,
            ref Result result
            )
        {
            if (rootKey == null)
            {
                result = "invalid root registry key";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(libraryKeyName))
            {
                result = "invalid library registry key name";
                return ReturnCode.Error;
            }

            try
            {
                using (RegistryKey key = rootKey.OpenSubKey(libraryKeyName))
                {
                    if (key == null)
                    {
                        result = "no setup instances found";
                        return ReturnCode.Error;
                    }

                    StringList list = new StringList();

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        StringList subList = new StringList("Name",
                            subKeyName);

                        try
                        {
                            using (RegistryKey subKey = key.OpenSubKey(
                                    subKeyName))
                            {
                                if (subKey != null)
                                {
                                    subList.Add(appIdValueName,
                                        subKey.GetValue(
                                            appIdValueName) as string);

                                    subList.Add(pathValueName,
                                        subKey.GetValue(
                                            pathValueName) as string);

                                    subList.Add(assemblyValueName,
                                        subKey.GetValue(
                                            assemblyValueName) as string);

                                    subList.Add(compileInfoValueName,
                                        subKey.GetValue(
                                            compileInfoValueName) as string);
                                }
                                else
                                {
                                    subList.Add("Error",
                                        String.Format("could not open sub-key " +
                                        "\"{0}\" of registry key \"{1}\"",
                                        subKeyName, key));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            subList.Add("Exception", e.ToString());

                            subList.Add("Error",
                                String.Format("could not open sub-key " +
                                "\"{0}\" of registry key \"{1}\"",
                                subKeyName, key));
                        }

                        list.Add(subList.ToString());
                    }

                    result = list;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Trusted Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method determines whether the core library should be checked
        /// for being trusted, based on the setting associated with the version
        /// of the currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be checked for being trusted;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldCheckCoreTrusted()
        {
            return ShouldCheckCoreTrusted(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for being trusted, based on the setting associated with the
        /// specified version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for being trusted;
        /// otherwise, false.
        /// </returns>
        private static bool ShouldCheckCoreTrusted(
            Version version
            )
        {
            string value = GetSettingValue(
                version, checkCoreTrustedValueName, DefaultReadSettingFlags,
                CheckCoreTrustedNoComplain);

            if (value == null)
                return DefaultCheckCoreTrusted;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!CheckCoreTrustedNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultCheckCoreTrusted;
            }

            return boolValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Verified Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method determines whether the core library should be checked
        /// for being verified, based on the setting associated with the
        /// version of the currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be checked for being verified;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldCheckCoreVerified()
        {
            return ShouldCheckCoreVerified(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for being verified, based on the setting associated with the
        /// specified version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for being verified;
        /// otherwise, false.
        /// </returns>
        private static bool ShouldCheckCoreVerified(
            Version version
            )
        {
            string value = GetSettingValue(
                version, checkCoreVerifiedValueName, DefaultReadSettingFlags,
                CheckCoreVerifiedNoComplain);

            if (value == null)
                return DefaultCheckCoreVerified;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!CheckCoreVerifiedNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultCheckCoreVerified;
            }

            return boolValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Safe Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method determines whether the core library should be made
        /// "safe", based on the setting associated with the version of the
        /// currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be made "safe"; otherwise, false.
        /// </returns>
        public static bool ShouldMakeCoreSafe()
        {
            return ShouldMakeCoreSafe(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be made
        /// "safe", based on the setting associated with the specified version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// True if the core library should be made "safe"; otherwise, false.
        /// </returns>
        private static bool ShouldMakeCoreSafe(
            Version version
            )
        {
            string value = GetSettingValue(
                version, makeCoreSafeValueName, DefaultReadSettingFlags,
                MakeCoreSafeNoComplain);

            if (value == null)
                return DefaultMakeCoreSafe;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!MakeCoreSafeNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultMakeCoreSafe;
            }

            return boolValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Secure Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method determines whether the core library should be made
        /// "secure", based on the setting associated with the version of the
        /// currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be made "secure"; otherwise, false.
        /// </returns>
        public static bool ShouldMakeCoreSecure()
        {
            return ShouldMakeCoreSecure(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be made
        /// "secure", based on the setting associated with the specified
        /// version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// True if the core library should be made "secure"; otherwise, false.
        /// </returns>
        private static bool ShouldMakeCoreSecure(
            Version version
            )
        {
            string value = GetSettingValue(
                version, makeCoreSecureValueName, DefaultReadSettingFlags,
                MakeCoreSecureNoComplain);

            if (value == null)
                return DefaultMakeCoreSecure;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!MakeCoreSecureNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultMakeCoreSecure;
            }

            return boolValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Isolated Support Methods
#if !NET_STANDARD_20 && ISOLATED_PLUGINS
        /// <summary>
        /// This method determines whether the core library should be made
        /// "isolated", based on the setting associated with the version of the
        /// currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be made "isolated"; otherwise,
        /// false.
        /// </returns>
        public static bool ShouldMakeCoreIsolated()
        {
            return ShouldMakeCoreIsolated(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be made
        /// "isolated", based on the setting associated with the specified
        /// version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <returns>
        /// True if the core library should be made "isolated"; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldMakeCoreIsolated(
            Version version
            )
        {
            string value = GetSettingValue(
                version, makeCoreIsolatedValueName, DefaultReadSettingFlags,
                MakeCoreIsolatedNoComplain);

            if (value == null)
                return DefaultMakeCoreIsolated;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!MakeCoreIsolatedNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultMakeCoreIsolated;
            }

            return boolValue;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Updates Support Methods
#if !NET_STANDARD_20 && THREADING
        /// <summary>
        /// This method determines whether enough time has elapsed since the
        /// specified date and time for the core library to be checked for
        /// updates again.
        /// </summary>
        /// <param name="dateTime">
        /// The (UTC) date and time of the most recent check for updates.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for updates; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldCheckCoreUpdatesViaValue(
            DateTime dateTime
            )
        {
            //
            // NOTE: A negative value means "never".
            //
            if (CheckCoreUpdatesTicks < 0)
                return false;

            //
            // NOTE: A zero value means "always".
            //
            if (CheckCoreUpdatesTicks == 0)
                return true;

            //
            // NOTE: A positive value means we should check it against
            //       the elapsed number of ticks since the last check
            //       for updates.
            //
            if (TimeOps.GetUtcNow().Subtract(
                    dateTime).Ticks >= CheckCoreUpdatesTicks)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for updates, based on the specified raw setting value, which may be
        /// either a boolean or a date and time indicating the most recent
        /// check for updates.
        /// </summary>
        /// <param name="value">
        /// The raw setting value to interpret.  This parameter may be null, in
        /// which case the default value is returned.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for updates; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldCheckCoreUpdatesViaValue(
            string value
            )
        {
            if (value == null)
                return DefaultCheckCoreUpdates;

            bool boolValue = false;
            Result localError = null;
            ResultList errors = null;

            if (Value.GetBoolean4(
                    value, ValueFlags.AnyBoolean, ref boolValue,
                    ref localError) == ReturnCode.Ok)
            {
                //
                // NOTE: Return the stored boolean value verbatim.  Since
                //       the setup initially defaults this value to "True",
                //       we should always see a Boolean before a DateTime.
                //
                return boolValue;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            DateTime dateTimeValue = DateTime.MinValue;

            localError = null;

            if (Value.GetDateTime3(
                    value, ValueFlags.AnyDateTime, DateTimeKind.Local,
                    DateTimeStyles.RoundtripKind, ref dateTimeValue,
                    ref localError) == ReturnCode.Ok)
            {
                //
                // NOTE: Convert the value to UTC for comparison.
                //
                dateTimeValue = dateTimeValue.ToUniversalTime();

                //
                // NOTE: Have enough ticks elapsed since the last check
                //       for updates?
                //
                if (ShouldCheckCoreUpdatesViaValue(dateTimeValue))
                {
                    //
                    // NOTE: Enough ticks have passed since the last
                    //       check for updates, return true.
                    //
                    return true;
                }
                else
                {
                    //
                    // NOTE: Not enough ticks have passed since the
                    //       last check for updates (i.e. it was too
                    //       recently), return false.
                    //
                    return false;
                }
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            //
            // NOTE: If we get to this point, the string value could not be
            //       converted to a Boolean or DateTime.  This is an error.
            //       Complain about this and return the default value.
            //
            if (errors != null)
                DebugOps.Complain(ReturnCode.Error, errors);

            return DefaultCheckCoreUpdates;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for updates, based on the setting associated with the version of
        /// the currently executing assembly.
        /// </summary>
        /// <returns>
        /// True if the core library should be checked for updates; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldCheckCoreUpdates()
        {
            bool missing = false;

            return ShouldCheckCoreUpdates(ref missing);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for updates, based on the setting associated with the version of
        /// the currently executing assembly.
        /// </summary>
        /// <param name="missing">
        /// Upon return, this is set to non-zero if the underlying setting
        /// value was not found.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for updates; otherwise,
        /// false.
        /// </returns>
        public static bool ShouldCheckCoreUpdates(
            ref bool missing
            )
        {
            return ShouldCheckCoreUpdates(
                GlobalState.GetAssemblyVersion(), ref missing);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the core library should be checked
        /// for updates, based on the setting associated with the specified
        /// version.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="missing">
        /// Upon return, this is set to non-zero if the underlying setting
        /// value was not found.
        /// </param>
        /// <returns>
        /// True if the core library should be checked for updates; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldCheckCoreUpdates(
            Version version,
            ref bool missing
            )
        {
            string value = null;

            missing = !GetSettingValue(
                version, checkCoreUpdatesValueName, SpecialReadSettingFlags,
                CheckCoreUpdatesNoComplain, ref value);

            return ShouldCheckCoreUpdatesViaValue(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the current date and time as the most recent
        /// check for updates, by writing it to the "CheckCoreUpdates" setting
        /// value for the version of the currently executing assembly.
        /// </summary>
        public static void MarkCheckCoreUpdatesNow()
        {
            SetSettingValue(
                GlobalState.GetAssemblyVersion(), checkCoreUpdatesValueName,
                FormatOps.Iso8601UpdateDateTime(TimeOps.GetNow()),
                SpecialWriteSettingFlags, CheckCoreUpdatesNoComplain);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Read/Write Setting Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method builds the array of root registry keys to be searched,
        /// based on the specified setting flags.
        /// </summary>
        /// <param name="flags">
        /// The setting flags used to determine which root registry keys are
        /// included.
        /// </param>
        /// <returns>
        /// An array of root registry keys; entries corresponding to disabled
        /// flags are null.
        /// </returns>
        private static RegistryKey[] GetRootKeys(
            SettingFlags flags
            )
        {
            return new RegistryKey[] {
                FlagOps.HasFlags(flags, SettingFlags.CurrentUser, true) ?
                    Registry.CurrentUser : null,
                FlagOps.HasFlags(flags, SettingFlags.LocalMachine, true) ?
                    Registry.LocalMachine : null
            };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the registry key name (relative to a root key)
        /// for the specified version and security level.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance.  This parameter may be null, in
        /// which case the versionless key name is built.
        /// </param>
        /// <param name="lowSecurity">
        /// Non-zero to build the "low security" key name, zero to build the
        /// "high security" key name, or null to determine the security level
        /// automatically based on whether the current user is an
        /// administrator.
        /// </param>
        /// <returns>
        /// The registry key name, or null if the base library key name is
        /// unavailable.
        /// </returns>
        private static string GetKeyName(
            Version version,
            bool? lowSecurity
            )
        {
            if (String.IsNullOrEmpty(libraryKeyName))
                return null;

            bool localLowSecurity = (lowSecurity != null) ?
                (bool)lowSecurity : !RuntimeOps.IsAdministrator();

            string keyNameSuffix = localLowSecurity ?
                lowSecurityKeyNameSuffix : String.Empty;

            if (version != null)
            {
                return String.Format(
                    "{0}\\{1}{2}", libraryKeyName, version,
                    keyNameSuffix);
            }
            else
            {
                return String.Format(
                    "{0}{1}", libraryKeyName, keyNameSuffix);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the ordered array of registry key names to be
        /// searched for the specified version, taking into account the
        /// specified setting flags, the permissions of the current user, and
        /// whether the operation is read-only.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance.  This parameter may be null, in
        /// which case the versionless key names are built.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to determine which security groups of
        /// settings are included.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the key names are being built for a read-only
        /// operation.
        /// </param>
        /// <returns>
        /// An array of registry key names; entries corresponding to disabled
        /// or inapplicable security groups are null.
        /// </returns>
        private static string[] GetKeyNames(
            Version version,
            SettingFlags flags,
            bool readOnly
            )
        {
            if (FlagOps.HasFlags(flags, SettingFlags.AnySecurity, true))
            {
                //
                // NOTE: Check all groups of settings that are enabled via
                //       the flags specified by the caller, regardless of
                //       the permissions for the current user and what type
                //       of operation this is.  However, the order in which
                //       these are checked may depend on the permissions of
                //       the current user.
                //
                StringList list = new StringList();

                if (FlagOps.HasFlags(flags, SettingFlags.UserSecurity, true))
                    list.Add(GetKeyName(version, null));

                if (RuntimeOps.IsAdministrator())
                {
                    if (FlagOps.HasFlags(
                            flags, SettingFlags.HighSecurity, true))
                    {
                        list.Add(GetKeyName(version, false));
                    }

                    if (FlagOps.HasFlags(
                            flags, SettingFlags.LowSecurity, true))
                    {
                        list.Add(GetKeyName(version, true));
                    }
                }
                else
                {
                    if (FlagOps.HasFlags(
                            flags, SettingFlags.LowSecurity, true))
                    {
                        list.Add(GetKeyName(version, true));
                    }

                    if (FlagOps.HasFlags(
                            flags, SettingFlags.HighSecurity, true))
                    {
                        list.Add(GetKeyName(version, false));
                    }
                }

                return list.ToArray();
            }
            else if (RuntimeOps.IsAdministrator())
            {
                //
                // NOTE: Check the "high security" group of settings only,
                //       when it is enabled.
                //
                return new string[] {
                    FlagOps.HasFlags(flags, SettingFlags.HighSecurity, true) ?
                        GetKeyName(version, false) : null
                };
            }
            else
            {
                //
                // NOTE: Check the "low security" group of settings, when it
                //       is enabled; however, only check the "high security"
                //       group of settings when it is enabled and this is a
                //       read operation.
                //
                return new string[] {
                    FlagOps.HasFlags(flags, SettingFlags.LowSecurity, true) ?
                        GetKeyName(version, true) : null, readOnly &&
                    FlagOps.HasFlags(flags, SettingFlags.HighSecurity, true) ?
                        GetKeyName(version, false) : null
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified version, along with its three-part,
        /// two-part, and empty (versionless) variants, to the specified
        /// dictionary, for use when searching for setting values.
        /// </summary>
        /// <param name="versions">
        /// The dictionary to which the version variants are added.  If this
        /// parameter is null, this method does nothing.
        /// </param>
        /// <param name="version">
        /// The version whose variants are added.  This parameter may be null,
        /// in which case only the empty (versionless) variant is added.
        /// </param>
        private static void AddVersionVariants(
            IDictionary<Version, Version> versions,
            Version version
            )
        {
            if (versions == null)
                return;

            if (version != null)
            {
                if (!versions.ContainsKey(version))
                    versions.Add(version, version);

                Version version3 = GlobalState.GetThreePartVersion(version);

                if (!versions.ContainsKey(version3))
                    versions.Add(version3, version3);

                Version version2 = GlobalState.GetTwoPartVersion(version);

                if (!versions.ContainsKey(version2))
                    versions.Add(version2, version2);
            }

            Version version0 = new Version();

            if (!versions.ContainsKey(version0))
                versions.Add(version0, null);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Read Setting Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method reads the named setting value for the specified version
        /// from the registry, searching all applicable root keys and version
        /// variants.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control the search.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting of any errors via the complaint
        /// subsystem.
        /// </param>
        /// <returns>
        /// The setting value, or null if it could not be found.
        /// </returns>
        private static string GetSettingValue(
            Version version,
            string name,
            SettingFlags flags,
            bool noComplain
            )
        {
            string value = null;

            if (GetSettingValue(
                    version, name, flags, noComplain, ref value))
            {
                return value;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the named setting value for the specified version
        /// from the registry, searching all applicable root keys and version
        /// variants.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control the search.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting of any errors via the complaint
        /// subsystem.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the setting value.
        /// </param>
        /// <returns>
        /// True if the setting value was found; otherwise, false.
        /// </returns>
        private static bool GetSettingValue(
            Version version,
            string name,
            SettingFlags flags,
            bool noComplain,
            ref string value
            )
        {
            IDictionary<Version, Version> versions =
                new Dictionary<Version, Version>();

            AddVersionVariants(versions, version);

            RegistryKey[] rootKeys = GetRootKeys(flags);

            foreach (RegistryKey rootKey in rootKeys)
            {
                if (rootKey == null)
                    continue;

                foreach (Version localVersion in versions.Values)
                {
                    value = GetSettingValue(
                        rootKey, localVersion, name, flags,
                        noComplain);

                    if (value != null)
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the named setting value for the specified version
        /// from the registry, using the specified root key, optionally
        /// reporting any errors via the complaint subsystem.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to search.
        /// </param>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control the search.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting of any errors via the complaint
        /// subsystem.
        /// </param>
        /// <returns>
        /// The setting value, or null if it could not be found.
        /// </returns>
        private static string GetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            SettingFlags flags,
            bool noComplain
            )
        {
            bool result;
            string value = null;
            Result error = null;

            result = GetSettingValue(
                rootKey, version, name, flags, ref value, ref error);

            if (!result && !noComplain && (error != null))
                DebugOps.Complain(ReturnCode.Error, error);

#if DEBUG && VERBOSE
            TraceOps.DebugTrace(String.Format(
                "GetSettingValue: rootKey = {0}, version = {1}, name = " +
                "{2}, value = {3}, flags = {4}, noComplain = {5}, " +
                "result = {6}, error = {7}", FormatOps.WrapOrNull(rootKey),
                FormatOps.WrapOrNull(version), FormatOps.WrapOrNull(name),
                FormatOps.WrapOrNull(value), FormatOps.WrapOrNull(flags),
                noComplain, result, FormatOps.WrapOrNull(true, true, error)),
                typeof(SetupOps).Name, TracePriority.StartupDebug);
#endif

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the named setting value for the specified version
        /// from the registry, using the specified root key, searching the
        /// applicable key names in order.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to search.
        /// </param>
        /// <param name="version">
        /// The version of the setup instance to query.  This parameter may be
        /// null, in which case the versionless instance is queried.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control the search.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the setting value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the setting value was found; otherwise, false.
        /// </returns>
        private static bool GetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            SettingFlags flags,
            ref string value,
            ref Result error
            )
        {
            if (rootKey == null)
            {
                error = "invalid root registry key";
                return false;
            }

            try
            {
                string[] keyNames = GetKeyNames(version, flags, true);

                if (keyNames == null)
                {
                    error = "no setup instances available";
                    return false;
                }

                bool verbose = FlagOps.HasFlags(
                    flags, SettingFlags.Verbose, true);

                int count = 0;
                ResultList errors = null;

                foreach (string keyName in keyNames)
                {
                    if (keyName == null)
                        continue;

                    count++;

                    using (RegistryKey key = rootKey.OpenSubKey(keyName))
                    {
                        if (key != null)
                        {
                            //
                            // NOTE: Per MSDN, null and empty string are
                            //       allowed here for the value name and
                            //       will result in the "default" value
                            //       being returned for the associated
                            //       registry key.
                            //
                            value = key.GetValue(name) as string;
                            return true;
                        }
                        else if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "setup instance not found: {0}\\{1}",
                                rootKey, keyName));
                        }
                    }
                }

                if (count == 0)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no setup instances checked");
                }

                if (errors != null)
                    error = errors;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Setting Support Methods
#if !NET_STANDARD_20
        /// <summary>
        /// This method writes the named setting value for the specified
        /// version to the registry, attempting each applicable root key and
        /// version variant until one succeeds.
        /// </summary>
        /// <param name="version">
        /// The version of the setup instance to update.  This parameter may be
        /// null, in which case the versionless instance is updated.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to write.
        /// </param>
        /// <param name="value">
        /// The setting value to write.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control where the value is written.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting of any errors via the complaint
        /// subsystem.
        /// </param>
        /// <returns>
        /// True if the setting value was written; otherwise, false.
        /// </returns>
        private static bool SetSettingValue(
            Version version,
            string name,
            string value,
            SettingFlags flags,
            bool noComplain
            )
        {
            IDictionary<Version, Version> versions =
                new Dictionary<Version, Version>();

            AddVersionVariants(versions, version);

            RegistryKey[] rootKeys = GetRootKeys(flags);

            foreach (RegistryKey rootKey in rootKeys)
            {
                if (rootKey == null)
                    continue;

                foreach (Version localVersion in versions.Values)
                {
                    if (SetSettingValue(
                            rootKey, localVersion, name, value,
                            flags, noComplain))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the named setting value for the specified
        /// version to the registry, using the specified root key, optionally
        /// reporting any errors via the complaint subsystem.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to update.
        /// </param>
        /// <param name="version">
        /// The version of the setup instance to update.  This parameter may be
        /// null, in which case the versionless instance is updated.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to write.
        /// </param>
        /// <param name="value">
        /// The setting value to write.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control where the value is written.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting of any errors via the complaint
        /// subsystem.
        /// </param>
        /// <returns>
        /// True if the setting value was written; otherwise, false.
        /// </returns>
        private static bool SetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            string value,
            SettingFlags flags,
            bool noComplain
            )
        {
            bool result;
            Result error = null;

            result = SetSettingValue(
                rootKey, version, name, value, flags, ref error);

            if (!result && !noComplain && (error != null))
                DebugOps.Complain(ReturnCode.Error, error);

            TraceOps.DebugTrace(String.Format(
                "SetSettingValue: rootKey = {0}, version = {1}, name = " +
                "{2}, value = {3}, flags = {4}, noComplain = {5}, " +
                "result = {6}, error = {7}", FormatOps.WrapOrNull(rootKey),
                FormatOps.WrapOrNull(version), FormatOps.WrapOrNull(name),
                FormatOps.WrapOrNull(value), FormatOps.WrapOrNull(flags),
                noComplain, result, FormatOps.WrapOrNull(true, true, error)),
                typeof(SetupOps).Name, TracePriority.StartupDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the named setting value for the specified
        /// version to the registry, using the specified root key, attempting
        /// each applicable key name in order until one succeeds.
        /// </summary>
        /// <param name="rootKey">
        /// The root registry key to update.
        /// </param>
        /// <param name="version">
        /// The version of the setup instance to update.  This parameter may be
        /// null, in which case the versionless instance is updated.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to write.
        /// </param>
        /// <param name="value">
        /// The setting value to write.  This parameter cannot be null.
        /// </param>
        /// <param name="flags">
        /// The setting flags used to control where the value is written.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the setting value was written; otherwise, false.
        /// </returns>
        private static bool SetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            string value,
            SettingFlags flags,
            ref Result error
            )
        {
            if (rootKey == null)
            {
                error = "invalid root registry key";
                return false;
            }

            if (value == null)
            {
                error = "invalid setting value";
                return false;
            }

            try
            {
                string[] keyNames = GetKeyNames(version, flags, false);

                if (keyNames == null)
                {
                    error = "no setup instances available";
                    return false;
                }

                bool verbose = FlagOps.HasFlags(
                    flags, SettingFlags.Verbose, true);

                int count = 0;
                ResultList errors = null;

                foreach (string keyName in keyNames)
                {
                    if (keyName == null)
                        continue;

                    count++;

                    using (RegistryKey key = rootKey.OpenSubKey(keyName, true))
                    {
                        if (key != null)
                        {
                            //
                            // NOTE: Per MSDN, null and empty string are
                            //       allowed here for the value name and
                            //       will result in the "default" value
                            //       being set for the associated registry
                            //       key.
                            //
                            key.SetValue(name, value);
                            return true;
                        }
                        else if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "setup instance not found: {0}\\{1}",
                                rootKey, keyName));
                        }
                    }
                }

                if (count == 0)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no setup instances checked");
                }

                if (errors != null)
                    error = errors;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Mutex Support Methods
#if NATIVE && WINDOWS
        /// <summary>
        /// This method conditionally reports the specified mutex-related error
        /// via the complaint subsystem (only when complaining is appropriate
        /// for the current application domain) and always emits a diagnostic
        /// trace message.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the error.
        /// </param>
        /// <param name="result">
        /// The result describing the error.  If this parameter is null, no
        /// complaint is issued.
        /// </param>
        /// <param name="dispose">
        /// Non-zero if this complaint is being issued while disposing or
        /// closing the mutexes; this affects the trace priority used.
        /// </param>
        private static void MaybeComplain(
            ReturnCode code,
            Result result,
            bool dispose
            )
        {
            //
            // TODO: *HACK* Maybe come up with a better semantic here?  For
            //       now, we assume that complaining about the setup mutexes
            //       from a non-default AppDomain is a "bad idea" because it
            //       can be quite difficult to predict and/or prevent issues
            //       (e.g. AppDomain isolation in [test2], [interp], etc).
            //
            if (AppDomainOps.ShouldComplain() && (result != null))
                DebugOps.Complain(null, code, result);

            TracePriority priority = dispose ?
                TracePriority.StartupError : TracePriority.StartupError2;

            TraceOps.DebugTrace(String.Format(
                "MaybeComplain: code = {0}, result = {1}",
                code, FormatOps.WrapOrNull(true, true, result)),
                typeof(SetupOps).Name, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers the exited event handler for the current
        /// application domain (using the domain unload event for non-default
        /// domains and the process exit event for the default domain), so the
        /// setup mutexes can be closed automatically, unless this behavior has
        /// been disabled via configuration.
        /// </summary>
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_SetupOps_Exited",
                    ConfigurationFlags.SetupOps))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= SetupOps_Exited;
                        appDomain.DomainUnload += SetupOps_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= SetupOps_Exited;
                        appDomain.ProcessExit += SetupOps_Exited;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the exited event handler previously
        /// registered for the current application domain.
        /// </summary>
        private static void RemoveExitedEventHandler()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= SetupOps_Exited;
                else
                    appDomain.ProcessExit -= SetupOps_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the event handler invoked when the current
        /// application domain is being unloaded or the process is exiting; it
        /// closes the setup mutexes and unregisters itself.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private static void SetupOps_Exited(
            object sender,
            EventArgs e
            )
        {
            ReturnCode mutexCode;
            Result mutexError = null;

            mutexCode = CloseMutexes(ref mutexError);

            if (mutexCode != ReturnCode.Ok)
                MaybeComplain(mutexCode, mutexError, true);

            RemoveExitedEventHandler();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and takes ownership of a named Win32 mutex.
        /// </summary>
        /// <param name="name">
        /// The name of the mutex to create.
        /// </param>
        /// <param name="lastError">
        /// Upon return, receives the last Win32 error code resulting from the
        /// attempt to create the mutex.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The handle of the created mutex, or <see cref="IntPtr.Zero" /> on
        /// failure.
        /// </returns>
        private static IntPtr CreateMutex(
            string name,
            ref int lastError,
            ref Result error
            )
        {
            IntPtr handle = IntPtr.Zero;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    handle = NativeOps.UnsafeNativeMethods.CreateMutex(
                        IntPtr.Zero, true, name);

                    lastError = Marshal.GetLastWin32Error();

                    if (handle == IntPtr.Zero)
                    {
                        error = String.Format(
                            "could not create mutex \"{0}\": {1}",
                            name, NativeOps.GetErrorMessage(lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified Win32 mutex handle.
        /// </summary>
        /// <param name="handle">
        /// The handle of the mutex to close.  Upon success, this is set to
        /// <see cref="IntPtr.Zero" />.
        /// </param>
        /// <param name="lastError">
        /// Upon return, receives the last Win32 error code resulting from the
        /// attempt to close the handle.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the mutex handle was closed; otherwise, false.
        /// </returns>
        private static bool CloseMutex(
            ref IntPtr handle,
            ref int lastError,
            ref Result error
            )
        {
            bool result = false;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    result = NativeOps.UnsafeNativeMethods.CloseHandle(
                        handle);

                    if (result) /* throw */
                    {
                        handle = IntPtr.Zero;
                    }
                    else
                    {
                        lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "could not close mutex \"{0}\": {1}",
                            handle, NativeOps.GetErrorMessage(lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers the exited event handler and creates the
        /// session-local and system-global setup mutexes, reporting any errors
        /// via the complaint subsystem.
        /// </summary>
        public static void CreateMutexes()
        {
            AddExitedEventHandler();

            ReturnCode mutexCode;
            Result mutexError = null;

            mutexCode = CreateMutexes(ref mutexError);

            if (mutexCode != ReturnCode.Ok)
                MaybeComplain(mutexCode, mutexError, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and takes ownership of the session-local and
        /// system-global setup mutexes (if they do not already exist),
        /// preventing the setup program from running while the library is in
        /// use.  An access-denied error is treated as success because it
        /// indicates the mutex already exists.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error(s).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode CreateMutexes(
            ref Result error
            )
        {
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                int lastError; /* REUSED */
                Result localError; /* REUSED */
                ResultList errors = null;

                //
                // NOTE: This should prevent setup from running while
                //       the library is in use (even if the setup is
                //       running in a different user session).
                //
                if ((globalMutex == IntPtr.Zero) &&
                    !String.IsNullOrEmpty(globalMutexName))
                {
                    lastError = 0;
                    localError = null;

                    globalMutex = CreateMutex(
                        globalMutexName, ref lastError, ref localError);

                    if ((globalMutex == IntPtr.Zero) &&
                        (lastError != NativeOps.UnsafeNativeMethods.ERROR_ACCESS_DENIED))
                    {
                        if (localError == null)
                        {
                            localError = String.Format(
                                "could not create global mutex \"{0}\": {1}",
                                globalMutexName, NativeOps.GetErrorMessage(lastError));
                        }

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                //
                // NOTE: This should prevent setup from running while
                //       the library is in use in this user session.
                //
                if ((mutex == IntPtr.Zero) &&
                    !String.IsNullOrEmpty(mutexName))
                {
                    lastError = 0;
                    localError = null;

                    mutex = CreateMutex(
                        mutexName, ref lastError, ref localError);

                    if ((mutex == IntPtr.Zero) &&
                        (lastError != NativeOps.UnsafeNativeMethods.ERROR_ACCESS_DENIED))
                    {
                        if (localError == null)
                        {
                            localError = String.Format(
                                "could not create normal mutex \"{0}\": {1}",
                                mutexName, NativeOps.GetErrorMessage(lastError));
                        }

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                //
                // NOTE: If there are errors, pass them back to the
                //       caller and return failure to indicate they
                //       should not be ignored; otherwise, succeed.
                //
                if (errors != null)
                {
                    error = errors;
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the session-local and system-global setup
        /// mutexes (if they are currently held), releasing the protection that
        /// prevents the setup program from running while the library is in use.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error(s).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode CloseMutexes(
            ref Result error
            )
        {
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                int lastError; /* REUSED */
                Result localError; /* REUSED */
                ResultList errors = null;

                if (mutex != IntPtr.Zero)
                {
                    lastError = 0;
                    localError = null;

                    if (CloseMutex(ref mutex, ref lastError, ref localError))
                    {
                        mutex = IntPtr.Zero;
                    }
                    else
                    {
                        if (localError == null)
                        {
                            localError = String.Format(
                                "could not close normal mutex \"{0}\": {1}",
                                mutex, NativeOps.GetErrorMessage(lastError));
                        }

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                if (globalMutex != IntPtr.Zero)
                {
                    lastError = 0;
                    localError = null;

                    if (CloseMutex(ref globalMutex, ref lastError, ref localError))
                    {
                        globalMutex = IntPtr.Zero;
                    }
                    else
                    {
                        if (localError == null)
                        {
                            localError = String.Format(
                                "could not close global mutex \"{0}\": {1}",
                                globalMutex, NativeOps.GetErrorMessage(lastError));
                        }

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                //
                // NOTE: If there are errors, pass them back to the
                //       caller and return failure to indicate they
                //       should not be ignored; otherwise, succeed.
                //
                if (errors != null)
                {
                    error = errors;
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }
        }
#endif
        #endregion
    }
}
