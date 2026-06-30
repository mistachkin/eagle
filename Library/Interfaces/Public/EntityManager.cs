/*
 * EntityManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;

#if DATA
using System.Data;
#endif

using System.IO;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

using AutomaticCollection = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<string,
    Eagle._Components.Public.AnyPair<System.Delegate,
    Eagle._Components.Public.DelegateFlags>>>;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the Eagle interpreter to manage the
    /// full set of named entities it owns, including aliases, callbacks, I/O
    /// channels, commands, math functions, executable entities, opaque
    /// objects, expression operators, packages, plugins, policies,
    /// procedures, scopes, traces, namespaces, and (optionally) database
    /// connections and native delegates and modules.  It provides the methods
    /// used to check for, detect, look up, add, rename, and remove these
    /// entities, as well as to gather and verify their identifiers and to
    /// obtain usage metrics.
    /// </summary>
    [ObjectId("c6db1693-7821-4859-b5a9-d37406aefec9")]
    public interface IEntityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // ENTITY CHECKING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this interpreter currently contains any command
        /// aliases.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more aliases are present; otherwise, false.
        /// </returns>
        bool HasAliases(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// callbacks.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more callbacks are present; otherwise, false.
        /// </returns>
        bool HasCallbacks(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any I/O
        /// channels.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more channels are present; otherwise, false.
        /// </returns>
        bool HasChannels(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// commands.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more commands are present; otherwise, false.
        /// </returns>
        bool HasCommands(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any math
        /// functions.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more functions are present; otherwise, false.
        /// </returns>
        bool HasFunctions(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// executable entities.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more executable entities are present; otherwise,
        /// false.
        /// </returns>
        bool HasIExecutes(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any opaque
        /// object handles.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more objects are present; otherwise, false.
        /// </returns>
        bool HasObjects(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// expression operators.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more operators are present; otherwise, false.
        /// </returns>
        bool HasOperators(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any package
        /// indexes.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more package indexes are present; otherwise, false.
        /// </returns>
        bool HasPackageIndexes(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// packages.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more packages are present; otherwise, false.
        /// </returns>
        bool HasPackages(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any plugins.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more plugins are present; otherwise, false.
        /// </returns>
        bool HasPlugins(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// policies.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more policies are present; otherwise, false.
        /// </returns>
        bool HasPolicies(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any
        /// procedures.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more procedures are present; otherwise, false.
        /// </returns>
        bool HasProcedures(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any scopes.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more scopes are present; otherwise, false.
        /// </returns>
        bool HasScopes(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any variable
        /// traces.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more traces are present; otherwise, false.
        /// </returns>
        bool HasTraces(ref Result error);

#if DATA
        /// <summary>
        /// Determines whether this interpreter currently contains any database
        /// connections.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more database connections are present; otherwise,
        /// false.
        /// </returns>
        bool HasDbConnections(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any database
        /// transactions.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more database transactions are present; otherwise,
        /// false.
        /// </returns>
        bool HasDbTransactions(ref Result error);
#endif

#if EMIT && NATIVE && LIBRARY
        /// <summary>
        /// Determines whether this interpreter currently contains any native
        /// delegates.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more delegates are present; otherwise, false.
        /// </returns>
        bool HasDelegates(ref Result error);

        /// <summary>
        /// Determines whether this interpreter currently contains any native
        /// modules.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if one or more modules are present; otherwise, false.
        /// </returns>
        bool HasModules(ref Result error);
#endif

        ///////////////////////////////////////////////////////////////////////
        // ENTITY DETECTION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an alias with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if an alias with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesAliasExist(string name);

        /// <summary>
        /// Determines whether a callback with the specified name exists in
        /// this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the callback to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a callback with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesCallbackExist(string name);

        /// <summary>
        /// Determines whether an I/O channel with the specified name exists in
        /// this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a channel with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesChannelExist(string name);

        /// <summary>
        /// Determines whether a command with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the command to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a command with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesCommandExist(string name);

        /// <summary>
        /// Determines whether a math function with the specified name exists
        /// in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the function to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a function with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesFunctionExist(string name);

        /// <summary>
        /// Determines whether an executable entity with the specified name
        /// exists in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the executable entity to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if an executable entity with the
        /// specified name exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesIExecuteExist(string name);

        /// <summary>
        /// Determines whether an opaque object handle with the specified name
        /// exists in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the object to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if an object with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesObjectExist(string name);

        /// <summary>
        /// Determines whether an expression operator with the specified name
        /// exists in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the operator to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if an operator with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesOperatorExist(string name);

        /// <summary>
        /// Determines whether a package with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a package with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesPackageExist(string name);

        /// <summary>
        /// Determines whether a plugin with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a plugin with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesPluginExist(string name);

        /// <summary>
        /// Determines whether a policy with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the policy to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a policy with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesPolicyExist(string name);

        /// <summary>
        /// Determines whether a procedure with the specified name exists in
        /// this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a procedure with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesProcedureExist(string name);

        /// <summary>
        /// Determines whether a scope with the specified name exists in this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the scope to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a scope with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesScopeExist(string name);

        /// <summary>
        /// Determines whether a variable trace with the specified name exists
        /// in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the trace to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a trace with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesTraceExist(string name);

#if DATA
        /// <summary>
        /// Determines whether a database connection with the specified name
        /// exists in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the database connection to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a database connection with the
        /// specified name exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesDbConnectionExist(string name);

        /// <summary>
        /// Determines whether a database transaction with the specified name
        /// exists in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the database transaction to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a database transaction with the
        /// specified name exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesDbTransactionExist(string name);
#endif

#if EMIT && NATIVE && LIBRARY
        /// <summary>
        /// Determines whether a native delegate with the specified name exists
        /// in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the delegate to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a delegate with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesDelegateExist(string name);

        /// <summary>
        /// Determines whether a native module with the specified name exists
        /// in this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the module to check for.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if a module with the specified name
        /// exists; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        ReturnCode DoesModuleExist(string name);
#endif

        ///////////////////////////////////////////////////////////////////////
        // ENTITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up an entity identifier of the specified kind by name.
        /// </summary>
        /// <param name="kind">
        /// The kind of entity identifier to look up.
        /// </param>
        /// <param name="name">
        /// The name of the entity to look up.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the lookup, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="identifier">
        /// Upon success, this receives the entity identifier that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetIdentifier(
            IdentifierKind kind,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref IIdentifier identifier,
            ref Result error
            );

        /// <summary>
        /// Gathers the identifiers of the specified kind into a rule set.
        /// </summary>
        /// <param name="kind">
        /// The kind of entity identifiers to gather.
        /// </param>
        /// <param name="ruleType">
        /// The type of rule to create for each gathered identifier.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when gathering identifiers.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing when the first error is encountered.
        /// </param>
        /// <param name="ruleSet">
        /// Upon success, this receives the rule set populated with the
        /// gathered identifiers.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives the list of error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode GatherIdentifiers(
            IdentifierKind kind,
            RuleType ruleType,
            MatchMode mode,
            bool stopOnError,
            ref IRuleSet ruleSet,
            ref ResultList errors
            );

        /// <summary>
        /// Verifies that the identifiers of the specified kind match the rules
        /// in the specified rule set.
        /// </summary>
        /// <param name="kind">
        /// The kind of entity identifiers to verify.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when verifying identifiers.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to verify the identifiers.
        /// </param>
        /// <param name="unverified">
        /// Upon return, this receives the number of identifiers that could not
        /// be verified.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives the list of error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode VerifyIdentifiers(
            IdentifierKind kind,
            MatchMode mode,
            IRuleSet ruleSet,
            ref int unverified,
            ref ResultList errors
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether namespace support is enabled for this
        /// interpreter.
        /// </summary>
        /// <returns>
        /// True if namespace support is enabled; otherwise, false.
        /// </returns>
        bool AreNamespacesEnabled();

        /// <summary>
        /// Looks up a namespace by name.
        /// </summary>
        /// <param name="name">
        /// The name of the namespace to look up.
        /// </param>
        /// <param name="absolute">
        /// Non-zero if the namespace name should be treated as an absolute
        /// (fully qualified) name.
        /// </param>
        /// <param name="namespace">
        /// Upon success, this receives the namespace that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode LookupNamespace(
            string name,
            bool absolute,
            ref INamespace @namespace,
            ref Result error
            );

        /// <summary>
        /// Creates a new namespace.
        /// </summary>
        /// <param name="namespaceData">
        /// The data used to create the namespace.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the namespace, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newFrame">
        /// Non-zero to create a new call frame for the namespace.
        /// </param>
        /// <param name="namespace">
        /// Upon success, this receives the namespace that was created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CreateNamespace(
            INamespaceData namespaceData,
            ArgumentList arguments,
            bool newFrame,
            ref INamespace @namespace,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up an asynchronous callback by name.
        /// </summary>
        /// <param name="name">
        /// The name of the callback to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the callback that was
        /// found.
        /// </param>
        /// <param name="callback">
        /// Upon success, this receives the callback that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCallback(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICallback callback,
            ref Result error
            );

        /// <summary>
        /// Adds a callback to this interpreter.
        /// </summary>
        /// <param name="callback">
        /// The callback to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the callback, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// callback.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddCallback(
            ICallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Removes the named callback from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the callback to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveCallback(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up an executable entity by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the executable entity to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the executable entity that
        /// was found.
        /// </param>
        /// <param name="execute">
        /// Upon success, this receives the executable entity that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetIExecute(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IExecute execute,
            ref Result error
            );

        /// <summary>
        /// Looks up an executable entity by name.
        /// </summary>
        /// <param name="name">
        /// The name of the executable entity to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the executable entity that
        /// was found.
        /// </param>
        /// <param name="execute">
        /// Upon success, this receives the executable entity that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetIExecute(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IExecute execute,
            ref Result error
            );

        /// <summary>
        /// Creates a list of the executable entities matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match executable entity names, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching executable entities are
        /// found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching executable entity
        /// names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListIExecutes(
            string pattern,
            bool noCase,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds an executable entity to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the executable entity.
        /// </param>
        /// <param name="execute">
        /// The executable entity to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the executable
        /// entity, if any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// executable entity.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddIExecute(
            string name,
            IExecute execute,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Renames an executable entity.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the executable entity.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the executable entity.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the executable entity instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames an executable entity, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the executable entity.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the executable entity.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the executable entity instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden executable entity.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden executable entity.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden executable entity.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the executable entity instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameHiddenIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden executable entity, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden executable entity.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden executable entity.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the executable entity instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameHiddenIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Removes the executable entity identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the executable entity to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveIExecute(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named executable entity from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the executable entity to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveIExecute(
            string name,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Matches an executable entity by name, optionally using command name
        /// abbreviations.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags used to control how the match is performed.
        /// </param>
        /// <param name="name">
        /// The name of the executable entity to match.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, this indicates whether the specified name matched more
        /// than one executable entity.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the matched executable
        /// entity.
        /// </param>
        /// <param name="execute">
        /// Upon success, this receives the matched executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MatchIExecute(
            EngineFlags engineFlags,
            string name,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up an opaque object handle by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the object that was found.
        /// </param>
        /// <param name="object">
        /// Upon success, this receives the object that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetObject(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IObject @object,
            ref Result error
            );

        /// <summary>
        /// Looks up an opaque object handle by name.
        /// </summary>
        /// <param name="name">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="object">
        /// Upon success, this receives the object that was found.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value.
        /// </returns>
        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref IObject @object
            );

        /// <summary>
        /// Looks up an opaque object handle by name.
        /// </summary>
        /// <param name="name">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="object">
        /// Upon success, this receives the object that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref IObject @object,
            ref Result error
            );

        /// <summary>
        /// Looks up an opaque object handle by name, also returning its token.
        /// </summary>
        /// <param name="name">
        /// The name of the object to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the object that was found.
        /// </param>
        /// <param name="object">
        /// Upon success, this receives the object that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IObject @object,
            ref Result error
            );

        /// <summary>
        /// Looks up an opaque object handle by its underlying managed value.
        /// </summary>
        /// <param name="value">
        /// The managed value of the object to look up.  This parameter may be
        /// null.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the object that was found.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the object that was found.
        /// </param>
        /// <param name="object">
        /// Upon success, this receives the object that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetObject(
            object value,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IObject @object,
            ref Result error
            );

        /// <summary>
        /// Adds an opaque object handle wrapping the specified managed value
        /// to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the object, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="type">
        /// The type of the object value.  This parameter may be null.
        /// </param>
        /// <param name="objectFlags">
        /// The flags used to control how the object is added and managed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the object, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="referenceCount">
        /// The initial reference count for the object.
        /// </param>
        /// <param name="interpName">
        /// The name of the native Tcl interpreter associated with the object,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="executeArguments">
        /// The arguments to use when the object is invoked, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The managed value to wrap.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(
            string name,
            Type type,
            ObjectFlags objectFlags,
            IClientData clientData,
            int referenceCount,
#if NATIVE && TCL
            string interpName,
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
            ArgumentList executeArguments,
#endif
            object value,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds an existing opaque object handle to this interpreter.
        /// </summary>
        /// <param name="object">
        /// The opaque object handle to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the object, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(
            IObject @object,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Renames an opaque object handle.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the object.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the object.
        /// </param>
        /// <param name="ignoreAlias">
        /// Non-zero to ignore any alias associated with the object during the
        /// rename.
        /// </param>
        /// <param name="noNamespaces">
        /// Non-zero to disable namespace resolution of the object names.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if the object does not exist.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameObject(
            string oldName,
            string newName,
            bool ignoreAlias,
            bool noNamespaces,
            bool strict,
            ref Result result
            );

        /// <summary>
        /// Removes the opaque object handle identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the object to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to request that the removed object be disposed; upon
        /// return, this indicates whether the object was disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveObject(
            long token,
            IClientData clientData,
            ref bool dispose,
            ref Result result
            );

        /// <summary>
        /// Removes the opaque object handle identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the object to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to dispose the object synchronously rather than deferring
        /// its disposal.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to request that the removed object be disposed; upon
        /// return, this indicates whether the object was disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveObject(
            long token,
            IClientData clientData,
            bool synchronous,
            ref bool dispose,
            ref Result result
            );

        /// <summary>
        /// Removes the named opaque object handle from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the object to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to request that the removed object be disposed; upon
        /// return, this indicates whether the object was disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveObject(
            string name,
            IClientData clientData,
            ref bool dispose,
            ref Result result
            );

        /// <summary>
        /// Removes the named opaque object handle from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the object to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to dispose the object synchronously rather than deferring
        /// its disposal.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to request that the removed object be disposed; upon
        /// return, this indicates whether the object was disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveObject(
            string name,
            IClientData clientData,
            bool synchronous,
            ref bool dispose,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a package by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the package to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the package that was found.
        /// </param>
        /// <param name="package">
        /// Upon success, this receives the package that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPackage(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPackage package,
            ref Result error
            );

        /// <summary>
        /// Looks up a package by name.
        /// </summary>
        /// <param name="name">
        /// The name of the package to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the package that was
        /// found.
        /// </param>
        /// <param name="package">
        /// Upon success, this receives the package that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPackage(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPackage package,
            ref Result error
            );

        /// <summary>
        /// Adds a package to this interpreter.
        /// </summary>
        /// <param name="package">
        /// The package to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the package, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// package.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddPackage(
            IPackage package,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Removes the package identified by token from this interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the package to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePackage(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named package from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePackage(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a plugin by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the plugin to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the plugin that was found.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this receives the plugin that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPlugin(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPlugin plugin,
            ref Result error
            );

        /// <summary>
        /// Looks up a plugin by name.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the plugin that was found.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this receives the plugin that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPlugin(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        /// <summary>
        /// Looks up a plugin by its assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name of the plugin to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the plugin that was found.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the plugin that was found.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this receives the plugin that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPlugin(
            AssemblyName assemblyName,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        /// <summary>
        /// Looks up a plugin by its assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly of the plugin to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the plugin that was found.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the plugin that was found.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this receives the plugin that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPlugin(
            Assembly assembly,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        /// <summary>
        /// Matches a plugin by name.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to match.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this receives the matched plugin.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MatchPlugin(
            string name,
            LookupFlags lookupFlags,
            ref IPlugin plugin,
            ref Result error
            );

        /// <summary>
        /// Creates a list of the plugins matching the specified criteria.
        /// </summary>
        /// <param name="hasFlags">
        /// The plugin flags that a plugin must have to be included.
        /// </param>
        /// <param name="notHasFlags">
        /// The plugin flags that a plugin must not have to be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a plugin have all of the flags specified by
        /// <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a plugin not have all of the flags
        /// specified by <paramref name="notHasFlags" />; otherwise, not having
        /// any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match plugin names, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified entity information in the
        /// list.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching plugins are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching plugin names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListPlugins(
            PluginFlags hasFlags,
            PluginFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds a plugin to this interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the plugin, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddPlugin(
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Removes the plugin identified by token from this interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the plugin to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePlugin(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named plugin from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the plugin to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePlugin(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up the name of a command by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the command to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the command that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCommandName(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref Result error
            );

        /// <summary>
        /// Looks up a command by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the command to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the command that was found.
        /// </param>
        /// <param name="command">
        /// Upon success, this receives the command that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCommand(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref ICommand command,
            ref Result error
            );

        /// <summary>
        /// Looks up a command by name.
        /// </summary>
        /// <param name="name">
        /// The name of the command to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the command that was
        /// found.
        /// </param>
        /// <param name="command">
        /// Upon success, this receives the command that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCommand(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICommand command,
            ref Result error
            );

        /// <summary>
        /// Looks up a command belonging to the specified plugin by name.
        /// </summary>
        /// <param name="plugin">
        /// The plugin that owns the command to look up.
        /// </param>
        /// <param name="name">
        /// The name of the command to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the command that was
        /// found.
        /// </param>
        /// <param name="command">
        /// Upon success, this receives the command that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetCommandForPlugin(
            IPlugin plugin,
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICommand command,
            ref Result error
            );

        /// <summary>
        /// Creates a list of the commands matching the specified criteria.
        /// </summary>
        /// <param name="hasFlags">
        /// The command flags that a command must have to be included.
        /// </param>
        /// <param name="notHasFlags">
        /// The command flags that a command must not have to be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a command have all of the flags specified
        /// by <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a command not have all of the flags
        /// specified by <paramref name="notHasFlags" />; otherwise, not having
        /// any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match command names, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified entity information in the
        /// list.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching commands are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching command names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListCommands(
            CommandFlags hasFlags,
            CommandFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds a command backed by the specified execution callback to this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the command.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallback(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a command backed by the specified execution callback and
        /// associated command to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the command.
        /// </param>
        /// <param name="command">
        /// The command to associate with the execution callback.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallback(
            string name,
            ICommand command,
            ExecuteCallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a command backed by the specified execution callback and owned
        /// by the specified plugin to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the command.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the command, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallback(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            IPlugin plugin,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a command backed by the specified execution callback and owned
        /// by the specified plugin to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the command.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the command, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control the behavior of the command.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallback(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            IPlugin plugin,
            CommandFlags commandFlags,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a command backed by the specified execution callback, owned by
        /// the specified plugin, and exposing the specified sub-commands to
        /// this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the command.
        /// </param>
        /// <param name="callback">
        /// The delegate to invoke when the command is executed.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the command, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="subCommands">
        /// The sub-commands to associate with the command, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control the behavior of the command.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallback(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            IPlugin plugin,
            EnsembleDictionary subCommands,
            CommandFlags commandFlags,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds multiple commands backed by execution callbacks to this
        /// interpreter.
        /// </summary>
        /// <param name="collection">
        /// The collection of execution callback data used to create the
        /// commands.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the commands, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the commands, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="ignoreNull">
        /// Non-zero to silently skip any null elements in the collection.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing when the first error is encountered.
        /// </param>
        /// <param name="errorCount">
        /// Upon return, this receives the number of errors that were
        /// encountered.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddExecuteCallbacks(
            IEnumerable<IExecuteCallbackData> collection,
            IPlugin plugin,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref int errorCount,
            ref Result result
            );

#if EMIT
        /// <summary>
        /// Adds commands automatically generated from the specified typed
        /// instances to this interpreter.
        /// </summary>
        /// <param name="plugin">
        /// The plugin to associate with the commands, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the commands, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="typedInstances">
        /// The typed instances from which to generate commands.
        /// </param>
        /// <param name="mapper">
        /// The delegate mapper used to generate the commands, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to locate members to expose as commands, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control marshalling of arguments and return
        /// values, if any.  This parameter may be null.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags used to control delegate creation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="safe">
        /// Non-zero to add the commands as safe, or null to use the default.
        /// This parameter may be null.
        /// </param>
        /// <param name="count">
        /// Upon return, this receives the number of commands that were added.
        /// </param>
        /// <param name="tokens">
        /// Upon success, this receives the tokens assigned to the added
        /// commands.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddAutomaticCommands(
            IPlugin plugin,
            IClientData clientData,
            IEnumerable<TypedInstance> typedInstances,
            IDelegateMapper mapper,
            BindingFlags? bindingFlags,
            MarshalFlags? marshalFlags,
            DelegateFlags? delegateFlags,
            bool? safe,
            ref long count,
            ref LongList tokens,
            ref Result result
            );
#endif

        /// <summary>
        /// Adds a sub-command to an existing command.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the sub-command.
        /// </param>
        /// <param name="command">
        /// The command to which the sub-command is added.
        /// </param>
        /// <param name="scriptCommand">
        /// The script command to invoke for the sub-command, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="nameIndex">
        /// The index of the sub-command name within the argument list, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the sub-command,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="subCommandFlags">
        /// The flags used to control the behavior of the sub-command.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// sub-command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddSubCommand(
            string name,
            ICommand command,
            StringList scriptCommand,
            int? nameIndex,
            IClientData clientData,
            SubCommandFlags subCommandFlags,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a command to this interpreter.
        /// </summary>
        /// <param name="command">
        /// The command to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddCommand(
            ICommand command,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Renames a command.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the command.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the command.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the command instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a command, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the command.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the command.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the command instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden command.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden command.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden command.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the command instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameHiddenCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden command, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden command.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden command.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the command instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameHiddenCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Removes multiple commands backed by execution callbacks from this
        /// interpreter.
        /// </summary>
        /// <param name="collection">
        /// The collection of execution callback data identifying the commands
        /// to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="ignoreNull">
        /// Non-zero to silently skip any null elements in the collection.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing when the first error is encountered.
        /// </param>
        /// <param name="errorCount">
        /// Upon return, this receives the number of errors that were
        /// encountered.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveExecuteCallbacks(
            IEnumerable<IExecuteCallbackData> collection,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref int errorCount,
            ref Result result
            );

        /// <summary>
        /// Removes the command identified by token from this interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the command to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveCommand(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named command from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the command to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveCommand(
            string name,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes multiple commands identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="tokens">
        /// The tokens identifying the commands to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing when the first error is encountered.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to return an error if any command could not be removed.
        /// </param>
        /// <param name="names">
        /// Upon success, this receives the names of the commands that were
        /// removed.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives the list of error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode RemoveCommands(
            IEnumerable<long> tokens,
            IClientData clientData,
            bool stopOnError,
            bool failOnError,
            ref StringList names,
            ref ResultList errors
            );

        /// <summary>
        /// Removes multiple commands identified by name from this interpreter.
        /// </summary>
        /// <param name="names">
        /// The names identifying the commands to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing when the first error is encountered.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to return an error if any command could not be removed.
        /// </param>
        /// <param name="tokens">
        /// Upon success, this receives the tokens of the commands that were
        /// removed.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives the list of error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode RemoveCommands(
            IEnumerable<string> names,
            IClientData clientData,
            bool stopOnError,
            bool failOnError,
            ref LongList tokens,
            ref ResultList errors
            );

        /// <summary>
        /// Matches a command by name, optionally using command name
        /// abbreviations.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags used to control how the match is performed.
        /// </param>
        /// <param name="name">
        /// The name of the command to match.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, this indicates whether the specified name matched more
        /// than one command.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the matched command.
        /// </param>
        /// <param name="command">
        /// Upon success, this receives the matched command.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MatchCommand(
            EngineFlags engineFlags,
            string name,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref ICommand command,
            ref Result error
            );

        /// <summary>
        /// Swaps commands within this interpreter according to the specified
        /// flags.
        /// </summary>
        /// <param name="swapFlags">
        /// The flags used to control how the commands are swapped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SwapCommands(
            SwapFlags swapFlags,
            ref Result error
            );

        /// <summary>
        /// Swaps commands within this interpreter according to the specified
        /// flags.
        /// </summary>
        /// <param name="swapFlags">
        /// The flags used to control how the commands are swapped.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the names of the commands that were
        /// swapped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SwapCommands(
            SwapFlags swapFlags,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Removes a previously swapped command identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the swapped command to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="swapFlags">
        /// The flags used to control how the swapped command is removed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveSwapCommand(
            long token,
            IClientData clientData,
            SwapFlags swapFlags,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// Adds sub-commands generated from the members of the specified type
        /// to an existing command.
        /// </summary>
        /// <param name="name">
        /// The name of the command to which the sub-commands are added.
        /// </param>
        /// <param name="type">
        /// The type whose members are exposed as sub-commands.
        /// </param>
        /// <param name="object">
        /// The object instance associated with the sub-commands, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the sub-commands, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the sub-commands,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="nameCallback">
        /// The callback used to generate the name for each sub-command, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags used to control delegate creation for the sub-commands.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddSubCommands(
            string name, /* commandName */
            Type type,
            object @object,
            IPlugin plugin,
            IClientData clientData,
            NewDelegateNameCallback nameCallback,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds sub-commands generated from the specified delegates to an
        /// existing command.
        /// </summary>
        /// <param name="name">
        /// The name of the command to which the sub-commands are added.
        /// </param>
        /// <param name="delegates">
        /// The delegates to expose as sub-commands.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the sub-commands, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the sub-commands,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags used to control delegate creation for the sub-commands.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// command.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddSubCommands(
            string name, /* commandName */
            DelegateDictionary delegates,
            IPlugin plugin,
            IClientData clientData,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a policy by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the policy to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the policy that was found.
        /// </param>
        /// <param name="policy">
        /// Upon success, this receives the policy that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPolicy(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPolicy policy,
            ref Result error
            );

        /// <summary>
        /// Looks up a policy by name.
        /// </summary>
        /// <param name="name">
        /// The name of the policy to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the policy that was found.
        /// </param>
        /// <param name="policy">
        /// Upon success, this receives the policy that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetPolicy(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPolicy policy,
            ref Result error
            );

        /// <summary>
        /// Adds a policy backed by the specified execution callback to this
        /// interpreter.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke when the policy is evaluated.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the policy, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the policy, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added policy.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddPolicy(
            ExecuteCallback callback,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a policy to this interpreter.
        /// </summary>
        /// <param name="policy">
        /// The policy to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the policy, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added policy.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddPolicy(
            IPolicy policy,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a script-based policy to this interpreter.
        /// </summary>
        /// <param name="scriptPolicy">
        /// The script policy to add.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the policy, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the policy, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added policy.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddScriptPolicy(
            IScriptPolicy scriptPolicy,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Removes the policy identified by token from this interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the policy to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePolicy(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named policy from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the policy to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemovePolicy(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a variable trace by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the trace to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the trace that was found.
        /// </param>
        /// <param name="trace">
        /// Upon success, this receives the trace that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetTrace(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref ITrace trace,
            ref Result error
            );

        /// <summary>
        /// Looks up a variable trace by name.
        /// </summary>
        /// <param name="name">
        /// The name of the trace to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the trace that was found.
        /// </param>
        /// <param name="trace">
        /// Upon success, this receives the trace that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetTrace(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ITrace trace,
            ref Result error
            );

        /// <summary>
        /// Adds a variable trace backed by the specified callback to this
        /// interpreter.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke when the trace is triggered.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the trace, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the trace, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added trace.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddTrace(
            TraceCallback callback,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a variable trace to this interpreter.
        /// </summary>
        /// <param name="trace">
        /// The trace to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the trace, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added trace.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddTrace(
            ITrace trace,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Removes the variable trace identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the trace to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveTrace(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named variable trace from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the trace to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveTrace(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a command alias by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the alias to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the alias that was found.
        /// </param>
        /// <param name="alias">
        /// Upon success, this receives the alias that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetAlias(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IAlias alias,
            ref Result error
            );

        /// <summary>
        /// Looks up a command alias by name.
        /// </summary>
        /// <param name="name">
        /// The name of the alias to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the alias that was found.
        /// </param>
        /// <param name="alias">
        /// Upon success, this receives the alias that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetAlias(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IAlias alias,
            ref Result error
            );

        //
        // NOTE: Currently, "aliases" created by this method can only be
        //       removed via RemoveCommand (either by name or by token).
        //
        // TODO: Change these to use the IInterpreter type.
        //
        /// <summary>
        /// Adds a command alias to this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the alias.
        /// </param>
        /// <param name="commandFlags">
        /// The flags used to control the behavior of the alias command.
        /// </param>
        /// <param name="aliasFlags">
        /// The flags used to control the behavior of the alias.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the alias, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="targetInterpreter">
        /// The interpreter in which the alias target is executed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="target">
        /// The executable entity that the alias invokes.
        /// </param>
        /// <param name="arguments">
        /// The arguments to prepend when the alias is invoked, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="options">
        /// The options associated with the alias, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first argument to pass through to the alias
        /// target.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the command created for
        /// the alias.
        /// </param>
        /// <param name="alias">
        /// Upon success, this receives the alias that was created.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddAlias(
            string name,
            CommandFlags commandFlags,
            AliasFlags aliasFlags,
            IClientData clientData,
            Interpreter targetInterpreter,
            IExecute target,
            ArgumentList arguments,
            OptionDictionary options,
            int startIndex,
            ref long /* command */ token,
            ref IAlias alias,
            ref Result result
            );

        /// <summary>
        /// Adds an existing command alias to this interpreter.
        /// </summary>
        /// <param name="alias">
        /// The alias to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the alias, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added alias.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddAlias(
            IAlias alias,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a math function by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the function to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the function that was
        /// found.
        /// </param>
        /// <param name="function">
        /// Upon success, this receives the function that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetFunction(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IFunction function,
            ref Result error
            );

        /// <summary>
        /// Looks up a math function by name.
        /// </summary>
        /// <param name="name">
        /// The name of the function to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the function that was
        /// found.
        /// </param>
        /// <param name="function">
        /// Upon success, this receives the function that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetFunction(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IFunction function,
            ref Result error
            );

        /// <summary>
        /// Creates a list of the math functions matching the specified
        /// criteria.
        /// </summary>
        /// <param name="hasFlags">
        /// The function flags that a function must have to be included.
        /// </param>
        /// <param name="notHasFlags">
        /// The function flags that a function must not have to be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a function have all of the flags specified
        /// by <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a function not have all of the flags
        /// specified by <paramref name="notHasFlags" />; otherwise, not having
        /// any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match function names, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified entity information in the
        /// list.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching functions are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching function names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListFunctions(
            FunctionFlags hasFlags,
            FunctionFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds a math function to this interpreter.
        /// </summary>
        /// <param name="type">
        /// The type that implements the function.
        /// </param>
        /// <param name="name">
        /// The name to assign to the function.
        /// </param>
        /// <param name="arguments">
        /// The number of arguments the function accepts.
        /// </param>
        /// <param name="types">
        /// The list of argument types for the function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="functionFlags">
        /// The flags used to control the behavior of the function.
        /// </param>
        /// <param name="plugin">
        /// The plugin to associate with the function, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the function, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if a function with the same name
        /// already exists.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// function.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddFunction(
            Type type,
            string name,
            int arguments,
            TypeList types,
            FunctionFlags functionFlags,
            IPlugin plugin,
            IClientData clientData,
            bool strict,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a math function to this interpreter.
        /// </summary>
        /// <param name="function">
        /// The function to add.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the function, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added
        /// function.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddFunction(
            IFunction function,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Renames a math function.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the function.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the function.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the function instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameFunction(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Removes the math function identified by token from this
        /// interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the function to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveFunction(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named math function from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the function to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveFunction(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a procedure by token.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the procedure to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="name">
        /// Upon success, this receives the name of the procedure that was
        /// found.
        /// </param>
        /// <param name="procedure">
        /// Upon success, this receives the procedure that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetProcedure(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IProcedure procedure,
            ref Result error
            );

        /// <summary>
        /// Looks up a procedure by name.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the procedure that was
        /// found.
        /// </param>
        /// <param name="procedure">
        /// Upon success, this receives the procedure that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetProcedure(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IProcedure procedure,
            ref Result error
            );

        /// <summary>
        /// Creates a list of the procedures matching the specified criteria.
        /// </summary>
        /// <param name="hasFlags">
        /// The procedure flags that a procedure must have to be included.
        /// </param>
        /// <param name="notHasFlags">
        /// The procedure flags that a procedure must not have to be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that a procedure have all of the flags specified
        /// by <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that a procedure not have all of the flags
        /// specified by <paramref name="notHasFlags" />; otherwise, not having
        /// any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match procedure names, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified entity information in the
        /// list.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching procedures are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching procedure names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListProcedures(
            ProcedureFlags hasFlags,
            ProcedureFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds a new procedure to this interpreter, or updates the existing
        /// procedure with the same name.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the procedure.
        /// </param>
        /// <param name="procedureFlags">
        /// The flags used to control the behavior of the procedure.
        /// </param>
        /// <param name="arguments">
        /// The formal arguments of the procedure, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="namedArguments">
        /// The named formal arguments of the procedure, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="overwriteArguments">
        /// The arguments used to overwrite the existing procedure arguments,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="cleanArguments">
        /// The cleaned arguments of the procedure, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="body">
        /// The body script of the procedure.
        /// </param>
        /// <param name="location">
        /// The script location associated with the procedure, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the procedure, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added or
        /// updated procedure.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddOrUpdateProcedure(
            string name,
            ProcedureFlags procedureFlags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            ArgumentList overwriteArguments,
            ArgumentList cleanArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Adds a new procedure to this interpreter, or updates the existing
        /// procedure with the same name.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to add or update.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the procedure, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token assigned to the added or
        /// updated procedure.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddOrUpdateProcedure(
            IProcedure procedure,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        /// <summary>
        /// Renames a procedure.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the procedure.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the procedure.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the procedure instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a procedure, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the procedure.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the procedure.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the procedure instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden procedure.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden procedure.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden procedure.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the procedure instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RenameHiddenProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Renames a hidden procedure, if it exists.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the hidden procedure.
        /// </param>
        /// <param name="newName">
        /// The new name to assign to the hidden procedure.
        /// </param>
        /// <param name="delete">
        /// Non-zero to delete the procedure instead of renaming it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode MaybeRenameHiddenProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        /// <summary>
        /// Removes the procedure identified by token from this interpreter.
        /// </summary>
        /// <param name="token">
        /// The unique token identifying the procedure to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveProcedure(
            long token,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Removes the named procedure from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to remove.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the operation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RemoveProcedure(
            string name,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Matches a procedure by name, optionally using command name
        /// abbreviations.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags used to control how the match is performed.
        /// </param>
        /// <param name="name">
        /// The name of the procedure to match.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, this indicates whether the specified name matched more
        /// than one procedure.
        /// </param>
        /// <param name="token">
        /// Upon success, this receives the token of the matched procedure.
        /// </param>
        /// <param name="procedure">
        /// Upon success, this receives the matched procedure.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MatchProcedure(
            EngineFlags engineFlags,
            string name,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IProcedure procedure,
            ref Result error
            );

        /// <summary>
        /// Enables or disables fast execution for the named procedure.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to modify.
        /// </param>
        /// <param name="fast">
        /// Non-zero to enable fast execution; otherwise, disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeProcedureFast( /* EXPERIMENTAL */
            string name,
            bool fast,
            ref Result error
            );

        /// <summary>
        /// Enables or disables atomic execution for the named procedure.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to modify.
        /// </param>
        /// <param name="atomic">
        /// Non-zero to enable atomic execution; otherwise, disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeProcedureAtomic( /* EXPERIMENTAL */
            string name,
            bool atomic,
            ref Result error
            );

        /// <summary>
        /// Enables or disables inline execution for the named procedure.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to modify.
        /// </param>
        /// <param name="inline">
        /// Non-zero to enable inline execution; otherwise, disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeProcedureInline(
            string name,
            bool inline,
            ref Result error
            );

