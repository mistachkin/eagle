/*
 * ResolveManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the experimental management surface for custom
    /// resolvers within an Eagle interpreter.  It provides methods to perform
    /// variable-frame, namespace, call-frame, command, and variable resolution
    /// via the registered resolvers, as well as to add, query, reset, and
    /// retrieve those resolvers.  WARNING: the members of this interface are
    /// experimental and subject to change.
    /// </summary>
    [ObjectId("751ae2fb-3b13-4d4a-adc1-05b02bb01f8b")]
    public interface IResolveManager
    {
        ///////////////////////////////////////////////////////////////////////
        // VARIABLE FRAME RESOLUTION (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the call frame that should be used when looking up the
        /// specified variable name, using the registered resolvers.
        /// </summary>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="frame">
        /// On input, the starting call frame; on output, the resolved call
        /// frame to use for the variable.
        /// </param>
        /// <param name="varName">
        /// On input, the variable name to resolve; on output, the resolved
        /// variable name.
        /// </param>
        /// <param name="variableFlags">
        /// On input, the flags that control how the variable is resolved; on
        /// output, the resulting flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableFrameViaResolvers(
            LookupFlags lookupFlags,
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags variableFlags,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // NAMESPACE MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resolves the global namespace for the interpreter.
        /// </summary>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="namespace">
        /// Upon success, receives the resolved global namespace.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode GetGlobalNamespace(
            LookupFlags lookupFlags,
            ref INamespace @namespace,
            ref Result result
            );

        /// <summary>
        /// Resolves the current namespace associated with the specified call
        /// frame, using the registered resolvers.
        /// </summary>
        /// <param name="frame">
        /// The call frame for which to resolve the current namespace.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="namespace">
        /// Upon success, receives the resolved current namespace.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCurrentNamespaceViaResolvers(
            ICallFrame frame,
            LookupFlags lookupFlags,
            ref INamespace @namespace,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // CALL FRAME MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified call frame has the specified flags.
        /// </summary>
        /// <param name="frame">
        /// The call frame to check.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are present;
        /// otherwise, the presence of any of the specified flags is sufficient.
        /// </param>
        /// <returns>
        /// True if the call frame has the specified flags; false if it does
        /// not; otherwise, null if the determination could not be made.
        /// </returns>
        bool? HasCallFrameFlags(
            ICallFrame frame,
            CallFrameFlags hasFlags,
            bool all
            );

        /// <summary>
        /// Resolves the call frame at the specified level that matches the
        /// specified flag criteria.
        /// </summary>
        /// <param name="absolute">
        /// Non-zero if <paramref name="level" /> is an absolute level;
        /// otherwise, it is relative to the current level.
        /// </param>
        /// <param name="level">
        /// The level of the call frame to resolve.
        /// </param>
        /// <param name="hasFlags">
        /// The flags that the call frame must have.
        /// </param>
        /// <param name="notHasFlags">
        /// The flags that the call frame must not have.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the <paramref name="hasFlags" /> are
        /// present; otherwise, the presence of any of them is sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the <paramref name="notHasFlags" />
        /// are absent; otherwise, the absence of any of them is sufficient.
        /// </param>
        /// <param name="frame">
        /// Upon success, receives the resolved call frame.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCallFrame(
            bool absolute,
            int level,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref ICallFrame frame,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // COMMAND & VARIABLE RESOLUTION (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the engine flags that should be used by the registered
        /// resolvers.
        /// </summary>
        /// <param name="exact">
        /// Non-zero to request flags suitable for exact (non-abbreviated) name
        /// matching.
        /// </param>
        /// <returns>
        /// The engine flags to be used by the registered resolvers.
        /// </returns>
        EngineFlags GetResolverEngineFlags(bool exact);

        /// <summary>
        /// Resolves the executable entity associated with the specified command
        /// name, using the registered resolvers.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// The command name to resolve.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the invocation, if any.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="execute">
        /// Upon success, receives the resolved executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetIExecuteViaResolvers(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref IExecute execute,
            ref Result error
            );

        /// <summary>
        /// Resolves the executable entity associated with the specified command
        /// name, using the registered resolvers.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// The command name to resolve.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the invocation, if any.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags that control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, receives a value indicating whether the name matched
        /// more than one executable entity.
        /// </param>
        /// <param name="token">
        /// Upon success, receives the token identifying the resolved executable
        /// entity.
        /// </param>
        /// <param name="execute">
        /// Upon success, receives the resolved executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetIExecuteViaResolvers(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            );

        /// <summary>
        /// Resolves the variable associated with the specified variable name
        /// and optional index, using the registered resolvers.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to resolve.
        /// </param>
        /// <param name="varIndex">
        /// The array element index of the variable to resolve, if any.
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the variable is resolved; on
        /// output, the resulting flags.
        /// </param>
        /// <param name="variable">
        /// Upon success, receives the resolved variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableViaResolvers(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            );

        /// <summary>
        /// Resolves the variable associated with the specified name, splitting
        /// it into its variable-name and array-index components, using the
        /// registered resolvers.
        /// </summary>
        /// <param name="frame">
        /// The call frame to use as the starting context for the lookup.
        /// </param>
        /// <param name="name">
        /// The combined variable name to resolve and split.
        /// </param>
        /// <param name="index">
        /// The combined array element index to resolve and split, if any.
        /// </param>
        /// <param name="varName">
        /// Upon success, receives the resolved variable name component.
        /// </param>
        /// <param name="varIndex">
        /// Upon success, receives the resolved array element index component,
        /// if any.
        /// </param>
        /// <param name="variableFlags">
        /// On input, the flags that control how the variable is resolved; on
        /// output, the resulting flags.
        /// </param>
        /// <param name="variable">
        /// Upon success, receives the resolved variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableViaResolversWithSplit(
            ICallFrame frame,
            string name,
            string index,
            ref string varName,
            ref string varIndex,
            ref VariableFlags variableFlags,
            ref IVariable variable,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // RESOLVER MANAGEMENT (WARNING: EXPERIMENTAL)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether any resolvers are currently registered.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more resolvers are registered; otherwise, false.
        /// </returns>
        bool HasResolvers(ref Result error);

        /// <summary>
        /// Resets the registered resolvers to their default state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ResetResolvers(ref Result error);

        /// <summary>
        /// Gets the registered resolver with the specified priority.
        /// </summary>
        /// <param name="priority">
        /// The priority of the resolver to retrieve.
        /// </param>
        /// <param name="resolve">
        /// Upon success, receives the resolver with the specified priority.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetResolver(
            Priority priority,
            ref IResolve resolve,
            ref Result error
            );

        /// <summary>
        /// Adds a resolver to the set of registered resolvers.
        /// </summary>
        /// <param name="resolve">
        /// The resolver to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, resolver-specific data to associate with the resolver, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The priority at which to add the resolver.
        /// </param>
        /// <param name="result">
        /// Upon success, may receive an informational result; upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddResolver(
            IResolve resolve,
            IClientData clientData,
            Priority priority,
            ref Result result
            );
    }
}
