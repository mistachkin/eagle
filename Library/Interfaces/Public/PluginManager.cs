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
    /// <summary>
    /// This interface is implemented by the component that manages the
    /// plugins associated with an interpreter.  It provides the operations
    /// to find, load, and unload plugins, to add and remove the commands,
    /// functions, policies, and traces they contribute, and to restore the
    /// built-in plugins.
    /// </summary>
    [ObjectId("84e5c0d1-d3e0-4389-b7c5-111fd820071d")]
    public interface IPluginManager
    {
        ///////////////////////////////////////////////////////////////////////
        // PLUGIN LOADER
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the base directory used when resolving the locations
        /// of plugins.  This value may be null.
        /// </summary>
        string PluginBaseDirectory { get; set; }

        /// <summary>
        /// Finds a loaded plugin whose name matches the specified pattern and
        /// other criteria.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to search within, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare the specified pattern against
        /// candidate plugin names.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match candidate plugin names.  This parameter
        /// may be null.
        /// </param>
        /// <param name="version">
        /// The specific plugin version to match, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token the matching plugin assembly must have, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching; otherwise,
        /// matching is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The matching plugin, or null if no matching plugin was found.
        /// </returns>
        IPlugin FindPlugin(
            AppDomain appDomain,
            MatchMode mode,
            string pattern,
            Version version,
            byte[] publicKeyToken,
            bool noCase,
            ref Result error
            );

        /// <summary>
        /// Finds a loaded plugin whose name matches the specified pattern and
        /// other criteria, using the specified lookup flags.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to search within, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare the specified pattern against
        /// candidate plugin names.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match candidate plugin names.  This parameter
        /// may be null.
        /// </param>
        /// <param name="version">
        /// The specific plugin version to match, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token the matching plugin assembly must have, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how candidate plugins are looked up.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching; otherwise,
        /// matching is case-sensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The matching plugin, or null if no matching plugin was found.
        /// </returns>
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

        /// <summary>
        /// Adds a value to the list of arguments associated with the named
        /// plugin.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to add the argument for.
        /// </param>
        /// <param name="value">
        /// The argument value to add.
        /// </param>
        /// <returns>
        /// The number of plugin arguments after the value was added.
        /// </returns>
        int AddPluginArguments(
            string name,
            string value
            );

        /// <summary>
        /// Removes all arguments associated with the named plugin.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to remove the arguments for.
        /// </param>
        /// <returns>
        /// The number of plugin arguments that were removed.
        /// </returns>
        int RemovePluginArguments(
            string name
            );

        /// <summary>
        /// Loads a plugin from the specified in-memory assembly image.
        /// </summary>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly image that contains the plugin.
        /// </param>
        /// <param name="symbolBytes">
        /// The raw bytes of the debugging symbols for the assembly, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="evidence">
        /// The security evidence to associate with the loaded assembly, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to instantiate, if any.  This
        /// parameter may be null to detect the type automatically.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is loaded.
        /// </param>
        /// <param name="plugin">
        /// Upon success, receives the loaded plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
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

        /// <summary>
        /// Loads a plugin from the assembly identified by the specified
        /// assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the assembly that contains the plugin.
        /// </param>
        /// <param name="evidence">
        /// The security evidence to associate with the loaded assembly, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to instantiate, if any.  This
        /// parameter may be null to detect the type automatically.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is loaded.
        /// </param>
        /// <param name="plugin">
        /// Upon success, receives the loaded plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
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

        /// <summary>
        /// Loads a plugin from the assembly contained in the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The path and file name of the assembly that contains the plugin.
        /// </param>
        /// <param name="evidence">
        /// The security evidence to associate with the loaded assembly, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="hashValue">
        /// The expected hash value of the assembly file, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="hashAlgorithm">
        /// The hash algorithm used to compute
        /// <c>hashValue</c>.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to instantiate, if any.  This
        /// parameter may be null to detect the type automatically.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is loaded.
        /// </param>
        /// <param name="plugin">
        /// Upon success, receives the loaded plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
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

        /// <summary>
        /// Unloads the specified plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin to unload.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is unloaded.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode UnloadPlugin(
            IPlugin plugin,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        /// <summary>
        /// Unloads the plugin identified by the specified token.
        /// </summary>
        /// <param name="token">
        /// The token that identifies the plugin to unload.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is unloaded.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode UnloadPlugin(
            long token,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        /// <summary>
        /// Unloads the plugin identified by the specified name.
        /// </summary>
        /// <param name="name">
        /// The name that identifies the plugin to unload.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the plugin is unloaded.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode UnloadPlugin(
            string name,
            IClientData clientData,
            PluginFlags flags,
            ref Result result
            );

        /// <summary>
        /// Adds the commands contributed by the specified plugin to the
        /// interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose commands are to be added.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the commands are added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode AddCommands(
            IPlugin plugin,
            IClientData clientData,
            CommandFlags flags,
            ref Result error
            );

        /// <summary>
        /// Removes the commands contributed by the specified plugin from the
        /// interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose commands are to be removed.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the commands are removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode RemoveCommands(
            IPlugin plugin,
            IClientData clientData,
            CommandFlags flags,
            ref Result error
            );

        /// <summary>
        /// Removes the functions contributed by the specified plugin from
        /// the interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose functions are to be removed.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the functions are removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode RemoveFunctions(
            IPlugin plugin,
            IClientData clientData,
            FunctionFlags flags,
            ref Result error
            );

        /// <summary>
        /// Adds the policies contributed by the specified plugin to the
        /// interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose policies are to be added.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the policies are added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode AddPolicies(
            IPlugin plugin,
            IClientData clientData,
            PolicyFlags flags,
            ref Result error
            );

        /// <summary>
        /// Removes the policies contributed by the specified plugin from the
        /// interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose policies are to be removed.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the policies are removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode RemovePolicies(
            IPlugin plugin,
            IClientData clientData,
            PolicyFlags flags,
            ref Result error
            );

        /// <summary>
        /// Removes the traces contributed by the specified plugin from the
        /// interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose traces are to be removed.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied to the plugin, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode RemoveTraces(
            IPlugin plugin,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Restores the built-in core plugin to the interpreter.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat any failure encountered while restoring the
        /// core plugin as an error.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit verbose diagnostic output while restoring the
        /// core plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode RestoreCorePlugin(
            bool strict,
            bool verbose,
            ref Result result
            );

#if NOTIFY && NOTIFY_ARGUMENTS
        /// <summary>
        /// Restores the built-in monitor plugin to the interpreter.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat any failure encountered while restoring the
        /// monitor plugin as an error.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit verbose diagnostic output while restoring the
        /// monitor plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode RestoreMonitorPlugin(
            bool strict,
            bool verbose,
            ref Result result
            );
#endif
    }
}
