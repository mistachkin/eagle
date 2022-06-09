/*
 * DatabaseVariable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Data;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    [ObjectId("3d4f0e30-9aaf-485e-8d5a-c2e2325ecfef")]
    public sealed class DatabaseVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constants
        #region IDbDataParameter Names
        private static readonly string RowIdParameterName = "@rowId";
        private static readonly string NameParameterName = "@name";
        private static readonly string ValueParameterName = "@value";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Database Column Names
        //
        // NOTE: This is the primary column name for the row identifier used
        //       by Oracle.
        //
        private static readonly string OracleRowIdColumnName = "ROWID";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the column name for the row identifier used by SQL
        //       Server.
        //
        private static readonly string SqlRowIdColumnName = "$IDENTITY";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the primary column name for the row identifier used
        //       by SQLite.
        //
        private static readonly string SQLiteRowIdColumnName = "rowid";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQL DML Statements
        //
        // NOTE: This is used to return a count of variables.  It must work
        //       with any SQL database.
        //
        private static readonly string SelectCountCommandText =
            "SELECT COUNT(*) FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names.  It must
        //       work with any SQL database.
        //
        private static readonly string SelectOneForAllCommandText =
            "SELECT {1} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names and their
        //       values.  It must work with any SQL database.
        //
        private static readonly string SelectTwoForAllCommandText =
            "SELECT {1}, {2} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with any SQL database.
        //
        private static readonly string SelectCommandText =
            "SELECT {0} FROM {1} WHERE {2} = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with SQLite.
        //
        private static readonly string SelectWhereCastCommandText =
            "SELECT {0} FROM {1} WHERE CAST({2} AS TEXT) = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with any SQL database.
        //
        private static readonly string SelectExistCommandText =
            "SELECT 1 FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with SQLite.
        //
        private static readonly string SelectExistWhereCastCommandText =
            "SELECT 1 FROM {0} WHERE CAST({1} AS TEXT) = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with any SQL database.
        //
        private static readonly string InsertCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4});";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with SQLite.
        //
        private static readonly string InsertWhereCastCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, CAST({4} AS TEXT));";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with any SQL database.
        //
        private static readonly string UpdateCommandText =
            "UPDATE {0} SET {1} = {3} WHERE {2} = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with SQLite.
        //
        private static readonly string UpdateWhereCastCommandText =
            "UPDATE {0} SET {1} = {3} WHERE CAST({2} AS TEXT) = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name.  It must work with any SQL
        //       database.
        //
        private static readonly string DeleteCommandText =
            "DELETE FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name [to be matched against].  It must
        //       work with SQLite.
        //
        private static readonly string DeleteWhereCastCommandText =
            "DELETE FROM {0} WHERE CAST({1} AS TEXT) = {2};";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private DatabaseVariable(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            string assemblyFileName,
            string typeName,
            string connectionString,
            string tableName,
            string nameColumnName,
            string valueColumnName,
            BreakpointType permissions,
            bool useRowId
            )
        {
            this.dbVariableFlags = dbVariableFlags;
            this.dbConnectionType = dbConnectionType;
            this.assemblyFileName = assemblyFileName;
            this.typeName = typeName;
            this.connectionString = connectionString;
            this.tableName = tableName;
            this.nameColumnName = nameColumnName;
            this.valueColumnName = valueColumnName;
            this.permissions = permissions;
            this.useRowId = useRowId;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static DatabaseVariable Create(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            string assemblyFileName,
            string typeName,
            string connectionString,
            string tableName,
            string nameColumnName,
            string valueColumnName,
            BreakpointType permissions,
            bool useRowId
            )
        {
            return new DatabaseVariable(
                dbVariableFlags, dbConnectionType, assemblyFileName,
                typeName, connectionString, tableName, nameColumnName,
                valueColumnName, permissions, useRowId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        private DbVariableFlags dbVariableFlags;
        public DbVariableFlags DbVariableFlags
        {
            get { CheckDisposed(); return dbVariableFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DbConnectionType dbConnectionType;
        public DbConnectionType DbConnectionType
        {
            get { CheckDisposed(); return dbConnectionType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string assemblyFileName;
        public string AssemblyFileName
        {
            get { CheckDisposed(); return assemblyFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string typeName;
        public string TypeName
        {
            get { CheckDisposed(); return typeName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string connectionString;
        public string ConnectionString
        {
            get { CheckDisposed(); return connectionString; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string tableName;
        public string TableName
        {
            get { CheckDisposed(); return tableName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string nameColumnName;
        public string NameColumnName
        {
            get { CheckDisposed(); return nameColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string valueColumnName;
        public string ValueColumnName
        {
            get { CheckDisposed(); return valueColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType permissions;
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useRowId;
        public bool UseRowId
        {
            get { CheckDisposed(); return useRowId; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string rowIdColumnName;
        public string RowIdColumnName
        {
            get { CheckDisposed(); return rowIdColumnName; }
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

            return DoesExistViaSelect(interpreter, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public long? GetCount(
            Interpreter interpreter,
            ref Result error
            )
        {
            CheckDisposed();

            long count = 0;

            if (GetCountViaSelect(
                    interpreter, ref count, ref error) == ReturnCode.Ok)
            {
                return count;
            }

            return null;
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

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, names, values, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }

            return null;
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

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, true, false, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, false, mode, pattern, null,
                    null, null, null, noCase, regExOptions) as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
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

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, true, true, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, true, StringOps.DefaultMatchMode,
                    pattern, null, null, null, null, noCase, RegexOptions.None)
                    as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        public ReturnCode AddVariable(
            Interpreter interpreter,
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

            list.Add("dbVariableFlags", dbVariableFlags.ToString());
            list.Add("dbConnectionType", dbConnectionType.ToString());

            if (assemblyFileName != null)
                list.Add("assemblyFileName", assemblyFileName);

            if (typeName != null)
                list.Add("typeName", typeName);

            if (connectionString != null)
                list.Add("connectionString", connectionString);

            if (tableName != null)
                list.Add("tableName", tableName);

            if (nameColumnName != null)
                list.Add("nameColumnName", nameColumnName);

            if (valueColumnName != null)
                list.Add("valueColumnName", valueColumnName);

            list.Add("permissions", permissions.ToString());
            list.Add("useRowId", useRowId.ToString());

            if (rowIdColumnName != null)
                list.Add("rowIdColumnName", rowIdColumnName);

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
        #region Connection Helper Methods
        private IDbConnection CreateDbConnection(
            Interpreter interpreter
            )
        {
            Result error = null;

            return CreateDbConnection(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private IDbConnection CreateDbConnection(
            Interpreter interpreter,
            ref Result error
            )
        {
            IDbConnection connection = null;

            if (DataOps.CreateDbConnection(
                    interpreter, dbConnectionType,
                    connectionString, assemblyFileName, typeName,
                    typeName, ObjectOps.GetDefaultObjectValueFlags(),
                    ref connection, ref error) == ReturnCode.Ok)
            {
                return connection;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Column Name Helper Methods
        private void GetRowIdColumnName()
        {
            //
            // HACK: For now, we only know how to do this for Oracle, SQL
            //       Server, and SQLite.
            //
            // TODO: Add support for more database backends here.
            //
            //
            switch (dbConnectionType)
            {
                case DbConnectionType.Oracle:
                    {
                        rowIdColumnName = OracleRowIdColumnName;
                        break;
                    }
                case DbConnectionType.Sql:
                    {
                        rowIdColumnName = SqlRowIdColumnName;
                        break;
                    }
                case DbConnectionType.SQLite:
                    {
                        rowIdColumnName = SQLiteRowIdColumnName;
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Command Text Helper Methods
        private static string GetVariableCountCommandText()
        {
            return SelectCountCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetVariableListCommandText(
            bool names,
            bool values
            )
        {
            if (names || values)
            {
                if (names && values)
                    return SelectTwoForAllCommandText;
                else
                    return SelectOneForAllCommandText;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Text Helper Methods
        private string GetSelectCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return SelectWhereCastCommandText;
            else
                return SelectCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableExistCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return SelectExistWhereCastCommandText;
            else
                return SelectExistCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableGetCommandText()
        {
            return GetSelectCommandText();
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableSetCommandText(
            bool exists
            )
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
            {
                return exists ?
                    UpdateWhereCastCommandText : InsertWhereCastCommandText;
            }
            else
            {
                return exists ? UpdateCommandText : InsertCommandText;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableUnsetCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return DeleteWhereCastCommandText;
            else
                return DeleteCommandText;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Variable Operation Helper Methods
        //
        // TODO: This method is not allowed to "fail"?  This seems like a
        //       design flaw.
        //
        private bool DoesExistViaSelect(
            Interpreter interpreter,
            string name
            )
        {
            bool success = false;
            Result error = null;

            try
            {
                if (!HasFlags(BreakpointType.BeforeVariableExist, true))
                {
                    error = "permission denied";
                    return false;
                }

                using (IDbConnection connection = CreateDbConnection(
                        interpreter, ref error))
                {
                    if (connection == null)
                        return false;

                    connection.Open();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        if (command == null)
                        {
                            error = "could not create command";
                            return false;
                        }

                        string commandText = GetVariableExistCommandText();

                        CheckIdentifier("TableName", tableName);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        command.CommandText = DataOps.FormatCommandText(
                            commandText, 1, tableName, nameColumnName,
                            NameParameterName);

                        IDbDataParameter whereParameter =
                            command.CreateParameter();

                        if (whereParameter == null)
                        {
                            error = "could not create where parameter";
                            return false;
                        }

                        whereParameter.ParameterName = NameParameterName;
                        whereParameter.Value = name;

                        command.Parameters.Add(whereParameter);

                        using (IDataReader reader = command.ExecuteReader())
                        {
                            if (reader == null)
                            {
                                error = "could not execute command";
                                return false;
                            }

                            bool result = reader.Read();

                            success = true;
                            return result;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
            finally
            {
                if (!success)
                {
                    TraceOps.DebugTrace(String.Format(
                        "DoesExistViaSelect: error = {0}", error),
                        typeof(DatabaseVariable).Name,
                        TracePriority.DataError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool GetRowIdViaSelect(
            Interpreter interpreter,
            string rowIdColumnName,
            string name,
            ref object rowId
            ) /* throw */
        {
            if (rowIdColumnName == null)
                return false;

            using (IDbConnection connection = CreateDbConnection(
                    interpreter))
            {
                if (connection == null)
                    return false;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                        return false;

                    string commandText = GetSelectCommandText();

                    CheckIdentifier("RowIdColumnName", rowIdColumnName);
                    CheckIdentifier("TableName", tableName);
                    CheckIdentifier("NameColumnName", nameColumnName);

                    command.CommandText = DataOps.FormatCommandText(
                        commandText, 1, rowIdColumnName, tableName,
                        nameColumnName, NameParameterName);

                    IDbDataParameter whereParameter =
                        command.CreateParameter();

                    if (whereParameter == null)
                        return false;

                    whereParameter.ParameterName = NameParameterName;
                    whereParameter.Value = name;

                    command.Parameters.Add(whereParameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return false;

                        bool exists = reader.Read();

                        if (exists)
                            rowId = reader.GetValue(0);

                        return exists;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetCountViaSelect(
            Interpreter interpreter,
            ref long count,
            ref Result error
            )
        {
            if (!HasFlags(BreakpointType.BeforeVariableCount, true))
            {
                error = "permission denied";
                return ReturnCode.Error;
            }

            using (IDbConnection connection = CreateDbConnection(
                    interpreter, ref error))
            {
                if (connection == null)
                    return ReturnCode.Error;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                    {
                        error = "could not create command";
                        return ReturnCode.Error;
                    }

                    string commandText = GetVariableCountCommandText();

                    CheckIdentifier("TableName", tableName);

                    command.CommandText = DataOps.FormatCommandText(
                        commandText, 0, tableName);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                        {
                            error = "could not execute command";
                            return ReturnCode.Error;
                        }

                        if (reader.Read())
                            count = reader.GetInt64(0);
                        else
                            count = 0;
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetListViaSelect(
            Interpreter interpreter,
            bool names,
            bool values,
            ref ObjectDictionary dictionary,
            ref Result error
            )
        {
            BreakpointType breakpointType = ScriptOps.GetBreakpointType(
                names, values);

            if (breakpointType == BreakpointType.None)
                return ReturnCode.Ok;

            if (!HasFlags(breakpointType, true))
            {
                error = "permission denied";
                return ReturnCode.Error;
            }

            using (IDbConnection connection = CreateDbConnection(
                    interpreter, ref error))
            {
                if (connection == null)
                    return ReturnCode.Error;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                    {
                        error = "could not create command";
                        return ReturnCode.Error;
                    }

                    string commandText = GetVariableListCommandText(
                        names, values);

                    CheckIdentifier("TableName", tableName);
                    CheckIdentifier("NameColumnName", nameColumnName);
                    CheckIdentifier("ValueColumnName", valueColumnName);

                    command.CommandText = DataOps.FormatCommandText(
                        commandText, 0, tableName, nameColumnName,
                        valueColumnName);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                        {
                            error = "could not execute command";
                            return ReturnCode.Error;
                        }

                        while (reader.Read())
                        {
                            string name = reader.GetString(0);

                            if (name == null)
                                continue;

                            object value = null;

                            if (reader.FieldCount >= 2)
                                value = reader.GetValue(1);

                            if (dictionary == null)
                                dictionary = new ObjectDictionary();

                            dictionary[name] = value;
                        }
                    }
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Static Helper Methods
        private static void CheckIdentifier(
            string propertyName,
            string propertyValue
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CheckIdentifier(
            string propertyName,
            string propertyValue,
            bool isParameterName
            ) /* throw */
        {
            //
            // HACK: A parameter name is exempt from the regular expression
            //       check in this method as long as it matches one of the
            //       "well-known" (constant) parameter names.
            //
            if (isParameterName)
            {
                if (SharedStringOps.SystemEquals(
                        propertyValue, RowIdParameterName) ||
                    SharedStringOps.SystemEquals(
                        propertyValue, NameParameterName) ||
                    SharedStringOps.SystemEquals(
                        propertyValue, ValueParameterName))
                {
                    return;
                }
            }

            DataOps.CheckIdentifier(
                propertyName, propertyValue, isParameterName);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Helper Methods
        #region Flags Helper Methods
        private bool HasFlags(
            DbVariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(dbVariableFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool HasFlags(
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private void CheckTraceAccess(
            BreakpointType breakpointType
            ) /* throw */
        {
            CheckTraceAccess(breakpointType, null);
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckTraceAccess(
            BreakpointType breakpointType,
            bool? exists
            ) /* throw */
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    {
                        if (!HasFlags(DbVariableFlags.AllowSelect, true))
                            throw new ScriptException("SELECT forbidden");

                        break;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        if (((exists == null) || (!(bool)exists)) &&
                            !HasFlags(DbVariableFlags.AllowInsert, true))
                        {
                            throw new ScriptException("INSERT forbidden");
                        }

                        if (((exists == null) || ((bool)exists)) &&
                            !HasFlags(DbVariableFlags.AllowUpdate, true))
                        {
                            throw new ScriptException("UPDATE forbidden");
                        }

                        break;
                    }
                case BreakpointType.BeforeVariableUnset:
                    {
                        if (!HasFlags(DbVariableFlags.AllowDelete, true))
                            throw new ScriptException("DELETE forbidden");

                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void GetCommandTextAndValues(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string name,
            out string commandText,
            out string whereColumnName,
            out DbType? whereParameterDbType,
            out string whereParameterName,
            out object whereParameterValue
            ) /* throw */
        {
            commandText = null;
            whereColumnName = null;
            whereParameterDbType = null;
            whereParameterName = null;
            whereParameterValue = null;

            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    {
                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            GetRowIdViaSelect(
                                interpreter, rowIdColumnName, name, ref rowId);

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = SelectCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }

                        CheckTraceAccess(breakpointType);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableGetCommandText();
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        bool exists;

                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            if (rowIdColumnName != null)
                            {
                                exists = GetRowIdViaSelect(
                                    interpreter, rowIdColumnName, name,
                                    ref rowId);
                            }
                            else
                            {
                                exists = DoesExistViaSelect(interpreter, name);
                            }

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = UpdateCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }
                        else
                        {
                            exists = DoesExistViaSelect(interpreter, name);
                        }

                        CheckTraceAccess(breakpointType, exists);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableSetCommandText(exists);
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
                case BreakpointType.BeforeVariableUnset:
                    {
                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            GetRowIdViaSelect(
                                interpreter, rowIdColumnName, name, ref rowId);

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = DeleteCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }

                        CheckTraceAccess(breakpointType);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableUnsetCommandText();
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
            }
        }
        #endregion

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
                    using (IDbConnection connection = CreateDbConnection(
                            interpreter, ref result))
                    {
                        if (connection == null)
                            return ReturnCode.Error;

                        connection.Open();

                        using (IDbCommand command = connection.CreateCommand())
                        {
                            if (command == null)
                            {
                                result = "could not create command";
                                return ReturnCode.Error;
                            }

                            switch (breakpointType)
                            {
                                case BreakpointType.BeforeVariableGet:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("ValueColumnName", valueColumnName);
                                        CheckIdentifier("TableName", tableName);

                                        command.CommandText = DataOps.FormatCommandText(
                                            commandText, 1, valueColumnName, tableName,
                                            whereColumnName, whereParameterName);

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(whereParameter);

                                        using (IDataReader reader = command.ExecuteReader())
                                        {
                                            if (reader == null)
                                            {
                                                result = "could not execute command";
                                                return ReturnCode.Error;
                                            }

                                            if (reader.Read())
                                            {
                                                result = StringOps.GetResultFromObject(
                                                    reader.GetValue(0));

                                                traceInfo.ReturnCode = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = FormatOps.ErrorElementName(
                                                    breakpointType, variable.Name,
                                                    traceInfo.Index);

                                                traceInfo.ReturnCode = ReturnCode.Error;
                                            }
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                                case BreakpointType.BeforeVariableSet:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("TableName", tableName);
                                        CheckIdentifier("ValueColumnName", valueColumnName);

                                        command.CommandText = DataOps.FormatCommandText(
                                            commandText, 2, tableName, valueColumnName,
                                            whereColumnName, ValueParameterName,
                                            whereParameterName);

                                        IDbDataParameter valueParameter = command.CreateParameter();

                                        if (valueParameter == null)
                                        {
                                            result = "could not create value parameter";
                                            return ReturnCode.Error;
                                        }

                                        object valueParameterValue = traceInfo.NewValue;

                                        valueParameter.ParameterName = ValueParameterName;
                                        valueParameter.Value = valueParameterValue;

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(valueParameter);
                                        command.Parameters.Add(whereParameter);

                                        if (command.ExecuteNonQuery() > 0) /* Did we do anything? */
                                        {
                                            result = StringOps.GetResultFromObject(
                                                valueParameterValue);

                                            EntityOps.SetUndefined(variable, false);
                                            EntityOps.SetDirty(variable, true);

                                            traceInfo.ReturnCode = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = FormatOps.ErrorElementName(
                                                breakpointType, variable.Name,
                                                traceInfo.Index);

                                            traceInfo.ReturnCode = ReturnCode.Error;
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                                case BreakpointType.BeforeVariableUnset:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("TableName", tableName);

                                        command.CommandText = DataOps.FormatCommandText(
                                            commandText, 1, tableName, whereColumnName,
                                            whereParameterName);

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(whereParameter);

                                        if (command.ExecuteNonQuery() > 0) /* Did we do anything? */
                                        {
                                            result = String.Empty;

                                            EntityOps.SetDirty(variable, true);

                                            traceInfo.ReturnCode = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = FormatOps.ErrorElementName(
                                                breakpointType, variable.Name,
                                                traceInfo.Index);

                                            traceInfo.ReturnCode = ReturnCode.Error;
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                            }
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
                    typeof(DatabaseVariable).Name);
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
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

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
        ~DatabaseVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
