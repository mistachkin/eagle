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
    [ObjectId("38a8621b-230f-45c3-a470-b0d4ffc1a2fd")]
    internal static class StrongNameMono
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string StrongNameTypeName =
            "Mono.Security.StrongName";

        private static string StrongNameManagerTypeName =
            "Mono.Security.StrongNameManager";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string IsAssemblyStrongNamedMethodName =
            "IsAssemblyStrongnamed";

        private static string MustVerifyMethodName = "MustVerify";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static Type strongNameType = null;
        private static Type strongNameManagerType = null;

        ///////////////////////////////////////////////////////////////////////

        private static MethodInfo isAssemblyStrongNamedMethodInfo = null;
        private static MethodInfo mustVerifyMethodInfo = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
