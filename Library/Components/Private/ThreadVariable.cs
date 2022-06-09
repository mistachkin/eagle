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
        private static bool DefaultToStringFull = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();
        private LongObjectDictionary values;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
        public static long GetThreadId()
        {
            return GlobalState.GetCurrentSystemThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

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

        private bool TryGetArray(
            long threadId,
            out ElementDictionary arrayValue
            )
        {
            Result error = null;

            return TryGetArray(threadId, out arrayValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
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
        public int CleanupForAll()
        {
            CheckDisposed();

            return PrivateCleanupForAll();
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Helper Methods
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
        public override string ToString()
        {
            CheckDisposed();

            return ToList(DefaultToStringFull).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~ThreadVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
