/*
 * PluginManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if CAS_POLICY
using System.Configuration.Assemblies;
#endif

using System.Reflection;

#if CAS_POLICY
using System.Security.Policy;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("84e5c0d1-d3e0-4389-b7c5-111fd820071d")]
    public interface IPluginManager
    {
        ///////////////////////////////////////////////////////////////////////
        // PLUGIN LOADER
        ///////////////////////////////////////////////////////////////////////

        string PluginBaseDirectory { get; set; }

        IPlugin FindPlugin(
            AppDomain appDomain,
            MatchMode mode,
            string pattern,
            Version version,
            byte[] publicKeyToken,
            bool noCase,
            ref Result error
            );

        IPlugin FindPlugin(
            AppDomain appDomain,
            MatchMode mode,
            string pattern,
            Version version,
            byte[] publicKeyToken,
            LookupFlags lookupFlags,
            bool noCase,
            ref Result error
            );

        int AddPluginArguments(
            string name,
            string value
            );

        int RemovePluginArguments(
            string name
            );

        ReturnCode LoadPlugin(
            byte[] assemblyBytes,
            byte[] symbolBytes,
#if CAS_POLICY
            Evidence evidence,
#endif
            string typeName,
            IClientData clientData,
            PluginFlags flags,
            ref IPlugin plugin,
            ref Result result
            );

        [Obsolete()]
        ReturnCode LoadPlugin(
            AssemblyName assemblyName,
#if CAS_POLICY
            Evidence evidence,
#endif
            string typeName,
            IClientData clientData,
            PluginFlags flags,
            ref IPlugin plugin,
            ref Result result
            );

        ReturnCode LoadPlugin(
            string fileName,
#if CAS_POLICY
            Evidence evidence,
            byte[] hashValue,
            AssemblyHashAlgorithm hashAlgorithm,
#endif
            string typeName,
            IClientData clientData,
            PluginFlags flags,
            ref IPlugin plugin,
            ref Result result
            );

        ReturnCode UnloadPlugin(
            IPlugin plugin,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        ReturnCode UnloadPlugin(
            long token,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        ReturnCode UnloadPlugin(
            string name,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        ReturnCode AddCommands(
            IPlugin plugin,
            IClientData clientData,
            CommandFlags flags,
            ref Result error
            );

        ReturnCode RemoveCommands(
            IPlugin plugin,
            IClientData clientData,
            CommandFlags flags,
            ref Result error
            );

        ReturnCode RemoveFunctions(
            IPlugin plugin,
            IClientData clientData,
            FunctionFlags flags,
            ref Result error
            );

        ReturnCode AddPolicies(
            IPlugin plugin,
            IClientData clientData,
            ref Result error
            );

        ReturnCode RemovePolicies(
            IPlugin plugin,
            IClientData clientData,
            ref Result error
            );

        ReturnCode RemoveTraces(
            IPlugin plugin,
            IClientData clientData,
            ref Result error
            );

        ReturnCode RestoreCorePlugin(
            bool strict,
            ref Result result
            );

#if NOTIFY && NOTIFY_ARGUMENTS
        ReturnCode RestoreMonitorPlugin(
            bool strict,
            ref Result result
            );
#endif
    }
}
