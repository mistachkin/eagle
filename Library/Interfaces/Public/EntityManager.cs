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

namespace Eagle._Interfaces.Public
{
    [ObjectId("c6db1693-7821-4859-b5a9-d37406aefec9")]
    public interface IEntityManager
    {
        ///////////////////////////////////////////////////////////////////////
        // ENTITY CHECKING
        ///////////////////////////////////////////////////////////////////////

        bool HasAliases(ref Result error);
        bool HasCallbacks(ref Result error);
        bool HasChannels(ref Result error);
        bool HasCommands(ref Result error);
        bool HasFunctions(ref Result error);
        bool HasIExecutes(ref Result error);
        bool HasObjects(ref Result error);
        bool HasOperators(ref Result error);
        bool HasPackageIndexes(ref Result error);
        bool HasPackages(ref Result error);
        bool HasPlugins(ref Result error);
        bool HasPolicies(ref Result error);
        bool HasProcedures(ref Result error);
        bool HasScopes(ref Result error);
        bool HasTraces(ref Result error);

#if DATA
        bool HasDbConnections(ref Result error);
        bool HasDbTransactions(ref Result error);
#endif

#if EMIT && NATIVE && LIBRARY
        bool HasDelegates(ref Result error);
        bool HasModules(ref Result error);
#endif

        ///////////////////////////////////////////////////////////////////////
        // ENTITY DETECTION
        ///////////////////////////////////////////////////////////////////////

        ReturnCode DoesAliasExist(string name);
        ReturnCode DoesCallbackExist(string name);
        ReturnCode DoesChannelExist(string name);
        ReturnCode DoesCommandExist(string name);
        ReturnCode DoesFunctionExist(string name);
        ReturnCode DoesIExecuteExist(string name);
        ReturnCode DoesObjectExist(string name);
        ReturnCode DoesOperatorExist(string name);
        ReturnCode DoesPackageExist(string name);
        ReturnCode DoesPluginExist(string name);
        ReturnCode DoesPolicyExist(string name);
        ReturnCode DoesProcedureExist(string name);
        ReturnCode DoesScopeExist(string name);
        ReturnCode DoesTraceExist(string name);

#if EMIT && NATIVE && LIBRARY
        ReturnCode DoesDelegateExist(string name);
        ReturnCode DoesModuleExist(string name);
#endif

        ///////////////////////////////////////////////////////////////////////
        // ENTITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetIdentifier(
            IdentifierKind kind,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref IIdentifier identifier,
            ref Result error
            );

        ReturnCode GatherIdentifiers(
            IdentifierKind kind,
            RuleType ruleType,
            MatchMode mode,
            bool stopOnError,
            ref IRuleSet ruleSet,
            ref ResultList errors
            );

