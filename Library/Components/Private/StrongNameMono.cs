/*
 * StrongNameMono.cs --
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
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides support, specific to the Mono runtime, for verifying
    /// the strong name signature of an assembly file by reflecting into the
    /// internal Mono.Security strong name types.
    /// </summary>
    [ObjectId("38a8621b-230f-45c3-a470-b0d4ffc1a2fd")]
    internal static class StrongNameMono
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The fully qualified name of the Mono strong name type.
        /// </summary>
        private static string StrongNameTypeName =
            "Mono.Security.StrongName";

        /// <summary>
        /// The fully qualified name of the Mono strong name manager type.
        /// </summary>
        private static string StrongNameManagerTypeName =
            "Mono.Security.StrongNameManager";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The name of the Mono method used to determine whether an assembly is
        /// strong named.
        /// </summary>
        private static string IsAssemblyStrongNamedMethodName =
            "IsAssemblyStrongnamed";

        /// <summary>
        /// The name of the Mono method used to determine whether an assembly
        /// must be verified.
        /// </summary>
        private static string MustVerifyMethodName = "MustVerify";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the cached reflection
        /// state of this class.
        /// </summary>
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached Mono strong name type, resolved on first use.
        /// </summary>
        private static Type strongNameType = null;
        /// <summary>
        /// The cached Mono strong name manager type, resolved on first use.
        /// </summary>
        private static Type strongNameManagerType = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached reflection handle for the Mono method used to determine
        /// whether an assembly is strong named, resolved on first use.
        /// </summary>
        private static MethodInfo isAssemblyStrongNamedMethodInfo = null;
        /// <summary>
        /// The cached reflection handle for the Mono method used to determine
        /// whether an assembly must be verified, resolved on first use.
        /// </summary>
        private static MethodInfo mustVerifyMethodInfo = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Determines, on the Mono runtime, whether the strong name signature
        /// of the specified assembly file is valid by reflecting into the
        /// internal Mono.Security strong name types.
        /// </summary>
        /// <param name="fileName">
        /// The path of the assembly file to check.
        /// </param>
        /// <param name="force">
        /// Non-zero to force verification even when it might otherwise be
        /// skipped.  This parameter is used for diagnostic tracing only.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, set to non-zero if the assembly is strong named.
        /// </param>
        /// <param name="verified">
        /// Upon return, set to non-zero if the assembly's strong name signature
        /// was verified.
        /// </param>
        /// <param name="error">
        /// Upon failure, set to an error message, or exception, describing the
        /// problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode IsStrongNameVerifiedMono(
            string fileName,
            bool force,
            ref bool returnValue,
            ref bool verified,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                goto done;
            }

            if (!CommonOps.Runtime.IsMono())
            {
                error = "not supported on this platform";
                goto done;
            }

            try
            {
                MethodInfo[] methodInfo = { null, null };

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    BindingFlags bindingFlags = (BindingFlags)0;

                    if (strongNameType == null)
                    {
                        strongNameType = Type.GetType(
                            StrongNameTypeName);
                    }

                    if ((strongNameType != null) &&
                        (isAssemblyStrongNamedMethodInfo == null))
                    {
                        if (bindingFlags == (BindingFlags)0)
                        {
                            bindingFlags = ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicStaticMethod,
                                true);
                        }

                        isAssemblyStrongNamedMethodInfo =
                            strongNameType.GetMethod(
                                IsAssemblyStrongNamedMethodName,
                                bindingFlags);
                    }

                    if (strongNameManagerType == null)
                    {
                        strongNameManagerType = Type.GetType(
                            StrongNameManagerTypeName);
                    }

                    if ((strongNameManagerType != null) &&
                        (mustVerifyMethodInfo == null))
                    {
                        if (bindingFlags == (BindingFlags)0)
                        {
                            bindingFlags = ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicStaticMethod,
                                true);
                        }

                        mustVerifyMethodInfo =
                            strongNameManagerType.GetMethod(
                                MustVerifyMethodName, bindingFlags);
                    }

                    methodInfo[0] = isAssemblyStrongNamedMethodInfo;
                    methodInfo[1] = mustVerifyMethodInfo;
                }

                if (methodInfo[0] == null)
                {
                    error = String.Format(
                        "missing runtime method {0}", FormatOps.MethodName(
                        StrongNameTypeName, IsAssemblyStrongNamedMethodName));

                    goto done;
                }

                if (methodInfo[1] == null)
                {
                    error = String.Format(
                        "missing runtime method {0}", FormatOps.MethodName(
                        StrongNameManagerTypeName, MustVerifyMethodName));

                    goto done;
                }

                returnValue = (bool)methodInfo[0].Invoke(
                    null, new object[] { fileName }); /* throw */

                if (returnValue)
                {
                    AssemblyName assemblyName =
                        AssemblyName.GetAssemblyName(fileName); /* throw */

                    if (assemblyName != null)
                    {
                        verified = (bool)methodInfo[1].Invoke(
                            null, new object[] { assemblyName }); /* throw */
                    }
                    else
                    {
                        verified = false;
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

        done:

            TraceOps.DebugTrace(String.Format(
                "IsStrongNameVerifiedMono: file {0} verification failure, " +
                "force = {1}, returnValue = {2}, verified = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), force, returnValue, verified,
                FormatOps.WrapOrNull(error)), typeof(StrongNameMono).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
    }
}
