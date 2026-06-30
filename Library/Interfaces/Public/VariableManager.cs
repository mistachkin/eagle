/*
 * VariableManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the variables of
    /// an interpreter.  It provides methods for checking, adding, querying,
    /// setting, resetting, linking, listing, unsetting, and synchronizing
    /// access to variables, both individually and in bulk.
    /// </summary>
    [ObjectId("4ce3748a-1099-4c93-9fc6-c25f1e543737")]
    public interface IVariableManager
    {
        ///////////////////////////////////////////////////////////////////////
        // VARIABLE CHECKING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the named variable exists.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to check for.  This parameter should not
        /// be null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the variable exists; otherwise, a
        /// non-Ok value.
        /// </returns>
        ReturnCode DoesVariableExist(
            VariableFlags flags,
            string name
            );

        /// <summary>
        /// Determines whether the named variable exists, returning any error
        /// encountered.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to check for.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the variable exists; otherwise, a
        /// non-Ok value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode DoesVariableExist(
            VariableFlags flags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE PERFORMANCE
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables fast access for the named variable, which can
        /// bypass certain checks in order to improve performance.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to modify.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="fast">
        /// Non-zero to enable fast access; otherwise, to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode MakeVariableFast(
            string name,
            bool fast,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE MANAGEMENT (SINGLE)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new variable with the specified name and traces.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is created.
        /// </param>
        /// <param name="name">
        /// The name of the variable to add.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="traces">
        /// The list of traces to associate with the new variable.  This
        /// parameter may be null.
        /// </param>
        /// <param name="errorOnExist">
        /// Non-zero to return an error if a variable with the specified name
        /// already exists.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddVariable(
            VariableFlags flags,
            string name,
            TraceList traces,
            bool errorOnExist,
            ref Result error
            );

        /// <summary>
        /// Gets the value of the named variable.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to query.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableValue(
            string name,
            ref Result value,
            ref Result error
            );

        /// <summary>
        /// Gets the value of the named variable, using the specified variable
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to query.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableValue(
            VariableFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        /// <summary>
        /// Resets the named variable to its initial, empty state.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to reset.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ResetVariable(
            VariableFlags flags,
            string name,
            ref Result error
            );

        /// <summary>
        /// Sets the named variable to wrap the specified enumerable
        /// collection.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved and set.
        /// </param>
        /// <param name="name">
        /// The name of the variable to set.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="collection">
        /// The enumerable collection to associate with the variable.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoReset">
        /// Non-zero to automatically reset the enumerator when it has been
        /// exhausted.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableEnumerable(
            VariableFlags flags,
            string name,
            IEnumerable collection,
            bool autoReset,
            ref Result error
            );

        /// <summary>
        /// Links the named variable to the specified member of the specified
        /// object, so that accessing the variable reads or writes that member.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved and set.
        /// </param>
        /// <param name="name">
        /// The name of the variable to set.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="memberInfo">
        /// The member (e.g. field or property) to link the variable to.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="object">
        /// The object instance whose member is to be linked, or null for a
        /// static member.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableLink(
            VariableFlags flags,
            string name,
            MemberInfo memberInfo,
            object @object,
            ref Result error
            );

        /// <summary>
        /// Sets the named variable to wrap the specified system array, so that
        /// the variable behaves as an array backed by that data.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved and set.
        /// </param>
        /// <param name="name">
        /// The name of the variable to set.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="array">
        /// The system array to associate with the variable.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableSystemArray(
            VariableFlags flags,
            string name,
            Array array,
            ref Result error
            );

        /// <summary>
        /// Sets the value of the named variable.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to set.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// The new value for the variable.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableValue(
            string name,
            string value,
            ref Result error
            );

        /// <summary>
        /// Sets the value of the named variable, using the specified variable
        /// flags and traces.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved and set.
        /// </param>
        /// <param name="name">
        /// The name of the variable to set.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// The new value for the variable.  This parameter may be null.
        /// </param>
        /// <param name="traces">
        /// The list of traces to associate with the variable.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableValue(
            VariableFlags flags,
            string name,
            string value,
            TraceList traces,
            ref Result error
            );

        /// <summary>
        /// Unsets (i.e. removes) the named variable.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to unset.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetVariable(
            string name,
            ref Result error
            );

        /// <summary>
        /// Unsets (i.e. removes) the named variable, using the specified
        /// variable flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variable is resolved and unset.
        /// </param>
        /// <param name="name">
        /// The name of the variable to unset.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetVariable(
            VariableFlags flags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE SYNCHRONIZATION (SINGLE)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits for the named variable to change, become ready, or for the
        /// specified timeout to elapse.
        /// </summary>
        /// <param name="eventWaitFlags">
        /// The flags that control how the wait operation is performed.
        /// </param>
        /// <param name="variableFlags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to wait on.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="microseconds">
        /// The maximum amount of time to wait, in microseconds.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread to wait for, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="limit">
        /// The maximum number of change notifications to wait for.
        /// </param>
        /// <param name="event">
        /// The event to wait on, if any.  This parameter may be null.
        /// </param>
        /// <param name="notReady">
        /// Upon return, indicates whether the variable was not ready to be
        /// waited on.
        /// </param>
        /// <param name="timedOut">
        /// Upon return, indicates whether the wait operation timed out.
        /// </param>
        /// <param name="changed">
        /// Upon return, indicates whether the variable was changed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode WaitVariable(
            EventWaitFlags eventWaitFlags,
            VariableFlags variableFlags,
            string name,
            long microseconds,
            long? threadId,
            int limit,
            EventWaitHandle @event,
            ref bool notReady,
            ref bool timedOut,
            ref bool changed,
            ref Result error
            );

        /// <summary>
        /// Acquires an exclusive lock on the named variable, waiting up to the
        /// specified timeout to do so.
        /// </summary>
        /// <param name="eventWaitFlags">
        /// The flags that control how the wait operation is performed.
        /// </param>
        /// <param name="variableFlags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to lock.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="microseconds">
        /// The maximum amount of time to wait for the lock, in microseconds.
        /// </param>
        /// <param name="event">
        /// The event to wait on, if any.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode LockVariable(
            EventWaitFlags eventWaitFlags,
            VariableFlags variableFlags,
            string name,
            long microseconds,
            EventWaitHandle @event,
            ref Result error
            );

        /// <summary>
        /// Releases an exclusive lock previously acquired on the named
        /// variable.
        /// </summary>
        /// <param name="variableFlags">
        /// The flags that control how the variable is resolved.
        /// </param>
        /// <param name="name">
        /// The name of the variable to unlock.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnlockVariable(
            VariableFlags variableFlags,
            string name,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // VARIABLE MANAGEMENT (MULTIPLE)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Lists the names of the variables that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match variable names, or null to match all
        /// variable names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching variable names.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListVariables(
            string pattern,
            bool noCase,
            bool stopOnError,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Lists the names of the variables that match the specified pattern,
        /// using the specified variable flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control which variables are considered.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match variable names, or null to match all
        /// variable names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching variable names.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ListVariables(
            VariableFlags flags,
            string pattern,
            bool noCase,
            bool stopOnError,
            ref StringList list,
            ref Result error
            );

        /// <summary>
        /// Gets the values of multiple variables, storing each value back into
        /// the supplied dictionary keyed by variable name.
        /// </summary>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to query;
        /// upon success, each value is populated with the corresponding
        /// variable value.  This parameter should not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="getOk">
        /// Upon return, receives the number of variables that were
        /// successfully queried.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableValues(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int getOk,
            ref Result error
            );

        /// <summary>
        /// Gets the values of multiple variables, using the specified variable
        /// flags, storing each value back into the supplied dictionary keyed
        /// by variable name.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variables are resolved.
        /// </param>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to query;
        /// upon success, each value is populated with the corresponding
        /// variable value.  This parameter should not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="getOk">
        /// Upon return, receives the number of variables that were
        /// successfully queried.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetVariableValues(
            VariableFlags flags,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int getOk,
            ref Result error
            );

        /// <summary>
        /// Sets the values of multiple variables from the supplied dictionary
        /// keyed by variable name.
        /// </summary>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to set and
        /// whose values are the new values to assign.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="setOk">
        /// Upon return, receives the number of variables that were
        /// successfully set.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableValues(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int setOk,
            ref Result error
            );

        /// <summary>
        /// Sets the values of multiple variables, using the specified variable
        /// flags and traces, from the supplied dictionary keyed by variable
        /// name.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variables are resolved and set.
        /// </param>
        /// <param name="traces">
        /// The list of traces to associate with the variables.  This parameter
        /// may be null.
        /// </param>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to set and
        /// whose values are the new values to assign.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="setOk">
        /// Upon return, receives the number of variables that were
        /// successfully set.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetVariableValues(
            VariableFlags flags,
            TraceList traces,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int setOk,
            ref Result error
            );

        /// <summary>
        /// Unsets (i.e. removes) multiple variables named by the keys of the
        /// supplied dictionary.
        /// </summary>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to unset.
        /// This parameter should not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="unsetOk">
        /// Upon return, receives the number of variables that were
        /// successfully unset.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetVariables(
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int unsetOk,
            ref Result error
            );

        /// <summary>
        /// Unsets (i.e. removes) multiple variables named by the keys of the
        /// supplied dictionary, using the specified variable flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the variables are resolved and unset.
        /// </param>
        /// <param name="variables">
        /// The dictionary whose keys are the names of the variables to unset.
        /// This parameter should not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first error encountered.
        /// </param>
        /// <param name="unsetOk">
        /// Upon return, receives the number of variables that were
        /// successfully unset.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetVariables(
            VariableFlags flags,
            IDictionary<string, object> variables,
            bool stopOnError,
            ref int unsetOk,
            ref Result error
            );
    }
}