        ReturnCode VerifyIdentifiers(
            IdentifierKind kind,
            MatchMode mode,
            IRuleSet ruleSet,
            ref int unverified,
            ref ResultList errors
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode LookupNamespace(
            string name,
            bool absolute,
            ref INamespace @namespace,
            ref Result error
            );

        ReturnCode CreateNamespace(
            INamespaceData namespaceData,
            ArgumentList arguments,
            bool newFrame,
            ref INamespace @namespace,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetCallback(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICallback callback,
            ref Result error
            );

        ReturnCode AddCallback(
            ICallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemoveCallback(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetIExecute(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IExecute execute,
            ref Result error
            );

        ReturnCode GetIExecute(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IExecute execute,
            ref Result error
            );

        ReturnCode ListIExecutes(
            string pattern,
            bool noCase,
            bool strict,
            ref StringList list,
            ref Result error
            );

        ReturnCode AddIExecute(
            string name,
            IExecute execute,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RenameIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RenameHiddenIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameHiddenIExecute(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RemoveIExecute(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveIExecute(
            string name,
            IClientData clientData,
            ref Result result
            );

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

        ReturnCode GetObject(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IObject @object,
            ref Result error
            );

        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref IObject @object
            );

        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref IObject @object,
            ref Result error
            );

        ReturnCode GetObject(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IObject @object,
            ref Result error
            );

        ReturnCode GetObject(
            object value,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IObject @object,
            ref Result error
            );

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

        ReturnCode AddObject(
            IObject @object,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RenameObject(
            string oldName,
            string newName,
            bool ignoreAlias,
            bool noNamespaces,
            bool strict,
            ref Result result
            );

        ReturnCode RemoveObject(
            long token,
            IClientData clientData,
            ref bool dispose,
            ref Result result
            );

        ReturnCode RemoveObject(
            long token,
            IClientData clientData,
            bool synchronous,
            ref bool dispose,
            ref Result result
            );

        ReturnCode RemoveObject(
            string name,
            IClientData clientData,
            ref bool dispose,
            ref Result result
            );

        ReturnCode RemoveObject(
            string name,
            IClientData clientData,
            bool synchronous,
            ref bool dispose,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetPackage(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPackage package,
            ref Result error
            );

        ReturnCode GetPackage(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPackage package,
            ref Result error
            );

        ReturnCode AddPackage(
            IPackage package,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemovePackage(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemovePackage(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetPlugin(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPlugin plugin,
            ref Result error
            );

        ReturnCode GetPlugin(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        ReturnCode GetPlugin(
            AssemblyName assemblyName,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        ReturnCode GetPlugin(
            Assembly assembly,
            LookupFlags lookupFlags,
            ref string name,
            ref long token,
            ref IPlugin plugin,
            ref Result error
            );

        ReturnCode MatchPlugin(
            string name,
            LookupFlags lookupFlags,
            ref IPlugin plugin,
            ref Result error
            );

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

        ReturnCode AddPlugin(
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemovePlugin(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemovePlugin(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetCommandName(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref Result error
            );

        ReturnCode GetCommand(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref ICommand command,
            ref Result error
            );

        ReturnCode GetCommand(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICommand command,
            ref Result error
            );

        ReturnCode GetCommandForPlugin(
            IPlugin plugin,
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ICommand command,
            ref Result error
            );

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

        ReturnCode AddExecuteCallback(
            string name,
            ExecuteCallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode AddExecuteCallback(
            string name,
            ICommand command,
            ExecuteCallback callback,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode AddExecuteCallbacks(
            IEnumerable<IExecuteCallbackData> collection,
            IPlugin plugin,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref int errorCount,
            ref Result result
            );

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

        ReturnCode AddCommand(
            ICommand command,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RenameCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RenameHiddenCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameHiddenCommand(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RemoveExecuteCallbacks(
            IEnumerable<IExecuteCallbackData> collection,
            IClientData clientData,
            bool ignoreNull,
            bool stopOnError,
            ref int errorCount,
            ref Result result
            );

        ReturnCode RemoveCommand(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveCommand(
            string name,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveCommands(
            IEnumerable<long> tokens,
            IClientData clientData,
            bool stopOnError,
            bool failOnError,
            ref StringList names,
            ref ResultList errors
            );

        ReturnCode RemoveCommands(
            IEnumerable<string> names,
            IClientData clientData,
            bool stopOnError,
            bool failOnError,
            ref LongList tokens,
            ref ResultList errors
            );

        ReturnCode MatchCommand(
            EngineFlags engineFlags,
            string name,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref ICommand command,
            ref Result error
            );

        ReturnCode SwapCommands(
            ref StringList list,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

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

        ReturnCode AddSubCommands(
            string name, /* commandName */
            DelegateDictionary delegates,
            IPlugin plugin,
            IClientData clientData,
            DelegateFlags delegateFlags,
            ref long token,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetPolicy(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IPolicy policy,
            ref Result error
            );

        ReturnCode GetPolicy(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IPolicy policy,
            ref Result error
            );

        ReturnCode AddPolicy(
            ExecuteCallback callback,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode AddPolicy(
            IPolicy policy,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode AddScriptPolicy(
            IScriptPolicy scriptPolicy,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemovePolicy(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemovePolicy(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetTrace(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref ITrace trace,
            ref Result error
            );

        ReturnCode GetTrace(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref ITrace trace,
            ref Result error
            );

        ReturnCode AddTrace(
            TraceCallback callback,
            IPlugin plugin,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode AddTrace(
            ITrace trace,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemoveTrace(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveTrace(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetAlias(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IAlias alias,
            ref Result error
            );

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

        ReturnCode AddAlias(
            IAlias alias,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetFunction(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IFunction function,
            ref Result error
            );

        ReturnCode GetFunction(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IFunction function,
            ref Result error
            );

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

        ReturnCode AddFunction(
            IFunction function,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RemoveFunction(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveFunction(
            string name,
            IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetProcedure(
            long token,
            LookupFlags lookupFlags,
            ref string name,
            ref IProcedure procedure,
            ref Result error
            );

        ReturnCode GetProcedure(
            string name,
            LookupFlags lookupFlags,
            ref long token,
            ref IProcedure procedure,
            ref Result error
            );

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

        ReturnCode AddOrUpdateProcedure(
            string name,
            ProcedureFlags procedureFlags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            ref long token,
            ref Result result
            );

        ReturnCode RenameProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RenameHiddenProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode MaybeRenameHiddenProcedure(
            string oldName,
            string newName,
            bool delete,
            ref Result result
            );

        ReturnCode RemoveProcedure(
            long token,
            IClientData clientData,
            ref Result result
            );

        ReturnCode RemoveProcedure(
            string name,
            IClientData clientData,
            ref Result result
            );

        ReturnCode MatchProcedure(
            EngineFlags engineFlags,
            string name,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IProcedure procedure,
            ref Result error
            );

        ReturnCode MakeProcedureFast( /* EXPERIMENTAL */
            string name,
            bool fast,
            ref Result error
            );

        ReturnCode MakeProcedureAtomic( /* EXPERIMENTAL */
            string name,
            bool atomic,
            ref Result error
            );

#if ARGUMENT_CACHE || PARSE_CACHE
        ReturnCode MakeProcedureNonCaching( /* EXPERIMENTAL */
            string name,
            bool nonCaching,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

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

        bool IsStreamForChannel(
            string name,
            ChannelType channelType,
            Stream stream
            );

        ReturnCode ListChannels(
            string pattern,
            bool noCase,
            bool strict,
            ref StringList list,
            ref Result error
            );

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

        ReturnCode RemoveChannel(
            string name,
            ChannelType channelType,
            bool flush,
            bool close,
            bool strict,
            ref Result error
            );

        ReturnCode SetChannelEncoding(
            string name,
            ChannelType channelType,
            Encoding encoding,
            ref Result error
            );

        ReturnCode SetChannelTranslation(
            string name,
            ChannelType channelType,
            StreamTranslation inTranslation,
            StreamTranslation outTranslation,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetScope(
            string name,
            LookupFlags lookupFlags,
            ref ICallFrame frame,
            ref Result error
            );

        ReturnCode LockScope(
            string name,
            ref Result error
            );

        ReturnCode UnlockScope(
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

#if DATA
        ReturnCode GetDbConnection(
            string name,
            LookupFlags lookupFlags,
            ref IDbConnection connection,
            ref Result error
            );

        ReturnCode GetDbTransaction(
            string name,
            LookupFlags lookupFlags,
            ref IDbTransaction transaction,
            ref Result error
            );

        ReturnCode GetAnyDbConnection(
            string name,
            LookupFlags lookupFlags,
            ref IDbConnection connection,
            ref Result error
            );

        ReturnCode GetAnyDbTransaction(
            string name,
            LookupFlags lookupFlags,
            ref IDbTransaction transaction,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetEncoding(
            string name,
            LookupFlags lookupFlags,
            ref Encoding encoding,
            ref Result error
            );

        ReturnCode GetEncodingOrDefault(
            string name,
            LookupFlags lookupFlags,
            ref Encoding encoding,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ENTITY METRICS
        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetUsageData(
            IdentifierKind kind,
            UsageType type,
            ref StringDictionary dictionary,
            ref Result error
            );
    }
}