#if ARGUMENT_CACHE || PARSE_CACHE
        /// <summary>
        /// Enables or disables caching for the named procedure.
        /// </summary>
        /// <param name="name">
        /// The name of the procedure to modify.
        /// </param>
        /// <param name="nonCaching">
        /// Non-zero to disable caching; otherwise, enable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeProcedureNonCaching( /* EXPERIMENTAL */
            string name,
            bool nonCaching,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a list of the expression operators matching the specified
        /// criteria.
        /// </summary>
        /// <param name="hasFlags">
        /// The operator flags that an operator must have to be included.
        /// </param>
        /// <param name="notHasFlags">
        /// The operator flags that an operator must not have to be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that an operator have all of the flags specified
        /// by <paramref name="hasFlags" />; otherwise, having any of them is
        /// sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that an operator not have all of the flags
        /// specified by <paramref name="notHasFlags" />; otherwise, not having
        /// any of them is sufficient.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match operator names, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified entity information in the
        /// list.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching operators are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching operator names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListOperators(
            OperatorFlags hasFlags,
            OperatorFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            bool full,
            bool strict,
            ref StringList list,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified stream is associated with the
        /// named channel.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to check.
        /// </param>
        /// <param name="channelType">
        /// The type of the channel to check.
        /// </param>
        /// <param name="stream">
        /// The stream to check for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified stream is associated with the named channel;
        /// otherwise, false.
        /// </returns>
        bool IsStreamForChannel(
            string name,
            ChannelType channelType,
            Stream stream
            );

        /// <summary>
        /// Creates a list of the channels matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match channel names, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if no matching channels are found.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of matching channel names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListChannels(
            string pattern,
            bool noCase,
            bool strict,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Adds an I/O channel wrapping the specified stream to this
        /// interpreter.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the channel.
        /// </param>
        /// <param name="channelType">
        /// The type of the channel to add.
        /// </param>
        /// <param name="stream">
        /// The underlying stream for the channel.
        /// </param>
        /// <param name="options">
        /// The options associated with the channel, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="streamFlags">
        /// The flags used to control the behavior of the channel stream.
        /// </param>
        /// <param name="inTranslation">
        /// The end-of-line translation to use for input.
        /// </param>
        /// <param name="outTranslation">
        /// The end-of-line translation to use for output.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use for the channel, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="nullEncoding">
        /// Non-zero to treat the channel as having no encoding (binary).
        /// </param>
        /// <param name="appendMode">
        /// Non-zero to open the channel in append mode.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero to automatically flush the channel after each write.
        /// </param>
        /// <param name="rawEndOfStream">
        /// Non-zero to use raw end-of-stream handling for the channel.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the channel, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddChannel(
            string name,
            ChannelType channelType,
            Stream stream,
            OptionDictionary options,
            StreamFlags streamFlags,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation,
            Encoding encoding,
            bool nullEncoding,
            bool appendMode,
            bool autoFlush,
            bool rawEndOfStream,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Removes the named channel from this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to remove.
        /// </param>
        /// <param name="channelType">
        /// The type of the channel to remove.
        /// </param>
        /// <param name="flush">
        /// Non-zero to flush the channel before removing it.
        /// </param>
        /// <param name="close">
        /// Non-zero to close the underlying stream when the channel is
        /// removed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return an error if the channel does not exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode RemoveChannel(
            string name,
            ChannelType channelType,
            bool flush,
            bool close,
            bool strict,
            ref Result error
            );

        /// <summary>
        /// Sets the encoding used by the named channel.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to modify.
        /// </param>
        /// <param name="channelType">
        /// The type of the channel to modify.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use for the channel, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetChannelEncoding(
            string name,
            ChannelType channelType,
            Encoding encoding,
            ref Result error
            );

        /// <summary>
        /// Sets the end-of-line translation used by the named channel.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to modify.
        /// </param>
        /// <param name="channelType">
        /// The type of the channel to modify.
        /// </param>
        /// <param name="inTranslation">
        /// The end-of-line translation to use for input.
        /// </param>
        /// <param name="outTranslation">
        /// The end-of-line translation to use for output.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetChannelTranslation(
            string name,
            ChannelType channelType,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation,
            ref Result error
            );

        /// <summary>
        /// Gets the virtual output buffer associated with the named channel.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to query.
        /// </param>
        /// <param name="copy">
        /// Non-zero to return a copy of the virtual output buffer rather than
        /// the buffer itself.
        /// </param>
        /// <param name="builder">
        /// Upon success, this receives the virtual output buffer for the
        /// channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetChannelVirtualOutput(
            string name,
            bool copy,
            ref StringBuilder builder,
            ref Result error
            );

        /// <summary>
        /// Enables or disables virtual output buffering for the named channel.
        /// </summary>
        /// <param name="name">
        /// The name of the channel to modify.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable virtual output buffering; otherwise, disable it.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SetChannelVirtualOutput(
            string name,
            bool enable,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a scope call frame by name.
        /// </summary>
        /// <param name="name">
        /// The name of the scope to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="frame">
        /// Upon success, this receives the call frame for the scope.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetScope(
            string name,
            LookupFlags lookupFlags,
            ref ICallFrame frame,
            ref Result error
            );

        /// <summary>
        /// Locks the named scope to prevent it from being modified.
        /// </summary>
        /// <param name="name">
        /// The name of the scope to lock.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode LockScope(
            string name,
            ref Result error
            );

        /// <summary>
        /// Unlocks the named scope.
        /// </summary>
        /// <param name="name">
        /// The name of the scope to unlock.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnlockScope(
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Looks up a database connection by name.
        /// </summary>
        /// <param name="name">
        /// The name of the database connection to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="connection">
        /// Upon success, this receives the database connection that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetDbConnection(
            string name,
            LookupFlags lookupFlags,
            ref IDbConnection connection,
            ref Result error
            );

        /// <summary>
        /// Looks up a database transaction by name.
        /// </summary>
        /// <param name="name">
        /// The name of the database transaction to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="transaction">
        /// Upon success, this receives the database transaction that was
        /// found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetDbTransaction(
            string name,
            LookupFlags lookupFlags,
            ref IDbTransaction transaction,
            ref Result error
            );

        /// <summary>
        /// Looks up a database connection by name, including those not
        /// directly managed by this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the database connection to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="connection">
        /// Upon success, this receives the database connection that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetAnyDbConnection(
            string name,
            LookupFlags lookupFlags,
            ref IDbConnection connection,
            ref Result error
            );

        /// <summary>
        /// Looks up a database transaction by name, including those not
        /// directly managed by this interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the database transaction to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="transaction">
        /// Upon success, this receives the database transaction that was
        /// found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetAnyDbTransaction(
            string name,
            LookupFlags lookupFlags,
            ref IDbTransaction transaction,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Looks up a text encoding by name.
        /// </summary>
        /// <param name="name">
        /// The name of the encoding to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="encoding">
        /// Upon success, this receives the encoding that was found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetEncoding(
            string name,
            LookupFlags lookupFlags,
            ref Encoding encoding,
            ref Result error
            );

        /// <summary>
        /// Looks up a text encoding by name, falling back to a default
        /// encoding if the named encoding cannot be found.
        /// </summary>
        /// <param name="name">
        /// The name of the encoding to look up.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the lookup is performed.
        /// </param>
        /// <param name="encoding">
        /// Upon success, this receives the encoding that was found, or the
        /// default encoding if the named encoding could not be found.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetEncodingOrDefault(
            string name,
            LookupFlags lookupFlags,
            ref Encoding encoding,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ENTITY METRICS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the usage metrics for the entities of the specified kind.
        /// </summary>
        /// <param name="kind">
        /// The kind of entity for which to retrieve usage metrics.
        /// </param>
        /// <param name="type">
        /// The type of usage metric to retrieve.
        /// </param>
        /// <param name="dictionary">
        /// Upon success, this receives the usage metrics keyed by entity name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details stored in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetUsageData(
            IdentifierKind kind,
            UsageType type,
            ref StringDictionary dictionary,
            ref Result error
            );
    }
}
