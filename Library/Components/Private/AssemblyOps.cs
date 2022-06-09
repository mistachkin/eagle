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
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    [ObjectId("adb2230c-58c9-4950-991d-d2e83931ad47")]
    internal static class AssemblyOps
    {
        #region Private Constants
        private const string CodeBaseDll = ".DLL"; /* CASE-SENSITIVE */
        private const string CodeBaseExe = ".EXE"; /* CASE-SENSITIVE */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Support Methods
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

        public static Assembly FindInAppDomain(
            AppDomain appDomain,
            string name,
            Version version,
            byte[] publicKeyToken,
            ref Result error
            )
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

                        AssemblyName assemblyName = assembly.GetName();

                        if (assemblyName == null)
                            continue;

                        if ((name != null) && !SharedStringOps.SystemEquals(
                                assemblyName.Name, name))
                        {
                            continue;
                        }

                        if ((version != null) && (PackageOps.VersionCompare(
                                assemblyName.Version, version) != 0))
                        {
                            continue;
                        }

                        if ((publicKeyToken != null) && !ArrayOps.Equals(
                                assemblyName.GetPublicKeyToken(), publicKeyToken))
                        {
                            continue;
                        }

                        return assembly;
                    }
                }
            }

            error = "assembly not found in application domain";
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
            if (PlatformOps.IsWindowsOperatingSystem() &&
                !RuntimeOps.IsFileTrusted(fileName, IntPtr.Zero))
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

        public static string GetAnchorPath() /* MAY RETURN NULL */
        {
            return CommonOps.Environment.GetVariable(
                EnvVars.AssemblyAnchorPath);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static Stream GetResourceStream(
            Assembly assembly,
            string name
            )
        {
            Result error = null;

            return GetResourceStream(assembly, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode GetStrongName(
            Assembly assembly,
            ref StrongName strongName
            )
        {
            Result error = null;

            return GetStrongName(assembly, ref strongName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static ReturnCode GetHash(
            Assembly assembly,
            ref Hash hash
            )
        {
            Result error = null;

            return GetHash(assembly, ref hash, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
#if !NET_STANDARD_20
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

        public static ReturnCode GetCertificate(
            Assembly assembly,
            ref X509Certificate certificate
            )
        {
            Result error = null;

            return GetCertificate(assembly, ref certificate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCertificate(
            Assembly assembly,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            if (assembly != null)
            {
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
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCertificate(
            byte[] assemblyBytes,
            ref X509Certificate certificate
            )
        {
            Result error = null;

            return GetCertificate(assemblyBytes, ref certificate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

#if !NET_STANDARD_20
        public static X509Certificate2 GetCertificate2(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                X509Certificate certificate = null;

                if (GetCertificate(assembly,
                        ref certificate) == ReturnCode.Ok)
                {
                    try
                    {
                        return (certificate != null) ?
                            new X509Certificate2(certificate) : null;
                    }
                    catch (Exception e)
                    {
                        //
                        // NOTE: Nothing we can do here except log the failure.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(AssemblyOps).Name,
                            TracePriority.SecurityError);
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
#endif
        #endregion
    }
}
