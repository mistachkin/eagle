/*
 * ThreadVariable.cs --
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
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using CleanupTriplet = Eagle._Components.Public.MutableAnyTriplet<
    Eagle._Components.Public.Interpreter, long, int>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class implements the backing store for a thread-local Tcl variable,
    /// keeping a separate value (scalar or array) for each thread that accesses
    /// it.  It installs a variable trace so that get, set, and unset operations
    /// on the associated Eagle variable are redirected to the per-thread storage
    /// it maintains, and it provides helper methods for the introspection and
    /// cleanup of that storage.
    /// </summary>
    [ObjectId("cec51b48-b670-4e51-ac05-4f45fa051233")]
    internal sealed class ThreadVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the string representation of this object includes the
        /// full string form of any array values rather than a summary.
        /// </summary>
        private static bool DefaultToStringFull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the per-thread values
        /// maintained by this object.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The per-thread values maintained by this object, keyed by thread
        /// identifier.  Each value is either a scalar value or an
        /// <see cref="ElementDictionary" /> representing an array.
        /// </summary>
        private LongObjectDictionary values;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class, initializing its empty
        /// per-thread value storage.
        /// </summary>
        private ThreadVariable()
        {
            lock (syncRoot) /* REDUNDANT */
            {
                this.values = new LongObjectDictionary();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new, empty thread-local variable backing store.
        /// </summary>
        /// <returns>
        /// The newly created thread-local variable backing store.
        /// </returns>
        public static ThreadVariable Create()
        {
            return new ThreadVariable();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        //
        // NOTE: This method assumes the interpreter lock is held.
        //
        /// <summary>
        /// This method removes the values associated with the specified thread
        /// from all thread-local variables in every scope, namespace, and the
        /// global frame of an interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose thread-local variables are to be cleaned up.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose values are to be removed.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to stop and return on the first error encountered; zero to
        /// continue cleaning up despite errors.
        /// </param>
        /// <param name="count">
        /// This parameter receives the number of thread-local values that were
        /// removed, accumulated with its incoming value.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this list receives one or more error messages
        /// describing why cleanup failed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode CleanupForThread(
            Interpreter interpreter,
            long threadId,
            bool failOnError,
            ref int count,
            ref ResultList errors
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            Result error; /* REUSED */

            int okCount = 0;
            int errorCount = 0;

            CleanupTriplet anyTriplet = new CleanupTriplet(
                true, interpreter, threadId, okCount);

            IClientData clientData = new ClientData(anyTriplet);

            error = null;

            if (interpreter.InvokeInEachScope(
                    CleanupForScope, clientData, true,
                    ref error) == ReturnCode.Ok)
            {
                okCount += anyTriplet.Z;
                anyTriplet.Z = 0;
            }
            else
            {
                errorCount++;

                if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }

                if (failOnError)
                    return ReturnCode.Error;
            }

            error = null;

            if (interpreter.InvokeInEachNamespace(
                    CleanupForNamespace, clientData,
                    ref error) == ReturnCode.Ok)
            {
                okCount += anyTriplet.Z;
                anyTriplet.Z = 0;
            }
            else
            {
                errorCount++;

                if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }

                if (failOnError)
                    return ReturnCode.Error;
            }

            ICallFrame globalFrame = interpreter.CurrentGlobalFrame;

            if (globalFrame != null)
            {
                error = null;

                if (CleanupForThread(interpreter, globalFrame, threadId,
                        ref okCount, ref error) != ReturnCode.Ok)
                {
                    errorCount++;

                    if (error != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(error);
                    }

                    if (failOnError)
                        return ReturnCode.Error;
                }
            }

            count += okCount;

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the interpreter lock is held.
        //
        /// <summary>
        /// This method removes the values associated with the specified thread
        /// from all thread-local variables contained in a single call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the call frame.
        /// </param>
        /// <param name="frame">
        /// The call frame whose thread-local variables are to be cleaned up.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose values are to be removed.
        /// </param>
        /// <param name="count">
        /// This parameter receives the number of thread-local values that were
        /// removed, accumulated with its incoming value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why cleanup failed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode CleanupForThread(
            Interpreter interpreter,
            ICallFrame frame,
            long threadId,
            ref int count,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (frame == null)
            {
                error = "invalid call frame";
                return ReturnCode.Error;
            }

            VariableDictionary variables = frame.Variables;

            if (variables == null)
            {
                error = "call frame does not support variables";
                return ReturnCode.Error;
            }

            int localCount = 0;

            foreach (KeyValuePair<string, IVariable> pair in variables)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                ThreadVariable threadVariable = null;

                if (!interpreter.IsThreadVariable(variable,
                        ref threadVariable))
                {
                    continue;
                }

                if (threadVariable.PrivateCleanupForThread(threadId))
                    localCount++;
            }

            count += localCount;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the interpreter lock is held.
        //
        /// <summary>
        /// This method is the per-scope callback used when cleaning up
        /// thread-local variables; it removes the values associated with a
        /// thread from the thread-local variables in a single scope call frame.
        /// </summary>
        /// <param name="frame">
        /// The scope call frame whose thread-local variables are to be cleaned
        /// up.
        /// </param>
        /// <param name="clientData">
        /// The client data that wraps the cleanup triplet (i.e. the interpreter,
        /// thread identifier, and running count).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why cleanup failed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode CleanupForScope(
            ICallFrame frame,
            IClientData clientData,
            ref Result error
            ) /* CallFrameCallback */
        {
            if (frame == null)
            {
                error = "invalid call frame";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                error = "invalid clientData";
                return ReturnCode.Error;
            }

            CleanupTriplet anyTriplet = clientData.Data as CleanupTriplet;

            if (anyTriplet == null)
            {
                error = "invalid cleanup triplet";
                return ReturnCode.Error;
            }

            ReturnCode code;
            int count = 0;

            code = CleanupForThread(
                anyTriplet.X, frame, anyTriplet.Y, ref count, ref error);

            anyTriplet.Z += count;
            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the interpreter lock is held.
        //
        /// <summary>
        /// This method is the per-namespace callback used when cleaning up
        /// thread-local variables; it removes the values associated with a
        /// thread from the thread-local variables in a single namespace variable
        /// frame.
        /// </summary>
        /// <param name="namespace">
        /// The namespace whose variable frame is to be cleaned up.
        /// </param>
        /// <param name="clientData">
        /// The client data that wraps the cleanup triplet (i.e. the interpreter,
        /// thread identifier, and running count).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why cleanup failed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode CleanupForNamespace(
            INamespace @namespace,
            IClientData clientData,
            ref Result error
            ) /* NamespaceCallback */
        {
            if (@namespace == null)
            {
                error = "invalid namespace";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                error = "invalid clientData";
                return ReturnCode.Error;
            }

            CleanupTriplet anyTriplet = clientData.Data as CleanupTriplet;

            if (anyTriplet == null)
            {
                error = "invalid cleanup triplet";
                return ReturnCode.Error;
            }

            ICallFrame frame = @namespace.VariableFrame;

            if (frame == null) // e.g. global frame
                return ReturnCode.Ok;

            ReturnCode code;
            int count = 0;

            code = CleanupForThread(
                anyTriplet.X, frame, anyTriplet.Y, ref count, ref error);

            anyTriplet.Z += count;
            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method returns the identifier of the current system thread,
        /// which is used to key the per-thread value storage.
        /// </summary>
        /// <returns>
        /// The identifier of the current system thread.
        /// </returns>
        public static long GetThreadId()
        {
            return GlobalState.GetCurrentSystemThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the values associated with the specified thread
        /// from all thread-local variables in an interpreter, tracing the
        /// outcome.  Any errors encountered are logged rather than thrown.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose thread-local variables are to be cleaned up.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose values are to be removed.
        /// </param>
        public static void CleanupForThread(
            Interpreter interpreter,
            long threadId
            )
        {
            ReturnCode code;
            int count = 0;
            ResultList errors = null;

            code = CleanupForThread(
                interpreter, threadId, false, ref count, ref errors);

            TracePriority priority = TracePriority.CleanupDebug;

            if (errors != null)
                priority = TracePriority.CleanupError;

            TraceOps.DebugTrace(String.Format(
                "CleanupForThread: interpreter = {0}, threadId = {1}, " +
                "count = {2}, code = {3}, errors = {4}",
                FormatOps.InterpreterNoThrow(interpreter),
                threadId, count, code, FormatOps.WrapOrNull(errors)),
                typeof(ThreadVariable).Name, priority);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method removes the value associated with the specified thread
        /// from this object's per-thread storage.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread whose value is to be removed.
        /// </param>
        /// <returns>
        /// True if a value was removed; otherwise, false.
        /// </returns>
        private bool PrivateCleanupForThread(
            long threadId
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                    return false;

                return values.Remove(threadId);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the values associated with all threads from this
        /// object's per-thread storage.
        /// </summary>
        /// <returns>
        /// The number of per-thread values that were removed.
        /// </returns>
        private int PrivateCleanupForAll()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (values != null)
                {
                    count += values.Count;
                    values.Clear();
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a value exists for the specified
        /// thread, discarding any error message produced.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation being performed, used when formatting
        /// error messages.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose value existence is to be checked.
        /// </param>
        /// <param name="varName">
        /// The name of the variable being checked, used when formatting error
        /// messages.
        /// </param>
        /// <returns>
        /// True if a value exists for the specified thread; otherwise, false.
        /// </returns>
        private bool TryHasValue(
            BreakpointType breakpointType,
            Interpreter interpreter,
            long threadId,
            string varName
            )
        {
            Result error = null;

            return TryHasValue(
                breakpointType, interpreter, threadId, varName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a value exists for the specified
        /// thread.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation being performed, used when formatting
        /// error messages.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose value existence is to be checked.
        /// </param>
        /// <param name="varName">
        /// The name of the variable being checked, used when formatting error
        /// messages.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why no value exists.
        /// </param>
        /// <returns>
        /// True if a value exists for the specified thread; otherwise, false.
        /// </returns>
        private bool TryHasValue(
            BreakpointType breakpointType,
            Interpreter interpreter,
            long threadId,
            string varName,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                {
                    error = FormatOps.MissingValuesName(
                        breakpointType, varName, null);

                    return false;
                }

                if (values.ContainsKey(threadId))
                {
                    return true;
                }
                else
                {
                    error = FormatOps.MissingVariableName(
                        breakpointType, varName, " for thread");

                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the array value associated with the
        /// specified thread, discarding any error message produced.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread whose array value is to be retrieved.
        /// </param>
        /// <param name="arrayValue">
        /// Upon success, this parameter receives the array value associated with
        /// the specified thread.
        /// </param>
        /// <returns>
        /// True if an array value was retrieved; otherwise, false.
        /// </returns>
        private bool TryGetArray(
            long threadId,
            out ElementDictionary arrayValue
            )
        {
            Result error = null;

            return TryGetArray(threadId, out arrayValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the array value associated with the
        /// specified thread.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread whose array value is to be retrieved.
        /// </param>
        /// <param name="arrayValue">
        /// Upon success, this parameter receives the array value associated with
        /// the specified thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the array value could not be retrieved.
        /// </param>
        /// <returns>
        /// True if an array value was retrieved; otherwise, false.
        /// </returns>
        private bool TryGetArray(
            long threadId,
            out ElementDictionary arrayValue,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                {
                    arrayValue = null;
                    error = "values unavailable";

                    return false;
                }

                object value; /* REUSED */

                if (!values.TryGetValue(threadId, out value))
                {
                    arrayValue = null;
                    error = "missing value for thread";

                    return false;
                }

                arrayValue = value as ElementDictionary;

                if (arrayValue == null)
                {
                    error = "missing array for thread";
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the value (i.e. a scalar value or an
        /// array element) associated with the specified thread.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation being performed, used when formatting
        /// error messages.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose value is to be retrieved.
        /// </param>
        /// <param name="varName">
        /// The name of the variable being retrieved, used when formatting error
        /// messages.
        /// </param>
        /// <param name="varIndex">
        /// The array element name to retrieve, or null to retrieve a scalar
        /// value.
        /// </param>
        /// <param name="oldValue">
        /// Upon success, this parameter receives the value that was retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the value could not be retrieved.
        /// </param>
        /// <returns>
        /// True if a value was retrieved; otherwise, false.
        /// </returns>
        private bool TryGetValue(
            BreakpointType breakpointType,
            Interpreter interpreter,
            long threadId,
            string varName,
            string varIndex,
            ref object oldValue,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                {
                    error = FormatOps.MissingValuesName(
                        breakpointType, varName, null);

                    return false;
                }

                object localValue; /* REUSED */

                if (values.TryGetValue(threadId, out localValue))
                {
                    ElementDictionary arrayValue =
                        localValue as ElementDictionary;

                    if (arrayValue != null)
                    {
                        if (varIndex != null)
                        {
                            if (arrayValue.TryGetValue(
                                    varIndex, out localValue))
                            {
                                oldValue = localValue;
                                return true;
                            }
                            else
                            {
                                error = FormatOps.ErrorElementName(
                                    breakpointType, varName, varIndex);

                                return false;
                            }
                        }
                        else
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, true);

                            return false;
                        }
                    }
                    else
                    {
                        if (varIndex != null)
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, false);

                            return false;
                        }
                        else
                        {
                            oldValue = localValue;
                            return true;
                        }
                    }
                }
                else
                {
                    error = FormatOps.MissingVariableName(
                        breakpointType, varName, " for thread");

                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the value (i.e. a scalar value or an
        /// array element) associated with the specified thread, creating the
        /// per-thread storage if necessary.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation being performed, used when formatting
        /// error messages.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose value is to be set.
        /// </param>
        /// <param name="varName">
        /// The name of the variable being set, used when formatting error
        /// messages.
        /// </param>
        /// <param name="varIndex">
        /// The array element name to set, or null to set a scalar value.
        /// </param>
        /// <param name="newValue">
        /// The new value to be stored.
        /// </param>
        /// <param name="variableFlags">
        /// The flags that control how the new value is combined with any
        /// existing value (e.g. for append operations).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the value could not be set.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        private bool TrySetValue(
            BreakpointType breakpointType,
            Interpreter interpreter,
            long threadId,
            string varName,
            string varIndex,
            object newValue,
            VariableFlags variableFlags,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                {
                    error = FormatOps.MissingValuesName(
                        breakpointType, varName, null);

                    return false;
                }

                object localValue; /* REUSED */
                ElementDictionary arrayValue; /* REUSED */
                object oldValue; /* REUSED */

                if (values.TryGetValue(threadId, out localValue))
                {
                    arrayValue = localValue as ElementDictionary;

                    if (arrayValue != null)
                    {
                        if (varIndex != null)
                        {
                            /* IGNORED */
                            arrayValue.TryGetValue(
                                varIndex, out oldValue);

                            newValue = EntityOps.GetNewValue(
                                variableFlags, oldValue, newValue);

                            arrayValue[varIndex] = newValue;
                            return true;
                        }
                        else
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, true);

                            return false;
                        }
                    }
                    else
                    {
                        if (varIndex != null)
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, false);

                            return false;
                        }
                        else
                        {
                            /* IGNORED */
                            values.TryGetValue(
                                threadId, out oldValue);

                            newValue = EntityOps.GetNewValue(
                                variableFlags, oldValue, newValue);

                            values[threadId] = newValue;
                            return true;
                        }
                    }
                }
                else
                {
                    if (varIndex != null)
                    {
                        EventWaitHandle variableEvent = null;

                        if (interpreter != null)
                            variableEvent = interpreter.VariableEvent;

                        arrayValue = new ElementDictionary(variableEvent);

                        arrayValue.Add(varIndex, newValue);
                        localValue = arrayValue;
                    }
                    else
                    {
                        localValue = newValue;
                    }

                    values.Add(threadId, localValue);
                    return true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to unset the value (i.e. a scalar value or an
        /// array element) associated with the specified thread.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation being performed, used when formatting
        /// error messages.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose value is to be unset.
        /// </param>
        /// <param name="varName">
        /// The name of the variable being unset, used when formatting error
        /// messages.
        /// </param>
        /// <param name="varIndex">
        /// The array element name to unset, or null to unset a scalar value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the value could not be unset.
        /// </param>
        /// <returns>
        /// True if the value was unset; otherwise, false.
        /// </returns>
        private bool TryUnsetValue(
            BreakpointType breakpointType,
            Interpreter interpreter,
            long threadId,
            string varName,
            string varIndex,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values == null)
                {
                    error = FormatOps.MissingValuesName(
                        breakpointType, varName, " before lookup");

                    return false;
                }

                object localValue; /* REUSED */

                if (values.TryGetValue(threadId, out localValue))
                {
                    ElementDictionary arrayValue =
                        localValue as ElementDictionary;

                    if (arrayValue != null)
                    {
                        if (varIndex != null)
                        {
                            if (arrayValue.Remove(varIndex))
                            {
                                return true;
                            }
                            else
                            {
                                error = FormatOps.MissingElementName(
                                    breakpointType, varName, true);

                                return false;
                            }
                        }
                        else
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, true);

                            return false;
                        }
                    }
                    else
                    {
                        if (varIndex != null)
                        {
                            error = FormatOps.MissingElementName(
                                breakpointType, varName, false);

                            return false;
                        }
                        else
                        {
                            if (values.Remove(threadId))
                            {
                                return true;
                            }
                            else
                            {
                                error = FormatOps.MissingValuesName(
                                    breakpointType, varName, " after lookup");

                                return false;
                            }
                        }
                    }
                }
                else
                {
                    error = FormatOps.MissingVariableName(
                        breakpointType, varName, " for thread");

                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Method
        /// <summary>
        /// This method is the variable trace callback that redirects get, set,
        /// and unset operations on the associated Eagle variable to this
        /// object's per-thread storage, canceling the normal variable operation.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation that triggered the trace.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter in which the traced variable operation is occurring.
        /// </param>
        /// <param name="traceInfo">
        /// The information that describes the traced variable operation,
        /// including the variable, its name and index, and the new value.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter receives the result of the operation; the
        /// retrieved value, an empty string, or an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        [MethodFlags(
            MethodFlags.VariableTrace | MethodFlags.System |
            MethodFlags.NoAdd)]
        private ReturnCode TraceCallback(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                result = "invalid trace";
                return ReturnCode.Error;
            }

            IVariable variable = traceInfo.Variable;

            if (variable == null)
            {
                result = "invalid variable";
                return ReturnCode.Error;
            }

            if (breakpointType == BreakpointType.BeforeVariableAdd)
                return traceInfo.ReturnCode;

            if ((breakpointType != BreakpointType.BeforeVariableGet) &&
                (breakpointType != BreakpointType.BeforeVariableSet) &&
                (breakpointType != BreakpointType.BeforeVariableUnset))
            {
                result = "unsupported operation";
                return ReturnCode.Error;
            }

            long threadId = GetThreadId();
            Result error; /* REUSED */

            try
            {
                switch (breakpointType)
                {
                    case BreakpointType.BeforeVariableGet:
                        {
                            object oldValue = null;

                            error = null;

                            if (TryGetValue(
                                    breakpointType, interpreter,
                                    threadId, traceInfo.Name,
                                    traceInfo.Index, ref oldValue,
                                    ref error))
                            {
                                result = Result.FromObject(
                                    oldValue, false, false, false);

                                traceInfo.ReturnCode = ReturnCode.Ok;
                            }
                            else
                            {
                                result = error;
                                traceInfo.ReturnCode = ReturnCode.Error;
                            }

                            traceInfo.Cancel = true;
                            break;
                        }
                    case BreakpointType.BeforeVariableSet:
                        {
                            error = null;

                            if (TrySetValue(
                                    breakpointType, interpreter,
                                    threadId, traceInfo.Name,
                                    traceInfo.Index, traceInfo.NewValue,
                                    traceInfo.Flags, ref error))
                            {
                                result = Result.FromObject(
                                    traceInfo.NewValue, false, false, false);

                                EntityOps.SetUndefined(variable, false);
                                EntityOps.SetDirty(variable, true);

                                traceInfo.ReturnCode = ReturnCode.Ok;
                            }
                            else
                            {
                                result = error;
                                traceInfo.ReturnCode = ReturnCode.Error;
                            }

                            traceInfo.Cancel = true;
                            break;
                        }
                    case BreakpointType.BeforeVariableUnset:
                        {
                            error = null;

                            if (TryUnsetValue(
                                    breakpointType, interpreter,
                                    threadId, traceInfo.Name,
                                    traceInfo.Index, ref error))
                            {
                                result = String.Empty;

                                EntityOps.SetDirty(variable, true);

                                traceInfo.ReturnCode = ReturnCode.Ok;
                            }
                            else
                            {
                                result = error;
                                traceInfo.ReturnCode = ReturnCode.Error;
                            }

                            traceInfo.Cancel = true;
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                result = e;
                traceInfo.ReturnCode = ReturnCode.Error;
            }

            return traceInfo.ReturnCode;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Scalar Sub-Command Helper Methods
        /// <summary>
        /// This method determines whether a scalar value exists for the current
        /// thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <returns>
        /// True if a scalar value exists for the current thread; otherwise,
        /// false.
        /// </returns>
        public bool DoesExist(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();

            return TryHasValue(
                BreakpointType.BeforeVariableExist, interpreter, threadId,
                null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Sub-Command Helper Methods
        /// <summary>
        /// This method determines whether an array, or a specified element of
        /// that array, exists for the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="name">
        /// The name of the array element to check, or null to check whether the
        /// array itself exists.
        /// </param>
        /// <returns>
        /// True if the array (or the specified element) exists for the current
        /// thread; otherwise, false.
        /// </returns>
        public bool DoesExist(
            Interpreter interpreter,
            string name
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();
            ElementDictionary arrayValue;

            if (!TryGetArray(threadId, out arrayValue))
                return false;

            if (name == null)
                return true;

            return arrayValue.ContainsKey(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of elements in the array for the
        /// current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the count could not be determined.
        /// </param>
        /// <returns>
        /// The number of elements in the array for the current thread, or null
        /// on failure.
        /// </returns>
        public long? GetCount(
            Interpreter interpreter,
            ref Result error
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();
            ElementDictionary arrayValue;

            if (!TryGetArray(threadId, out arrayValue, ref error))
                return null;

            return arrayValue.Count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the elements of the array for the current thread
        /// as a dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the element names in the returned dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the element values in the returned dictionary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the list could not be produced.
        /// </param>
        /// <returns>
        /// A dictionary containing the array elements for the current thread, or
        /// null on failure.
        /// </returns>
        public ObjectDictionary GetList(
            Interpreter interpreter,
            bool names,
            bool values,
            ref Result error
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();
            ElementDictionary arrayValue;

            if (!TryGetArray(threadId, out arrayValue, ref error))
                return null;

            return new ObjectDictionary(
                (IDictionary<string, object>)arrayValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the element names of the array for the current
        /// thread, optionally filtered by a pattern, as a string list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="mode">
        /// The matching mode used when filtering element names by the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter element names, or null to include all
        /// names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the matching mode is regular
        /// expression based.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the string could not be produced.
        /// </param>
        /// <returns>
        /// A string list of the matching element names, or null on failure.
        /// </returns>
        public string KeysToString(
            Interpreter interpreter,
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();
            ElementDictionary arrayValue;

            if (!TryGetArray(threadId, out arrayValue, ref error))
                return null;

            ObjectDictionary dictionary = new ObjectDictionary(
                (IDictionary<string, object>)arrayValue);

            StringList list = GenericOps<string, object>.KeysAndValues(
                dictionary, false, true, false, mode, pattern, null,
                null, null, null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the element names and values of the array for the
        /// current thread, optionally filtered by a pattern, as a string list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that provides context for the operation.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter element names, or null to include all
        /// names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the string could not be produced.
        /// </param>
        /// <returns>
        /// A string list of the matching element names and values, or null on
        /// failure.
        /// </returns>
        public string KeysAndValuesToString(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            CheckDisposed();

            long threadId = GetThreadId();
            ElementDictionary arrayValue;

            if (!TryGetArray(threadId, out arrayValue, ref error))
                return null;

            ObjectDictionary dictionary = new ObjectDictionary(
                (IDictionary<string, object>)arrayValue);

            StringList list = GenericOps<string, object>.KeysAndValues(
                dictionary, false, true, true, StringOps.DefaultMatchMode,
                pattern, null, null, null, null, noCase, RegexOptions.None)
                as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        /// <summary>
        /// This method adds a variable to the interpreter that is traced by this
        /// object, so that operations on it are redirected to the per-thread
        /// storage.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to add the variable.
        /// </param>
        /// <param name="variableFlags">
        /// The flags that control how the variable is added.
        /// </param>
        /// <param name="name">
        /// The name of the variable to be added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that describes
        /// why the variable could not be added.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode AddVariable(
            Interpreter interpreter,
            VariableFlags variableFlags,
            string name,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddVariable(variableFlags, name,
                new TraceList(new TraceCallback[] { TraceCallback }),
                true, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Helper Methods
        /// <summary>
        /// This method removes the values associated with all threads from this
        /// object's per-thread storage.
        /// </summary>
        /// <returns>
        /// The number of per-thread values that were removed.
        /// </returns>
        public int CleanupForAll()
        {
            CheckDisposed();

            return PrivateCleanupForAll();
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Helper Methods
        /// <summary>
        /// This method returns a list describing the per-thread values
        /// maintained by this object, keyed by thread identifier, for
        /// introspection purposes.
        /// </summary>
        /// <param name="full">
        /// Non-zero to include the full string form of any array values; zero to
        /// include only a summary.
        /// </param>
        /// <returns>
        /// A list of name and value pairs describing the per-thread values.
        /// </returns>
        private StringPairList ToList(
            bool full
            )
        {
            // CheckDisposed();

            StringPairList list = new StringPairList();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (values != null)
                {
                    foreach (KeyValuePair<long, object> pair in values)
                    {
                        string stringValue;
                        object value = pair.Value;

                        if (value != null)
                        {
                            ElementDictionary arrayValue =
                                value as ElementDictionary;

                            if (arrayValue != null)
                            {
                                string subStringValue;

                                if (full)
                                {
                                    subStringValue =
                                        arrayValue.KeysAndValuesToString(
                                            null, false);
                                }
                                else
                                {
                                    subStringValue =
                                        StringOps.GetStringFromObject(
                                            arrayValue);
                                }

                                stringValue = StringList.MakeList("<array>",
                                    subStringValue);
                            }
                            else
                            {
                                stringValue = StringList.MakeList("<scalar>",
                                    StringOps.GetStringFromObject(value));
                            }
                        }
                        else
                        {
                            stringValue = FormatOps.DisplayNull;
                        }

                        list.Add(pair.Key.ToString(), stringValue);
                    }
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the per-thread values
        /// maintained by this object.
        /// </summary>
        /// <returns>
        /// A string representation of the per-thread values.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList(DefaultToStringFull).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object has been disposed and
        /// the engine is configured to throw on access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(ThreadVariable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object, clearing its
        /// per-thread value storage.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (values != null)
                        {
                            values.Clear();
                            values = null;
                        }
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this object and suppresses
        /// finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes an instance of this class, releasing any resources that
        /// were not released by an explicit call to the <see cref="Dispose()" />
        /// method.
        /// </summary>
        ~ThreadVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
