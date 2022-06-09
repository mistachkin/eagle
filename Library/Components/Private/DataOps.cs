/*
 * DataOps.cs --
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

#if !NET_STANDARD_20
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
#endif

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using ConnectionDictionary = Eagle._Containers.Private.DbConnectionTypeStringPairDictionary;

namespace Eagle._Components.Private
{
    [ObjectId("2e72f5b2-15df-4d65-98ec-fa01f3300ac8")]
    internal static class DataOps
    {
        #region Synchronization Objects
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region System.Data.SQLite Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string SQLiteAssemblyFileName =
            "System.Data.SQLite.dll";

        ///////////////////////////////////////////////////////////////////////

        private static string SQLiteFullTypeName =
            "System.Data.SQLite.SQLiteConnection, System.Data.SQLite, " +
            "Version=1.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139";

        ///////////////////////////////////////////////////////////////////////

        private static string SQLiteTypeName =
            "System.Data.SQLite.SQLiteConnection";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Table/Column/Parameter Name Validation Regular Expressions
        //
        // HACK: These are hard-coded for now.  Maybe make these configurable
        //       at some point.
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex parameterRegEx = RegExOps.Create(
            "^[@A-Z_][0-9A-Z_]*$", RegexOptions.IgnoreCase);

        private static Regex identifierRegEx = RegExOps.Create(
            "^[$A-Z_][$0-9A-Z_]*$", RegexOptions.IgnoreCase);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static ConnectionDictionary DbConnectionTypeFullNames;
        private static ConnectionDictionary DbConnectionTypeNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Support Methods
        public static void CheckIdentifier(
            string propertyName,
            string propertyValue
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CheckIdentifier(
            string propertyName,
            string propertyValue,
            bool isParameterName
            ) /* throw */
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName);

            if (isParameterName)
            {
                if (parameterRegEx != null)
                {
                    Match match = parameterRegEx.Match(propertyValue);

                    if ((match == null) || !match.Success)
                    {
                        throw new ArgumentException(String.Format(
                            "value {0} is not a valid database parameter, " +
                            "pattern {1}", FormatOps.WrapOrNull(propertyValue),
                            FormatOps.WrapOrNull(parameterRegEx)),
                            propertyName);
                    }
                }
            }
            else
            {
                if (identifierRegEx != null)
                {
                    Match match = identifierRegEx.Match(propertyValue);

                    if ((match == null) || !match.Success)
                    {
                        throw new ArgumentException(String.Format(
                            "value {0} is not a valid database identifier, " +
                            "pattern {1}", FormatOps.WrapOrNull(propertyValue),
                            FormatOps.WrapOrNull(identifierRegEx)),
                            propertyName);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is used to format the command text for execution
        //       against the target database.  It performs some "last resort"
        //       checks for valid identifiers.  Since all callers should have
        //       already checked their identifier names, this method should
        //       never throw any exceptions.
        //
        // NOTE: The caller is expected to know (and pass) the number of
        //       parameter names that occur as the final (X) parameters.
        //       These parameter names must be valid identifiers unless
        //       they are one of the "well-known" (constant) parameter
        //       names.
        //
        public static string FormatCommandText(
            string format,
            int parameterCount,
            params string[] names
            ) /* throw */
        {
            if (names == null)
                throw new ArgumentNullException("names");

            int length = names.Length;
            int lastIndex = length - 1;

            for (int index = 0; index < length; index++)
            {
                //
                // HACK: This assumes that all parameter names only occur
                //       at the end of the parameter list.  This library
                //       is designed to conform with this assumption.
                //
                bool isParameterName = (parameterCount > 0) &&
                    (index > (lastIndex - parameterCount));

                //
                // NOTE: The property name is unknown at this point.  That
                //       does not matter because they are not used in the
                //       actual command text.
                //
                CheckIdentifier(null, names[index], isParameterName);
            }

            return String.Format(format, names);
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringPair GetStringPairForSQLite(
            bool useFullName,
            bool useFileName
            )
        {
            if (useFullName)
            {
                if (useFileName)
                {
                    return new StringPair(
                        SQLiteFullTypeName, Path.Combine(
                        GlobalState.GetAnyEntryAssemblyPath(),
                        SQLiteAssemblyFileName));
                }
                else
                {
                    return new StringPair(SQLiteFullTypeName);
                }
            }
            else
            {
                if (useFileName)
                {
                    return new StringPair(
                        SQLiteTypeName, Path.Combine(
                        GlobalState.GetAnyEntryAssemblyPath(),
                        SQLiteAssemblyFileName));
                }
                else
                {
                    return new StringPair(SQLiteTypeName);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ConnectionDictionary GetDbConnectionTypeNames()
        {
            ConnectionDictionary result = new ConnectionDictionary();

            result.Add(DbConnectionType.None,
                typeof(object).AssemblyQualifiedName);

#if !NET_STANDARD_20
            result.Add(DbConnectionType.Odbc,
                typeof(OdbcConnection).AssemblyQualifiedName);

            result.Add(DbConnectionType.OleDb,
                typeof(OleDbConnection).AssemblyQualifiedName);

            result.Add(DbConnectionType.Sql,
                typeof(SqlConnection).AssemblyQualifiedName);
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ConnectionDictionary GetOtherDbConnectionTypeNames(
            bool useFullName,
            bool copy
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Is the assembly file name going to be required when
                //       creating the required types (i.e. so it can be used
                //       to pre-load the assembly).
                //
                bool useFileName = CommonOps.Runtime.IsDotNetCore();

                //
                // NOTE: One-time initialization, these are not per-interpreter
                //       datums and never change.
                //
                if (DbConnectionTypeFullNames == null)
                {
                    DbConnectionTypeFullNames = new ConnectionDictionary();

                    //
                    // NOTE: This type name is optional because it requires the
                    //       System.Data.OracleClient assembly to be loaded.
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.Oracle,
                        "System.Data.OracleClient.OracleConnection, " +
                        "System.Data.OracleClient, Version=2.0.0.0, " +
                        "Culture=neutral, PublicKeyToken=b77a5c561934e089");

                    //
                    // NOTE: This type name is optional because it requires the
                    //       .NET Framework v3.5 (SP1 or higher?) to be installed.
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.SqlCe,
                        "System.Data.SqlServerCe.SqlCeConnection, " +
                        "System.Data.SqlServerCe, Version=3.5.1.0, " +
                        "Culture=neutral, PublicKeyToken=89845dcd8080cc91");

                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.SQLite assembly to be loaded
                    //       (i.e. from "https://system.data.sqlite.org/" OR
                    //       "https://sf.net/projects/sqlite-dotnet2/").
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.SQLite,
                        GetStringPairForSQLite(true, useFileName));
                }

                if (DbConnectionTypeNames == null)
                {
                    DbConnectionTypeNames = new ConnectionDictionary();

                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.SQLite assembly to be loaded
                    //       (i.e. from "https://system.data.sqlite.org/" OR
                    //       "https://sf.net/projects/sqlite-dotnet2/").
                    //
                    DbConnectionTypeNames.Add(DbConnectionType.SQLite,
                        GetStringPairForSQLite(false, useFileName));
                }

                ConnectionDictionary result = useFullName ?
                    DbConnectionTypeFullNames : DbConnectionTypeNames;

                return copy ? new ConnectionDictionary(result) : result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CreateOtherDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string assemblyFileName,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(typeFullName) &&
                String.IsNullOrEmpty(typeName))
            {
                error = String.Format(
                    "bad type name for database connection type {0}",
                    FormatOps.WrapOrNull(dbConnectionType));

                return ReturnCode.Error;
            }

            AppDomain appDomain;
            CultureInfo cultureInfo = null;

            if (interpreter != null)
            {
                appDomain = interpreter.GetAppDomain();
                cultureInfo = interpreter.InternalCultureInfo;
            }
            else
            {
                appDomain = AppDomainOps.GetCurrent();
            }

            Assembly assembly = null;
            bool attemptedLoad = false;
            ResultList errors = null;

            foreach (string thisTypeName in new string[] {
                    typeFullName, typeName })
            {
                TraceOps.DebugTrace(String.Format(
                    "CreateOtherDbConnection: attempting to use " +
                    "type name {0} with assembly {1} ({2}) from " +
                    "file {3} for database connection type {4}...",
                    FormatOps.WrapOrNull(thisTypeName),
                    FormatOps.DisplayAssemblyName(assembly),
                    attemptedLoad ? "loaded" : "not loaded",
                    FormatOps.WrapOrNull(assemblyFileName),
                    FormatOps.WrapOrNull(dbConnectionType)),
                    typeof(DataOps).Name,
                    TracePriority.DataDebug);

                if (String.IsNullOrEmpty(thisTypeName))
                    continue;

                bool success = false;
                Type type = null;
                object @object = null;

                try
                {
                    if (!attemptedLoad && (assemblyFileName != null))
                    {
                        attemptedLoad = true; /* NOTE: One-shot. */

                        assembly = Assembly.LoadFrom(
                            assemblyFileName); /* throw */

                        if (assembly != null)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "CreateOtherDbConnection: loaded assembly " +
                                "{0} from file {1} before resolving type " +
                                "name {2} for database connection type {3}",
                                FormatOps.DisplayAssemblyName(assembly),
                                FormatOps.WrapOrNull(assemblyFileName),
                                FormatOps.WrapOrNull(thisTypeName),
                                FormatOps.WrapOrNull(dbConnectionType)),
                                typeof(DataOps).Name,
                                TracePriority.DataDebug);
                        }
                    }

                    ResultList localErrors = null;

                    if (Value.GetAnyType(
                            interpreter, thisTypeName, null,
                            appDomain, valueFlags, cultureInfo,
                            ref type, ref localErrors) == ReturnCode.Ok)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "CreateOtherDbConnection: resolved type name {0} " +
                            "to type {1}", FormatOps.WrapOrNull(thisTypeName),
                            FormatOps.TypeNameOrFullName(type)),
                            typeof(DataOps).Name, TracePriority.DataDebug);

                        @object = Activator.CreateInstance(
                            type, new object[] { connectionString });

                        connection = @object as IDbConnection;

                        if (connection != null)
                        {
                            success = true;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "type {0} could not be converted to type {1}",
                                FormatOps.TypeName(type), FormatOps.TypeName(
                                typeof(IDbConnection))));
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "CreateOtherDbConnection: cannot resolve " +
                            "type name {0}: {1}", FormatOps.WrapOrNull(
                            thisTypeName), FormatOps.WrapOrNull(localErrors)),
                            typeof(DataOps).Name, TracePriority.DataError);

                        if (localErrors != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.AddRange(localErrors);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(e);
                }
                finally
                {
                    if (!success && (@object != null))
                    {
                        ReturnCode disposeCode;
                        Result disposeError = null;

                        disposeCode = ObjectOps.TryDispose<object>(
                            ref @object, ref disposeError);

                        if (disposeCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "could not dispose of type {0}: {1}",
                                FormatOps.TypeName(type), disposeError));
                        }
                    }
                }
            }

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string assemblyFileName,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            return CreateDbConnection(
                interpreter, dbConnectionType, connectionString,
                assemblyFileName, typeFullName, typeName, valueFlags,
                GetOtherDbConnectionTypeNames(true, false),
                GetOtherDbConnectionTypeNames(false, false),
                ref connection, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string assemblyFileName,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ConnectionDictionary dbConnectionTypeFullNames,
            ConnectionDictionary dbConnectionTypeNames,
            ref IDbConnection connection,
            ref Result error
            )
        {
            try
            {
                switch (dbConnectionType & DbConnectionType.TypeMask)
                {
                    case DbConnectionType.None:
                        {
                            //
                            // NOTE: The caller explicitly requested
                            //       an invalid database connection;
                            //       therefore, return one.
                            //
                            connection = null;
                            return ReturnCode.Ok;
                        }
                    case DbConnectionType.Odbc:
                        {
#if !NET_STANDARD_20
                            connection = new OdbcConnection(connectionString);
                            return ReturnCode.Ok;
#else
                            error = "not implemented";
                            return ReturnCode.Error;
#endif
                        }
                    case DbConnectionType.OleDb:
                        {
#if !NET_STANDARD_20
                            connection = new OleDbConnection(connectionString);
                            return ReturnCode.Ok;
#else
                            error = "not implemented";
                            return ReturnCode.Error;
#endif
                        }
                    case DbConnectionType.Sql:
                        {
#if !NET_STANDARD_20
                            connection = new SqlConnection(connectionString);
                            return ReturnCode.Ok;
#else
                            error = "not implemented";
                            return ReturnCode.Error;
#endif
                        }
                    case DbConnectionType.Other:
                        {
                            return CreateOtherDbConnection(
                                interpreter, dbConnectionType,
                                connectionString, assemblyFileName,
                                typeFullName, typeName, valueFlags,
                                ref connection, ref error);
                        }
                    default:
                        {
                            //
                            // NOTE: Lookup the type name and/or full name and
                            //       then go to the "other" case (for database
                            //       connection types that are not "built-in").
                            //
                            StringPair value; /* REUSED */
                            bool found = false;

                            if ((dbConnectionTypeFullNames != null) &&
                                dbConnectionTypeFullNames.TryGetValue(
                                    dbConnectionType, out value))
                            {
                                if (value != null)
                                {
                                    typeFullName = value.X;
                                    assemblyFileName = value.Y;
                                }

                                found = true;
                            }

                            if ((dbConnectionTypeNames != null) &&
                                dbConnectionTypeNames.TryGetValue(
                                    dbConnectionType, out value))
                            {
                                if (value != null)
                                {
                                    typeName = value.X;
                                    assemblyFileName = value.Y;
                                }

                                found = true;
                            }

                            if (found)
                                goto case DbConnectionType.Other;

                            error = String.Format(
                                "unsupported database connection type {0}",
                                FormatOps.WrapOrNull(dbConnectionType));

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataReaderFieldNames(
            IDataReader reader, /* in */
            bool clear,         /* in */
            ref StringList list /* in, out */
            )
        {
            if (reader == null)
                return;

            int fieldCount = reader.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(reader.GetName(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataReaderFieldValues(
            Interpreter interpreter,           /* in */
            IDataReader reader,                /* in */
            CultureInfo cultureInfo,           /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            bool clear,                        /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            ref StringList list                /* in, out */
            )
        {
            if (reader == null)
                return;

            int fieldCount = reader.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
            {
                object value = reader.GetValue(index);

                if (allowNull ||
                    ((value != null) && (value != DBNull.Value)))
                {
                    if (pairs)
                    {
                        StringList element = new StringList();

                        if (names)
                            element.Add(reader.GetName(index));

                        element.Add(MarshalOps.FixupDataValue(
                            value, cultureInfo, dateTimeBehavior,
                            dateTimeKind, dateTimeFormat,
                            numberFormat, nullValue, dbNullValue,
                            errorValue));

                        list.Add(element.ToString());
                    }
                    else
                    {
                        if (names)
                            list.Add(reader.GetName(index));

                        list.Add(MarshalOps.FixupDataValue(
                            value, cultureInfo, dateTimeBehavior,
                            dateTimeKind, dateTimeFormat,
                            numberFormat, nullValue, dbNullValue,
                            errorValue));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToList(
            Interpreter interpreter,           /* in: NOT USED */
            IDataReader reader,                /* in */
            CultureInfo cultureInfo,           /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int limit,                         /* in */
            bool nested,                       /* in */
            bool clear,                        /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            ref StringList list,               /* in, out */
            ref int count,                     /* in, out */
            ref Result error                   /* out */
            )
        {
            if (reader == null)
            {
                error = "invalid data reader";
                return ReturnCode.Error;
            }

            int localCount = 0;

            while (reader.Read())
            {
                localCount++;

                StringList row = null;

                GetDataReaderFieldValues(
                    interpreter, reader, cultureInfo,
                    dateTimeBehavior, dateTimeKind,
                    dateTimeFormat, numberFormat,
                    nullValue, dbNullValue, errorValue,
                    clear, allowNull, pairs, names,
                    ref row);

                if (row != null)
                {
                    if (nested)
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(row.ToString());
                    }
                    else
                    {
                        if (list == null)
                            list = new StringList();

                        list.AddRange(row);
                    }
                }

                if ((limit != 0) && (--limit == 0))
                    break;
            }

            count += localCount;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToArray(
            Interpreter interpreter,           /* in */
            IDataReader reader,                /* in */
            string varName,                    /* in */
            CultureInfo cultureInfo,           /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int limit,                         /* in */
            bool clear,                        /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            ref int count,                     /* in, out */
            ref Result error                   /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (reader == null)
            {
                error = "invalid data reader";
                return ReturnCode.Error;
            }

            if (interpreter.ResetExistingVariable(
                    VariableFlags.NoElement, varName,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            StringList nameList = null;

            GetDataReaderFieldNames(
                reader, false, ref nameList);

            if (interpreter.SetVariableValue2(
                    VariableFlags.None, varName,
                    Vars.ResultSet.Names,
                    nameList.ToString(),
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            int localCount = 0;

            while (reader.Read())
            {
                localCount++;

                StringList row = null;

                GetDataReaderFieldValues(
                    interpreter, reader, cultureInfo,
                    dateTimeBehavior, dateTimeKind,
                    dateTimeFormat, numberFormat,
                    nullValue, dbNullValue, errorValue,
                    clear, allowNull, pairs, names,
                    ref row);

                if (row != null)
                {
                    if (interpreter.SetVariableValue2(
                            VariableFlags.None, varName,
                            localCount.ToString(),
                            row.ToString(),
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                if ((limit != 0) && (--limit == 0))
                    break;
            }

            if (interpreter.SetVariableValue2(
                    VariableFlags.None, varName,
                    Vars.ResultSet.Count,
                    localCount.ToString(),
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            count += localCount;
            return ReturnCode.Ok;
        }
        #endregion
    }
}
