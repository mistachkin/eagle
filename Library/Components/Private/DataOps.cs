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
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
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
        //
        // HACK: This is purposely not read-only.
        //
        private static bool ComplainOnUnsetError = true;

        ///////////////////////////////////////////////////////////////////////

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

        private static bool MaybeResolveTypeForOtherDbConnection(
            Interpreter interpreter,           /* in */
            AppDomain appDomain,               /* in */
            CultureInfo cultureInfo,           /* in */
            DbConnectionType dbConnectionType, /* in */
            string assemblyFileName,           /* in */
            object typeOrName,                 /* in */
            ValueFlags valueFlags,             /* in */
            ref Assembly assembly,             /* in */
            ref bool attemptedLoad,            /* in, out */
            ref Type type,                     /* out */
            ref ResultList errors              /* in, out */
            )
        {
            if (!attemptedLoad && (assemblyFileName != null))
            {
                attemptedLoad = true; /* NOTE: One-shot. */

                assembly = Assembly.LoadFrom(
                    assemblyFileName); /* throw */

                if (assembly != null)
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeResolveTypeForOtherDbConnection: " +
                        "loaded assembly {0} from file {1} before " +
                        "resolving type name {2} for database " +
                        "connection type {3}",
                        FormatOps.DisplayAssemblyName(assembly),
                        FormatOps.WrapOrNull(assemblyFileName),
                        FormatOps.TypeOrName(typeOrName),
                        FormatOps.WrapOrNull(dbConnectionType)),
                        typeof(DataOps).Name,
                        TracePriority.DataDebug);
                }
            }

            string localTypeName = typeOrName as string;

            if (String.IsNullOrEmpty(localTypeName))
                return false;

            Type localType = null;
            ResultList localErrors = null;

            if (Value.GetAnyType(interpreter,
                    localTypeName, null, appDomain,
                    valueFlags, cultureInfo, ref localType,
                    ref localErrors) == ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "MaybeResolveTypeForOtherDbConnection: " +
                    "resolved type name {0} to type {1}",
                    FormatOps.TypeOrName(typeOrName),
                    FormatOps.TypeNameOrFullName(localType)),
                    typeof(DataOps).Name,
                    TracePriority.DataDebug);

                type = localType;
                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "MaybeResolveTypeForOtherDbConnection: " +
                    "cannot resolve type name {0}: {1}",
                    FormatOps.TypeOrName(typeOrName),
                    FormatOps.WrapOrNull(localErrors)),
                    typeof(DataOps).Name,
                    TracePriority.DataError);

                if (localErrors != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.AddRange(localErrors);
                }

                return false;
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
            Type type,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(typeFullName) &&
                String.IsNullOrEmpty(typeName) &&
                (type == null))
            {
                error = String.Format(
                    "bad types for database connection type {0}",
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

            foreach (object typeOrName in new object[] {
                    type, typeFullName, typeName
                })
            {
                if (typeOrName == null)
                    continue;

                TraceOps.DebugTrace(String.Format(
                    "CreateOtherDbConnection: attempting to use " +
                    "type {0} with assembly {1} ({2}) from file " +
                    "{3} for database connection type {4}...",
                    FormatOps.TypeOrName(typeOrName),
                    FormatOps.DisplayAssemblyName(assembly),
                    attemptedLoad ? "loaded" : "not loaded",
                    FormatOps.WrapOrNull(assemblyFileName),
                    FormatOps.WrapOrNull(dbConnectionType)),
                    typeof(DataOps).Name,
                    TracePriority.DataDebug);

                bool success = false;
                Type localType = typeOrName as Type;
                object @object = null;

                try
                {
                    if (localType == null)
                    {
                        if (!MaybeResolveTypeForOtherDbConnection(
                                interpreter, appDomain, cultureInfo,
                                dbConnectionType, assemblyFileName,
                                typeOrName, valueFlags, ref assembly,
                                ref attemptedLoad, ref localType,
                                ref errors))
                        {
                            continue;
                        }
                    }

                    @object = Activator.CreateInstance(
                        localType, new object[] { connectionString });

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
                            FormatOps.TypeName(localType),
                            FormatOps.TypeName(typeof(IDbConnection))));
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
                                FormatOps.TypeName(localType),
                                disposeError));
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
            Type type,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            return CreateDbConnection(
                interpreter, dbConnectionType, connectionString,
                assemblyFileName, typeFullName, typeName, type,
                valueFlags,
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
            Type type,
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
                                interpreter, dbConnectionType, connectionString,
                                assemblyFileName, typeFullName, typeName, type,
                                valueFlags, ref connection, ref error);
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

        public static ReturnCode GetParameters(
            Interpreter interpreter,
            CultureInfo cultureInfo,
            string valueFormat,
            ValueFlags valueFlags,
            DateTimeKind dateTimeKind,
            DateTimeStyles dateTimeStyles,
            IDbCommand command,
            ArgumentList arguments,
            int startIndex,
            int stopIndex,
            bool verbatim,
            ref Result error
            )
        {
            if (command == null)
            {
                error = "invalid database command";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            IDataParameterCollection parameters = command.Parameters;

            if (parameters == null)
            {
                error = "invalid command parameter list";
                return ReturnCode.Error;
            }

            int count = arguments.Count;

            if (stopIndex >= 0)
            {
                if (stopIndex > (count - 1))
                {
                    error = String.Format(
                        "index {0} out-of-bounds, must be less than {1}",
                        stopIndex, (count - 1));

                    return ReturnCode.Error;
                }
            }
            else
            {
                stopIndex = count - 1;
            }

            if (startIndex > stopIndex)
            {
                error = String.Format(
                    "start index {0} cannot be greater than stop index {1}",
                    startIndex, stopIndex);

                return ReturnCode.Error;
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                StringList parameterList = null;

                if (ListOps.GetOrCopyOrSplitList(interpreter,
                        arguments[index], true, ref parameterList,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (parameterList.Count < 1)
                {
                    error = "parameter missing required element \"name\"";
                    return ReturnCode.Error;
                }

                IDbDataParameter parameter = command.CreateParameter();

                parameter.ParameterName = parameterList[0];

                if ((parameterList.Count >= 2) &&
                    !String.IsNullOrEmpty(parameterList[1]))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(DbType), parameterList[1], true, true);

                    if (enumValue is DbType)
                    {
                        parameter.DbType = (DbType)enumValue;
                    }
                    else
                    {
                        error = ScriptOps.BadValue(
                            null, "database type", parameterList[1],
                            Enum.GetNames(typeof(DbType)), null, null);

                        return ReturnCode.Error;
                    }
                }

                if (parameterList.Count >= 3)
                {
                    object parameterValue = parameterList[2];

                    if (parameterValue is string)
                    {
                        /* IGNORED */
                        Value.GetObject(
                            interpreter, (string)parameterValue,
                            ref parameterValue);
                    }

                    if (!verbatim && (parameterValue is string))
                    {
                        ValueFlags parameterValueFlags = valueFlags;

                        if (parameterList.Count >= 5)
                        {
                            object enumValue = EnumOps.TryParseFlags(
                                interpreter, typeof(ValueFlags),
                                parameterValueFlags.ToString(),
                                parameterList[4], cultureInfo,
                                true, true, true, ref error);

                            if (enumValue is ValueFlags)
                            {
                                parameterValueFlags = (ValueFlags)enumValue;
                            }
                            else
                            {
                                error = ScriptOps.BadValue(
                                    null, "value flags", parameterList[4],
                                    Enum.GetNames(typeof(ValueFlags)), null,
                                    null);

                                return ReturnCode.Error;
                            }
                        }

                        /* IGNORED */
                        Value.GetValue(
                            (string)parameterValue, valueFormat,
                            parameterValueFlags | ValueFlags.Strict,
                            dateTimeKind, dateTimeStyles,
                            cultureInfo, ref parameterValue);
                    }

                    parameter.Value = parameterValue;
                }
                else
                {
                    parameter.Value = DBNull.Value;
                }

                if ((parameterList.Count >= 4) &&
                    !String.IsNullOrEmpty(parameterList[3]))
                {
                    int size = 0;

                    if (Value.GetInteger2(parameterList[3],
                            ValueFlags.AnyInteger, cultureInfo,
                            ref size, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    parameter.Size = size;
                }

                parameters.Add(parameter);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DataRecordToResults(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            IDataRecord record,                /* in */
            OptionDictionary options,          /* in */
            DbResultFormat resultFormat,       /* in */
            string varName,                    /* in */
            string varIndex,                   /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int count,                         /* in */
            int limit,                         /* in */
            bool nested,                       /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            bool andCount,                     /* in */
            Type returnType,                   /* in */
            ObjectFlags objectFlags,           /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasRaw,                     /* in */
            bool aliasAll,                     /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            bool noFixup,                      /* in */
            ref Result result                  /* out */
            )
        {
            Result value = null;

            switch (resultFormat & DbResultFormat.FormatMask)
            {
                case DbResultFormat.None:
                    {
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                case DbResultFormat.RawArray:
                    {
                        if (DataRecordToVariable(
                                interpreter, record, varName,
                                varIndex, cultureInfo,
                                dateTimeBehavior, dateTimeKind,
                                dateTimeFormat, numberFormat,
                                nullValue, dbNullValue,
                                errorValue, false, allowNull,
                                pairs, names, noFixup,
                                ref result) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        if (andCount)
                        {
                            varIndex = Vars.ResultSet.Count;
                            value = count.ToString();
                        }
                        else
                        {
                            varName = null;
                            value = String.Empty;
                        }
                        break;
                    }
                case DbResultFormat.RawList:
                    {
                        StringList list = null;

                        if (DataRecordToList(
                                interpreter, record, cultureInfo,
                                dateTimeBehavior, dateTimeKind,
                                dateTimeFormat, numberFormat,
                                nullValue, dbNullValue, errorValue,
                                nested, false, allowNull, pairs,
                                names, noFixup, ref list,
                                ref result) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        if (andCount)
                        {
                            StringList list2 = new StringList();

                            list2.Add(count.ToString());

                            if (list != null)
                                list2.Add(list);
                            else
                                list2.Add((string)null);

                            value = list2;
                        }
                        else if (list != null)
                        {
                            value = list;
                        }
                        else
                        {
                            value = String.Empty;
                        }
                        break;
                    }
                case DbResultFormat.Array:
                    {
                        pairs = true;
                        names = true;

                        goto case DbResultFormat.RawArray;
                    }
                case DbResultFormat.List:
                    {
                        nested = false;
                        pairs = false;
                        names = false;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.Dictionary:
                    {
                        nested = false;
                        pairs = false;
                        names = true;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.NestedList:
                    {
                        nested = true;
                        pairs = false;
                        names = false;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.NestedDictionary:
                    {
                        nested = true;
                        pairs = false;
                        names = true;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.DataRecord:
                    {
                        IDataRecord localRecord = CreateDataRecord(
                            record, ref result);

                        if (localRecord == null)
                            return ReturnCode.Error;

                        ObjectOptionType objectOptionType =
                            ObjectOptionType.Execute |
                            ObjectOps.GetOptionType(aliasRaw, aliasAll);

                        if (MarshalOps.FixupReturnValue(
                                interpreter, binder, cultureInfo,
                                returnType, objectFlags, options,
                                ObjectOps.GetInvokeOptions(objectOptionType),
                                objectOptionType, objectName, interpName,
                                localRecord, create, dispose, alias,
                                aliasReference, toString,
                                ref value) != ReturnCode.Ok)
                        {
                            result = value;
                            return ReturnCode.Error;
                        }

                        if ((interpreter != null) && (varName != null))
                        {
                            if (interpreter.SetVariableValue2(
                                    VariableFlags.None, varName,
                                    varIndex, value, null,
                                    ref result) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }

                        if (andCount)
                        {
                            varIndex = Vars.ResultSet.Count;
                            value = count.ToString();
                        }
                        else
                        {
                            varName = null;
                            value = String.Empty;
                        }
                        break;
                    }
                default:
                    {
                        result = String.Format(
                            "unsupported result format {0}",
                            FormatOps.WrapOrNull(resultFormat));

                        return ReturnCode.Error;
                    }
            }

            if ((interpreter != null) && (varName != null))
            {
                if (interpreter.SetVariableValue2(
                        VariableFlags.None, varName,
                        varIndex, value, null,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                result = value;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasDataReaderObject(
            Interpreter interpreter, /* in */
            IDataReader reader       /* in */
            )
        {
            //
            // NOTE: Was the IDataReader [opaque object handle]
            //       transferred to the interpreter object list?
            //       If so, we no longer need (or want) to close
            //       it.
            //
            if ((reader == null) || (interpreter == null))
                return false;

            return (interpreter.GetObject(
                reader, LookupFlags.NoVerbose) == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DataReaderToResults(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            IDataReader reader,                /* in */
            OptionDictionary options,          /* in */
            DbResultFormat resultFormat,       /* in */
            string varName,                    /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int limit,                         /* in */
            bool nested,                       /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            bool andCount,                     /* in */
            Type returnType,                   /* in */
            ObjectFlags objectFlags,           /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasRaw,                     /* in */
            bool aliasAll,                     /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            bool noFixup,                      /* in */
            ref bool close,                    /* in, out */
            ref Result result                  /* out */
            )
        {
            switch (resultFormat & DbResultFormat.FormatMask)
            {
                case DbResultFormat.None:
                    {
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                case DbResultFormat.RawArray:
                    {
                        int count = 0;

                        if (DataReaderToArray(
                                interpreter, reader, varName,
                                cultureInfo, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue,
                                limit, false, allowNull,
                                pairs, names, noFixup, ref count,
                                ref result) == ReturnCode.Ok)
                        {
                            result = andCount ?
                                count.ToString() : String.Empty;

                            return ReturnCode.Ok;
                        }
                        break;
                    }
                case DbResultFormat.RawList:
                    {
                        StringList list = null;
                        int count = 0;

                        if (DataReaderToList(
                                interpreter, reader, cultureInfo,
                                dateTimeBehavior, dateTimeKind,
                                dateTimeFormat, numberFormat,
                                nullValue, dbNullValue,
                                errorValue, limit, nested,
                                false, allowNull, pairs, names,
                                noFixup, ref list, ref count,
                                ref result) == ReturnCode.Ok)
                        {
                            if (andCount)
                            {
                                StringList list2 = new StringList();

                                list2.Add(count.ToString());

                                if (list != null)
                                    list2.Add(list);
                                else
                                    list2.Add((string)null);

                                result = list2;
                            }
                            else if (list != null)
                            {
                                result = list;
                            }
                            else
                            {
                                result = String.Empty;
                            }

                            return ReturnCode.Ok;
                        }
                        break;
                    }
                case DbResultFormat.Array:
                    {
                        pairs = true;
                        names = true;

                        goto case DbResultFormat.RawArray;
                    }
                case DbResultFormat.List:
                    {
                        nested = false;
                        pairs = false;
                        names = false;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.Dictionary:
                    {
                        nested = false;
                        pairs = false;
                        names = true;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.NestedList:
                    {
                        nested = true;
                        pairs = false;
                        names = false;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.NestedDictionary:
                    {
                        nested = true;
                        pairs = false;
                        names = true;

                        goto case DbResultFormat.RawList;
                    }
                case DbResultFormat.DataReader:
                    {
                        ObjectOptionType objectOptionType =
                            ObjectOptionType.Execute |
                            ObjectOps.GetOptionType(aliasRaw, aliasAll);

                        if (MarshalOps.FixupReturnValue(
                                interpreter, binder, cultureInfo,
                                returnType, objectFlags, options,
                                ObjectOps.GetInvokeOptions(objectOptionType),
                                objectOptionType, objectName, interpName,
                                reader, create, dispose, alias,
                                aliasReference, toString,
                                ref result) == ReturnCode.Ok)
                        {
                            if (HasDataReaderObject(interpreter, reader))
                                close = false;

                            return ReturnCode.Ok;
                        }
                        break;
                    }
                default:
                    {
                        result = String.Format(
                            "unsupported result format {0}",
                            FormatOps.WrapOrNull(resultFormat));

                        break;
                    }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string DataValueToString(
            object value /* in */
            )
        {
            return StringOps.GetStringFromObject(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteCommandAndGetResults(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            IDbCommand command,                /* in */
            OptionDictionary options,          /* in */
            DbExecuteType executeType,         /* in */
            CommandBehavior commandBehavior,   /* in */
            DbResultFormat resultFormat,       /* in */
            string varName,                    /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int limit,                         /* in */
            bool nested,                       /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            Type returnType,                   /* in */
            ObjectFlags objectFlags,           /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasRaw,                     /* in */
            bool aliasAll,                     /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            bool noFixup,                      /* in */
            ref Result result                  /* out */
            )
        {
            if (command == null)
            {
                result = "invalid database command";
                return ReturnCode.Error;
            }

            switch (executeType & DbExecuteType.TypeMask)
            {
                case DbExecuteType.None:
                    {
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                case DbExecuteType.NonQuery:
                    {
                        try
                        {
                            result = command.ExecuteNonQuery();
                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            result = e;
                        }
                        break;
                    }
                case DbExecuteType.Scalar:
                    {
                        try
                        {
                            object value = command.ExecuteScalar();

                            if (noFixup)
                            {
                                result = DataValueToString(value);
                            }
                            else
                            {
                                result = MarshalOps.FixupDataValue(
                                    value, cultureInfo, dateTimeBehavior,
                                    dateTimeKind, dateTimeFormat,
                                    numberFormat, nullValue, dbNullValue,
                                    errorValue);
                            }

                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            result = e;
                        }
                        break;
                    }
                case DbExecuteType.Reader:
                case DbExecuteType.ReaderAndCount:
                    {
                        bool andCount = false;

                        if (executeType == DbExecuteType.ReaderAndCount)
                            andCount = true;

                        bool close = true;
                        IDataReader reader = null;

                        try
                        {
                            reader = command.ExecuteReader(commandBehavior);

                            return DataReaderToResults(
                                interpreter, binder, cultureInfo,
                                reader, options, resultFormat,
                                varName, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, limit,
                                nested, allowNull, pairs, names,
                                andCount, returnType, objectFlags,
                                objectName, interpName, create,
                                dispose, alias, aliasRaw, aliasAll,
                                aliasReference, toString, noFixup,
                                ref close, ref result);
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            result = e;
                        }
                        finally
                        {
                            if (reader != null)
                            {
                                if (close)
                                    reader.Close();

                                reader = null;
                            }
                        }
                        break;
                    }
                default:
                    {
                        result = String.Format(
                            "unsupported execution type {0}",
                            FormatOps.WrapOrNull(executeType));

                        break;
                    }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataRecordFieldNames(
            IDataRecord record, /* in */
            bool clear,         /* in */
            ref StringList list /* in, out */
            )
        {
            if (record == null)
                return;

            int fieldCount = record.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(record.GetName(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataRecordFieldValues(
            IDataRecord record, /* in */
            bool clear,         /* in */
            ref ObjectList list /* in, out */
            )
        {
            if (record == null)
                return;

            int fieldCount = record.FieldCount;

            if (clear || (list == null))
                list = new ObjectList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(record.GetValue(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataRecordFieldTypeNames(
            IDataRecord record, /* in */
            bool clear,         /* in */
            ref StringList list /* in, out */
            )
        {
            if (record == null)
                return;

            int fieldCount = record.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(record.GetDataTypeName(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataRecordFieldTypes(
            IDataRecord record, /* in */
            bool clear,         /* in */
            ref TypeList list   /* in, out */
            )
        {
            if (record == null)
                return;

            int fieldCount = record.FieldCount;

            if (clear || (list == null))
                list = new TypeList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(record.GetFieldType(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataRecordFieldValues(
            Interpreter interpreter,           /* in: NOT USED */
            IDataRecord record,                /* in */
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
            bool noFixup,                      /* in */
            ref StringList list                /* in, out */
            )
        {
            if (record == null)
                return;

            int fieldCount = record.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
            {
                object value = record.GetValue(index);

                if (allowNull ||
                    ((value != null) && (value != DBNull.Value)))
                {
                    if (pairs)
                    {
                        StringList element = new StringList();

                        if (names)
                            element.Add(record.GetName(index));

                        if (noFixup)
                        {
                            element.Add(DataValueToString(value));
                        }
                        else
                        {
                            element.Add(MarshalOps.FixupDataValue(
                                value, cultureInfo, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue, dbNullValue,
                                errorValue));
                        }

                        list.Add(element.ToString());
                    }
                    else
                    {
                        if (names)
                            list.Add(record.GetName(index));

                        if (noFixup)
                        {
                            list.Add(DataValueToString(value));
                        }
                        else
                        {
                            list.Add(MarshalOps.FixupDataValue(
                                value, cultureInfo, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue, dbNullValue,
                                errorValue));
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IDataRecord CreateDataRecord(
            IDataRecord record, /* in */
            ref Result error    /* out */
            )
        {
            StringList names = null;

            GetDataRecordFieldNames(
                record, false, ref names);

            if (names == null)
            {
                error = "invalid field names";
                return null;
            }

            ObjectList values = null;

            GetDataRecordFieldValues(
                record, false, ref values);

            if (values == null)
            {
                error = "invalid field values";
                return null;
            }

            StringList typeNames = null;

            GetDataRecordFieldTypeNames(
                record, false, ref typeNames);

            if (typeNames == null)
            {
                error = "invalid field type names";
                return null;
            }

            TypeList types = null;

            GetDataRecordFieldTypes(
                record, false, ref types);

            if (types == null)
            {
                error = "invalid field types";
                return null;
            }

            return new DataRecord(
                names, values, typeNames, types);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method cannot currently "fail"; however, its
        //          return code should still be checked by the caller.
        //
        private static ReturnCode DataRecordToList(
            Interpreter interpreter,           /* in: NOT USED */
            IDataRecord record,                /* in */
            CultureInfo cultureInfo,           /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            bool nested,                       /* in */
            bool clear,                        /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            bool noFixup,                      /* in */
            ref StringList list,               /* in, out */
            ref Result error                   /* out */
            )
        {
            StringList row = null;

            /* NO RESULT */
            GetDataRecordFieldValues(
                interpreter, record, cultureInfo,
                dateTimeBehavior, dateTimeKind,
                dateTimeFormat, numberFormat,
                nullValue, dbNullValue, errorValue,
                clear, allowNull, pairs, names,
                noFixup, ref row);

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

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetVariableOrMaybeComplain(
            Interpreter interpreter, /* in */
            string varName,          /* in */
            string varIndex          /* in: OPTIONAL */
            )
        {
            if ((interpreter == null) || (varName == null))
                return;

            Result error = null;

            if (interpreter.UnsetVariable2(
                    VariableFlags.NoComplain, varName, varIndex,
                    null, ref error) != ReturnCode.Ok)
            {
                if (ComplainOnUnsetError)
                {
                    DebugOps.Complain(
                        interpreter, ReturnCode.Error, error);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DataValueToVariable(
            Interpreter interpreter,           /* in */
            object value,                      /* in */
            string varName,                    /* in */
            string varIndex,                   /* in */
            CultureInfo cultureInfo,           /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            bool noFixup,                      /* in */
            ref Result error                   /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (!noFixup)
            {
                value = MarshalOps.FixupDataValue(
                   value, cultureInfo, dateTimeBehavior,
                   dateTimeKind, dateTimeFormat,
                   numberFormat, nullValue, dbNullValue,
                   errorValue);
            }

            if (varName != null)
            {
                UnsetVariableOrMaybeComplain(
                    interpreter, varName, varIndex);

                if (interpreter.SetVariableValue2(
                        VariableFlags.None, null,
                        varName, varIndex, value, null,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DataRecordToVariable(
            Interpreter interpreter,           /* in */
            IDataRecord record,                /* in */
            string varName,                    /* in */
            string varIndex,                   /* in */
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
            bool noFixup,                      /* in */
            ref Result error                   /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            StringList row = null;

            /* NO RESULT */
            GetDataRecordFieldValues(
                interpreter, record, cultureInfo,
                dateTimeBehavior, dateTimeKind,
                dateTimeFormat, numberFormat,
                nullValue, dbNullValue, errorValue,
                clear, allowNull, pairs, names,
                noFixup, ref row);

            if ((row != null) && (varName != null))
            {
                UnsetVariableOrMaybeComplain(
                    interpreter, varName, varIndex);

                if (interpreter.SetVariableValue2(
                        VariableFlags.None, varName,
                        varIndex, row.ToString(),
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
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
            bool noFixup,                      /* in */
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

                if (DataRecordToList(
                        interpreter, reader, cultureInfo,
                        dateTimeBehavior, dateTimeKind,
                        dateTimeFormat, numberFormat,
                        nullValue, dbNullValue, errorValue,
                        nested, clear, allowNull, pairs,
                        names, noFixup, ref list,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((limit != Limits.Unlimited) &&
                    (--limit == 0))
                {
                    break;
                }
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
            bool noFixup,                      /* in */
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

            GetDataRecordFieldNames(
                reader, false, ref nameList);

            if (varName != null)
            {
                if (interpreter.SetVariableValue2(
                        VariableFlags.None, varName,
                        Vars.ResultSet.Names,
                        nameList.ToString(),
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            int localCount = 0;

            while (reader.Read())
            {
                localCount++;

                if (DataRecordToVariable(
                        interpreter, reader, varName,
                        localCount.ToString(), cultureInfo,
                        dateTimeBehavior, dateTimeKind,
                        dateTimeFormat, numberFormat,
                        nullValue, dbNullValue, errorValue,
                        clear, allowNull, pairs, names,
                        noFixup, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((limit != Limits.Unlimited) &&
                    (--limit == 0))
                {
                    break;
                }
            }

            if (varName != null)
            {
                if (interpreter.SetVariableValue2(
                        VariableFlags.None, varName,
                        Vars.ResultSet.Count,
                        localCount.ToString(),
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            count += localCount;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteCommandAndEvaluateBody(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            IDbCommand command,                /* in */
            OptionDictionary options,          /* in */
            DbExecuteType executeType,         /* in */
            CommandBehavior commandBehavior,   /* in */
            DbResultFormat resultFormat,       /* in */
            string commandName,                /* in */
            string varName,                    /* in */
            string body,                       /* in */
            IScriptLocation location,          /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            int limit,                         /* in */
            bool nested,                       /* in */
            bool allowNull,                    /* in */
            bool pairs,                        /* in */
            bool names,                        /* in */
            Type returnType,                   /* in */
            ObjectFlags objectFlags,           /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasRaw,                     /* in */
            bool aliasAll,                     /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            bool noFixup,                      /* in */
            ref Result result                  /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (command == null)
            {
                result = "invalid database command";
                return ReturnCode.Error;
            }

            object value; /* REUSED */
            Result localResult; /* REUSED */
            bool andCount = false;

            switch (executeType & DbExecuteType.TypeMask)
            {
                case DbExecuteType.None:
                    {
                        result = String.Empty;
                        return ReturnCode.Ok;
                    }
                case DbExecuteType.NonQuery:
                    {
                        try
                        {
                            value = command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            result = e;
                            return ReturnCode.Error;
                        }

                        try
                        {
                            localResult = null;

                            if (DataValueToVariable(
                                    interpreter, value, varName,
                                    Vars.ResultSet.Count,
                                    cultureInfo, dateTimeBehavior,
                                    dateTimeKind, dateTimeFormat,
                                    numberFormat, nullValue,
                                    dbNullValue, errorValue, noFixup,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                result = localResult;
                                return ReturnCode.Error;
                            }

                            localResult = null;

                            if (interpreter.EvaluateScript(
                                    body, location,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                result = localResult;
                                return ReturnCode.Error;
                            }

                            result = String.Empty;
                            return ReturnCode.Ok;
                        }
                        finally
                        {
                            UnsetVariableOrMaybeComplain(
                                interpreter, varName,
                                Vars.ResultSet.Count);
                        }
                    }
                case DbExecuteType.Scalar:
                    {
                        try
                        {
                            value = command.ExecuteScalar();
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            result = e;
                            return ReturnCode.Error;
                        }

                        try
                        {
                            localResult = null;

                            if (DataValueToVariable(
                                    interpreter, value, varName,
                                    Vars.ResultSet.Value,
                                    cultureInfo, dateTimeBehavior,
                                    dateTimeKind, dateTimeFormat,
                                    numberFormat, nullValue,
                                    dbNullValue, errorValue, noFixup,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                result = localResult;
                                return ReturnCode.Error;
                            }

                            localResult = null;

                            if (interpreter.EvaluateScript(
                                    body, location,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                result = localResult;
                                return ReturnCode.Error;
                            }

                            result = String.Empty;
                            return ReturnCode.Ok;
                        }
                        finally
                        {
                            UnsetVariableOrMaybeComplain(
                                interpreter, varName,
                                Vars.ResultSet.Value);
                        }
                    }
                case DbExecuteType.Reader:
                case DbExecuteType.ReaderAndCount:
                    {
                        if (executeType == DbExecuteType.ReaderAndCount)
                            andCount = true;

                        goto loop;
                    }
                default:
                    {
                        result = String.Format(
                            "unsupported execution type {0}",
                            FormatOps.WrapOrNull(executeType));

                        return ReturnCode.Error;
                    }
            }

        loop:

            IDataReader reader = null;

            try
            {
                try
                {
                    reader = command.ExecuteReader(commandBehavior);
                }
                catch (Exception e)
                {
                    Engine.SetExceptionErrorCode(interpreter, e);

                    result = e;
                    return ReturnCode.Error;
                }

                ReturnCode code = ReturnCode.Ok; /* REUSED */

                int iterationLimit = interpreter.InternalIterationLimit;
                int iterationCount = 0;

                int localCount = 0;

                while (true)
                {
                    if (!reader.Read())
                        break;

                    localCount++;
                    localResult = null;

                    code = DataRecordToResults(
                        interpreter, binder, cultureInfo,
                        reader, options, resultFormat,
                        varName, localCount.ToString(),
                        dateTimeBehavior, dateTimeKind,
                        dateTimeFormat, numberFormat,
                        nullValue, dbNullValue,
                        errorValue, localCount, limit,
                        nested, allowNull, pairs, names,
                        andCount, returnType, objectFlags,
                        objectName, interpName, create,
                        dispose, alias, aliasRaw, aliasAll,
                        aliasReference, toString, noFixup,
                        ref localResult);

                    if (code != ReturnCode.Ok)
                    {
                        result = localResult;
                        break;
                    }

                    if (andCount && (varName != null))
                    {
                        UnsetVariableOrMaybeComplain(
                            interpreter, varName,
                            Vars.ResultSet.Count);

                        localResult = null;

                        if (interpreter.SetVariableValue2(
                                VariableFlags.None, null,
                                varName, Vars.ResultSet.Count,
                                localCount.ToString(), null,
                                ref localResult) != ReturnCode.Ok)
                        {
                            result = localResult;
                            return ReturnCode.Error;
                        }
                    }

                    try
                    {
                        localResult = null;

                        code = interpreter.EvaluateScript(
                            body, location, ref localResult);
                    }
                    finally
                    {
                        UnsetVariableOrMaybeComplain(
                            interpreter, varName,
                            Vars.ResultSet.Count);

                        UnsetVariableOrMaybeComplain(
                            interpreter, varName,
                            localCount.ToString());
                    }

                    if (code != ReturnCode.Ok)
                    {
                        if (code == ReturnCode.Continue)
                        {
                            code = ReturnCode.Ok;
                        }
                        else if (code == ReturnCode.Break)
                        {
                            result = localResult;
                            code = ReturnCode.Ok;

                            break;
                        }
                        else if (code == ReturnCode.Error)
                        {
                            Engine.AddErrorInformation(
                                interpreter, localResult,
                                String.Format(
                                    "{0}    (\"{1} foreach\" body line {2})",
                                    Environment.NewLine, commandName,
                                    Interpreter.GetErrorLine(
                                        interpreter)));

                            result = localResult;
                            break;
                        }
                        else
                        {
                            //
                            // TODO: Can we actually get to this point?
                            //
                            result = localResult;
                            break;
                        }
                    }

                    if ((limit != Limits.Unlimited) &&
                        (--limit == 0))
                    {
                        break;
                    }

                    if ((iterationLimit != Limits.Unlimited) &&
                        (++iterationCount > iterationLimit))
                    {
                        result = String.Format(
                            "iteration limit {0} exceeded",
                            iterationLimit);

                        code = ReturnCode.Error;
                        break;
                    }
                }

                if (code == ReturnCode.Ok)
                    result = String.Empty;

                return code;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }
        #endregion
    }
}
