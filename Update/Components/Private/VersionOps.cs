/*
 * VersionOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides helper methods for querying the operating system,
    /// the assembly, the runtime version, and whether the application is
    /// running under Mono.
    /// </summary>
    [Guid("8b3a9b91-dd52-4165-aaa7-f64eef0cab05")]
    internal static class VersionOps
    {
        #region Private Constants
        //
        // NOTE: This type is only present in Mono.
        //
        /// <summary>
        /// The name of the type that is only present when running under Mono.
        /// </summary>
        private static readonly string MonoRuntimeType = "Mono.Runtime";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The display name used to identify the Mono runtime.
        /// </summary>
        private static readonly string MonoRuntimeName = "Mono";

        /// <summary>
        /// The display name used to identify the Microsoft .NET runtime.
        /// </summary>
        private static readonly string MicrosoftRuntimeName = "Microsoft.NET";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// The cached result indicating whether the application is running
        /// under Mono, or null if it has not yet been determined.
        /// </summary>
        private static bool? isMono = null; // NOTE: Are we running in Mono?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Platform Support Methods
        /// <summary>
        /// This method determines whether the current operating system is a
        /// version of Windows.
        /// </summary>
        /// <returns>
        /// True if running on Windows; otherwise, false.
        /// </returns>
        public static bool IsWindowsOperatingSystem()
        {
            OperatingSystem operatingSystem = Environment.OSVersion;

            if (operatingSystem != null)
            {
                PlatformID platformId = operatingSystem.Platform;

                return ((platformId == PlatformID.Win32Windows) ||
                    (platformId == PlatformID.Win32NT));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Vista or a later version.
        /// </summary>
        /// <returns>
        /// True if running on Windows Vista or higher; otherwise, false.
        /// </returns>
        public static bool IsWindowsVistaOrHigher()
        {
            OperatingSystem operatingSystem = Environment.OSVersion;

            if ((operatingSystem != null) &&
                (operatingSystem.Platform == PlatformID.Win32NT))
            {
                Version version = operatingSystem.Version;

                if (version.Major >= 6) /* VISTA = 6.0 */
                    return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Support Methods
        /// <summary>
        /// This method returns the simple name of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose name is returned.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The simple name of the assembly, or null if it is unavailable.
        /// </returns>
        public static string GetAssemblyName(
            Assembly assembly
            )
        {
            if (assembly == null)
                return null;

            AssemblyName assemblyName = assembly.GetName();

            if (assemblyName == null)
                return null;

            return assemblyName.Name;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the version of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose version is returned.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The version of the assembly, or null if it is unavailable.
        /// </returns>
        public static Version GetAssemblyVersion(
            Assembly assembly
            )
        {
            if (assembly == null)
                return null;

            AssemblyName assemblyName = assembly.GetName();

            if (assemblyName == null)
                return null;

            return assemblyName.Version;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Support Methods
        /// <summary>
        /// This method compares two version values, treating a null version as
        /// less than any non-null version and two null versions as equal.
        /// </summary>
        /// <param name="version1">
        /// The first version to compare.  This parameter may be null.
        /// </param>
        /// <param name="version2">
        /// The second version to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A negative number if <paramref name="version1" /> is less than
        /// <paramref name="version2" />, a positive number if it is greater, or
        /// zero if they are equal.
        /// </returns>
        public static int Compare(
            Version version1,
            Version version2
            )
        {
            if ((version1 != null) && (version2 != null))
                return version1.CompareTo(version2);
            else if ((version1 == null) && (version2 == null))
                return 0;  // x (null) is equal to y (null)
            else if (version1 == null)
                return -1; // x (null) is less than y (non-null)
            else
                return 1;  // x (non-null) is greater than y (null)
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Mono Support Methods
        /// <summary>
        /// This method determines whether the application is running under the
        /// Mono runtime, caching the result after the first call.
        /// </summary>
        /// <returns>
        /// True if running under Mono; otherwise, false.
        /// </returns>
        public static bool IsMono()
        {
            try
            {
                if (isMono == null)
                    isMono = (Type.GetType(MonoRuntimeType) != null);

                return (bool)isMono;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the display name of the runtime the application
        /// is currently running under.
        /// </summary>
        /// <returns>
        /// The Mono runtime name when running under Mono; otherwise, the
        /// Microsoft .NET runtime name.
        /// </returns>
        public static string GetRuntimeName()
        {
            return IsMono() ? MonoRuntimeName : MicrosoftRuntimeName;
        }
        #endregion
    }
}
