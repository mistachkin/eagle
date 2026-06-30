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
    /// <summary>
    /// This class represents an array variable whose elements are backed by
    /// the rows of a table in a SQL database.  It is attached to an Eagle
    /// array variable as a variable trace callback; element get, set, and
    /// unset operations on the array are translated into SELECT, INSERT,
    /// UPDATE, and DELETE statements executed against the configured
    /// database connection.  The supported operations are governed by the
    /// associated <see cref="DbVariableFlags" /> and
    /// <see cref="BreakpointType" /> permissions.
    /// </summary>
    [ObjectId("3d4f0e30-9aaf-485e-8d5a-c2e2325ecfef")]
    public sealed class DatabaseVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ISupportVariable, ITypeAndName, IDisposable
    {
        #region Private Constants
        #region IDbDataParameter Names
        /// <summary>
        /// The bound parameter name used for the row identifier value in
        /// generated SQL command text.
        /// </summary>
        private static readonly string RowIdParameterName = "@rowId";

        /// <summary>
        /// The bound parameter name used for the variable name (element name)
        /// value in generated SQL command text.
        /// </summary>
        private static readonly string NameParameterName = "@name";

        /// <summary>
        /// The bound parameter name used for the variable value (element
        /// value) in generated SQL command text.
        /// </summary>
        private static readonly string ValueParameterName = "@value";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Database Column Names
        //
        // NOTE: This is the primary column name for the row identifier used
        //       by Oracle.
        //
        /// <summary>
        /// The column name for the row identifier used by Oracle.
        /// </summary>
        private static readonly string OracleRowIdColumnName = "ROWID";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the column name for the row identifier used by SQL
        //       Server.
        //
        /// <summary>
        /// The column name for the row identifier used by SQL Server.
        /// </summary>
        private static readonly string SqlRowIdColumnName = "$IDENTITY";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the primary column name for the row identifier used
        //       by SQLite.
        //
        /// <summary>
        /// The column name for the row identifier used by SQLite.
        /// </summary>
        private static readonly string SQLiteRowIdColumnName = "rowid";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQL DML Statements
        //
        // NOTE: This is used to return a count of variables.  It must work
        //       with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to return a count of variables.
        /// It must work with any SQL database.
        /// </summary>
        private static readonly string SelectCountCommandText =
            "SELECT COUNT(*) FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names.  It must
        //       work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to return a single column (the
        /// list of variable names) for all rows.  It must work with any SQL
        /// database.
        /// </summary>
        private static readonly string SelectOneForAllCommandText =
            "SELECT {1} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names and their
        //       values.  It must work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to return two columns (the list
        /// of variable names and their values) for all rows.  It must work
        /// with any SQL database.
        /// </summary>
        private static readonly string SelectTwoForAllCommandText =
            "SELECT {1}, {2} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to return a single column value
        /// for a matching row.  It must work with any SQL database.
        /// </summary>
        private static readonly string SelectCommandText =
            "SELECT {0} FROM {1} WHERE {2} = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with SQLite.
        //
        /// <summary>
        /// The SQL command text template used to return a single column value
        /// for a matching row, casting the matched column to text.  It must
        /// work with SQLite.
        /// </summary>
        private static readonly string SelectWhereCastCommandText =
            "SELECT {0} FROM {1} WHERE CAST({2} AS TEXT) = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to check whether a single
        /// matching row exists.  It must work with any SQL database.
        /// </summary>
        private static readonly string SelectExistCommandText =
            "SELECT 1 FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with SQLite.
        //
        /// <summary>
        /// The SQL command text template used to check whether a single
        /// matching row exists, casting the matched column to text.  It must
        /// work with SQLite.
        /// </summary>
        private static readonly string SelectExistWhereCastCommandText =
            "SELECT 1 FROM {0} WHERE CAST({1} AS TEXT) = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to insert a single row with two
        /// columns, one for the new variable name and one for the new variable
        /// value.  It must work with any SQL database.
        /// </summary>
        private static readonly string InsertCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4});";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with SQLite.
        //
        /// <summary>
        /// The SQL command text template used to insert a single row with two
        /// columns, one for the new variable name and one for the new variable
        /// value (cast to text).  It must work with SQLite.
        /// </summary>
        private static readonly string InsertWhereCastCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, CAST({4} AS TEXT));";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with any SQL database.
        //
        /// <summary>
        /// The SQL command text template used to update a single row with two
        /// columns, one for the existing variable name and one for the new
        /// variable value.  It must work with any SQL database.
        /// </summary>
        private static readonly string UpdateCommandText =
            "UPDATE {0} SET {1} = {3} WHERE {2} = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with SQLite.
        //
        /// <summary>
        /// The SQL command text template used to update a single row with two
        /// columns, one for the existing variable name (matched as text) and
        /// one for the new variable value.  It must work with SQLite.
        /// </summary>
        private static readonly string UpdateWhereCastCommandText =
            "UPDATE {0} SET {1} = {3} WHERE CAST({2} AS TEXT) = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name.  It must work with any SQL
        //       database.
        //
        /// <summary>
        /// The SQL command text template used to delete a single row matched
        /// by the existing variable name.  It must work with any SQL database.
        /// </summary>
        private static readonly string DeleteCommandText =
            "DELETE FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name [to be matched against].  It must
        //       work with SQLite.
        //
        /// <summary>
        /// The SQL command text template used to delete a single row matched
        /// by the existing variable name (matched as text).  It must work with
        /// SQLite.
        /// </summary>
        private static readonly string DeleteWhereCastCommandText =
            "DELETE FROM {0} WHERE CAST({1} AS TEXT) = {2};";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a database-backed array variable from the fully
        /// specified set of database, connection, and column parameters.
        /// </summary>
        /// <param name="dbVariableFlags">
        /// The flags that control which database operations (SELECT, INSERT,
        /// UPDATE, DELETE) are permitted on this variable.
        /// </param>
        /// <param name="dbConnectionType">
        /// The type of database connection to create when accessing the
        /// backing table.
        /// </param>
        /// <param name="publicKeyToken">
        /// The optional public key token used to locate the database provider
        /// assembly.  This parameter may be null.
        /// </param>
        /// <param name="assemblyFileName">
        /// The optional file name of the assembly that contains the database
        /// provider type.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The optional type name of the database connection type to create.
        /// This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The optional database connection type to create.  This parameter
        /// may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string used to open the database connection.
        /// </param>
        /// <param name="tableName">
        /// The name of the database table that backs the array variable.
        /// </param>
        /// <param name="nameColumnName">
        /// The name of the column that holds the variable (element) names.
        /// </param>
        /// <param name="valueColumnName">
        /// The name of the column that holds the variable (element) values.
        /// </param>
        /// <param name="permissions">
        /// The breakpoint-based permissions that control which variable trace
        /// operations are allowed.
        /// </param>
        /// <param name="useRowId">
        /// Non-zero to match rows using the database-specific row identifier
        /// column when possible; otherwise, the name column is used.
        /// </param>
        private DatabaseVariable(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            byte[] publicKeyToken,
            string assemblyFileName,
            string typeName,
            Type type,
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
            this.publicKeyToken = publicKeyToken;
            this.assemblyFileName = assemblyFileName;
            this.typeName = typeName;
            this.type = type;
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
        /// <summary>
        /// This method creates a new database-backed array variable from the
        /// fully specified set of database, connection, and column parameters.
        /// </summary>
        /// <param name="dbVariableFlags">
        /// The flags that control which database operations (SELECT, INSERT,
        /// UPDATE, DELETE) are permitted on this variable.
        /// </param>
        /// <param name="dbConnectionType">
        /// The type of database connection to create when accessing the
        /// backing table.
        /// </param>
        /// <param name="publicKeyToken">
        /// The optional public key token used to locate the database provider
        /// assembly.  This parameter may be null.
        /// </param>
        /// <param name="assemblyFileName">
        /// The optional file name of the assembly that contains the database
        /// provider type.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The optional type name of the database connection type to create.
        /// This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The optional database connection type to create.  This parameter
        /// may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string used to open the database connection.
        /// </param>
        /// <param name="tableName">
        /// The name of the database table that backs the array variable.
        /// </param>
        /// <param name="nameColumnName">
        /// The name of the column that holds the variable (element) names.
        /// </param>
        /// <param name="valueColumnName">
        /// The name of the column that holds the variable (element) values.
        /// </param>
        /// <param name="permissions">
        /// The breakpoint-based permissions that control which variable trace
        /// operations are allowed.
        /// </param>
        /// <param name="useRowId">
        /// Non-zero to match rows using the database-specific row identifier
        /// column when possible; otherwise, the name column is used.
        /// </param>
        /// <returns>
        /// The newly created <see cref="DatabaseVariable" /> instance.
        /// </returns>
        public static DatabaseVariable Create(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            byte[] publicKeyToken,
            string assemblyFileName,
            string typeName,
            Type type,
            string connectionString,
            string tableName,
            string nameColumnName,
            string valueColumnName,
            BreakpointType permissions,
            bool useRowId
            )
        {
            return new DatabaseVariable(
                dbVariableFlags, dbConnectionType, publicKeyToken,
                assemblyFileName, typeName, type, connectionString,
                tableName, nameColumnName, valueColumnName,
                permissions, useRowId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        /// <summary>
        /// The flags that control which database operations are permitted on
        /// this variable.
        /// </summary>
        private DbVariableFlags dbVariableFlags;

        /// <summary>
        /// Gets the flags that control which database operations are permitted
        /// on this variable.
        /// </summary>
        public DbVariableFlags DbVariableFlags
        {
            get { CheckDisposed(); return dbVariableFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of database connection to create when accessing the
        /// backing table.
        /// </summary>
        private DbConnectionType dbConnectionType;

        /// <summary>
        /// Gets the type of database connection to create when accessing the
        /// backing table.
        /// </summary>
        public DbConnectionType DbConnectionType
        {
            get { CheckDisposed(); return dbConnectionType; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional public key token used to locate the database provider
        /// assembly.
        /// </summary>
        private byte[] publicKeyToken;

        /// <summary>
        /// Gets the optional public key token used to locate the database
        /// provider assembly.
        /// </summary>
        public byte[] PublicKeyToken
        {
            get { CheckDisposed(); return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional file name of the assembly that contains the database
        /// provider type.
        /// </summary>
        private string assemblyFileName;

        /// <summary>
        /// Gets the optional file name of the assembly that contains the
        /// database provider type.
        /// </summary>
        public string AssemblyFileName
        {
            get { CheckDisposed(); return assemblyFileName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The connection string used to open the database connection.
        /// </summary>
        private string connectionString;

        /// <summary>
        /// Gets the connection string used to open the database connection.
        /// </summary>
        public string ConnectionString
        {
            get { CheckDisposed(); return connectionString; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the database table that backs the array variable.
        /// </summary>
        private string tableName;

        /// <summary>
        /// Gets the name of the database table that backs the array variable.
        /// </summary>
        public string TableName
        {
            get { CheckDisposed(); return tableName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the column that holds the variable (element) names.
        /// </summary>
        private string nameColumnName;

        /// <summary>
        /// Gets the name of the column that holds the variable (element)
        /// names.
        /// </summary>
        public string NameColumnName
        {
            get { CheckDisposed(); return nameColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the column that holds the variable (element) values.
        /// </summary>
        private string valueColumnName;

        /// <summary>
        /// Gets the name of the column that holds the variable (element)
        /// values.
        /// </summary>
        public string ValueColumnName
        {
            get { CheckDisposed(); return valueColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The breakpoint-based permissions that control which variable trace
        /// operations are allowed.
        /// </summary>
        private BreakpointType permissions;

        /// <summary>
        /// Gets the breakpoint-based permissions that control which variable
        /// trace operations are allowed.
        /// </summary>
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero to match rows using the database-specific row identifier
        /// column when possible; otherwise, the name column is used.
        /// </summary>
        private bool useRowId;

        /// <summary>
        /// Gets a value indicating whether rows are matched using the
        /// database-specific row identifier column when possible.
        /// </summary>
        public bool UseRowId
        {
            get { CheckDisposed(); return useRowId; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached database-specific row identifier column name, determined
        /// lazily based on the connection type.
        /// </summary>
        private string rowIdColumnName;

        /// <summary>
        /// Gets the database-specific row identifier column name, if it has
        /// been determined.
        /// </summary>
        public string RowIdColumnName
        {
            get { CheckDisposed(); return rowIdColumnName; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        /// <summary>
        /// This method adds an array variable to the specified interpreter and
        /// attaches this object as its variable trace callback so that element
        /// access is routed to the backing database table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to which the array variable will be added.
        /// </param>
        /// <param name="name">
        /// The name of the array variable to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
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
        /// <summary>
        /// This method produces a list of name/value pairs that describe the
        /// configuration of this object, suitable for introspection.
        /// </summary>
        /// <returns>
        /// A <see cref="StringPairList" /> containing the configuration of
        /// this object.
        /// </returns>
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

        #region ITypeAndName Members
        /// <summary>
        /// The optional type name of the database connection type to create.
        /// </summary>
        private string typeName;

        /// <summary>
        /// Gets the optional type name of the database connection type to
        /// create.  Setting this property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public string TypeName
        {
            get { CheckDisposed(); return typeName; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional database connection type to create.
        /// </summary>
        private Type type;

        /// <summary>
        /// Gets the optional database connection type to create.  Setting this
        /// property is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        public Type Type
        {
            get { CheckDisposed(); return type; }
            set { throw new NotSupportedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISupportVariable Members
        /// <summary>
        /// This method determines whether a variable (element) with the
        /// specified name exists in the backing database table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="name">
        /// The name of the variable (element) to check for.
        /// </param>
        /// <returns>
        /// True if a matching row exists; otherwise, false.
        /// </returns>
        public bool DoesExist(
            Interpreter interpreter,
            string name
            )
        {
            CheckDisposed();

            return DoesExistViaSelect(interpreter, name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of variables (elements) stored in
        /// the backing database table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The count of variables, or null if the count could not be obtained.
        /// </returns>
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

        /// <summary>
        /// This method returns a dictionary of the variables (elements) stored
        /// in the backing database table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the variable names in the resulting dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the variable values in the resulting
        /// dictionary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// An <see cref="ObjectDictionary" /> of the requested variables, or
        /// null if the list could not be obtained.
        /// </returns>
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

        /// <summary>
        /// This method returns a dictionary of the variables (elements) stored
        /// in the backing database table.  The pattern matching parameters are
        /// accepted for interface compatibility but are not used.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="pattern">
        /// The match pattern.  This parameter is not used.
        /// </param>
        /// <param name="noCase">
        /// Non-zero for case-insensitive matching.  This parameter is not
        /// used.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the variable names in the resulting dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the variable values in the resulting
        /// dictionary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// An <see cref="ObjectDictionary" /> of the requested variables, or
        /// null if the list could not be obtained.
        /// </returns>
        public ObjectDictionary GetList(
            Interpreter interpreter, /* in */
            string pattern,          /* in: NOT USED */
            bool noCase,             /* in: NOT USED */
            bool names,              /* in */
            bool values,             /* in */
            ref Result error         /* out */
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

        /// <summary>
        /// This method returns a string containing the variable (element)
        /// names stored in the backing database table, optionally filtered by
        /// the specified match criteria.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when filtering the names.
        /// </param>
        /// <param name="pattern">
        /// The match pattern to use when filtering the names.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero for case-insensitive matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular-expression based.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// A string containing the matching variable names, or null if the
        /// names could not be obtained.
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
                    Characters.SpaceString, null, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string containing the variable (element)
        /// names and their values stored in the backing database table,
        /// optionally filtered by the specified match criteria.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="pattern">
        /// The match pattern to use when filtering the names.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero for case-insensitive matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// A string containing the matching variable names and values, or null
        /// if they could not be obtained.
        /// </returns>
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
                    Characters.SpaceString, null, false);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this object.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Members
        #region Connection Helper Methods
        /// <summary>
        /// This method creates and returns a database connection using the
        /// configured connection settings, discarding any error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <returns>
        /// The new database connection, or null if it could not be created.
        /// </returns>
        private IDbConnection CreateDbConnection(
            Interpreter interpreter
            )
        {
            Result error = null;

            return CreateDbConnection(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and returns a database connection using the
        /// configured connection settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The new database connection, or null if it could not be created.
        /// </returns>
        private IDbConnection CreateDbConnection(
            Interpreter interpreter,
            ref Result error
            )
        {
            IDbConnection connection = null;

            if (DataOps.CreateDbConnection(
                    interpreter, dbConnectionType, publicKeyToken,
                    connectionString, assemblyFileName, typeName,
                    typeName, type,
                    ObjectOps.GetDefaultObjectValueFlags(),
                    ref connection, ref error) == ReturnCode.Ok)
            {
                return connection;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Column Name Helper Methods
        /// <summary>
        /// This method determines and caches the database-specific row
        /// identifier column name based on the configured connection type.
        /// </summary>
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
                case DbConnectionType.SQLiteEnterprise:
                    {
                        rowIdColumnName = SQLiteRowIdColumnName;
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Command Text Helper Methods
        /// <summary>
        /// This method returns the SQL command text template used to count the
        /// variables in the backing table.
        /// </summary>
        /// <returns>
        /// The SQL command text template used to count variables.
        /// </returns>
        private static string GetVariableCountCommandText()
        {
            return SelectCountCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the SQL command text template used to list the
        /// variables in the backing table, based on whether names and/or
        /// values are requested.
        /// </summary>
        /// <param name="names">
        /// Non-zero if the variable names are requested.
        /// </param>
        /// <param name="values">
        /// Non-zero if the variable values are requested.
        /// </param>
        /// <returns>
        /// The SQL command text template used to list variables, or null if
        /// neither names nor values are requested.
        /// </returns>
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
        /// <summary>
        /// This method returns the SQL command text template used to select a
        /// single column value for a matching row, choosing the appropriate
        /// template for the configured connection type.
        /// </summary>
        /// <returns>
        /// The SQL command text template used to select a single value.
        /// </returns>
        private string GetSelectCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if ((dbConnectionType == DbConnectionType.SQLite) ||
                (dbConnectionType == DbConnectionType.SQLiteEnterprise))
            {
                return SelectWhereCastCommandText;
            }
            else
            {
                return SelectCommandText;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the SQL command text template used to check
        /// whether a matching row exists, choosing the appropriate template
        /// for the configured connection type.
        /// </summary>
        /// <returns>
        /// The SQL command text template used to check for existence.
        /// </returns>
        private string GetVariableExistCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if ((dbConnectionType == DbConnectionType.SQLite) ||
                (dbConnectionType == DbConnectionType.SQLiteEnterprise))
            {
                return SelectExistWhereCastCommandText;
            }
            else
            {
                return SelectExistCommandText;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the SQL command text template used to get the
        /// value of a single variable.
        /// </summary>
        /// <returns>
        /// The SQL command text template used to get a variable value.
        /// </returns>
        private string GetVariableGetCommandText()
        {
            return GetSelectCommandText();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the SQL command text template used to set the
        /// value of a single variable, choosing an UPDATE or INSERT template
        /// (and the appropriate variant for the connection type) based on
        /// whether the variable already exists.
        /// </summary>
        /// <param name="exists">
        /// Non-zero if the variable already exists (an UPDATE is required);
        /// otherwise, an INSERT is required.
        /// </param>
        /// <returns>
        /// The SQL command text template used to set a variable value.
        /// </returns>
        private string GetVariableSetCommandText(
            bool exists
            )
        {
            //
            // TODO: Add support for more database backends here.
            //
            if ((dbConnectionType == DbConnectionType.SQLite) ||
                (dbConnectionType == DbConnectionType.SQLiteEnterprise))
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

        /// <summary>
        /// This method returns the SQL command text template used to unset
        /// (delete) a single variable, choosing the appropriate template for
        /// the configured connection type.
        /// </summary>
        /// <returns>
        /// The SQL command text template used to unset a variable.
        /// </returns>
        private string GetVariableUnsetCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if ((dbConnectionType == DbConnectionType.SQLite) ||
                (dbConnectionType == DbConnectionType.SQLiteEnterprise))
            {
                return DeleteWhereCastCommandText;
            }
            else
            {
                return DeleteCommandText;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Variable Operation Helper Methods
        //
        // TODO: This method is not allowed to "fail"?  This seems like a
        //       design flaw.
        //
        /// <summary>
        /// This method determines whether a variable (element) with the
        /// specified name exists by executing a SELECT statement against the
        /// backing table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="name">
        /// The name of the variable (element) to check for.
        /// </param>
        /// <returns>
        /// True if a matching row exists; otherwise, false.
        /// </returns>
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
                        TracePriority.DataError2);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the database-specific row identifier for the
        /// variable (element) with the specified name by executing a SELECT
        /// statement against the backing table.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="rowIdColumnName">
        /// The name of the row identifier column to select.  This parameter
        /// may be null, in which case no lookup is performed.
        /// </param>
        /// <param name="name">
        /// The name of the variable (element) whose row identifier is sought.
        /// </param>
        /// <param name="rowId">
        /// Upon success, this parameter will be modified to contain the row
        /// identifier of the matching row.
        /// </param>
        /// <returns>
        /// True if a matching row was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the number of variables (elements) in the
        /// backing table by executing a SELECT COUNT statement.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="count">
        /// Upon success, this parameter will be modified to contain the count
        /// of variables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the variables (elements) in the backing table
        /// by executing a SELECT statement, populating a dictionary with the
        /// requested names and/or values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the variable names in the resulting dictionary.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the variable values in the resulting
        /// dictionary.
        /// </param>
        /// <param name="dictionary">
        /// Upon success, this parameter will be modified to contain the
        /// requested variables.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
        /// </returns>
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
        /// <summary>
        /// This method validates that the specified property value is a legal
        /// SQL identifier, throwing an exception when it is not.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property being validated, used in any resulting
        /// error message.
        /// </param>
        /// <param name="propertyValue">
        /// The property value to validate as a SQL identifier.
        /// </param>
        private static void CheckIdentifier(
            string propertyName,
            string propertyValue
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates that the specified property value is a legal
        /// SQL identifier (or a well-known parameter name), throwing an
        /// exception when it is not.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property being validated, used in any resulting
        /// error message.
        /// </param>
        /// <param name="propertyValue">
        /// The property value to validate as a SQL identifier.
        /// </param>
        /// <param name="isParameterName">
        /// Non-zero if the value is a bound parameter name; in that case, a
        /// value matching one of the well-known parameter names is exempt from
        /// the regular expression check.
        /// </param>
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
        /// <summary>
        /// This method determines whether the configured database variable
        /// flags include the specified flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are present;
        /// zero to require that any of them are present.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        private bool HasFlags(
            DbVariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(dbVariableFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the configured permissions include
        /// the specified breakpoint flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The breakpoint flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are present;
        /// zero to require that any of them are present.
        /// </param>
        /// <returns>
        /// True if the required flags are present; otherwise, false.
        /// </returns>
        private bool HasFlags(
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the database variable flags permit the
        /// operation associated with the specified breakpoint type, throwing
        /// an exception when the operation is forbidden.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type identifying the operation being attempted.
        /// </param>
        private void CheckTraceAccess(
            BreakpointType breakpointType
            ) /* throw */
        {
            CheckTraceAccess(breakpointType, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the database variable flags permit the
        /// operation associated with the specified breakpoint type, throwing
        /// an exception when the operation is forbidden.  For set operations,
        /// the existence of the row determines whether INSERT or UPDATE
        /// permission is required.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type identifying the operation being attempted.
        /// </param>
        /// <param name="exists">
        /// For set operations, indicates whether the row already exists (an
        /// UPDATE) or not (an INSERT).  This parameter may be null when the
        /// existence is unknown, in which case both permissions are checked.
        /// </param>
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

        /// <summary>
        /// This method determines the SQL command text and the WHERE clause
        /// column, parameter type, parameter name, and parameter value to use
        /// for the operation associated with the specified breakpoint type.
        /// It also enforces the relevant access permissions.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type identifying the operation being attempted.
        /// </param>
        /// <param name="name">
        /// The name of the variable (element) being operated upon.
        /// </param>
        /// <param name="commandText">
        /// Upon return, this parameter will be modified to contain the SQL
        /// command text template to use.
        /// </param>
        /// <param name="whereColumnName">
        /// Upon return, this parameter will be modified to contain the name of
        /// the column used in the WHERE clause.
        /// </param>
        /// <param name="whereParameterDbType">
        /// Upon return, this parameter will be modified to contain the
        /// database type of the WHERE clause parameter, or null to use the
        /// default.
        /// </param>
        /// <param name="whereParameterName">
        /// Upon return, this parameter will be modified to contain the bound
        /// parameter name used in the WHERE clause.
        /// </param>
        /// <param name="whereParameterValue">
        /// Upon return, this parameter will be modified to contain the value
        /// of the WHERE clause parameter.
        /// </param>
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
        /// <summary>
        /// This method is the variable trace callback that routes array
        /// element get, set, and unset operations to the backing database
        /// table.  Operations on the entire array (when no element index is
        /// present) other than unset are not supported.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type identifying the variable operation being
        /// performed.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information describing the variable, element index, and
        /// value involved in the operation.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will be modified to contain the value
        /// produced by the operation, or an appropriate error message on
        /// failure.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code.
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
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws <see cref="ObjectDisposedException" /> if this
        /// object has been disposed and the engine is configured to throw on
        /// access to disposed objects.
        /// </summary>
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

        /// <summary>
        /// This method releases the resources used by this object.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method (and managed resources should be
        /// released); zero if it is being called from the finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources used by this object.
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
        /// Finalizes this object, releasing any unmanaged resources.
        /// </summary>
        ~DatabaseVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
