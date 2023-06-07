/*
 * RegistryVariable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("235a191e-06a3-4c8b-9aa5-e3dd1c3e3fb6")]
    public sealed class RegistryVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constructors
        private RegistryVariable(
            RegistryKey rootKey,        /* in */
            string subKeyName,          /* in */
            bool rootKeyOwned,          /* in */
            bool readOnly,              /* in */
            BreakpointType permissions, /* in */
            bool forceQWord,            /* in */
            bool expandString           /* in */
            )
        {
            SetupRootKey(this, ref rootKey, rootKeyOwned, readOnly);

            ///////////////////////////////////////////////////////////////////

            this.subKeyName = subKeyName;
            this.readOnly = readOnly;
            this.permissions = permissions;
            this.forceQWord = forceQWord;
            this.expandString = expandString;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static RegistryVariable Create(
            RegistryKey rootKey,        /* in */
            string subKeyName,          /* in */
            bool rootKeyOwned,          /* in */
            bool readOnly,              /* in */
            BreakpointType permissions, /* in */
            bool forceQWord,            /* in */
            bool expandString           /* in */
            )
        {
            return new RegistryVariable(
                rootKey, subKeyName, rootKeyOwned, readOnly, permissions,
                forceQWord, expandString);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        private RegistryKey rootKey;
        public RegistryKey RootKey
        {
            get { CheckDisposed(); return rootKey; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string subKeyName;
        public string SubKeyName
        {
            get { CheckDisposed(); return subKeyName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool rootKeyOwned;
        public bool RootKeyOwned
        {
            get { CheckDisposed(); return rootKeyOwned; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool readOnly;
        public bool ReadOnly
        {
            get { CheckDisposed(); return readOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType permissions;
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool forceQWord;
        public bool ForceQWord
        {
            get { CheckDisposed(); return forceQWord; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool expandString;
        public bool ExpandString
        {
            get { CheckDisposed(); return expandString; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Sub-Command Helper Methods
        public bool DoesExist(
            Interpreter interpreter, /* in: OPTIONAL */
            string name              /* in */
            )
        {
            CheckDisposed();

            bool success = false;
            Result error = null;

            try
            {
                if (!HasFlags(BreakpointType.BeforeVariableExist, true))
                {
                    error = "permission denied";
                    return false;
                }

                object defaultValue = new object(); /* unique */
                object value = null;

                if (GetValue(
                        name, defaultValue, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                bool result = !Object.ReferenceEquals(value, defaultValue);

                success = true;
                return result;
            }
            finally
            {
                if (!success)
                {
                    TraceOps.DebugTrace(String.Format(
                        "DoesExist: error = {0}", error),
                        typeof(RegistryVariable).Name,
                        TracePriority.DataError2);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public long? GetCount(
            Interpreter interpreter, /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            if (!HasFlags(BreakpointType.BeforeVariableCount, true))
            {
                error = "permission denied";
                return null;
            }

            long count = 0;

            if (GetCount(ref count, ref error) != ReturnCode.Ok)
                return null;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary GetList(
            Interpreter interpreter, /* in: OPTIONAL */
            bool names,              /* in */
            bool values,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            BreakpointType breakpointType = ScriptOps.GetBreakpointType(
                names, values);

            if (breakpointType == BreakpointType.None)
                return null; /* TODO: Sanity? */

            if (!HasFlags(breakpointType, true))
            {
                error = "permission denied";
                return null;
            }

            ObjectDictionary dictionary = null;

            if (GetNamesAndMaybeValues(
                    interpreter, MatchMode.None, null, false,
                    RegexOptions.None, values, true, true,
                    ref dictionary, ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            Interpreter interpreter,   /* in: OPTIONAL */
            MatchMode mode,            /* in */
            string pattern,            /* in */
            bool noCase,               /* in */
            RegexOptions regExOptions, /* in */
            ref Result error           /* out */
            )
        {
            CheckDisposed();

            BreakpointType breakpointType = ScriptOps.GetBreakpointType(
                true, false);

            if (breakpointType == BreakpointType.None)
                return null; /* TODO: Sanity? */

            if (!HasFlags(breakpointType, true))
            {
                error = "permission denied";
                return null;
            }

            StringList list = null;

            if (GetNames(
                    interpreter, mode, pattern, noCase, regExOptions,
                    ref list, ref error) == ReturnCode.Ok)
            {
                return (list != null) ?
                    list.ToString() : String.Empty;
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            Interpreter interpreter, /* in: OPTIONAL */
            string pattern,          /* in */
            bool noCase,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            BreakpointType breakpointType = ScriptOps.GetBreakpointType(
                true, true);

            if (breakpointType == BreakpointType.None)
                return null; /* TODO: Sanity? */

            if (!HasFlags(breakpointType, true))
            {
                error = "permission denied";
                return null;
            }

            ObjectDictionary dictionary = null;

            if (GetNamesAndMaybeValues(
                    interpreter, StringOps.DefaultMatchMode, pattern,
                    noCase, RegexOptions.None, true, true, true,
                    ref dictionary, ref error) == ReturnCode.Ok)
            {
                return (dictionary != null) ?
                    dictionary.KeysAndValuesToString(null, false) : null;
            }
            else
            {
                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        public ReturnCode AddVariable(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddVariable(VariableFlags.Array, name,
                new TraceList(new TraceCallback[] { TraceCallback }),
                true, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Helper Methods
        public StringPairList ToList()
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            if (rootKey != null)
                list.Add("rootKey", rootKey.ToString());

            list.Add("rootKeyOwned", rootKeyOwned.ToString());
            list.Add("readOnly", readOnly.ToString());
            list.Add("permissions", permissions.ToString());
            list.Add("forceQWord", forceQWord.ToString());
            list.Add("expandString", expandString.ToString());

            return list;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Members
        #region Constructor Helper Methods
        private void SetRootKey(
            RegistryKey rootKey, /* in */
            bool rootKeyOwned    /* in */
            )
        {
            this.rootKey = rootKey;
            this.rootKeyOwned = rootKeyOwned;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CloseRootKey(
            ref RegistryKey rootKey, /* in, out */
            bool rootKeyOwned        /* in */
            )
        {
            if (rootKey != null) /* REDUNDANT? */
            {
                if (rootKeyOwned)
                    rootKey.Close();

                rootKey = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetupRootKey(
            RegistryVariable registryVariable, /* in */
            ref RegistryKey rootKey,           /* in, out */
            bool rootKeyOwned,                 /* in */
            bool readOnly                      /* in */
            )
        {
            if (rootKey != null)
            {
                //
                // HACK: This actually "clones" the specified root key
                //       while possibly giving us a read-only version
                //       of it.
                //
                if (registryVariable != null)
                {
                    registryVariable.SetRootKey(rootKey.OpenSubKey(
                        String.Empty, !readOnly), true); /* throw */
                }
                else
                {
                    throw new ArgumentNullException("registryVariable");
                }

                //
                // HACK: If (constructor) caller specified ownership of
                //       the originally passed root key, close it now.
                //
                CloseRootKey(ref rootKey, rootKeyOwned);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Helper Methods
        #region Flags Helper Methods
        private bool HasFlags(
            BreakpointType hasFlags, /* in */
            bool all                 /* in */
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetCount(
            ref long count,  /* in, out */
            ref Result error /* out */
            )
        {
            try
            {
                if (rootKey == null)
                {
                    error = "invalid root key";
                    return ReturnCode.Error;
                }

                if (subKeyName == null)
                {
                    error = "invalid sub-key name";
                    return ReturnCode.Error;
                }

                using (RegistryKey key = rootKey.OpenSubKey(
                        subKeyName, false)) /* throw */
                {
                    if (key == null)
                    {
                        error = String.Format(
                            "could not open sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    count += key.ValueCount;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetNames(
            Interpreter interpreter,   /* in */
            MatchMode mode,            /* in */
            string pattern,            /* in */
            bool noCase,               /* in */
            RegexOptions regExOptions, /* in */
            ref StringList names,      /* in, out */
            ref Result error           /* out */
            )
        {
            try
            {
                if (rootKey == null)
                {
                    error = "invalid root key";
                    return ReturnCode.Error;
                }

                if (subKeyName == null)
                {
                    error = "invalid sub-key name";
                    return ReturnCode.Error;
                }

                using (RegistryKey key = rootKey.OpenSubKey(
                        subKeyName, false)) /* throw */
                {
                    if (key == null)
                    {
                        error = String.Format(
                            "could not open sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    string[] localNames = key.GetValueNames();

                    if (localNames == null)
                    {
                        error = String.Format(
                            "bad value names for sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    if (pattern != null)
                    {
                        foreach (string localName in localNames)
                        {
                            if (StringOps.Match(interpreter,
                                    mode, localName, pattern,
                                    noCase, null, regExOptions))
                            {
                                if (names == null)
                                    names = new StringList();

                                names.Add(localName);
                            }
                        }
                    }
                    else if (names != null)
                    {
                        names.AddRange(localNames);
                    }
                    else
                    {
                        names = new StringList(localNames);
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private RegistryValueOptions GetValueOptions()
        {
            return expandString ? RegistryValueOptions.None :
                RegistryValueOptions.DoNotExpandEnvironmentNames;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetValue(
            string varIndex,     /* in */
            object defaultValue, /* in: OPTIONAL */
            ref object value     /* out */
            )
        {
            Result error = null;

            return GetValue(
                varIndex, defaultValue, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetValue(
            string varIndex,     /* in */
            object defaultValue, /* in: OPTIONAL */
            ref object value,    /* out */
            ref Result error     /* out */
            )
        {
            try
            {
                if (rootKey == null)
                {
                    error = "invalid root key";
                    return ReturnCode.Error;
                }

                if (subKeyName == null)
                {
                    error = "invalid sub-key name";
                    return ReturnCode.Error;
                }

                if (varIndex == null)
                {
                    error = "invalid value name";
                    return ReturnCode.Error;
                }

                using (RegistryKey key = rootKey.OpenSubKey(
                        subKeyName, false)) /* throw */
                {
                    if (key == null)
                    {
                        error = String.Format(
                            "could not open sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    value = key.GetValue(
                        varIndex, defaultValue,
                        GetValueOptions()); /* throw */

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode SetValue(
            string varIndex, /* in */
            object value,    /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (rootKey == null)
                {
                    error = "invalid root key";
                    return ReturnCode.Error;
                }

                if (subKeyName == null)
                {
                    error = "invalid sub-key name";
                    return ReturnCode.Error;
                }

                if (varIndex == null)
                {
                    error = "invalid value name";
                    return ReturnCode.Error;
                }

                using (RegistryKey key = rootKey.OpenSubKey(
                        subKeyName, !readOnly)) /* throw */
                {
                    if (key == null)
                    {
                        error = String.Format(
                            "could not open sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    if (expandString && (value is string))
                    {
                        key.SetValue(varIndex, value,
                            RegistryValueKind.ExpandString); /* throw */
                    }
                    else if (forceQWord && (value is int))
                    {
                        key.SetValue(varIndex, value,
                            RegistryValueKind.QWord); /* throw */
                    }
                    else
                    {
                        key.SetValue(varIndex, value); /* throw */
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode UnsetValue(
            string varName,  /* in */
            string varIndex, /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (rootKey == null)
                {
                    error = "invalid root key";
                    return ReturnCode.Error;
                }

                if (subKeyName == null)
                {
                    error = "invalid sub-key name";
                    return ReturnCode.Error;
                }

                if (varIndex == null)
                {
                    error = "invalid value name";
                    return ReturnCode.Error;
                }

                using (RegistryKey key = rootKey.OpenSubKey(
                        subKeyName, !readOnly)) /* throw */
                {
                    if (key == null)
                    {
                        error = String.Format(
                            "could not open sub-key {0}",
                            FormatOps.WrapOrNull(subKeyName));

                        return ReturnCode.Error;
                    }

                    key.DeleteValue(varIndex, true); /* throw */
                    return ReturnCode.Ok;
                }
            }
            catch (ArgumentException) /* Arg_RegSubKeyValueAbsent (?) */
            {
                error = FormatOps.ErrorElementName(
                    BreakpointType.BeforeVariableUnset,
                    varName, varIndex);

                return ReturnCode.Error;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the GetNamesAndMaybeValues method only.
        //
        private ReturnCode GetValue(
            string varIndex,      /* in */
            object defaultValue,  /* in: OPTIONAL */
            bool errorOnNotFound, /* in */
            bool failOnError,     /* in */
            ref object value,     /* in */
            ref ResultList errors /* out */
            )
        {
            object localValue = null;
            Result localError = null;

            if (GetValue(
                    varIndex, defaultValue, ref localValue,
                    ref localError) == ReturnCode.Ok)
            {
                if (Object.ReferenceEquals(
                        localValue, defaultValue))
                {
                    if (errorOnNotFound)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "value {0} not found",
                            FormatOps.WrapOrNull(
                            varIndex)));

                        if (failOnError)
                            return ReturnCode.Error;
                    }
                }
                else
                {
                    value = localValue;
                }
            }
            else
            {
                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                if (failOnError)
                    return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetNamesAndMaybeValues(
            Interpreter interpreter,     /* in */
            MatchMode mode,              /* in */
            string pattern,              /* in */
            bool noCase,                 /* in */
            RegexOptions regExOptions,   /* in */
            bool getValues,              /* in */
            bool errorOnNotFound,        /* in */
            bool failOnError,            /* in */
            ref ObjectDictionary values, /* in, out */
            ref Result error             /* out */
            )
        {
            try
            {
                StringList varIndexes = null;

                if (GetNames(
                        interpreter, mode, pattern, noCase,
                        regExOptions, ref varIndexes,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (getValues)
                {
                    ResultList errors = null;
                    object defaultValue = new object(); /* unique */

                    foreach (string varIndex in varIndexes)
                    {
                        if (varIndex == null)
                            continue;

                        object value = null;

                        if (GetValue(
                                varIndex, defaultValue,
                                errorOnNotFound,
                                failOnError, ref value,
                                ref errors) == ReturnCode.Ok)
                        {
                            if (values == null)
                                values = new ObjectDictionary();

                            values[varIndex] = value;
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    if (errors != null)
                        error = errors;

                    return ReturnCode.Ok;
                }
                else
                {
                    foreach (string varIndex in varIndexes)
                    {
                        if (varIndex == null)
                            continue;

                        if (values == null)
                            values = new ObjectDictionary();

                        values[varIndex] = null;
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Method
        [MethodFlags(
            MethodFlags.VariableTrace | MethodFlags.System |
            MethodFlags.NoAdd)]
        private ReturnCode TraceCallback(
            BreakpointType breakpointType, /* in */
            Interpreter interpreter,       /* in */
            ITraceInfo traceInfo,          /* in */
            ref Result result              /* out */
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

            //
            // NOTE: *SPECIAL* Ignore the index when we initially add the
            //       variable since we do not perform any trace actions during
            //       add anyhow.
            //
            if (breakpointType == BreakpointType.BeforeVariableAdd)
                return traceInfo.ReturnCode;

            //
            // NOTE: Check if we support the requested operation at all.
            //
            if ((breakpointType != BreakpointType.BeforeVariableGet) &&
                (breakpointType != BreakpointType.BeforeVariableSet) &&
                (breakpointType != BreakpointType.BeforeVariableUnset))
            {
                result = "unsupported operation";
                return ReturnCode.Error;
            }

            //
            // NOTE: *WARNING* Empty array element names are allowed, please do
            //       not change this to "!String.IsNullOrEmpty".
            //
            if (traceInfo.Index != null)
            {
                //
                // NOTE: Check if we are allowing this type of operation.  This
                //       does not apply if the entire variable is being removed
                //       from the interpreter (i.e. for "unset" operations when
                //       the index is null).
                //
                if (!HasFlags(breakpointType, true))
                {
                    result = "permission denied";
                    return ReturnCode.Error;
                }

                try
                {
                    switch (breakpointType)
                    {
                        case BreakpointType.BeforeVariableGet:
                            {
                                object defaultValue = new object(); /* unique */
                                object value = null;

                                if (GetValue(
                                        traceInfo.Index, defaultValue,
                                        ref value, ref result) == ReturnCode.Ok)
                                {
                                    if (Object.ReferenceEquals(value, defaultValue))
                                    {
                                        result = FormatOps.ErrorElementName(
                                            breakpointType, variable.Name,
                                            traceInfo.Index);

                                        traceInfo.ReturnCode = ReturnCode.Error;
                                    }
                                    else
                                    {
                                        result = StringOps.GetResultFromObject(
                                            value);

                                        traceInfo.ReturnCode = ReturnCode.Ok;
                                    }
                                }
                                else
                                {
                                    traceInfo.ReturnCode = ReturnCode.Error;
                                }

                                traceInfo.Cancel = true;
                                break;
                            }
                        case BreakpointType.BeforeVariableSet:
                            {
                                if (SetValue(traceInfo.Index,
                                        traceInfo.NewValue,
                                        ref result) == ReturnCode.Ok)
                                {
                                    EntityOps.SetUndefined(variable, false);
                                    EntityOps.SetDirty(variable, true);

                                    traceInfo.ReturnCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    traceInfo.ReturnCode = ReturnCode.Error;
                                }

                                traceInfo.Cancel = true;
                                break;
                            }
                        case BreakpointType.BeforeVariableUnset:
                            {
                                if (UnsetValue(
                                        variable.Name, traceInfo.Index,
                                        ref result) == ReturnCode.Ok)
                                {
                                    result = String.Empty;

                                    EntityOps.SetDirty(variable, true);

                                    traceInfo.ReturnCode = ReturnCode.Ok;
                                }
                                else
                                {
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
            else if (breakpointType == BreakpointType.BeforeVariableUnset)
            {
                //
                // NOTE: They want to unset the entire DB array.  I guess
                //       this should be allowed, it is in Tcl.  Also, make
                //       sure it is purged from the call frame so that it
                //       cannot be magically restored with this trace
                //       callback in place.
                //
                traceInfo.Flags &= ~VariableFlags.NoRemove;

                //
                // NOTE: Ok, allow the variable removal.
                //
                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We (this trace procedure) expect the variable
                //       to always be an array.
                //
                result = FormatOps.MissingElementName(
                    breakpointType, variable.Name, true);

                return ReturnCode.Error;
            }
        }
        #endregion
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
                    typeof(RegistryVariable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    CloseRootKey(ref rootKey, rootKeyOwned);
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
        ~RegistryVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
