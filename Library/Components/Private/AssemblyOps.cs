/*
 * AssemblyOps.cs --
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
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

#if CAS_POLICY
using System.Security.Policy;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods used to query
    /// and manipulate assemblies and assembly names, including locating
    /// assemblies in an application domain, loading assemblies, reading assembly
    /// metadata (such as the version, public key, and code base path), accessing
    /// manifest resources, and obtaining strong name, hash, and certificate
    /// information for an assembly.
    /// </summary>
    [ObjectId("adb2230c-58c9-4950-991d-d2e83931ad47")]
    internal static class AssemblyOps
    {
        #region Private Constants
        /// <summary>
        /// The uppercase file extension used by the .NET Framework runtime in
        /// the code base of a dynamic-link library assembly.
        /// </summary>
        private const string CodeBaseDll = ".DLL"; /* CASE-SENSITIVE */

        /// <summary>
        /// The uppercase file extension used by the .NET Framework runtime in
        /// the code base of an executable assembly.
        /// </summary>
        private const string CodeBaseExe = ".EXE"; /* CASE-SENSITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Support Methods
        /// <summary>
        /// This method determines whether two assembly names refer to the same
        /// assembly, comparing them by reference and by full name.
        /// </summary>
        /// <param name="assemblyName1">
        /// The first assembly name to compare.
        /// </param>
        /// <param name="assemblyName2">
        /// The second assembly name to compare.
        /// </param>
        /// <returns>
        /// True if the assembly names are considered the same (including when
        /// both are null); otherwise, false.
        /// </returns>
        public static bool IsSameAssemblyName(
            AssemblyName assemblyName1,
            AssemblyName assemblyName2
            )
        {
            if ((assemblyName1 == null) && (assemblyName2 == null))
                return true;

            if ((assemblyName1 == null) || (assemblyName2 == null))
                return false;

            if (Object.ReferenceEquals(assemblyName1, assemblyName2))
                return true;

            if (SharedStringOps.SystemEquals(
                    assemblyName1.FullName, assemblyName2.FullName))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether an assembly name matches the specified
        /// criteria.  Each criterion that is null is ignored.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name to test.
        /// </param>
        /// <param name="name">
        /// The simple name to require, or null to ignore the name.
        /// </param>
        /// <param name="version">
        /// The version to require, or null to ignore the version.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to require, or null to ignore the culture.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token to require, or null to ignore the public key
        /// token.
        /// </param>
        /// <returns>
        /// True if the assembly name matches all of the specified criteria;
        /// otherwise, false.
        /// </returns>
        private static bool MatchAssemblyName(
            AssemblyName assemblyName, /* in */
            string name,               /* in: OPTIONAL */
            Version version,           /* in: OPTIONAL */
            CultureInfo cultureInfo,   /* in: OPTIONAL */
            byte[] publicKeyToken      /* in: OPTIONAL */
            )
        {
            if (assemblyName == null)
                return false;

            if ((name != null) && !SharedStringOps.SystemEquals(
                    assemblyName.Name, name))
            {
                return false;
            }

            if ((version != null) && (PackageOps.VersionCompare(
                    assemblyName.Version, version) != 0))
            {
                return false;
            }

            if ((cultureInfo != null) &&
                !cultureInfo.Equals(assemblyName.CultureInfo))
            {
                return false;
            }

            if ((publicKeyToken != null) && !ArrayOps.Equals(
                    assemblyName.GetPublicKeyToken(), publicKeyToken))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the full name of an assembly name
        /// matches the specified pattern, using the specified matching mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used during pattern matching.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name whose full name is tested.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against the full name.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare the full name to the pattern.
        /// </param>
        /// <returns>
        /// True if the full name matches the pattern; otherwise, false.
        /// </returns>
        private static bool MatchAssemblyName(
            Interpreter interpreter,   /* in */
            AssemblyName assemblyName, /* in */
            string pattern,            /* in */
            MatchMode mode             /* in */
            )
        {
            if (assemblyName == null)
                return false;

            return StringOps.Match(
                interpreter, mode, assemblyName.FullName, pattern, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an assembly name object from its string
        /// representation.
        /// </summary>
        /// <param name="assemblyName">
        /// The string representation of the assembly name to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The parsed assembly name, or null on failure.
        /// </returns>
        public static AssemblyName GetName(
            string assemblyName,
            ref Result error
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return new AssemblyName(
                        assemblyName); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid assembly name";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds an assembly loaded into the specified application
        /// domain whose name is the same as the specified assembly name.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to search.  When null, the current application
        /// domain is used.
        /// </param>
        /// <param name="assemblyName">
        /// The assembly name to search for.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindInAppDomain(
            AppDomain appDomain,
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                if (appDomain == null)
                    appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    Assembly[] assemblies = appDomain.GetAssemblies();

                    if (assemblies != null)
                    {
                        foreach (Assembly assembly in assemblies)
                        {
                            if (assembly == null)
                                continue;

                            if (IsSameAssemblyName(
                                    assembly.GetName(), assemblyName))
                            {
                                return assembly;
                            }
                        }
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds an assembly loaded into the specified application
        /// domain whose location refers to the same file as the specified path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when comparing file paths.  This value is not
        /// otherwise used.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request.  This value is not
        /// used.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search.  When null, the current application
        /// domain is used.
        /// </param>
        /// <param name="path">
        /// The file path to match against each assembly location.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first assembly to consider.  When null, the search
        /// starts at the beginning.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindInAppDomain(
            Interpreter interpreter, /* in: NOT USED */
            IClientData clientData,  /* in: NOT USED */
            AppDomain appDomain,     /* in: OPTIONAL */
            string path,             /* in */
            int? startIndex,         /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (appDomain == null)
                appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                Assembly[] assemblies = null;

                try
                {
                    assemblies = appDomain.GetAssemblies();
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }

                if (assemblies != null)
                {
                    int length = assemblies.Length;

                    if (length > 0)
                    {
                        int index = (startIndex != null) ?
                            (int)startIndex : 0;

                        for (; index < length; index++)
                        {
                            Assembly assembly = assemblies[index];

                            if (assembly == null)
                                continue;

                            string location;

                            try
                            {
                                location = assembly.Location; /* throw */
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(AssemblyOps).Name,
                                    TracePriority.AssemblyError);

                                continue;
                            }

                            if ((path != null) && !PathOps.IsSameFile(
                                    interpreter, location, path))
                            {
                                continue;
                            }

                            return assembly;
                        }
                    }
                }
            }

            error = "assembly not found in application domain";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds an assembly loaded into the specified application
        /// domain whose name matches the specified simple name, version, and
        /// public key token.  Each criterion that is null is ignored.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to search.  When null, the current application
        /// domain is used.
        /// </param>
        /// <param name="name">
        /// The simple name to require, or null to ignore the name.
        /// </param>
        /// <param name="version">
        /// The version to require, or null to ignore the version.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token to require, or null to ignore the public key
        /// token.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindInAppDomain(
            AppDomain appDomain,   /* in: OPTIONAL */
            string name,           /* in: OPTIONAL */
            Version version,       /* in: OPTIONAL */
            byte[] publicKeyToken, /* in: OPTIONAL */
            ref Result error       /* out */
            )
        {
            return FindInAppDomain(
                null, null, appDomain, name, version, null,
                publicKeyToken, null, null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds an assembly loaded into the specified application
        /// domain whose name matches the specified simple name, version,
        /// culture, and public key token.  Each criterion that is null is
        /// ignored.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.  This value is not
        /// used.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request.  This value is not
        /// used.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search.  When null, the current application
        /// domain is used.
        /// </param>
        /// <param name="name">
        /// The simple name to require, or null to ignore the name.
        /// </param>
        /// <param name="version">
        /// The version to require, or null to ignore the version.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to require, or null to ignore the culture.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token to require, or null to ignore the public key
        /// token.
        /// </param>
        /// <param name="mode">
        /// The matching mode associated with the request.  This value is not
        /// used.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first assembly to consider.  When null, the search
        /// starts at the beginning.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindInAppDomain(
            Interpreter interpreter, /* in: NOT USED */
            IClientData clientData,  /* in: NOT USED */
            AppDomain appDomain,     /* in: OPTIONAL */
            string name,             /* in: OPTIONAL */
            Version version,         /* in: OPTIONAL */
            CultureInfo cultureInfo, /* in: OPTIONAL */
            byte[] publicKeyToken,   /* in: OPTIONAL */
            MatchMode? mode,         /* in: NOT USED */
            int? startIndex,         /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (appDomain == null)
                appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                Assembly[] assemblies = null;

                try
                {
                    assemblies = appDomain.GetAssemblies();
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }

                if (assemblies != null)
                {
                    int length = assemblies.Length;

                    if (length > 0)
                    {
                        int index = (startIndex != null) ?
                            (int)startIndex : 0;

                        for (; index < length; index++)
                        {
                            Assembly assembly = assemblies[index];

                            if (assembly == null)
                                continue;

                            AssemblyName assemblyName = assembly.GetName();

                            if (!MatchAssemblyName(
                                    assemblyName, name, version,
                                    cultureInfo, publicKeyToken))
                            {
                                continue;
                            }

                            return assembly;
                        }
                    }
                }
            }

            error = "assembly not found in application domain";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds an assembly loaded into the specified application
        /// domain whose full name matches the specified pattern, using the
        /// specified matching mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used during pattern matching.  This value may be
        /// null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search.  When null, the current application
        /// domain is used.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare each full name to the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against each assembly full name, or null to
        /// match any assembly.
        /// </param>
        /// <param name="noCase">
        /// When non-zero, the pattern match is performed without regard to
        /// case.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first assembly to consider.  When null, the search
        /// starts at the beginning.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The matching assembly, or null when no matching assembly is found.
        /// </returns>
        public static Assembly FindInAppDomain(
            Interpreter interpreter, /* in: OPTIONAL */
            AppDomain appDomain,     /* in: OPTIONAL */
            MatchMode mode,          /* in */
            string pattern,          /* in: OPTIONAL */
            bool noCase,             /* in */
            int? startIndex,         /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (appDomain == null)
                appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                Assembly[] assemblies = null;

                try
                {
                    assemblies = appDomain.GetAssemblies();
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }

                if (assemblies != null)
                {
                    int length = assemblies.Length;

                    if (length > 0)
                    {
                        int index = (startIndex != null) ?
                            (int)startIndex : 0;

                        for (; index < length; index++)
                        {
                            Assembly assembly = assemblies[index];

                            if (assembly == null)
                                continue;

                            AssemblyName assemblyName = assembly.GetName();

                            if ((pattern != null) && !StringOps.Match(
                                    interpreter, mode, assemblyName.FullName,
                                    pattern, noCase))
                            {
                                continue;
                            }

                            return assembly;
                        }
                    }
                }
            }

            error = "assembly not found in application domain";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads an assembly from the bytes available in the
        /// specified stream.
        /// </summary>
        /// <param name="stream">
        /// The readable, seekable stream containing the raw assembly bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The loaded assembly, or null on failure.
        /// </returns>
        public static Assembly LoadFromStream(
            Stream stream,
            ref Result error
            )
        {
            if (stream == null)
            {
                error = "invalid stream";
                return null;
            }

            if (!stream.CanRead)
            {
                error = "stream is not readable";
                return null;
            }

            if (!stream.CanSeek)
            {
                error = "stream is not seekable";
                return null;
            }

            try
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    byte[] bytes = binaryReader.ReadBytes(
                        (int)stream.Length); /* throw */

                    return Assembly.Load(bytes); /* throw */
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies the assembly file with the specified name,
        /// optionally checking that it is trusted and that it has the expected
        /// public key token.
        /// </summary>
        /// <param name="fileName">
        /// The name of the assembly file to verify.  The file must exist.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token, or null to skip the public key token
        /// check.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request.  This value is not
        /// used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the assembly file is verified;
        /// otherwise, an error code.
        /// </returns>
        public static ReturnCode VerifyFromFile(
            string fileName,        /* in */
            byte[] publicKeyToken,  /* in: OPTIONAL */
            IClientData clientData, /* in: NOT USED */
            ref Result error        /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = "assembly file name does not exist";
                return ReturnCode.Error;
            }

#if NATIVE
            //
            // HACK: Will end up using the list of trusted hashes from the
            //       GlobalState.GetTrustedHashes method only.
            //
            if (!RuntimeOps.IsFileTrusted(null, null, fileName, IntPtr.Zero))
            {
#if DEBUG
                TraceOps.DebugTrace(String.Format(
                    "VerifyFromFile: assembly file name {0} is not trusted",
                    FormatOps.WrapOrNull(fileName)), typeof(GlobalState).Name,
                    TracePriority.SecurityError);
#else
                error = "assembly file name is not trusted";
                return ReturnCode.Error;
#endif
            }
#endif

            if ((publicKeyToken != null) &&
                !RuntimeOps.CheckPublicKeyToken(fileName, publicKeyToken))
            {
                error = "assembly file name has wrong public key";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Property Helper Methods
        /// <summary>
        /// This method gets the full name of the assembly that contains the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose containing assembly full name is returned.
        /// </param>
        /// <returns>
        /// The full name of the containing assembly, or null when it cannot be
        /// determined.
        /// </returns>
        public static string GetFullName(
            Type type
            )
        {
            if (type != null)
            {
                try
                {
                    Assembly assembly = type.Assembly;

                    if (assembly != null)
                    {
                        AssemblyName assemblyName = assembly.GetName();

                        if (assemblyName != null)
                            return assemblyName.FullName;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the public key of the specified assembly name as a
        /// hexadecimal string.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose public key is returned.
        /// </param>
        /// <returns>
        /// The public key as a hexadecimal string, or null when it cannot be
        /// determined.
        /// </returns>
        public static string GetPublicKey(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return ArrayOps.ToHexadecimalString(
                        assemblyName.GetPublicKey());
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the public key token of the specified assembly name
        /// as a hexadecimal string.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose public key token is returned.
        /// </param>
        /// <returns>
        /// The public key token as a hexadecimal string, or null when it cannot
        /// be determined.
        /// </returns>
        public static string GetPublicKeyToken(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return ArrayOps.ToHexadecimalString(
                        assemblyName.GetPublicKeyToken());
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the version of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose version is returned.
        /// </param>
        /// <returns>
        /// The version of the assembly, or null when it cannot be determined.
        /// </returns>
        public static Version GetVersion(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    AssemblyName assemblyName = assembly.GetName();

                    if (assemblyName != null)
                        return assemblyName.Version;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the version of the specified assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose version is returned.
        /// </param>
        /// <returns>
        /// The version of the assembly name, or null when it cannot be
        /// determined.
        /// </returns>
        public static Version GetVersion(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return assemblyName.Version;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the runtime version string of the common language
        /// runtime image for the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose image runtime version is returned.
        /// </param>
        /// <returns>
        /// The image runtime version string, or null when it cannot be
        /// determined.
        /// </returns>
        public static string GetImageRuntimeVersion(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    return assembly.ImageRuntimeVersion;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the module version identifier of the manifest module
        /// for the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose manifest module version identifier is returned.
        /// </param>
        /// <returns>
        /// The module version identifier, or <see cref="Guid.Empty" /> when it
        /// cannot be determined.
        /// </returns>
        public static Guid GetModuleVersionId(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    Module module = assembly.ManifestModule;

                    if (module != null)
                        return module.ModuleVersionId;
                }
                catch
                {
                    // do nothing.
                }
            }

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the directory of the current location of the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose current directory is returned.
        /// </param>
        /// <returns>
        /// The directory of the assembly location, or null when it cannot be
        /// determined.
        /// </returns>
        public static string GetCurrentPath(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    string location = assembly.Location;

                    if (!String.IsNullOrEmpty(location))
                        return Path.GetDirectoryName(location);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the directory of the original code base location of
        /// the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose original code base directory is returned.
        /// </param>
        /// <returns>
        /// The directory of the original code base location, or null when it
        /// cannot be determined.
        /// </returns>
        public static string GetOriginalPath(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    return GetOriginalPath(assembly.CodeBase);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the original local file path derived from the code
        /// base of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose original local file path is returned.
        /// </param>
        /// <returns>
        /// The original local file path, or null when it cannot be determined.
        /// </returns>
        public static string GetOriginalLocalPath(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    return GetOriginalLocalPath(assembly.CodeBase);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the original local file path derived from the code
        /// base of the specified assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose original local file path is returned.
        /// </param>
        /// <returns>
        /// The original local file path, or null when it cannot be determined.
        /// </returns>
        public static string GetOriginalLocalPath(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return GetOriginalLocalPath(assemblyName.CodeBase);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the directory of the original local file path
        /// derived from the specified code base.
        /// </summary>
        /// <param name="codeBase">
        /// The code base from which the original directory is derived.
        /// </param>
        /// <returns>
        /// The directory of the original local file path, or null when it cannot
        /// be determined.
        /// </returns>
        public static string GetOriginalPath(
            string codeBase
            )
        {
            try
            {
                if (!String.IsNullOrEmpty(codeBase))
                {
                    string localPath = GetOriginalLocalPath(codeBase);

                    if (!String.IsNullOrEmpty(localPath))
                        return Path.GetDirectoryName(localPath);
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the original local file path derived from the
        /// specified code base, fixing up the hard-coded uppercase ".DLL" and
        /// ".EXE" file extensions used by the .NET Framework runtime.
        /// </summary>
        /// <param name="codeBase">
        /// The code base from which the original local file path is derived.
        /// </param>
        /// <returns>
        /// The original local file path, or null when it cannot be determined or
        /// the code base does not refer to a file.
        /// </returns>
        public static string GetOriginalLocalPath(
            string codeBase
            )
        {
            try
            {
                if (!String.IsNullOrEmpty(codeBase))
                {
                    Uri uri;

                    if (Uri.TryCreate(
                            codeBase, UriKind.Absolute, out uri) &&
                        uri.IsFile)
                    {
                        string localPath = uri.LocalPath;

                        if (!String.IsNullOrEmpty(localPath))
                        {
                            //
                            // HACK: Fixup the hard-coded uppercase ".DLL" and
                            //       ".EXE" file extension strings used by the
                            //       .NET Framework RuntimeAssembly class.
                            //
                            if (localPath.EndsWith(
                                    CodeBaseDll, PathOps.ComparisonType))
                            {
                                localPath = localPath.Substring(0,
                                    localPath.Length - CodeBaseDll.Length) +
                                    FileExtension.Library;
                            }
                            else if (localPath.EndsWith(
                                    CodeBaseExe, PathOps.ComparisonType))
                            {
                                localPath = localPath.Substring(0,
                                    localPath.Length - CodeBaseExe.Length) +
                                    FileExtension.Executable;
                            }
                        }

                        return localPath;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the assembly anchor path from the environment, which
        /// may be used to override the base directory when resolving assembly
        /// paths.
        /// </summary>
        /// <returns>
        /// The configured assembly anchor path, or null when none is configured.
        /// </returns>
        public static string GetAnchorPath() /* MAY RETURN NULL */
        {
            return CommonOps.Environment.GetVariable(
                EnvVars.AssemblyAnchorPath);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines a usable directory path for the specified
        /// assembly that resides underneath the base directory of the current
        /// application domain, preferring the current location and then the
        /// original location, and falling back to the current location when
        /// neither resides underneath the base directory.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when comparing paths.  This value may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose path is determined.
        /// </param>
        /// <returns>
        /// A directory path for the assembly.  This method cannot return null.
        /// </returns>
        public static string GetPath(
            Interpreter interpreter, /* OPTIONAL */
            Assembly assembly
            ) /* CANNOT RETURN NULL */
        {
            //
            // NOTE: Fetch the base directory for the current application
            //       domain.  This will be used to check if the candidate
            //       assembly paths are underneath it.  It is now possible
            //       to override the value used here via the environment.
            //
            string path0 = GetAnchorPath();

            if (path0 == null)
                path0 = GlobalState.GetAppDomainBaseDirectory();

            //
            // NOTE: First, try to use the current path to the assembly,
            //       checking to make sure that it resides underneath the
            //       base directory for the application domain.
            //
            string path1 = GetCurrentPath(assembly);

            if (PathOps.IsUnderPath(interpreter, path1, path0))
                return path1;

            //
            // NOTE: Second, try to use the original path to the assembly,
            //       checking to make sure that it resides underneath the
            //       base directory for the application domain.
            //
            string path2 = GetOriginalPath(assembly);

            if (PathOps.IsUnderPath(interpreter, path2, path0))
                return path2;

            //
            // NOTE: At this point, we have failed to figure out a path for
            //       this assembly that actually resides within the current
            //       application domain.  This condition is not impossible;
            //       however, generally it should not happen via the core
            //       library assembly itself.
            //
            TraceOps.DebugTrace(String.Format(
                "could not determine a path for assembly {1} underneath " +
                "the application domain path {0}", FormatOps.DisplayPath(
                path0), FormatOps.DisplayAssemblyName(assembly)),
                typeof(AssemblyOps).Name, TracePriority.StartupError);

            //
            // NOTE: This method cannot return null; therefore, the legacy
            //       return value will be used instead (i.e. the current
            //       path to the assembly).
            //
            return path1;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Resource Helper Methods
        /// <summary>
        /// This method gets the manifest resource stream with the specified name
        /// from the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to read the manifest resource from.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource.
        /// </param>
        /// <returns>
        /// The manifest resource stream, or null when it cannot be obtained.
        /// </returns>
        public static Stream GetResourceStream(
            Assembly assembly,
            string name
            )
        {
            Result error = null;

            return GetResourceStream(assembly, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the manifest resource stream with the specified name
        /// from the specified assembly, reporting any error that occurs.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to read the manifest resource from.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The manifest resource stream, or null on failure.
        /// </returns>
        public static Stream GetResourceStream(
            Assembly assembly,
            string name,
            ref Result error
            )
        {
            if (assembly == null)
            {
                error = "invalid assembly";
                return null;
            }

            if (name == null)
            {
                error = "invalid resource name";
                return null;
            }

            try
            {
                Stream stream = assembly.GetManifestResourceStream(name);

                if (stream != null)
                {
                    return stream;
                }
                else
                {
                    error = String.Format(
                        "assembly {0} missing manifest resource stream {1}",
                        FormatOps.DisplayAssemblyName(assembly),
                        FormatOps.WrapOrNull(name));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the contents of the manifest resource stream with
        /// the specified name from the specified assembly, returning either the
        /// raw bytes or the decoded text.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to read the manifest resource from.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to decode the resource as text.  When null, a
        /// default encoding is used.  This value is ignored when
        /// <paramref name="raw" /> is non-zero.
        /// </param>
        /// <param name="raw">
        /// When non-zero, the raw bytes are returned; otherwise, the decoded
        /// text is returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The byte array or string read from the resource, or null on failure.
        /// </returns>
        public static object GetResourceStreamData(
            Assembly assembly,
            string name,
            Encoding encoding,
            bool raw,
            ref Result error
            )
        {
            Stream stream = GetResourceStream(
                assembly, name, ref error);

            if (stream != null)
            {
                try
                {
                    if (raw) /* NOTE: Binary data? */
                    {
                        byte[] bytes = null;

                        if (RuntimeOps.ReadStream(
                                stream, ref bytes,
                                ref error) == ReturnCode.Ok)
                        {
                            return bytes;
                        }
                    }
                    else
                    {
                        string text = null;

                        if (encoding != null)
                        {
                            if (RuntimeOps.ReadStream(
                                    stream, encoding, ref text,
                                    ref error) == ReturnCode.Ok)
                            {
                                return text;
                            }
                        }
                        else
                        {
                            if (RuntimeOps.ReadStream(
                                    stream, ref text,
                                    ref error) == ReturnCode.Ok)
                            {
                                return text;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the contents of the manifest resource stream with
        /// the specified name from the specified assembly and parses it as a
        /// list, optionally enforcing minimum and maximum element counts.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to read the manifest resource from.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to decode the resource as text.  When null, a
        /// default encoding is used.
        /// </param>
        /// <param name="minimumCount">
        /// The minimum number of elements required, or a negative value to skip
        /// the minimum check.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of elements allowed, or a negative value to skip
        /// the maximum check.
        /// </param>
        /// <param name="readOnly">
        /// When non-zero, the resulting list is created as read-only.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// The parsed list, or null on failure.
        /// </returns>
        public static StringList GetResourceStreamList(
            Assembly assembly,
            string name,
            Encoding encoding,
            int minimumCount,
            int maximumCount,
            bool readOnly,
            ref Result error
            )
        {
            Stream stream = GetResourceStream(
                assembly, name, ref error);

            if (stream != null)
            {
                try
                {
                    string text = null;

                    if (encoding != null)
                    {
                        if (RuntimeOps.ReadStream(
                                stream, encoding, ref text,
                                ref error) != ReturnCode.Ok)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (RuntimeOps.ReadStream(
                                stream, ref text,
                                ref error) != ReturnCode.Ok)
                        {
                            return null;
                        }
                    }

                    StringList list = null;

                    if (ParserOps<string>.SplitList(
                            null, text, 0, Length.Invalid, readOnly,
                            ref list, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    int count = list.Count;

                    if ((minimumCount >= 0) &&
                        (list.Count < minimumCount))
                    {
                        error = String.Format(
                            "list {0} has less than {1} elements",
                            FormatOps.WrapOrNull(name), minimumCount);

                        return null;
                    }

                    if ((maximumCount >= 0) &&
                        (list.Count > maximumCount))
                    {
                        error = String.Format(
                            "list {0} has more than {1} elements",
                            FormatOps.WrapOrNull(name), maximumCount);

                        return null;
                    }

                    return list;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the manifest resource stream that contains the icon
        /// for the core library package.
        /// </summary>
        /// <returns>
        /// The icon resource stream, or null when it cannot be obtained.
        /// </returns>
        public static Stream GetIconStream()
        {
            Assembly assembly = GlobalState.GetAssembly();

            if (assembly == null)
                return null;

            string packageName = GlobalState.GetPackageName();

            if (String.IsNullOrEmpty(packageName))
                return null;

            return GetResourceStream(
                assembly, packageName + FileExtension.Icon);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly StrongName Helper Methods
#if CAS_POLICY
        /// <summary>
        /// This method gets the strong name evidence of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose strong name is returned.
        /// </param>
        /// <returns>
        /// The strong name of the assembly, or null when it cannot be obtained.
        /// </returns>
        public static StrongName GetStrongName(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                StrongName strongName = null;

                if (GetStrongName(assembly,
                        ref strongName) == ReturnCode.Ok)
                {
                    return strongName;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the strong name evidence of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose strong name is returned.
        /// </param>
        /// <param name="strongName">
        /// Upon success, receives the strong name of the assembly.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetStrongName(
            Assembly assembly,
            ref StrongName strongName
            )
        {
            Result error = null;

            return GetStrongName(assembly, ref strongName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the strong name evidence of the specified assembly,
        /// reporting any error that occurs.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose strong name is returned.
        /// </param>
        /// <param name="strongName">
        /// Upon success, receives the strong name of the assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetStrongName(
            Assembly assembly,
            ref StrongName strongName,
            ref Result error
            )
        {
            if (assembly != null)
            {
                Evidence evidence = assembly.Evidence;

                if (evidence != null)
                {
                    try
                    {
                        foreach (object item in evidence)
                        {
                            if (item is StrongName)
                            {
                                strongName = (StrongName)item;
                                return ReturnCode.Ok;
                            }
                        }

                        error = "no strong name found";
                        return ReturnCode.Error;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid evidence";
                }
            }
            else
            {
                error = "invalid assembly";
            }

            TraceOps.DebugTrace(String.Format(
                "GetStrongName: assembly {0} query failure, error = {1}",
                FormatOps.WrapOrNull(assembly),
                FormatOps.WrapOrNull(true, true, error)),
                typeof(AssemblyOps).Name, TracePriority.SecurityError);

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Hash Helper Methods
#if CAS_POLICY
        /// <summary>
        /// This method gets the hash evidence of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose hash is returned.
        /// </param>
        /// <returns>
        /// The hash of the assembly, or null when it cannot be obtained.
        /// </returns>
        public static Hash GetHash(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                Hash hash = null;

                if (GetHash(assembly, ref hash) == ReturnCode.Ok)
                    return hash;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the hash evidence of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose hash is returned.
        /// </param>
        /// <param name="hash">
        /// Upon success, receives the hash of the assembly.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode GetHash(
            Assembly assembly,
            ref Hash hash
            )
        {
            Result error = null;

            return GetHash(assembly, ref hash, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the hash evidence of the specified assembly,
        /// reporting any error that occurs.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose hash is returned.
        /// </param>
        /// <param name="hash">
        /// Upon success, receives the hash of the assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetHash(
            Assembly assembly,
            ref Hash hash,
            ref Result error
            )
        {
            if (assembly != null)
            {
                Evidence evidence = assembly.Evidence;

                if (evidence != null)
                {
                    try
                    {
                        foreach (object item in evidence)
                        {
                            if (item is Hash)
                            {
                                hash = (Hash)item;
                                return ReturnCode.Ok;
                            }
                        }

                        error = "no hash found";
                        return ReturnCode.Error;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid evidence";
                }
            }
            else
            {
                error = "invalid assembly";
            }

            TraceOps.DebugTrace(String.Format(
                "GetHash: assembly {0} query failure, error = {1}",
                FormatOps.WrapOrNull(assembly),
                FormatOps.WrapOrNull(true, true, error)),
                typeof(AssemblyOps).Name, TracePriority.SecurityError);

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Certificate Helper Methods
        /// <summary>
        /// This method gets the X.509 certificate used to sign the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose signer certificate is returned.
        /// </param>
        /// <returns>
        /// The signer certificate, or null when it cannot be obtained.
        /// </returns>
        public static X509Certificate GetCertificate(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                X509Certificate certificate = null;

                if (GetCertificate(assembly,
                        ref certificate) == ReturnCode.Ok)
                {
                    return certificate;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the X.509 certificate used to sign the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose signer certificate is returned.
        /// </param>
        /// <param name="certificate">
        /// Upon success, receives the signer certificate of the assembly.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetCertificate(
            Assembly assembly,
            ref X509Certificate certificate
            )
        {
            Result error = null;

            return GetCertificate(assembly, ref certificate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the X.509 certificate used to sign the specified
        /// assembly, reporting any error that occurs.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose signer certificate is returned.
        /// </param>
        /// <param name="certificate">
        /// Upon success, receives the signer certificate of the assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetCertificate(
            Assembly assembly,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            if (assembly != null)
            {
#if !NET_STANDARD_20
                Module module = assembly.ManifestModule;

                if (module != null)
                {
                    try
                    {
                        certificate = module.GetSignerCertificate();

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid module";
                }
#else
                try
                {
                    certificate = X509Certificate.CreateFromSignedFile(
                        assembly.Location);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
#endif
            }
            else
            {
                error = "invalid assembly";
            }

#if DEBUG
            if (!GlobalState.IsAssembly(assembly))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate: assembly {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(assembly),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(AssemblyOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an X.509 certificate from the raw bytes of an
        /// assembly.
        /// </summary>
        /// <param name="assemblyBytes">
        /// The raw assembly bytes from which the certificate is created.
        /// </param>
        /// <param name="certificate">
        /// Upon success, receives the certificate created from the bytes.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetCertificate(
            byte[] assemblyBytes,
            ref X509Certificate certificate
            )
        {
            Result error = null;

            return GetCertificate(assemblyBytes, ref certificate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an X.509 certificate from the raw bytes of an
        /// assembly, reporting any error that occurs.
        /// </summary>
        /// <param name="assemblyBytes">
        /// The raw assembly bytes from which the certificate is created.
        /// </param>
        /// <param name="certificate">
        /// Upon success, receives the certificate created from the bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetCertificate(
            byte[] assemblyBytes,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            if (assemblyBytes != null)
            {
                try
                {
                    certificate = new X509Certificate(assemblyBytes);
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid assembly bytes";
            }

            TraceOps.DebugTrace(String.Format(
                "GetCertificate: query failure, error = {0}",
                FormatOps.WrapOrNull(
                true, true, error)),
                typeof(AssemblyOps).Name, TracePriority.SecurityError);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the X.509 version 2 certificate used to sign the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose signer certificate is returned.
        /// </param>
        /// <param name="strict">
        /// When non-zero, the absence of a certificate is treated as a failure;
        /// otherwise, a missing certificate yields a successful return with a
        /// null certificate.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, receives the version 2 signer certificate of the
        /// assembly, which may be null when <paramref name="strict" /> is zero
        /// and no certificate is present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetCertificate2(
            Assembly assembly,
            bool strict,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            X509Certificate certificate = null;

            if (GetCertificate(assembly, ref certificate,
                    ref error) == ReturnCode.Ok)
            {
                if (certificate != null)
                {
                    try
                    {
                        certificate2 = new X509Certificate2(certificate);
                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else if (!strict)
                {
                    certificate2 = null;

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid certificate";
                }
            }

#if DEBUG
            if (!GlobalState.IsAssembly(assembly))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate2: assembly {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(assembly),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(AssemblyOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
