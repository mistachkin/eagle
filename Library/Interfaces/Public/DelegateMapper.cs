/*
 * DelegateMapper.cs --
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
using Eagle._Containers.Public;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the component that maps managed
    /// methods to delegates so they can be invoked from within an
    /// interpreter.  It extends <see cref="IDisposable" /> with members to
    /// count, clear, load, look up, and enumerate the mapped delegates.
    /// </summary>
    [ObjectId("0e9836bb-d62a-48f4-b1c8-fb0a639d9cf6")]
    public interface IDelegateMapper : IDisposable
    {
        /// <summary>
        /// Counts the entries currently held by this mapper.
        /// </summary>
        /// <param name="delegatesOnly">
        /// Non-zero to count only the entries that have an associated
        /// delegate.
        /// </param>
        /// <param name="count">
        /// Upon success, this will contain the number of matching entries.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Count(
            bool delegatesOnly,
            ref int count,
            ref Result error
        );

        /// <summary>
        /// Removes entries currently held by this mapper.
        /// </summary>
        /// <param name="delegatesOnly">
        /// Non-zero to remove only the entries that have an associated
        /// delegate.
        /// </param>
        /// <param name="count">
        /// Upon success, this will contain the number of entries that were
        /// removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Clear(
            bool delegatesOnly,
            ref int count,
            ref Result error
        );

        /// <summary>
        /// Loads the methods of the specified type into this mapper, creating
        /// delegate entries for them.
        /// </summary>
        /// <param name="objectType">
        /// The type whose methods are to be loaded.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to select the methods to load, or null to
        /// use the default binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags used when creating the delegates, or null to
        /// use the default marshalling flags.
        /// </param>
        /// <param name="delegateFlags">
        /// The delegate flags used when creating the delegates, or null to
        /// use the default delegate flags.
        /// </param>
        /// <param name="clear">
        /// Non-zero to remove any existing entries before loading.
        /// </param>
        /// <param name="count">
        /// Upon success, this will contain the number of entries that were
        /// loaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Load(
            Type objectType,
            BindingFlags? bindingFlags,
            MarshalFlags? marshalFlags,
            DelegateFlags? delegateFlags,
            bool clear,
            ref int count,
            ref Result error
        );

        /// <summary>
        /// Creates an ensemble of sub-commands corresponding to the loaded
        /// methods of the specified type.
        /// </summary>
        /// <param name="objectType">
        /// The type whose methods are used to build the ensemble.
        /// </param>
        /// <param name="parameterCount">
        /// The number of parameters used to select the methods to include.
        /// </param>
        /// <returns>
        /// The created ensemble of sub-commands, or null if it could not be
        /// created.
        /// </returns>
        EnsembleDictionary CreateEnsemble(
            Type objectType,
            int parameterCount
        );

        /// <summary>
        /// Looks up the delegate entries that match the specified type, method
        /// name, and parameter count.
        /// </summary>
        /// <param name="objectType">
        /// The type whose method is being looked up.
        /// </param>
        /// <param name="methodName">
        /// The name of the method to look up.
        /// </param>
        /// <param name="parameterCount">
        /// The number of parameters used to select the matching methods.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching entries to return, or null for no
        /// limit.
        /// </param>
        /// <param name="index">
        /// The index of a single matching entry to return, or null to return
        /// all matching entries.
        /// </param>
        /// <param name="delegates">
        /// Upon success, this will contain the matching delegate entries.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Lookup(
            Type objectType,
            string methodName,
            int parameterCount,
            int? limit,
            int? index,
            ref DelegateList delegates,
            ref Result error
        );

        /// <summary>
        /// Builds a dictionary of sub-commands for the loaded methods of the
        /// specified type that match the supplied criteria.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="objectType">
        /// The type whose methods are used to build the sub-commands.
        /// </param>
        /// <param name="methodName">
        /// The method name pattern used to select the methods, or null to
        /// select all methods.
        /// </param>
        /// <param name="parameterCount">
        /// The number of parameters used to select the methods, or null to
        /// ignore the parameter count.
        /// </param>
        /// <param name="mode">
        /// The <see cref="MatchMode" /> used when matching the method name
        /// pattern.
        /// </param>
        /// <param name="marshalFlags">
        /// The marshalling flags used when processing the methods.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case when matching the method name pattern.
        /// </param>
        /// <param name="safe">
        /// Non-zero to include only methods that are considered safe, false
        /// to exclude them, or null to ignore safety.
        /// </param>
        /// <param name="subCommands">
        /// Upon success, this will contain the resulting dictionary of
        /// sub-commands.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ToList(
            Interpreter interpreter,
            Type objectType,
            string methodName,
            int? parameterCount,
            MatchMode mode,
            MarshalFlags marshalFlags,
            bool noCase,
            bool? safe,
            ref EnsembleDictionary subCommands,
            ref Result error
        );
    }
}
