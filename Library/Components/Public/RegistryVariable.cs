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
    /// <summary>
    /// This class implements a variable trace that exposes a Windows registry
    /// sub-key as a Tcl array within an interpreter.  Reading an element of the
    /// array reads the corresponding registry value, writing an element writes
    /// (or creates) the value, and unsetting an element deletes it.  Access is
    /// gated by a set of <see cref="BreakpointType" /> permissions and may be
    /// restricted to read-only operation.
    /// </summary>
    [ObjectId("235a191e-06a3-4c8b-9aa5-e3dd1c3e3fb6")]
    public sealed class RegistryVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="rootKey">
        /// The registry key under which the sub-key resides.  A read-only or
        /// writable clone of this key is opened and retained by the new
        /// instance.
        /// </param>
        /// <param name="subKeyName">
        /// The name of the sub-key, relative to <paramref name="rootKey" />,
        /// whose values are exposed as array elements.
        /// </param>
        /// <param name="rootKeyOwned">
        /// Non-zero if this instance owns <paramref name="rootKey" /> and is
        /// therefore responsible for closing it.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the sub-key should be opened for read-only access,
        /// preventing modification of any registry values.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that are permitted on the exposed array.
        /// </param>
        /// <param name="forceQWord">
        /// Non-zero to store integer values using the
        /// <see cref="RegistryValueKind.QWord" /> kind.
        /// </param>
        /// <param name="expandString">
        /// Non-zero to store string values using the
        /// <see cref="RegistryValueKind.ExpandString" /> kind and to expand
        /// environment names when reading values.
        /// </param>
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
        /// <summary>
        /// This method creates a new <see cref="RegistryVariable" /> instance
        /// for the specified registry sub-key.
        /// </summary>
        /// <param name="rootKey">
        /// The registry key under which the sub-key resides.  A read-only or
        /// writable clone of this key is opened and retained by the new
        /// instance.
        /// </param>
        /// <param name="subKeyName">
        /// The name of the sub-key, relative to <paramref name="rootKey" />,
        /// whose values are exposed as array elements.
        /// </param>
        /// <param name="rootKeyOwned">
        /// Non-zero if the new instance owns <paramref name="rootKey" /> and is
        /// therefore responsible for closing it.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the sub-key should be opened for read-only access,
        /// preventing modification of any registry values.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that are permitted on the exposed array.
        /// </param>
        /// <param name="forceQWord">
        /// Non-zero to store integer values using the
        /// <see cref="RegistryValueKind.QWord" /> kind.
        /// </param>
        /// <param name="expandString">
        /// Non-zero to store string values using the
        /// <see cref="RegistryValueKind.ExpandString" /> kind and to expand
        /// environment names when reading values.
        /// </param>
        /// <returns>
        /// The newly created <see cref="RegistryVariable" /> instance.
        /// </returns>
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
        /// <summary>
        /// Stores the (cloned) registry key that contains the exposed sub-key.
        /// </summary>
        private RegistryKey rootKey;
        /// <summary>
        /// Gets the registry key that contains the exposed sub-key.
        /// </summary>
        public RegistryKey RootKey
        {
            get { CheckDisposed(); return rootKey; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the sub-key whose values are exposed as array
        /// elements.
        /// </summary>
        private string subKeyName;
        /// <summary>
        /// Gets the name of the sub-key whose values are exposed as array
        /// elements.
        /// </summary>
        public string SubKeyName
        {
            get { CheckDisposed(); return subKeyName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether this instance owns its root key
        /// and is responsible for closing it.
        /// </summary>
        private bool rootKeyOwned;
        /// <summary>
        /// Gets a value indicating whether this instance owns its root key and
        /// is responsible for closing it.
        /// </summary>
        public bool RootKeyOwned
        {
            get { CheckDisposed(); return rootKeyOwned; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the sub-key was opened for
        /// read-only access.
        /// </summary>
        private bool readOnly;
        /// <summary>
        /// Gets a value indicating whether the sub-key was opened for read-only
        /// access.
        /// </summary>
        public bool ReadOnly
        {
            get { CheckDisposed(); return readOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the set of operations that are permitted on the exposed
        /// array.
        /// </summary>
        private BreakpointType permissions;
        /// <summary>
        /// Gets the set of operations that are permitted on the exposed array.
        /// </summary>
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether integer values are stored using
        /// the <see cref="RegistryValueKind.QWord" /> kind.
        /// </summary>
        private bool forceQWord;
        /// <summary>
        /// Gets a value indicating whether integer values are stored using the
        /// <see cref="RegistryValueKind.QWord" /> kind.
        /// </summary>
        public bool ForceQWord
        {
            get { CheckDisposed(); return forceQWord; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether string values are stored using the
        /// <see cref="RegistryValueKind.ExpandString" /> kind and environment
        /// names are expanded when reading values.
        /// </summary>
        private bool expandString;
        /// <summary>
        /// Gets a value indicating whether string values are stored using the
        /// <see cref="RegistryValueKind.ExpandString" /> kind and environment
        /// names are expanded when reading values.
        /// </summary>
        public bool ExpandString
        {
            get { CheckDisposed(); return expandString; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Sub-Command Helper Methods
        /// <summary>
        /// This method determines whether a value with the specified name
        /// exists within the exposed sub-key.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="name">
        /// The name of the registry value to check for.
        /// </param>
        /// <returns>
        /// True if the value exists and the operation is permitted; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method counts the number of values within the exposed sub-key.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The number of values within the sub-key, or null if the operation is
        /// not permitted or fails.
        /// </returns>
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

        /// <summary>
        /// This method builds a dictionary of the value names and/or values
        /// within the exposed sub-key.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the value names in the operation.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the values in the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// A dictionary mapping value names to values, or null if the operation
        /// is not permitted or fails.
        /// </returns>
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

        /// <summary>
        /// This method returns the value names within the exposed sub-key,
        /// optionally filtered by a pattern, formatted as a string list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare value names against
        /// <paramref name="pattern" />.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the value names.  If this value is null,
        /// all value names are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching against <paramref name="pattern" /> should be
        /// case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when
        /// <paramref name="mode" /> specifies regular expression matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching value names formatted as a string list, or null if the
        /// operation is not permitted or fails.
        /// </returns>
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

        /// <summary>
        /// This method returns the value names and their values within the
        /// exposed sub-key, optionally filtered by a pattern, formatted as a
        /// string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the value names.  If this value is null,
        /// all value names are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching against <paramref name="pattern" /> should be
        /// case-insensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching value names and values formatted as a string, or null
        /// if the operation is not permitted or fails.
        /// </returns>
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
        /// <summary>
        /// This method adds an array variable to the specified interpreter that
        /// is backed by this registry variable via a variable trace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to which the array variable should be added.
        /// </param>
        /// <param name="name">
        /// The name of the array variable to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// configuration of this registry variable, for introspection purposes.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs describing this registry variable.
        /// </returns>
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
        /// <summary>
        /// This method returns a string representation of this registry
        /// variable.
        /// </summary>
        /// <returns>
        /// A string representation of this registry variable.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Members
        #region Constructor Helper Methods
        /// <summary>
        /// This method sets the root key and its ownership flag for this
        /// instance.
        /// </summary>
        /// <param name="rootKey">
        /// The registry key to retain as the root key.
        /// </param>
        /// <param name="rootKeyOwned">
        /// Non-zero if this instance owns <paramref name="rootKey" /> and is
        /// responsible for closing it.
        /// </param>
        private void SetRootKey(
            RegistryKey rootKey, /* in */
            bool rootKeyOwned    /* in */
            )
        {
            this.rootKey = rootKey;
            this.rootKeyOwned = rootKeyOwned;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified root key, if it is owned, and sets
        /// the reference to null.
        /// </summary>
        /// <param name="rootKey">
        /// The registry key to close.  Upon return, this is set to null.
        /// </param>
        /// <param name="rootKeyOwned">
        /// Non-zero if <paramref name="rootKey" /> is owned and should
        /// therefore be closed.
        /// </param>
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

        /// <summary>
        /// This method opens a (possibly read-only) clone of the specified root
        /// key, stores it on the specified instance, and closes the original
        /// key if it was owned.
        /// </summary>
        /// <param name="registryVariable">
        /// The instance on which the cloned root key should be stored.
        /// </param>
        /// <param name="rootKey">
        /// The registry key to clone.  Upon return, the original key is closed
        /// (if owned) and the reference is set to null.
        /// </param>
        /// <param name="rootKeyOwned">
        /// Non-zero if <paramref name="rootKey" /> is owned and should
        /// therefore be closed after cloning.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the cloned key should be opened for read-only access.
        /// </param>
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
        /// <summary>
        /// This method determines whether the configured permissions include
        /// the specified breakpoint type flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The breakpoint type flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero if all of the specified flags must be present; zero if any
        /// of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the configured permissions include the specified flags;
        /// otherwise, false.
        /// </returns>
        private bool HasFlags(
            BreakpointType hasFlags, /* in */
            bool all                 /* in */
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the number of values within the exposed sub-key to
        /// the specified running count.
        /// </summary>
        /// <param name="count">
        /// The running count of values.  Upon success, the number of values in
        /// the sub-key is added to this value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method collects the value names within the exposed sub-key,
        /// optionally filtered by a pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare value names against
        /// <paramref name="pattern" />.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the value names.  If this value is null,
        /// all value names are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching against <paramref name="pattern" /> should be
        /// case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when
        /// <paramref name="mode" /> specifies regular expression matching.
        /// </param>
        /// <param name="names">
        /// The list of value names.  Upon success, the matching value names are
        /// added to this list, which is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines the registry value options to use when
        /// reading values, based on the configured environment name expansion
        /// behavior.
        /// </summary>
        /// <returns>
        /// The registry value options to use when reading values.
        /// </returns>
        private RegistryValueOptions GetValueOptions()
        {
            return expandString ? RegistryValueOptions.None :
                RegistryValueOptions.DoNotExpandEnvironmentNames;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single value from the exposed sub-key,
        /// discarding any error information.
        /// </summary>
        /// <param name="varIndex">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="defaultValue">
        /// The value to return if the named value does not exist.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was read, or
        /// <paramref name="defaultValue" /> if the named value does not exist.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method reads a single value from the exposed sub-key.
        /// </summary>
        /// <param name="varIndex">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="defaultValue">
        /// The value to return if the named value does not exist.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was read, or
        /// <paramref name="defaultValue" /> if the named value does not exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method writes a single value to the exposed sub-key, honoring
        /// the configured value kind preferences.
        /// </summary>
        /// <param name="varIndex">
        /// The name of the registry value to write.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method deletes a single value from the exposed sub-key.
        /// </summary>
        /// <param name="varName">
        /// The name of the array variable, used when formatting error messages.
        /// </param>
        /// <param name="varIndex">
        /// The name of the registry value to delete.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method reads a single value from the exposed sub-key,
        /// accumulating any errors into a list rather than overwriting a single
        /// error.  It is intended for use only by the
        /// <c>GetNamesAndMaybeValues</c> method.
        /// </summary>
        /// <param name="varIndex">
        /// The name of the registry value to read.
        /// </param>
        /// <param name="defaultValue">
        /// The sentinel value used to detect a missing value.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to record an error when the named value does not exist.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to stop and return failure when an error is recorded; zero
        /// to record the error and continue.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was read.  If the named value
        /// does not exist, this value is left unchanged.
        /// </param>
        /// <param name="errors">
        /// The list of accumulated errors.  Any error encountered is added to
        /// this list, which is created if necessary.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method collects the value names within the exposed sub-key
        /// and, optionally, their values, into a dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation, if any.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare value names against
        /// <paramref name="pattern" />.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the value names.  If this value is null,
        /// all value names are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching against <paramref name="pattern" /> should be
        /// case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when
        /// <paramref name="mode" /> specifies regular expression matching.
        /// </param>
        /// <param name="getValues">
        /// Non-zero to also read the value associated with each name; zero to
        /// collect only the names (with null values).
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to record an error when a named value does not exist.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to stop and return failure when an error is recorded; zero
        /// to record the error and continue.
        /// </param>
        /// <param name="values">
        /// The dictionary of names and values.  Upon success, the matching
        /// names (and values, if requested) are added to this dictionary, which
        /// is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method is the variable trace callback that maps interpreter
        /// array operations onto registry reads, writes, and deletes.  It
        /// handles get, set, and unset operations on array elements, as well as
        /// removal of the entire array, subject to the configured permissions.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of variable operation that triggered this trace.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter in which the variable operation is taking place.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information describing the variable operation.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the value read or an empty string; upon
        /// failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// Stores a value indicating whether this registry variable has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this registry variable has
        /// already been disposed.  It is called at the start of most members to
        /// guard against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this registry variable has been disposed and the engine
        /// is configured to throw on use of a disposed object.
        /// </exception>
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

        /// <summary>
        /// This method releases the resources held by this registry variable.
        /// It implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this registry variable
        /// and suppresses finalization.
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
        /// Finalizes this registry variable, releasing any resources that were
        /// not released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~RegistryVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
