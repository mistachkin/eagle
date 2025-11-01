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
using System.Collections.Generic;
using System.Data;

#if !NET_STANDARD_20
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
#endif

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _IsolationLevel = Eagle._Components.Public.IsolationLevel;

using ConnectionTriplet = Eagle._Components.Public.AnyTriplet<
    string, string, byte[]>;

using ConnectionDictionary =
    Eagle._Containers.Private.DbConnectionTypeDictionary;
using System.Collections;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

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

        #region Assembly Qualified Type Name Constants
        private static string OracleFullTypeFormat =
            "System.Data.OracleClient.OracleConnection, " +
            "System.Data.OracleClient, Version=2.0.0.0, " +
            "Culture=neutral, PublicKeyToken={0}";

        ///////////////////////////////////////////////////////////////////////

        private static string SqlCeFullTypeFormat =
            "System.Data.SqlServerCe.SqlCeConnection, " +
            "System.Data.SqlServerCe, Version=3.5.1.0, " +
            "Culture=neutral, PublicKeyToken={0}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Data.SQLite Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string SQLiteAssemblyFileName =
            "System.Data.SQLite.dll";

        ///////////////////////////////////////////////////////////////////////

        private static string SQLiteFullTypeFormat =
            "System.Data.SQLite.SQLiteConnection, System.Data.SQLite, " +
            "Version=1.0, Culture=neutral, PublicKeyToken={0}";

        ///////////////////////////////////////////////////////////////////////

        private static string SQLiteTypeName =
            "System.Data.SQLite.SQLiteConnection";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Table/Column/Parameter Name Validation Regular Expressions
        //
        // HACK: These are hard-coded for now.
        //
        // TODO: Maybe make these configurable at some point?
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex parameterRegEx = RegExOps.Create(
            "^[@A-Z_][0-9A-Z_]*$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

        private static Regex identifierRegEx = RegExOps.Create(
            "^[$A-Z_][$0-9A-Z_]*$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Bundle Column Value Validation Constants
        //
        // HACK: These are hard-coded for now.
        //
        // TODO: Maybe make these configurable at some point?
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex bundleFullNameRegEx = RegExOps.Create(
            String.Format("^\\/(?:[A-Z_][0-9A-Z_]*\\/)*" +
            "(?:[A-Z_][0-9A-Z_\\-]*)(?:{0}|{0}{2}|{1}|{1}{2}|{3}|{3}{2})$",
            String.Format(
                "{0}{1}", Characters.Backslash, FileExtension.Library),
            String.Format(
                "{0}{1}", Characters.Backslash, FileExtension.Script),
            String.Format(
                "{0}{1}", Characters.Backslash, FileExtension.Signature),
            String.Format(
                "{0}{1}", Characters.Backslash, FileExtension.Markup)),
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This "logical constant" represents a purposely absent
        //       script signature value in a script bundle.  Currently,
        //       these are only used for ".harpy" files that have their
        //       own embedded signature value.
        //
        private static byte[] nullBundleSignature = {
            78, /* N */
            85, /* U */
            76, /* L */
            76  /* L */
        };

        ///////////////////////////////////////////////////////////////////////

        private static int minimumSignatureLength = 2048; /* 16384-bit RSA */

        ///////////////////////////////////////////////////////////////////////

        private static int minimumBundlePathSize = 3; /* "<fileName>:<fullName>" */
        private static int minimumBundleFileSize = 512; /* 1 database page */
        private static readonly char bundleNameDelimiter = Characters.Colon;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Support Methods
        public static void CheckIdentifier(
            string propertyName, /* in */
            string propertyValue /* in */
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CheckIdentifier(
            string propertyName,  /* in */
            string propertyValue, /* in */
            bool isParameterName  /* in */
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
            string format,        /* in */
            int parameterCount,   /* in */
            params string[] names /* in */
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

        private static string GetPublicKeyToken(
            DbConnectionType dbConnectionType /* in */
            )
        {
            switch (dbConnectionType & DbConnectionType.TypeMask)
            {
                case DbConnectionType.Odbc:
                case DbConnectionType.OleDb:
                case DbConnectionType.Oracle:
                case DbConnectionType.Sql:
                    {
                        return PublicKeyToken.Ecma;
                    }
                case DbConnectionType.SqlCe:
                    {
                        return PublicKeyToken.SqlServer;
                    }
                case DbConnectionType.SQLite:
                    {
                        return PublicKeyToken.SQLite;
                    }
                case DbConnectionType.SQLiteEnterprise:
                    {
                        return PublicKeyToken.SQLiteEnterprise;
                    }
                default:
                    {
                        return PublicKeyToken.Null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ConnectionTriplet GetConnectionTripletForSQLite(
            DbConnectionType dbConnectionType, /* in */
            bool useFullName,                  /* in */
            bool useFileName                   /* in */
            )
        {
            string publicKeyTokenString = GetPublicKeyToken(
                dbConnectionType);

            byte[] publicKeyToken = null;
            Result error = null;

            if (RuntimeOps.GetPublicKeyToken(String.Format(
                    "0x{0}", publicKeyTokenString), null,
                    ref publicKeyToken, ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetConnectionTripletForSQLite: error = {0}",
                    FormatOps.WrapOrNull(error)), typeof(DataOps).Name,
                    TracePriority.DataError3);
            }

            if (useFullName)
            {
                if (useFileName)
                {
                    return new ConnectionTriplet(String.Format(
                        SQLiteFullTypeFormat, publicKeyTokenString),
                        Path.Combine(
                            GlobalState.GetAnyEntryAssemblyPath(),
                            SQLiteAssemblyFileName), publicKeyToken);
                }
                else
                {
                    return new ConnectionTriplet(String.Format(
                        SQLiteFullTypeFormat, publicKeyTokenString),
                        null, publicKeyToken);
                }
            }
            else
            {
                if (useFileName)
                {
                    return new ConnectionTriplet(
                        SQLiteTypeName, Path.Combine(
                        GlobalState.GetAnyEntryAssemblyPath(),
                        SQLiteAssemblyFileName), publicKeyToken);
                }
                else
                {
                    return new ConnectionTriplet(SQLiteTypeName);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringDictionary GetDbConnectionTypeNames()
        {
            StringDictionary result = new StringDictionary();

            result.AddFrom(DbConnectionType.None,
                typeof(object).AssemblyQualifiedName);

#if !NET_STANDARD_20
            result.AddFrom(DbConnectionType.Odbc,
                typeof(OdbcConnection).AssemblyQualifiedName);

            result.AddFrom(DbConnectionType.OleDb,
                typeof(OleDbConnection).AssemblyQualifiedName);

            result.AddFrom(DbConnectionType.Sql,
                typeof(SqlConnection).AssemblyQualifiedName);
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringDictionary GetOtherDbConnectionTypeNames(
            bool useSqlite,   /* in */
            bool useFullName, /* in */
            bool useFileName  /* in */
            )
        {
            StringDictionary result = new StringDictionary();

            if (useFullName && !useFileName)
            {
                //
                // NOTE: This type name is optional because it requires
                //       the System.Data.OracleClient managed assembly
                //       to be loaded.
                //
                result.AddFrom(
                    DbConnectionType.Oracle, String.Format(
                    OracleFullTypeFormat, GetPublicKeyToken(
                    DbConnectionType.Oracle)));

                //
                // NOTE: This type name is optional because it requires
                //       the .NET Framework v3.5 (SP1 or higher?) to be
                //       installed.
                //
                result.AddFrom(
                    DbConnectionType.SqlCe, String.Format(
                    SqlCeFullTypeFormat, GetPublicKeyToken(
                    DbConnectionType.SqlCe)));
            }

            if (useSqlite)
            {
                //
                // NOTE: This type name is optional because it requires
                //       the System.Data.SQLite assembly to be loaded
                //       (i.e. from "https://system.data.sqlite.org/"
                //       OR "https://sf.net/projects/sqlite-dotnet2/").
                //
                foreach (DbConnectionType dbConnectionType in
                    new DbConnectionType[] {
                        DbConnectionType.SQLite,
                        DbConnectionType.SQLiteEnterprise
                    })
                {
                    ConnectionTriplet connectionTriplet;

                    connectionTriplet = GetConnectionTripletForSQLite(
                        dbConnectionType, useFullName, useFileName);

                    if (connectionTriplet == null)
                        continue;

                    result.AddFrom(
                        dbConnectionType, new StringPair(
                        connectionTriplet.X, connectionTriplet.Y));
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ConnectionDictionary GetOtherDbConnectionTypes(
            ValueFlags valueFlags,  /* in */
            bool useSqlite,         /* in */
            bool usePublicKeyToken, /* in */
            bool useFullName        /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Is the assembly file name going to be required when
                //       creating the required types (i.e. so it can be used
                //       to pre-load the assembly).  Also, it is required if
                //       callers wish to verify its Authenticode signature.
                //
                bool useFileName = CommonOps.Runtime.IsDotNetCore();
                bool wasTrustedOnly = false;

                if (!useFileName && (usePublicKeyToken || FlagOps.HasFlags(
                        valueFlags, ValueFlags.TrustedOnly, true)))
                {
                    useFileName = true;
                    wasTrustedOnly = true;
                }

                ConnectionDictionary result = new ConnectionDictionary();

                //
                // HACK: Assume that the "other" database connection types
                //       reside in "trusted" managed assembly files, since
                //       they are (basically?) part of the BCL.  This will
                //       be done via ignoring the "useFileName" flag if it
                //       was automatically set (above).
                //
                if (useFullName && (!useFileName || wasTrustedOnly))
                {
                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.OracleClient managed assembly
                    //       to be loaded.
                    //
                    result.Add(
                        DbConnectionType.Oracle, new ConnectionTriplet(
                        String.Format(OracleFullTypeFormat,
                        GetPublicKeyToken(DbConnectionType.Oracle))));

                    //
                    // NOTE: This type name is optional because it requires
                    //       the .NET Framework v3.5 (SP1 or higher?) to be
                    //       installed.
                    //
                    result.Add(
                        DbConnectionType.SqlCe, new ConnectionTriplet(
                        String.Format(SqlCeFullTypeFormat,
                        GetPublicKeyToken(DbConnectionType.SqlCe))));
                }

                if (useSqlite)
                {
                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.SQLite assembly to be loaded
                    //       (i.e. from "https://system.data.sqlite.org/" OR
                    //       "https://sf.net/projects/sqlite-dotnet2/").
                    //
                    foreach (DbConnectionType dbConnectionType in
                        new DbConnectionType[] {
                            DbConnectionType.SQLite,
                            DbConnectionType.SQLiteEnterprise
                        })
                    {
                        result.Add(
                            dbConnectionType, GetConnectionTripletForSQLite(
                            dbConnectionType, useFullName, useFileName));
                    }
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeResolveTypeForOtherDbConnection(
            Interpreter interpreter,           /* in */
            AppDomain appDomain,               /* in */
            CultureInfo cultureInfo,           /* in */
            DbConnectionType dbConnectionType, /* in */
            byte[] publicKeyToken,             /* in */
            string assemblyFileName,           /* in */
            object typeOrName,                 /* in */
            ValueFlags valueFlags,             /* in */
            ref Assembly assembly,             /* in */
            ref bool attemptedLoad,            /* in, out */
            ref Type type,                     /* out */
            ref ResultList errors              /* in, out */
            )
        {
            ResultList localErrors = null;

            if (!attemptedLoad && (assemblyFileName != null))
            {
                if (FlagOps.HasFlags(
                        valueFlags, ValueFlags.TrustedOnly, true) &&
                    !RuntimeOps.IsFileTrusted(
                        interpreter, null, assemblyFileName,
                        IntPtr.Zero))
                {
                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(String.Format(
                        "cannot resolve type name {0}: " +
                        "assembly file name {1} is not " +
                        "Authenticode signed or cannot " +
                        "be trusted",
                        FormatOps.TypeOrName(typeOrName),
                        FormatOps.WrapOrNull(assemblyFileName)));

                    goto errors;
                }

                if (publicKeyToken != null)
                {
                    Result localError = null;

                    if (!RuntimeOps.IsStrongNameVerified(
                            assemblyFileName, true) ||
                        !RuntimeOps.CheckPublicKeyToken(
                            assemblyFileName, publicKeyToken,
                            ref localError))
                    {
                        if (localErrors == null)
                            localErrors = new ResultList();

                        localErrors.Add(String.Format(
                            "cannot resolve type name {0}: " +
                            "assembly file name {1} is not " +
                            "strong name signed or cannot " +
                            "be verified",
                            FormatOps.TypeOrName(typeOrName),
                            FormatOps.WrapOrNull(assemblyFileName)));

                        if (localError != null)
                            localErrors.Add(localError);

                        goto errors;
                    }
                }

                attemptedLoad = true; /* NOTE: One-shot. */

                try
                {
                    assembly = Assembly.LoadFrom(
                        assemblyFileName); /* throw */
                }
                catch (Exception e)
                {
                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(e);

                    goto errors;
                }

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

        errors:

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

        ///////////////////////////////////////////////////////////////////////

        private static void AppendToBundleConnectionStringForSQLite(
            int idIndex,              /* in */
            byte[] hashValue,         /* in */
            ref StringBuilder builder /* in, out */
            )
        {
            if (builder == null)
                builder = StringBuilderFactory.Create();

            //
            // WARNING: DO NOT CHANGE THESE, CONSIDER THEM
            //          TO BE CONSTANTS.
            //
            builder.Append("Read Only=True;");
            builder.Append("Pooling=False;");
            builder.Append("DateTimeFormat=Ticks;");
            builder.Append("DateTimeKind=Utc;");

            if (hashValue != null)
            {
                builder.AppendFormat("Id4{0}={1};", idIndex,
                    ArrayOps.ToHexadecimalString(hashValue));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string BuildBundlePath(
            string fileName, /* in */
            string fullName, /* in */
            bool demand      /* in */
            )
        {
            Result error = null;

            return BuildBundlePath(fileName, fullName, demand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string BuildBundlePath(
            string fileName, /* in */
            string fullName, /* in */
            bool demand,     /* in */
            ref Result error /* out */
            )
        {
            if (!VerifyBundleFileName(ref fileName, ref error))
                return null;

            if (!VerifyBundleFullName(fullName, demand, ref error))
                return null;

            return String.Format(
                "{0}{1}{2}", fileName, bundleNameDelimiter, fullName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyBundlePath(
            string path,         /* in */
            bool demand,         /* in */
            out string fileName, /* out */
            out string fullName, /* out */
            ref Result error     /* out */
            )
        {
            fileName = null;
            fullName = null;

            if (String.IsNullOrEmpty(path))
            {
                error = "invalid bundle path";
                return false;
            }

            int length = path.Length;

            if (length < minimumBundlePathSize)
            {
                error = String.Format(
                    "bundle path length must be at least {0}",
                    minimumBundleFileSize);

                return false;
            }

            int index = path.LastIndexOf(bundleNameDelimiter);

            if (index == Index.Invalid)
            {
                error = "malformed bundle path";
                return false;
            }

            if (index == 0)
            {
                error = "bundle path missing file name";
                return false;
            }

            if ((index + 1) >= length)
            {
                error = "bundle path missing full name";
                return false;
            }

            fileName = path.Substring(0, index);
            fullName = path.Substring(index + 1);

            if (!VerifyBundleFileName(ref fileName, ref error))
                return false;

            if (!VerifyBundleFullName(fullName, demand, ref error))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyBundleFileName(
            ref string fileName, /* in, out */
            ref Result error     /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return false;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "file {0} does not exist",
                    FormatOps.WrapOrNull(fileName));

                return false;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(fileName);
                long length = fileInfo.Length;

                if (length < minimumBundleFileSize)
                {
                    error = String.Format(
                        "file {0} too small to be database",
                        FormatOps.WrapOrNull(fileName));

                    return false;
                }

                if ((length % minimumBundleFileSize) != 0)
                {
                    error = String.Format(
                        "file {0} wrong size to be database",
                        FormatOps.WrapOrNull(fileName));

                    return false;
                }

                fileName = fileInfo.FullName;
                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyBundleFullName(
            string fullName, /* in */
            bool demand,     /* in */
            ref Result error /* out */
            )
        {
            if (String.IsNullOrEmpty(fullName))
            {
                error = String.Format(
                    "invalid or empty {0}",
                    BundleField.FullName);

                return false;
            }

            Regex fullNameRegEx = bundleFullNameRegEx;

            if (fullNameRegEx == null)
            {
                error = String.Format(
                    "missing regular expression for {0}",
                    BundleField.FullName);

                return false;
            }

            if (!fullNameRegEx.IsMatch(fullName))
            {
                error = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.FullName,
                    FormatOps.WrapOrNull(fullName));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBundleConnectionString(
            string fileName, /* in */
            byte[] password, /* in */
            ref Result error /* out */
            )
        {
            if (!VerifyBundleFileName(ref fileName, ref error))
                return null;

            byte[] hashValue = RuntimeOps.HashFile(
                HashOps.ModernBytesAlgorithmName, fileName, null,
                ref error);

            if (hashValue == null)
                return null;

            StringBuilder builder = null;

            try
            {
                builder = StringBuilderFactory.Create();
                builder.AppendFormat("Data Source={0};", fileName);

                if (password != null)
                {
                    builder.AppendFormat("TextHexPassword={0};",
                        ArrayOps.ToHexadecimalString(password));
                }

                AppendToBundleConnectionStringForSQLite(
                    1, hashValue, ref builder);

                return StringBuilderCache.GetStringAndRelease(
                    ref builder);
            }
            finally
            {
                StringBuilderCache.Release(ref builder);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static int ExecuteNonQuery(
            IDbConnection connection, /* in */
            string commandText        /* in */
            )
        {
            if (commandText == null)
                return Count.None;

            if (connection == null)
                throw new ScriptException("invalid connection");

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                return command.ExecuteNonQuery(); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static object ExecuteScalar(
            IDbConnection connection, /* in */
            string commandText        /* in */
            )
        {
            if (commandText == null)
                return Count.None;

            if (connection == null)
                throw new ScriptException("invalid connection");

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                return command.ExecuteScalar(); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetBundleConnectionTypes(
            out DbConnectionType dbConnectionType1, /* out */
            out DbConnectionType dbConnectionType2  /* out */
            )
        {
            dbConnectionType1 = DbConnectionType.SQLiteEnterprise;
            dbConnectionType2 = DbConnectionType.SQLite;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBundleCommandText(
            bool demand,     /* in */
            ref Result error /* out */
            )
        {
            StringBuilder builder = null;

            try
            {
                builder = StringBuilderFactory.Create();

                //
                // TODO: Move to embedded resource file?
                //
                builder.AppendFormat(
                    @"  SELECT Id, Language, Sequence, Vendor,
                               HashAlgorithm, IsolationLevel,
                               SecurityLevel, SecurityFlags,
                               RuleSet, BlockType, FullName,
                               ""Group"", Description,
                               TimeStamp, PublicKeyToken,
                               Text, Signature
                          FROM Scripts
                         WHERE Language = '{0}'
                           AND Sequence {1} 0
                           AND ((:pattern IS NULL) OR
                                (FullName GLOB :pattern))
                      ORDER BY Sequence ASC;",
                    GlobalState.GetPackageName(),
                    demand ? "<=" : ">=");

                return StringBuilderCache.GetStringAndRelease(
                    ref builder);
            }
            finally
            {
                StringBuilderCache.Release(ref builder);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetBundleRecord(
            out Guid id,                           /* out */
            out string language,                   /* out */
            out long sequence,                     /* out */
            out string vendor,                     /* out */
            out string hashAlgorithmName,          /* out */
            out _IsolationLevel isolationLevel,    /* out */
            out SecurityLevel securityLevel,       /* out */
            out ScriptSecurityFlags securityFlags, /* out */
            out IRuleSet ruleSet,                  /* out */
            out XmlBlockType blockType,            /* out */
            out string fullName,                   /* out */
            out string group,                      /* out */
            out string description,                /* out */
            out DateTime timeStamp,                /* out */
            out byte[] publicKeyToken,             /* out */
            out string text,                       /* out */
            out byte[] signature                   /* out */
            )
        {
            //
            // TODO: Are these hard-coded "defaults" reasonable?
            //
            id = Guid.Empty;
            language = null;
            sequence = 0;
            vendor = null;
            hashAlgorithmName = null;
            isolationLevel = _IsolationLevel.None;
            securityLevel = SecurityLevel.None;
            securityFlags = ScriptSecurityFlags.BundleMask;
            ruleSet = null;
            blockType = XmlBlockType.None;
            fullName = null;
            group = null;
            description = null;
            timeStamp = default(DateTime);
            publicKeyToken = null;
            text = null;
            signature = null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyOneBundleScript(
            string fileName,      /* in */
            string fullName,      /* in */
            Encoding encoding,    /* in */
            List<Script> scripts, /* in */
            ref byte[] data,      /* out */
            ref Result error      /* out */
            )
        {
            if (encoding == null)
            {
                error = "invalid bundle encoding";
                return false;
            }

            if (scripts == null)
            {
                error = "invalid bundle scripts";
                return false;
            }

            if (scripts.Count == 0)
            {
                error = String.Format(
                    "bundle {0} script {1} not found",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(fullName));

                return false;
            }

            Script script = scripts[0]; /* TODO: First one? */

            if (script == null)
            {
                error = String.Format(
                    "bundle {0} script {1} object is invalid",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(fullName));

                return false;
            }

            string text = script.Text;

            if (String.IsNullOrEmpty(text))
            {
                error = String.Format(
                    "bundle {0} script {1} text is invalid",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(fullName));

                return false;
            }

            data = encoding.GetBytes(text);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool VerifyBundleRecordSignature(
            byte[] signature, /* in */
            bool demand,      /* in */
            ref Result error  /* out */
            )
        {
            if (signature == null)
            {
                error = String.Format(
                    "invalid {0} {1} field value",
                    demand ? "demand" : "bundle",
                    BundleField.Signature);

                return false;
            }

            byte[] nullSignature = nullBundleSignature;

            if (ArrayOps.Equals(signature, nullSignature))
                return true;

            int wantSignatureLength = minimumSignatureLength;

            if (wantSignatureLength == 0)
                return true;

            if (wantSignatureLength < 0)
            {
                //
                // HACK: This means the length is in bits, not
                //       bytes.
                //
                wantSignatureLength /= ConversionOps.ByteBits;
                wantSignatureLength = -wantSignatureLength;
            }

            int haveSignatureLength = signature.Length;

            if (haveSignatureLength < wantSignatureLength)
            {
                error = String.Format(
                    "bad {0} {1} field value: have {2} bytes, " +
                    "want {3} bytes", demand ? "demand" : "bundle",
                    BundleField.Signature, haveSignatureLength,
                    wantSignatureLength);

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode VerifyBundleRecord(
            Interpreter interpreter,               /* in: OPTIONAL */
            IDataRecord record,                    /* in */
            CultureInfo cultureInfo,               /* in: OPTIONAL */
            bool demand,                           /* in */
            out Guid id,                           /* out */
            out string language,                   /* out */
            out long sequence,                     /* out */
            out string vendor,                     /* out */
            out string hashAlgorithmName,          /* out */
            out _IsolationLevel isolationLevel,    /* out */
            out SecurityLevel securityLevel,       /* out */
            out ScriptSecurityFlags securityFlags, /* out */
            out IRuleSet ruleSet,                  /* out */
            out XmlBlockType blockType,            /* out */
            out string fullName,                   /* out */
            out string group,                      /* out */
            out string description,                /* out */
            out DateTime timeStamp,                /* out */
            out byte[] publicKeyToken,             /* out */
            out string text,                       /* out */
            out byte[] signature,                  /* out */
            ref ResultList errors                  /* out */
            )
        {
            ResetBundleRecord(
                out id, out language, out sequence, out vendor,
                out hashAlgorithmName, out isolationLevel,
                out securityLevel, out securityFlags, out ruleSet,
                out blockType, out fullName, out group,
                out description, out timeStamp, out publicKeyToken,
                out text, out signature);

            ///////////////////////////////////////////////////////////////////

            ResultList localErrors = null;
            Result localError; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            if (record == null)
            {
                localError = "invalid data record";

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
                goto done;
            }

            ///////////////////////////////////////////////////////////////////

            int fieldCount = record.FieldCount;

            if (fieldCount != (int)BundleField.Count)
            {
                localError = String.Format(
                    "have {0} fields, want {1} fields",
                    fieldCount, (int)BundleField.Count);

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
                goto done;
            }

            ///////////////////////////////////////////////////////////////////

            byte[] bytes; /* REUSED */
            object enumValue; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            bytes = record[(int)BundleField.Id] as byte[];

            if ((bytes != null) &&
                (bytes.Length == Marshal.SizeOf(typeof(Guid))))
            {
                try
                {
                    id = new Guid(bytes); /* throw */
                }
                catch (Exception e)
                {
                    localError = e;

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }
            else
            {
                localError = String.Format(
                    "invalid {0} {1} field value",
                    demand ? "demand" : "bundle",
                    BundleField.Id);

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            language = record[(int)BundleField.Language] as string;

            if (String.IsNullOrEmpty(language) ||
                !SharedStringOps.SystemEquals(
                    language, GlobalState.GetPackageName()))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.Language,
                    FormatOps.WrapOrNull(language));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            try
            {
                sequence = (long)record[(int)BundleField.Sequence];

                if ((demand && (sequence >= 0)) ||
                    (!demand && (sequence <= 0)))
                {
                    localError = String.Format(
                        "invalid {0} {1} field value: {2}",
                        demand ? "demand" : "bundle",
                        BundleField.Sequence, sequence);

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }
            catch (Exception e)
            {
                localError = e;

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            vendor = record[(int)BundleField.Vendor] as string;

            if (String.IsNullOrEmpty(vendor))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.Vendor,
                    FormatOps.WrapOrNull(vendor));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////
            //
            // TODO (?): Use of "SHA512" is always enforced here.
            //
            hashAlgorithmName = record[(int)BundleField.HashAlgorithm] as string;

            if (String.IsNullOrEmpty(hashAlgorithmName) ||
                !SharedStringOps.SystemEquals(
                    hashAlgorithmName, HashOps.ModernBytesAlgorithmName))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.HashAlgorithm,
                    FormatOps.WrapOrNull(hashAlgorithmName));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            if (!record.IsDBNull((int)BundleField.IsolationLevel))
            {
                localError = null;

                enumValue = EnumOps.TryParseFlags(interpreter,
                    typeof(_IsolationLevel), isolationLevel.ToString(),
                    record[(int)BundleField.IsolationLevel] as string,
                    cultureInfo, true, true, true, ref localError);

                if (enumValue != null)
                {
                    isolationLevel = (_IsolationLevel)enumValue;

                    if ((isolationLevel != _IsolationLevel.None) &&
                        !FlagOps.HasFlags(isolationLevel,
                            _IsolationLevel.BaseMask, false))
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value: {2}",
                            demand ? "demand" : "bundle",
                            BundleField.IsolationLevel,
                            isolationLevel);

                        if (localErrors == null)
                            localErrors = new ResultList();

                        localErrors.Add(localError);
                    }
                }
                else
                {
                    if (localError == null)
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value",
                            demand ? "demand" : "bundle",
                            BundleField.IsolationLevel);
                    }

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!record.IsDBNull((int)BundleField.SecurityLevel))
            {
                localError = null;

                enumValue = EnumOps.TryParseFlags(interpreter,
                    typeof(SecurityLevel), securityLevel.ToString(),
                    record[(int)BundleField.SecurityLevel] as string,
                    cultureInfo, true, true, true, ref localError);

                if (enumValue != null)
                {
                    securityLevel = (SecurityLevel)enumValue;

                    if ((securityLevel != SecurityLevel.None) &&
                        !FlagOps.HasFlags(securityLevel,
                            SecurityLevel.BaseMask, false))
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value: {2}",
                            demand ? "demand" : "bundle",
                            BundleField.SecurityLevel,
                            securityLevel);

                        if (localErrors == null)
                            localErrors = new ResultList();

                        localErrors.Add(localError);
                    }
                }
                else
                {
                    if (localError == null)
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value",
                            demand ? "demand" : "bundle",
                            BundleField.SecurityLevel);
                    }

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!record.IsDBNull((int)BundleField.SecurityFlags))
            {
                localError = null;

                enumValue = EnumOps.TryParseFlags(interpreter,
                    typeof(ScriptSecurityFlags), securityFlags.ToString(),
                    record[(int)BundleField.SecurityFlags] as string,
                    cultureInfo, true, true, true, ref localError);

                if (enumValue != null)
                {
                    securityFlags = (ScriptSecurityFlags)enumValue;
                }
                else
                {
                    if (localError == null)
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value",
                            demand ? "demand" : "bundle",
                            BundleField.SecurityFlags);
                    }

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!record.IsDBNull((int)BundleField.RuleSet))
            {
                localError = null;

                ruleSet = RuleSet.Create(
                    record[(int)BundleField.RuleSet] as string,
                    cultureInfo, ref localError);

                if (ruleSet == null)
                {
                    if (localError == null)
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value",
                            demand ? "demand" : "bundle",
                            BundleField.RuleSet);
                    }

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!record.IsDBNull((int)BundleField.BlockType))
            {
                localError = null;

                enumValue = EnumOps.TryParseFlags(interpreter,
                    typeof(XmlBlockType), blockType.ToString(),
                    record[(int)BundleField.BlockType] as string,
                    cultureInfo, true, true, true, ref localError);

                if (enumValue != null)
                {
                    blockType = (XmlBlockType)enumValue;
                }
                else
                {
                    if (localError == null)
                    {
                        localError = String.Format(
                            "invalid {0} {1} field value",
                            demand ? "demand" : "bundle",
                            BundleField.BlockType);
                    }

                    if (localErrors == null)
                        localErrors = new ResultList();

                    localErrors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            fullName = record[(int)BundleField.FullName] as string;
            localError = null;

            if (!VerifyBundleFullName(fullName, demand, ref localError))
            {
                if (localError == null)
                {
                    localError = String.Format(
                        "invalid {0} {1} field value",
                        demand ? "demand" : "bundle",
                        BundleField.FullName);
                }

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            group = record[(int)BundleField.Group] as string;

            if (String.IsNullOrEmpty(group))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.Group,
                    FormatOps.WrapOrNull(group));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            description = record[(int)BundleField.Description] as string;

            if (String.IsNullOrEmpty(description))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.Description,
                    FormatOps.WrapOrNull(description));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            try
            {
                timeStamp = (DateTime)record[(int)BundleField.TimeStamp];
            }
            catch (Exception e)
            {
                localError = e;

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            publicKeyToken = record[(int)BundleField.PublicKeyToken] as byte[];

            if ((publicKeyToken == null) ||
                (publicKeyToken.Length != sizeof(long)))
            {
                localError = String.Format(
                    "invalid {0} {1} field value: {2}",
                    demand ? "demand" : "bundle",
                    BundleField.PublicKeyToken,
                    FormatOps.WrapOrNull(publicKeyToken, true));

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            text = record[(int)BundleField.Text] as string;
            localError = null;

            if (String.IsNullOrEmpty(text) || !Parser.IsComplete(
                    interpreter, text, ref localError))
            {
                if (localError == null)
                {
                    localError = String.Format(
                        "invalid {0} {1} field value: {2}",
                        demand ? "demand" : "bundle",
                        BundleField.Text,
                        FormatOps.WrapOrNull(text));
                }

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

            ///////////////////////////////////////////////////////////////////

            signature = record[(int)BundleField.Signature] as byte[];
            localError = null;

            if (!VerifyBundleRecordSignature(
                    signature, demand, ref localError))
            {
                if (localError == null)
                {
                    localError = String.Format(
                        "invalid {0} {1} field value",
                        demand ? "demand" : "bundle",
                        BundleField.Signature);
                }

                if (localErrors == null)
                    localErrors = new ResultList();

                localErrors.Add(localError);
            }

        done:

            if (localErrors != null)
            {
                if (errors != null)
                    errors.AddRange(localErrors);
                else
                    errors = localErrors;

                return ReturnCode.Error;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GatherBundleScripts(
            Interpreter interpreter,          /* in */
            CultureInfo cultureInfo,          /* in: OPTIONAL */
            IHaveScriptFlags haveScriptFlags, /* in: OPTIONAL */
            IClientData clientData,           /* in: OPTIONAL */
            Encoding encoding,                /* in */
            string fileName,                  /* in */
            byte[] password,                  /* in */
            string pattern,                   /* in */
            bool noCase,                      /* in */
            bool demand,                      /* in */
            ref List<Script> scripts          /* in, out */
            )
        {
            Result error = null;

            return GatherBundleScripts(
                interpreter, cultureInfo, haveScriptFlags, clientData,
                encoding, fileName, password, pattern, noCase, demand,
                ref scripts, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GatherBundleScripts(
            Interpreter interpreter,          /* in */
            CultureInfo cultureInfo,          /* in: OPTIONAL */
            IHaveScriptFlags haveScriptFlags, /* in: OPTIONAL */
            IClientData clientData,           /* in: OPTIONAL */
            Encoding encoding,                /* in */
            string fileName,                  /* in */
            byte[] password,                  /* in */
            string pattern,                   /* in */
            bool noCase,                      /* in */
            bool demand,                      /* in */
            ref List<Script> scripts,         /* in, out */
            ref Result error                  /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (encoding == null)
            {
                error = "invalid encoding";
                return ReturnCode.Error;
            }

            string connectionString = GetBundleConnectionString(
                fileName, password, ref error);

            if (connectionString == null)
                return ReturnCode.Error;

            string commandText = GetBundleCommandText(
                demand, ref error);

            if (commandText == null)
                return ReturnCode.Error;

            byte[] publicKeyToken1 = null;

            if (RuntimeOps.GetPublicKeyToken(String.Format(
                    "0x{0}", PublicKeyToken.SQLiteEnterprise),
                    cultureInfo, ref publicKeyToken1,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            byte[] publicKeyToken2 = null;

            if (RuntimeOps.GetPublicKeyToken(String.Format(
                    "0x{0}", PublicKeyToken.SQLite),
                    cultureInfo, ref publicKeyToken2,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            IDbConnection connection = null;

            try
            {
                DbConnectionType dbConnectionType1;
                DbConnectionType dbConnectionType2;

                GetBundleConnectionTypes(
                    out dbConnectionType1, out dbConnectionType2);

                DbConnectionType dbConnectionType = DbConnectionType.None;
                byte[] publicKeyToken = null; /* REUSED */
                ValueFlags valueFlags = ValueFlags.TrustedOnly;

                if (CreateDbConnection(
                        interpreter, dbConnectionType1,
                        dbConnectionType2, publicKeyToken1,
                        publicKeyToken2, connectionString,
                        null, null, null, null, valueFlags,
                        GetOtherDbConnectionTypes(
                            valueFlags, true, true, true),
                        GetOtherDbConnectionTypes(
                            valueFlags, true, true, false),
                        ref connection, ref dbConnectionType,
                        ref publicKeyToken, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((dbConnectionType != dbConnectionType1) &&
                    (dbConnectionType != dbConnectionType2))
                {
                    error = String.Format(
                        "database connection type mismatch, " +
                        "{0} versus {1} and {2}", dbConnectionType,
                        dbConnectionType1, dbConnectionType2);

                    return ReturnCode.Error;
                }

                if (!ArrayOps.Equals(publicKeyToken, publicKeyToken1) &&
                    !ArrayOps.Equals(publicKeyToken, publicKeyToken2))
                {
                    error = String.Format(
                        "database connection public key " +
                        "token mismatch, {0} versus {1} and {2}",
                        ArrayOps.ToHexadecimalString(publicKeyToken),
                        ArrayOps.ToHexadecimalString(publicKeyToken1),
                        ArrayOps.ToHexadecimalString(publicKeyToken2));

                    return ReturnCode.Error;
                }

                if (connection == null)
                {
                    error = String.Format(
                        "database connection type {0} with public " +
                        "key token {1} is missing", dbConnectionType,
                        ArrayOps.ToHexadecimalString(publicKeyToken));

                    return ReturnCode.Error;
                }

                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                ExecuteNonQuery(connection,
                    "PRAGMA locking_mode = EXCLUSIVE;");

                string integrityResult = ExecuteScalar(
                    connection, "PRAGMA integrity_check;") as string;

                if (!SharedStringOps.SystemEquals(integrityResult, "ok"))
                {
                    error = String.Format(
                        "database file {0} integrity check failed: {1}",
                        FormatOps.WrapOrNull(fileName), integrityResult);

                    return ReturnCode.Error;
                }

                ExecuteNonQuery(connection, String.Format(
                    "PRAGMA case_sensitive_like = {0};", !noCase));

                ScriptFlags localScriptFlags;
                EngineFlags localEngineFlags;
                SubstitutionFlags localSubstitutionFlags;
                EventFlags localEventFlags;
                ExpressionFlags localExpressionFlags;

                if (haveScriptFlags != null)
                {
                    localScriptFlags = haveScriptFlags.ScriptFlags;
                    localEngineFlags = haveScriptFlags.EngineFlags;
                    localSubstitutionFlags = haveScriptFlags.SubstitutionFlags;
                    localEventFlags = haveScriptFlags.EventFlags;
                    localExpressionFlags = haveScriptFlags.ExpressionFlags;
                }
                else
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        localScriptFlags = interpreter.ScriptFlagsNoLock;
                        localEngineFlags = interpreter.EngineFlagsNoLock;
                        localSubstitutionFlags = interpreter.SubstitutionFlagsNoLock;
                        localEventFlags = interpreter.EngineEventFlagsNoLock;
                        localExpressionFlags = interpreter.ExpressionFlagsNoLock;
                    }
                }

                List<Script> localScripts = new List<Script>();

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = commandText;

                    IDataParameterCollection parameters = command.Parameters;

                    if (parameters == null)
                    {
                        error = "database command is missing parameters";
                        return ReturnCode.Error;
                    }

                    IDbDataParameter parameter = command.CreateParameter();

                    if (parameter == null)
                    {
                        error = "could not create database parameter";
                        return ReturnCode.Error;
                    }

                    parameter.ParameterName = ":pattern";
                    parameter.DbType = DbType.String;
                    parameter.Value = pattern;

                    parameters.Add(parameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Guid id;
                            string language;
                            long sequence;
                            string vendor;
                            string hashAlgorithmName;
                            _IsolationLevel isolationLevel;
                            SecurityLevel securityLevel;
                            ScriptSecurityFlags securityFlags;
                            IRuleSet ruleSet;
                            XmlBlockType blockType;
                            string fullName;
                            string group;
                            string description;
                            DateTime timeStamp;
                            string text;
                            byte[] signature;
                            ResultList errors = null;

                            if (VerifyBundleRecord(
                                    interpreter, reader, cultureInfo, demand,
                                    out id, out language, out sequence,
                                    out vendor, out hashAlgorithmName,
                                    out isolationLevel, out securityLevel,
                                    out securityFlags, out ruleSet,
                                    out blockType, out fullName, out group,
                                    out description, out timeStamp,
                                    out publicKeyToken, out text,
                                    out signature, ref errors) != ReturnCode.Ok)
                            {
                                error = errors;
                                return ReturnCode.Error;
                            }

                            string path = String.Format(
                                "{0}{1}{2}", fileName, bundleNameDelimiter,
                                fullName);

                            byte[] fileBytes = encoding.GetBytes(text);

                            Script script = Script.InternalCreate(
                                id, null, group, description,
                                ScriptTypes.Bundle, text, fileName,
                                Parser.UnknownLine, Parser.UnknownLine,
                                true,
#if XML
                                blockType, timeStamp,
                                ArrayOps.ToHexadecimalString(publicKeyToken),
                                signature,
#endif
                                EngineMode.EvaluateScript,
                                localScriptFlags, localEngineFlags,
                                localSubstitutionFlags, localEventFlags,
                                localExpressionFlags, clientData,
                                new BundleData(
                                    language, sequence, vendor, path,
                                    fullName, hashAlgorithmName,
                                    fileBytes, isolationLevel,
                                    securityLevel, securityFlags,
                                    ruleSet
                                )) as Script;

                            if (script == null)
                            {
                                error = String.Format(
                                    "could not create script at sequence {0}",
                                    sequence);

                                return ReturnCode.Error;
                            }

                            localScripts.Add(script);
                        }
                    }
                }

                if (localScripts != null)
                {
                    if (scripts != null)
                        scripts.AddRange(localScripts);
                    else
                        scripts = localScripts;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CreateOtherDbConnection(
            Interpreter interpreter,           /* in */
            DbConnectionType dbConnectionType, /* in */
            byte[] publicKeyToken,             /* in */
            string connectionString,           /* in */
            string assemblyFileName,           /* in */
            string typeFullName,               /* in */
            string typeName,                   /* in */
            Type type,                         /* in */
            ValueFlags valueFlags,             /* in */
            ref IDbConnection connection,      /* out */
            ref Result error                   /* out */
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

            object[] args = new object[] { connectionString };
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

                Type localType = typeOrName as Type;

                if (localType == null)
                {
                    if (!MaybeResolveTypeForOtherDbConnection(
                            interpreter, appDomain, cultureInfo,
                            dbConnectionType, publicKeyToken,
                            assemblyFileName, typeOrName,
                            valueFlags, ref assembly,
                            ref attemptedLoad, ref localType,
                            ref errors))
                    {
                        continue;
                    }
                }

                bool success = false;
                object @object = null;

                try
                {
                    @object = Activator.CreateInstance(
                        localType, args);

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

                        @object = null;

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
            Interpreter interpreter,               /* in */
            DbConnectionType dbConnectionType1,    /* in */
            DbConnectionType dbConnectionType2,    /* in */
            byte[] publicKeyToken1,                /* in */
            byte[] publicKeyToken2,                /* in */
            string connectionString,               /* in */
            string assemblyFileName,               /* in */
            string typeFullName,                   /* in */
            string typeName,                       /* in */
            Type type,                             /* in */
            ValueFlags valueFlags,                 /* in */
            ref IDbConnection connection,          /* out */
            ref DbConnectionType dbConnectionType, /* out */
            ref byte[] publicKeyToken,             /* out */
            ref Result error                       /* out */
            )
        {
            bool usePublicKeyToken = (publicKeyToken1 != null) ||
                (publicKeyToken2 != null);

            return CreateDbConnection(
                interpreter, dbConnectionType1,
                dbConnectionType2, publicKeyToken1,
                publicKeyToken2, connectionString,
                assemblyFileName, typeFullName,
                typeName, type, valueFlags,
                GetOtherDbConnectionTypes(
                    valueFlags, true, usePublicKeyToken,
                    true),
                GetOtherDbConnectionTypes(
                    valueFlags, true, usePublicKeyToken,
                    false),
                ref connection, ref dbConnectionType,
                ref publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,                        /* in */
            DbConnectionType dbConnectionType1,             /* in */
            DbConnectionType dbConnectionType2,             /* in */
            byte[] publicKeyToken1,                         /* in */
            byte[] publicKeyToken2,                         /* in */
            string connectionString,                        /* in */
            string assemblyFileName,                        /* in */
            string typeFullName,                            /* in */
            string typeName,                                /* in */
            Type type,                                      /* in */
            ValueFlags valueFlags,                          /* in */
            ConnectionDictionary dbConnectionTypeFullNames, /* in */
            ConnectionDictionary dbConnectionTypeNames,     /* in */
            ref IDbConnection connection,                   /* out */
            ref DbConnectionType dbConnectionType,          /* out */
            ref byte[] publicKeyToken,                      /* out */
            ref Result error                                /* out */
            )
        {
            DbConnectionType[] dbConnectionTypes = {
                dbConnectionType1, dbConnectionType2
            };

            byte[][] publicKeyTokens = {
                publicKeyToken1, publicKeyToken2
            };

            int length = dbConnectionTypes.Length;
            ResultList errors = null;

            for (int index = 0; index < length; index++)
            {
                DbConnectionType localDbConnectionType =
                    dbConnectionTypes[index];

                if (localDbConnectionType == DbConnectionType.None)
                    continue;

                byte[] localPublicKeyToken = publicKeyTokens[index];
                Result localError = null;

                if (CreateDbConnection(
                        interpreter, localDbConnectionType,
                        localPublicKeyToken, connectionString,
                        assemblyFileName, typeFullName, typeName,
                        type, valueFlags, dbConnectionTypeFullNames,
                        dbConnectionTypeNames, ref connection,
                        ref localError) == ReturnCode.Ok)
                {
                    dbConnectionType = localDbConnectionType;
                    publicKeyToken = localPublicKeyToken;

                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,           /* in */
            DbConnectionType dbConnectionType, /* in */
            byte[] publicKeyToken,             /* in */
            string connectionString,           /* in */
            string assemblyFileName,           /* in */
            string typeFullName,               /* in */
            string typeName,                   /* in */
            Type type,                         /* in */
            ValueFlags valueFlags,             /* in */
            ref IDbConnection connection,      /* out */
            ref Result error                   /* out */
            )
        {
            bool usePublicKeyToken = (publicKeyToken != null);

            return CreateDbConnection(
                interpreter, dbConnectionType,
                publicKeyToken, connectionString,
                assemblyFileName, typeFullName,
                typeName, type, valueFlags,
                GetOtherDbConnectionTypes(
                    valueFlags, true, usePublicKeyToken,
                    true),
                GetOtherDbConnectionTypes(
                    valueFlags, true, usePublicKeyToken,
                    false),
                ref connection, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CreateDbConnection(
            Interpreter interpreter,                        /* in */
            DbConnectionType dbConnectionType,              /* in */
            byte[] publicKeyToken,                          /* in */
            string connectionString,                        /* in */
            string assemblyFileName,                        /* in */
            string typeFullName,                            /* in */
            string typeName,                                /* in */
            Type type,                                      /* in */
            ValueFlags valueFlags,                          /* in */
            ConnectionDictionary dbConnectionTypeFullNames, /* in */
            ConnectionDictionary dbConnectionTypeNames,     /* in */
            ref IDbConnection connection,                   /* out */
            ref Result error                                /* out */
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
                                interpreter, dbConnectionType, publicKeyToken,
                                connectionString, assemblyFileName, typeFullName,
                                typeName, type, valueFlags, ref connection,
                                ref error);
                        }
                    default:
                        {
                            //
                            // NOTE: Lookup the type name and/or full name and
                            //       then go to the "other" case (for database
                            //       connection types that are not "built-in").
                            //
                            int count = 0;
                            ConnectionTriplet value; /* REUSED */
                            string localTypeFullName = null; /* REUSED */
                            string localTypeName = null; /* REUSED */
                            string localAssemblyFileName = null; /* REUSED */
                            byte[] localPublicKeyToken = null; /* REUSED */

                            if ((dbConnectionTypeFullNames != null) &&
                                dbConnectionTypeFullNames.TryGetValue(
                                    dbConnectionType, out value))
                            {
                                if (value != null)
                                {
                                    localTypeFullName = value.X;

                                    if (localAssemblyFileName == null)
                                        localAssemblyFileName = value.Y;

                                    if (localPublicKeyToken == null)
                                        localPublicKeyToken = value.Z;
                                }

                                count++;
                            }

                            if ((dbConnectionTypeNames != null) &&
                                dbConnectionTypeNames.TryGetValue(
                                    dbConnectionType, out value))
                            {
                                if (value != null)
                                {
                                    localTypeName = value.X;

                                    if (localAssemblyFileName == null)
                                        localAssemblyFileName = value.Y;

                                    if (localPublicKeyToken == null)
                                        localPublicKeyToken = value.Z;
                                }

                                count++;
                            }

                            if (count > 0)
                            {
                                typeFullName = localTypeFullName;
                                typeName = localTypeName;
                                assemblyFileName = localAssemblyFileName;
                                publicKeyToken = localPublicKeyToken;

                                goto case DbConnectionType.Other;
                            }

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
            Interpreter interpreter,       /* in */
            CultureInfo cultureInfo,       /* in */
            string valueFormat,            /* in */
            ValueFlags valueFlags,         /* in */
            DateTimeKind dateTimeKind,     /* in */
            DateTimeStyles dateTimeStyles, /* in */
            IDbCommand command,            /* in */
            ArgumentList arguments,        /* in */
            int startIndex,                /* in */
            int stopIndex,                 /* in */
            bool verbatim,                 /* in */
            ref Result error               /* out */
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
            BlobBehavior blobBehavior,         /* in */
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
                                blobBehavior, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, false,
                                allowNull, pairs, names, noFixup,
                                alias, ref result) != ReturnCode.Ok)
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
                                blobBehavior, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, nested,
                                false, allowNull, pairs, names,
                                noFixup, alias, ref list,
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
                            ObjectOptionType.SqlExecute |
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
            BlobBehavior blobBehavior,         /* in */
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
                                cultureInfo, blobBehavior,
                                dateTimeBehavior, dateTimeKind,
                                dateTimeFormat, numberFormat,
                                nullValue, dbNullValue, errorValue,
                                limit, false, allowNull, pairs,
                                names, noFixup, alias, ref count,
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
                                blobBehavior, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, limit,
                                nested, false, allowNull, pairs,
                                names, noFixup, alias, ref list,
                                ref count,
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
                            ObjectOptionType.SqlExecute |
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
            BlobBehavior blobBehavior,         /* in */
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
                                    interpreter, value, cultureInfo,
                                    blobBehavior, dateTimeBehavior,
                                    dateTimeKind, dateTimeFormat,
                                    numberFormat, nullValue,
                                    dbNullValue, errorValue, alias);
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
                                varName, blobBehavior,
                                dateTimeBehavior, dateTimeKind,
                                dateTimeFormat, numberFormat,
                                nullValue, dbNullValue, errorValue,
                                limit, nested, allowNull, pairs,
                                names, andCount, returnType,
                                objectFlags, objectName, interpName,
                                create, dispose, alias, aliasRaw,
                                aliasAll, aliasReference, toString,
                                noFixup, ref close, ref result);
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
            BlobBehavior blobBehavior,         /* in */
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
            bool alias,                        /* in */
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
                                interpreter, value, cultureInfo,
                                blobBehavior, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, alias));
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
                                interpreter, value, cultureInfo,
                                blobBehavior, dateTimeBehavior,
                                dateTimeKind, dateTimeFormat,
                                numberFormat, nullValue,
                                dbNullValue, errorValue, alias));
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
            BlobBehavior blobBehavior,         /* in */
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
            bool alias,                        /* in */
            ref StringList list,               /* in, out */
            ref Result error                   /* out */
            )
        {
            StringList row = null;

            /* NO RESULT */
            GetDataRecordFieldValues(
                interpreter, record, cultureInfo,
                blobBehavior, dateTimeBehavior,
                dateTimeKind, dateTimeFormat,
                numberFormat, nullValue,
                dbNullValue, errorValue, clear,
                allowNull, pairs, names, noFixup,
                alias, ref row);

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
            BlobBehavior blobBehavior,         /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind,         /* in */
            string dateTimeFormat,             /* in */
            string numberFormat,               /* in */
            string nullValue,                  /* in */
            string dbNullValue,                /* in */
            string errorValue,                 /* in */
            bool noFixup,                      /* in */
            bool alias,                        /* in */
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
                   interpreter, value, cultureInfo,
                   blobBehavior, dateTimeBehavior,
                   dateTimeKind, dateTimeFormat,
                   numberFormat, nullValue,
                   dbNullValue, errorValue, alias);
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
            BlobBehavior blobBehavior,         /* in */
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
            bool alias,                        /* in */
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
                blobBehavior, dateTimeBehavior,
                dateTimeKind, dateTimeFormat,
                numberFormat, nullValue,
                dbNullValue, errorValue, clear,
                allowNull, pairs, names, noFixup,
                alias, ref row);

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
            BlobBehavior blobBehavior,         /* in */
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
            bool alias,                        /* in */
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
                        blobBehavior, dateTimeBehavior,
                        dateTimeKind, dateTimeFormat,
                        numberFormat, nullValue,
                        dbNullValue, errorValue, nested,
                        clear, allowNull, pairs, names,
                        noFixup, alias, ref list,
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
            BlobBehavior blobBehavior,         /* in */
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
            bool alias,                        /* in */
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
                        blobBehavior, dateTimeBehavior,
                        dateTimeKind, dateTimeFormat,
                        numberFormat, nullValue,
                        dbNullValue, errorValue, clear,
                        allowNull, pairs, names, noFixup,
                        alias, ref error) != ReturnCode.Ok)
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
            BlobBehavior blobBehavior,         /* in */
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
                                    cultureInfo, blobBehavior,
                                    dateTimeBehavior, dateTimeKind,
                                    dateTimeFormat, numberFormat,
                                    nullValue, dbNullValue,
                                    errorValue, noFixup, alias,
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
                                    cultureInfo, blobBehavior,
                                    dateTimeBehavior, dateTimeKind,
                                    dateTimeFormat, numberFormat,
                                    nullValue, dbNullValue,
                                    errorValue, noFixup, alias,
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
                        blobBehavior, dateTimeBehavior,
                        dateTimeKind, dateTimeFormat,
                        numberFormat, nullValue,
                        dbNullValue, errorValue,
                        localCount, limit, nested,
                        allowNull, pairs, names, andCount,
                        returnType, objectFlags, objectName,
                        interpName, create, dispose, alias,
                        aliasRaw, aliasAll, aliasReference,
                        toString, noFixup, ref localResult);

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
                            /* IGNORED */
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
