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
    [Guid("8b3a9b91-dd52-4165-aaa7-f64eef0cab05")]
    internal static class VersionOps
    {
        #region Private Constants
        //
        // NOTE: This type is only present in Mono.
        //
        private static readonly string MonoRuntimeType = "Mono.Runtime";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string MonoRuntimeName = "Mono";
        private static readonly string MicrosoftRuntimeName = "Microsoft.NET";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static bool? isMono = null; // NOTE: Are we running in Mono?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Platform Support Methods
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

        public static string GetRuntimeName()
        {
            return IsMono() ? MonoRuntimeName : MicrosoftRuntimeName;
        }
        #endregion
    }
}
