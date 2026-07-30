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
    /// <summary>
    /// This class provides the private helper methods used to support the
    /// scripting integration with ADO.NET style databases (e.g. the
    /// <c>[sql]</c> command) as well as the encrypted, signed script
    /// "bundle" subsystem.  It handles creating database connections of
    /// various kinds, validating database identifiers and parameters,
    /// building and verifying bundle paths and records, binding command
    /// parameters, and converting data records and readers into Eagle
    /// results, lists, arrays, and variables.
    /// </summary>
    [ObjectId("2e72f5b2-15df-4d65-98ec-fa01f3300ac8")]
    internal static class DataOps
    {
        #region Synchronization Objects
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class when building the set of "other" database connection types.
        /// </summary>
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, a failure to unset a result-set variable is reported
        /// via the interpreter complaint subsystem.
        /// </summary>
        private static bool ComplainOnUnsetError = true;

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Qualified Type Name Constants
        /// <summary>
        /// The format string used to build the assembly qualified type name of
        /// the Oracle database connection type; the public key token is the
        /// only format argument.
        /// </summary>
        private static string OracleFullTypeFormat =
            "System.Data.OracleClient.OracleConnection, " +
            "System.Data.OracleClient, Version=2.0.0.0, " +
            "Culture=neutral, PublicKeyToken={0}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the assembly qualified type name of
        /// the SQL Server Compact Edition database connection type; the public
        /// key token is the only format argument.
        /// </summary>
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
        /// <summary>
        /// The file name of the System.Data.SQLite managed assembly used when
        /// loading the SQLite database connection type by file.
        /// </summary>
        private static string SQLiteAssemblyFileName =
            "System.Data.SQLite.dll";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the assembly qualified type name of
        /// the System.Data.SQLite database connection type; the public key
        /// token is the only format argument.
        /// </summary>
        private static string SQLiteFullTypeFormat =
            "System.Data.SQLite.SQLiteConnection, System.Data.SQLite, " +
            "Version=1.0, Culture=neutral, PublicKeyToken={0}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The simple (non-assembly-qualified) type name of the
        /// System.Data.SQLite database connection type.
        /// </summary>
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
        /// <summary>
        /// The regular expression used to validate a database parameter name.
        /// </summary>
        private static Regex parameterRegEx = RegExOps.Create(
            "^[@A-Z_][0-9A-Z_]*$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

        /// <summary>
        /// The regular expression used to validate a database identifier (e.g.
        /// a table or column name).
        /// </summary>
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
        /// <summary>
        /// The regular expression used to validate the full name field value
        /// of a script bundle record.
        /// </summary>
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
        /// <summary>
        /// The sentinel byte sequence (the ASCII letters "NULL") used to
        /// represent a purposely absent script signature value in a script
        /// bundle.
        /// </summary>
        private static byte[] nullBundleSignature = {
            78, /* N */
            85, /* U */
            76, /* L */
            76  /* L */
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The minimum acceptable length, in bytes, of a script bundle record
        /// signature value.  A negative value is interpreted as a length in
        /// bits instead of bytes.
        /// </summary>
        private static int minimumSignatureLength = 2048; /* 16384-bit RSA */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The minimum acceptable length, in characters, of a combined script
        /// bundle path (file name, delimiter, and full name).
        /// </summary>
        private static int minimumBundlePathSize = 3; /* "<fileName>:<fullName>" */

        /// <summary>
        /// The minimum acceptable size, in bytes, of a script bundle database
        /// file; it is also the database page size used to validate the file
        /// size.
        /// </summary>
        private static int minimumBundleFileSize = 512; /* 1 database page */

        /// <summary>
        /// The character used to delimit the file name from the full name
        /// within a combined script bundle path.
        /// </summary>
        private static readonly char bundleNameDelimiter = Characters.Colon;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Support Methods
        /// <summary>
        /// This method validates that the specified value is a legal database
        /// identifier, throwing an exception if it is not.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property or argument being checked; this is used as
        /// the parameter name when throwing an exception.
        /// </param>
        /// <param name="propertyValue">
        /// The candidate identifier value to validate.
        /// </param>
        public static void CheckIdentifier(
            string propertyName, /* in */
            string propertyValue /* in */
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates that the specified value is a legal database
        /// identifier or parameter name, throwing an exception if it is not.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property or argument being checked; this is used as
        /// the parameter name when throwing an exception.
        /// </param>
        /// <param name="propertyValue">
        /// The candidate identifier or parameter name value to validate.
        /// </param>
        /// <param name="isParameterName">
        /// Non-zero if the value should be validated as a database parameter
        /// name; otherwise, it is validated as a database identifier.
        /// </param>
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
        /// <summary>
        /// This method formats the command text for execution against the
        /// target database, validating each supplied identifier or parameter
        /// name as a "last resort" check.
        /// </summary>
        /// <param name="format">
        /// The format string into which the identifier names are substituted.
        /// </param>
        /// <param name="parameterCount">
        /// The number of trailing names, from the end of the list, that are
        /// parameter names rather than identifiers.
        /// </param>
        /// <param name="names">
        /// The identifier and parameter names to validate and substitute into
        /// the format string.
        /// </param>
        /// <returns>
        /// The formatted command text.
        /// </returns>
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

        /// <summary>
        /// This method returns the public key token string associated with the
        /// specified database connection type.
        /// </summary>
        /// <param name="dbConnectionType">
        /// The database connection type whose public key token is needed.
        /// </param>
        /// <returns>
        /// The public key token string, or the null public key token string if
        /// the connection type is not recognized.
        /// </returns>
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

        /// <summary>
        /// This method builds the connection triplet (type name, optional
        /// assembly file name, and public key token) used to create a SQLite
        /// database connection.
        /// </summary>
        /// <param name="dbConnectionType">
        /// The SQLite database connection type whose triplet is needed.
        /// </param>
        /// <param name="useFullName">
        /// Non-zero to use the assembly qualified type name; otherwise, the
        /// simple type name is used.
        /// </param>
        /// <param name="useFileName">
        /// Non-zero to include the assembly file name in the triplet.
        /// </param>
        /// <returns>
        /// The connection triplet describing how to create the SQLite database
        /// connection.
        /// </returns>
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

        /// <summary>
        /// This method returns the mapping of the "built-in" database
        /// connection types to their assembly qualified type names.
        /// </summary>
        /// <returns>
        /// A dictionary mapping each built-in database connection type to its
        /// assembly qualified type name.
        /// </returns>
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

        /// <summary>
        /// This method returns the mapping of the "other" (optional, non
        /// built-in) database connection types to their type names.
        /// </summary>
        /// <param name="useSqlite">
        /// Non-zero to include the SQLite database connection types.
        /// </param>
        /// <param name="useFullName">
        /// Non-zero to use assembly qualified type names; otherwise, simple
        /// type names are used.
        /// </param>
        /// <param name="useFileName">
        /// Non-zero to include the assembly file name with each type name.
        /// </param>
        /// <returns>
        /// A dictionary mapping each "other" database connection type to its
        /// type name (and optionally its assembly file name).
        /// </returns>
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
                //       -OR- "https://sf.net/projects/sqlite-dotnet2/",
                //       etc).
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

        /// <summary>
        /// This method returns the mapping of the "other" (optional, non
        /// built-in) database connection types to their connection triplets,
        /// honoring the supplied trust and signing requirements.
        /// </summary>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="useSqlite">
        /// Non-zero to include the SQLite database connection types.
        /// </param>
        /// <param name="usePublicKeyToken">
        /// Non-zero if the public key token is required, which forces use of
        /// the assembly file name.
        /// </param>
        /// <param name="useFullName">
        /// Non-zero to use assembly qualified type names; otherwise, simple
        /// type names are used.
        /// </param>
        /// <returns>
        /// A dictionary mapping each "other" database connection type to its
        /// connection triplet.
        /// </returns>
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

        /// <summary>
        /// This method attempts to resolve the type used to create an "other"
        /// database connection, optionally loading its assembly from a trusted,
        /// strong-name verified file beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used for trust checks and type resolution;
        /// this parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain used when resolving the type.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during type resolution; this parameter may be
        /// null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The database connection type being resolved, used for diagnostics.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token of the assembly; when present, the
        /// assembly file must be strong-name signed and match it.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeOrName">
        /// The type, or the type name string, to resolve.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="assembly">
        /// Upon success, receives the assembly that was loaded from the file,
        /// if any.
        /// </param>
        /// <param name="attemptedLoad">
        /// On input and output, whether an assembly load has already been
        /// attempted; the load is a one-shot operation.
        /// </param>
        /// <param name="type">
        /// Upon success, receives the resolved type.
        /// </param>
        /// <param name="errors">
        /// On input and output, the list to which any errors encountered are
        /// appended.
        /// </param>
        /// <returns>
        /// True if the type was resolved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method appends the standard SQLite connection string settings
        /// used for reading a script bundle database, optionally including a
        /// per-file content hash value.
        /// </summary>
        /// <param name="idIndex">
        /// The index used to form the identifier setting name for the supplied
        /// hash value.
        /// </param>
        /// <param name="hashValue">
        /// The content hash value to embed in the connection string; this
        /// parameter may be null.
        /// </param>
        /// <param name="builder">
        /// On input and output, the string builder to which the connection
        /// string settings are appended; it is created if it is null.
        /// </param>
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

        /// <summary>
        /// This method builds a combined script bundle path from the specified
        /// file name and full name, after verifying both.
        /// </summary>
        /// <param name="fileName">
        /// The bundle database file name.
        /// </param>
        /// <param name="fullName">
        /// The bundle full name (the script path within the bundle).
        /// </param>
        /// <param name="demand">
        /// Non-zero if the full name is being verified for a demand-loaded
        /// script; otherwise, zero.
        /// </param>
        /// <returns>
        /// The combined bundle path, or null if verification fails.
        /// </returns>
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

        /// <summary>
        /// This method builds a combined script bundle path from the specified
        /// file name and full name, after verifying both.
        /// </summary>
        /// <param name="fileName">
        /// The bundle database file name.
        /// </param>
        /// <param name="fullName">
        /// The bundle full name (the script path within the bundle).
        /// </param>
        /// <param name="demand">
        /// Non-zero if the full name is being verified for a demand-loaded
        /// script; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The combined bundle path, or null if verification fails.
        /// </returns>
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

        /// <summary>
        /// This method verifies a combined script bundle path and splits it
        /// into its file name and full name components.
        /// </summary>
        /// <param name="path">
        /// The combined bundle path to verify and split.
        /// </param>
        /// <param name="demand">
        /// Non-zero if the full name is being verified for a demand-loaded
        /// script; otherwise, zero.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the bundle database file name.
        /// </param>
        /// <param name="fullName">
        /// Upon success, receives the bundle full name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the bundle path is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies that the specified script bundle database file
        /// exists and has a valid size, normalizing the file name to its full
        /// path on success.
        /// </summary>
        /// <param name="fileName">
        /// On input, the bundle database file name to verify; upon success, it
        /// is replaced with the fully qualified file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the bundle file name is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies that the specified script bundle full name is
        /// non-empty and matches the bundle full name regular expression.
        /// </summary>
        /// <param name="fullName">
        /// The bundle full name to verify.
        /// </param>
        /// <param name="demand">
        /// Non-zero if the full name is being verified for a demand-loaded
        /// script; otherwise, zero.  This affects the error message wording.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the bundle full name is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method builds the SQLite connection string used to open the
        /// specified script bundle database file, embedding a content hash and
        /// an optional password.
        /// </summary>
        /// <param name="fileName">
        /// The bundle database file name.
        /// </param>
        /// <param name="password">
        /// The optional password bytes used to open an encrypted bundle
        /// database; this parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The connection string, or null if the file cannot be verified or
        /// hashed.
        /// </returns>
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

        /// <summary>
        /// This method executes the specified non-query command text against
        /// the specified database connection.
        /// </summary>
        /// <param name="connection">
        /// The open database connection against which the command is executed.
        /// </param>
        /// <param name="commandText">
        /// The command text to execute; if it is null, nothing is executed.
        /// </param>
        /// <returns>
        /// The number of rows affected, or the value of
        /// <see cref="Count.None" /> if the command text is null.
        /// </returns>
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

        /// <summary>
        /// This method executes the specified command text against the
        /// specified database connection and returns the first column of the
        /// first row of the result set.
        /// </summary>
        /// <param name="connection">
        /// The open database connection against which the command is executed.
        /// </param>
        /// <param name="commandText">
        /// The command text to execute; if it is null, nothing is executed.
        /// </param>
        /// <returns>
        /// The scalar result value, or the value of
        /// <see cref="Count.None" /> if the command text is null.
        /// </returns>
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

        /// <summary>
        /// This method returns the pair of database connection types, in
        /// preference order, that are tried when opening a script bundle
        /// database.
        /// </summary>
        /// <param name="dbConnectionType1">
        /// Upon return, receives the first (preferred) bundle database
        /// connection type.
        /// </param>
        /// <param name="dbConnectionType2">
        /// Upon return, receives the second (fallback) bundle database
        /// connection type.
        /// </param>
        public static void GetBundleConnectionTypes(
            out DbConnectionType dbConnectionType1, /* out */
            out DbConnectionType dbConnectionType2  /* out */
            )
        {
            dbConnectionType1 = DbConnectionType.SQLiteEnterprise;
            dbConnectionType2 = DbConnectionType.SQLite;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the SQL command text used to select the script
        /// records from a script bundle database.
        /// </summary>
        /// <param name="demand">
        /// Non-zero to select demand-loaded scripts (negative sequence
        /// numbers); otherwise, the normally-loaded scripts are selected.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The SQL command text used to select the bundle script records.
        /// </returns>
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

        /// <summary>
        /// This method resets every output field of a script bundle record to
        /// its default value.
        /// </summary>
        /// <param name="id">
        /// Upon return, receives the default bundle record identifier.
        /// </param>
        /// <param name="language">
        /// Upon return, receives the default bundle record language.
        /// </param>
        /// <param name="sequence">
        /// Upon return, receives the default bundle record sequence number.
        /// </param>
        /// <param name="vendor">
        /// Upon return, receives the default bundle record vendor.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// Upon return, receives the default bundle record hash algorithm name.
        /// </param>
        /// <param name="isolationLevel">
        /// Upon return, receives the default bundle record isolation level.
        /// </param>
        /// <param name="securityLevel">
        /// Upon return, receives the default bundle record security level.
        /// </param>
        /// <param name="securityFlags">
        /// Upon return, receives the default bundle record security flags.
        /// </param>
        /// <param name="ruleSet">
        /// Upon return, receives the default bundle record rule set.
        /// </param>
        /// <param name="blockType">
        /// Upon return, receives the default bundle record block type.
        /// </param>
        /// <param name="fullName">
        /// Upon return, receives the default bundle record full name.
        /// </param>
        /// <param name="group">
        /// Upon return, receives the default bundle record group.
        /// </param>
        /// <param name="description">
        /// Upon return, receives the default bundle record description.
        /// </param>
        /// <param name="timeStamp">
        /// Upon return, receives the default bundle record time stamp.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon return, receives the default bundle record public key token.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the default bundle record script text.
        /// </param>
        /// <param name="signature">
        /// Upon return, receives the default bundle record signature.
        /// </param>
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

        /// <summary>
        /// This method verifies that exactly one usable script was gathered
        /// from a bundle and returns its text as encoded bytes.
        /// </summary>
        /// <param name="fileName">
        /// The bundle database file name, used for diagnostics.
        /// </param>
        /// <param name="fullName">
        /// The bundle full name, used for diagnostics.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the script text into bytes.
        /// </param>
        /// <param name="scripts">
        /// The list of scripts gathered from the bundle; the first one is
        /// used.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the encoded bytes of the script text.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if a usable script was found and encoded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies that a script bundle record signature is
        /// present and of sufficient length, treating the null-signature
        /// sentinel as valid.
        /// </summary>
        /// <param name="signature">
        /// The signature bytes to verify.
        /// </param>
        /// <param name="demand">
        /// Non-zero if the record belongs to a demand-loaded script; otherwise,
        /// zero.  This affects the error message wording.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the signature is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method validates every field of a script bundle data record,
        /// extracting and converting each field value while accumulating any
        /// errors encountered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used for enumeration parsing and script
        /// completeness checks; this parameter may be null.
        /// </param>
        /// <param name="record">
        /// The data record whose fields are validated and extracted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during enumeration parsing; this parameter may be
        /// null.
        /// </param>
        /// <param name="demand">
        /// Non-zero if the record belongs to a demand-loaded script; otherwise,
        /// zero.  This affects validation and error message wording.
        /// </param>
        /// <param name="id">
        /// Upon return, receives the bundle record identifier.
        /// </param>
        /// <param name="language">
        /// Upon return, receives the bundle record language.
        /// </param>
        /// <param name="sequence">
        /// Upon return, receives the bundle record sequence number.
        /// </param>
        /// <param name="vendor">
        /// Upon return, receives the bundle record vendor.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// Upon return, receives the bundle record hash algorithm name.
        /// </param>
        /// <param name="isolationLevel">
        /// Upon return, receives the bundle record isolation level.
        /// </param>
        /// <param name="securityLevel">
        /// Upon return, receives the bundle record security level.
        /// </param>
        /// <param name="securityFlags">
        /// Upon return, receives the bundle record security flags.
        /// </param>
        /// <param name="ruleSet">
        /// Upon return, receives the bundle record rule set.
        /// </param>
        /// <param name="blockType">
        /// Upon return, receives the bundle record block type.
        /// </param>
        /// <param name="fullName">
        /// Upon return, receives the bundle record full name.
        /// </param>
        /// <param name="group">
        /// Upon return, receives the bundle record group.
        /// </param>
        /// <param name="description">
        /// Upon return, receives the bundle record description.
        /// </param>
        /// <param name="timeStamp">
        /// Upon return, receives the bundle record time stamp.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon return, receives the bundle record public key token.
        /// </param>
        /// <param name="text">
        /// Upon return, receives the bundle record script text.
        /// </param>
        /// <param name="signature">
        /// Upon return, receives the bundle record signature.
        /// </param>
        /// <param name="errors">
        /// On input and output, the list to which any errors encountered are
        /// appended.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if every field was valid; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method opens the specified script bundle database and gathers
        /// the matching, verified scripts into the supplied list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to open the connection, verify the
        /// records, and create the scripts.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during parsing and conversion; this parameter may
        /// be null.
        /// </param>
        /// <param name="haveScriptFlags">
        /// The optional source of the script and engine flags used when
        /// creating the scripts; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data associated with each created script; this
        /// parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the script text into bytes.
        /// </param>
        /// <param name="fileName">
        /// The bundle database file name.
        /// </param>
        /// <param name="password">
        /// The optional password bytes used to open an encrypted bundle
        /// database; this parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to match script full names; this parameter
        /// may be null to match all scripts.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching of script full names.
        /// </param>
        /// <param name="demand">
        /// Non-zero to gather demand-loaded scripts; otherwise, the
        /// normally-loaded scripts are gathered.
        /// </param>
        /// <param name="scripts">
        /// On input and output, the list to which the gathered scripts are
        /// added; it is created if it is null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method opens the specified script bundle database and gathers
        /// the matching, verified scripts into the supplied list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to open the connection, verify the
        /// records, and create the scripts.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during parsing and conversion; this parameter may
        /// be null.
        /// </param>
        /// <param name="haveScriptFlags">
        /// The optional source of the script and engine flags used when
        /// creating the scripts; this parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data associated with each created script; this
        /// parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the script text into bytes.
        /// </param>
        /// <param name="fileName">
        /// The bundle database file name.
        /// </param>
        /// <param name="password">
        /// The optional password bytes used to open an encrypted bundle
        /// database; this parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to match script full names; this parameter
        /// may be null to match all scripts.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching of script full names.
        /// </param>
        /// <param name="demand">
        /// Non-zero to gather demand-loaded scripts; otherwise, the
        /// normally-loaded scripts are gathered.
        /// </param>
        /// <param name="scripts">
        /// On input and output, the list to which the gathered scripts are
        /// added; it is created if it is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates an "other" (non built-in) database connection
        /// by resolving and instantiating the supplied type, full type name, or
        /// type name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this parameter
        /// may be null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The database connection type being created, used for diagnostics.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token of the assembly containing the type;
        /// this parameter may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string passed to the connection constructor.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeFullName">
        /// The assembly qualified type name of the connection type, if any.
        /// </param>
        /// <param name="typeName">
        /// The simple type name of the connection type, if any.
        /// </param>
        /// <param name="type">
        /// The connection type itself, if already resolved; this parameter may
        /// be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="connection">
        /// Upon success, receives the created database connection.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates a database connection by trying two candidate
        /// connection types in order, reporting which one succeeded.  The sets
        /// of "other" connection types are derived automatically.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this parameter
        /// may be null.
        /// </param>
        /// <param name="dbConnectionType1">
        /// The first (preferred) candidate database connection type.
        /// </param>
        /// <param name="dbConnectionType2">
        /// The second (fallback) candidate database connection type.
        /// </param>
        /// <param name="publicKeyToken1">
        /// The expected public key token for the first candidate type; this
        /// parameter may be null.
        /// </param>
        /// <param name="publicKeyToken2">
        /// The expected public key token for the second candidate type; this
        /// parameter may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string passed to the connection constructor.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeFullName">
        /// The assembly qualified type name of the connection type, if any.
        /// </param>
        /// <param name="typeName">
        /// The simple type name of the connection type, if any.
        /// </param>
        /// <param name="type">
        /// The connection type itself, if already resolved; this parameter may
        /// be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="connection">
        /// Upon success, receives the created database connection.
        /// </param>
        /// <param name="dbConnectionType">
        /// Upon success, receives the candidate connection type that was used.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, receives the public key token that was used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates a database connection by trying two candidate
        /// connection types in order, using the supplied dictionaries of
        /// "other" connection type names, and reports which one succeeded.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this parameter
        /// may be null.
        /// </param>
        /// <param name="dbConnectionType1">
        /// The first (preferred) candidate database connection type.
        /// </param>
        /// <param name="dbConnectionType2">
        /// The second (fallback) candidate database connection type.
        /// </param>
        /// <param name="publicKeyToken1">
        /// The expected public key token for the first candidate type; this
        /// parameter may be null.
        /// </param>
        /// <param name="publicKeyToken2">
        /// The expected public key token for the second candidate type; this
        /// parameter may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string passed to the connection constructor.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeFullName">
        /// The assembly qualified type name of the connection type, if any.
        /// </param>
        /// <param name="typeName">
        /// The simple type name of the connection type, if any.
        /// </param>
        /// <param name="type">
        /// The connection type itself, if already resolved; this parameter may
        /// be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="dbConnectionTypeFullNames">
        /// The dictionary mapping "other" connection types to their assembly
        /// qualified type name triplets.
        /// </param>
        /// <param name="dbConnectionTypeNames">
        /// The dictionary mapping "other" connection types to their simple type
        /// name triplets.
        /// </param>
        /// <param name="connection">
        /// Upon success, receives the created database connection.
        /// </param>
        /// <param name="dbConnectionType">
        /// Upon success, receives the candidate connection type that was used.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, receives the public key token that was used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates a database connection of the specified single
        /// connection type.  The sets of "other" connection types are derived
        /// automatically.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this parameter
        /// may be null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The database connection type to create.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token of the assembly containing the type;
        /// this parameter may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string passed to the connection constructor.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeFullName">
        /// The assembly qualified type name of the connection type, if any.
        /// </param>
        /// <param name="typeName">
        /// The simple type name of the connection type, if any.
        /// </param>
        /// <param name="type">
        /// The connection type itself, if already resolved; this parameter may
        /// be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="connection">
        /// Upon success, receives the created database connection.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method creates a database connection of the specified single
        /// connection type, using the supplied dictionaries of "other"
        /// connection type names to resolve non built-in types.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used during type resolution; this parameter
        /// may be null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The database connection type to create.
        /// </param>
        /// <param name="publicKeyToken">
        /// The expected public key token of the assembly containing the type;
        /// this parameter may be null.
        /// </param>
        /// <param name="connectionString">
        /// The connection string passed to the connection constructor.
        /// </param>
        /// <param name="assemblyFileName">
        /// The file name of the assembly to load, if any.
        /// </param>
        /// <param name="typeFullName">
        /// The assembly qualified type name of the connection type, if any.
        /// </param>
        /// <param name="typeName">
        /// The simple type name of the connection type, if any.
        /// </param>
        /// <param name="type">
        /// The connection type itself, if already resolved; this parameter may
        /// be null.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags that govern, among other things, whether only
        /// trusted assembly files may be used.
        /// </param>
        /// <param name="dbConnectionTypeFullNames">
        /// The dictionary mapping "other" connection types to their assembly
        /// qualified type name triplets.
        /// </param>
        /// <param name="dbConnectionTypeNames">
        /// The dictionary mapping "other" connection types to their simple type
        /// name triplets.
        /// </param>
        /// <param name="connection">
        /// Upon success, receives the created database connection.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method parses the specified arguments, each describing a
        /// database parameter, and adds the resulting parameters to the
        /// specified database command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve opaque object handles and
        /// values; this parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value parsing.
        /// </param>
        /// <param name="valueFormat">
        /// The optional format string used when parsing parameter values.
        /// </param>
        /// <param name="valueFlags">
        /// The value flags used when parsing parameter values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when parsing date and time values.
        /// </param>
        /// <param name="dateTimeStyles">
        /// The date and time styles used when parsing date and time values.
        /// </param>
        /// <param name="command">
        /// The database command to which the parsed parameters are added.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments, each describing one parameter as a list of
        /// name, type, value, size, and value flags.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first argument to process.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last argument to process; a negative value means
        /// the final argument.
        /// </param>
        /// <param name="verbatim">
        /// Non-zero to treat each parameter value verbatim, without value
        /// conversion.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method converts a single database data record into an Eagle
        /// result in the requested format, optionally storing it into a
        /// variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to store variables and fix up values;
        /// this parameter may be null.
        /// </param>
        /// <param name="binder">
        /// The binder used when fixing up an object return value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="record">
        /// The data record to convert.
        /// </param>
        /// <param name="options">
        /// The options used when fixing up an object return value.
        /// </param>
        /// <param name="resultFormat">
        /// The format in which the record is converted into a result.
        /// </param>
        /// <param name="varName">
        /// The name of the variable into which the result is stored; this
        /// parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index used when storing the result into a
        /// variable; this parameter may be null.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="count">
        /// The current record count, used when an associated count value is
        /// also stored.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records; this is used by related conversion
        /// methods.
        /// </param>
        /// <param name="nested">
        /// Non-zero to produce a nested list (one sub-list per record).
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="andCount">
        /// Non-zero to also store the record count alongside the result.
        /// </param>
        /// <param name="returnType">
        /// The desired return type used when fixing up an object return value.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when fixing up an object return value.
        /// </param>
        /// <param name="objectName">
        /// The object name used when fixing up an object return value.
        /// </param>
        /// <param name="interpName">
        /// The interpreter name used when fixing up an object return value.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle for an object return
        /// value.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose of the object when its handle is removed.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for an object return value.
        /// </param>
        /// <param name="aliasRaw">
        /// Non-zero to create a raw command alias for an object return value.
        /// </param>
        /// <param name="aliasAll">
        /// Non-zero to create aliases for all members of an object return
        /// value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add an opaque object reference for the created alias.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert an object return value to its string form.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the converted result or the empty string;
        /// upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified data reader has already
        /// been transferred to the interpreter as an opaque object handle, in
        /// which case it no longer needs to be closed here.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose object list is checked; this parameter
        /// may be null.
        /// </param>
        /// <param name="reader">
        /// The data reader to look for; this parameter may be null.
        /// </param>
        /// <returns>
        /// True if the data reader is owned by the interpreter; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method converts every record produced by a database data
        /// reader into an Eagle result in the requested format, optionally
        /// storing it into a variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to store variables and fix up values;
        /// this parameter may be null.
        /// </param>
        /// <param name="binder">
        /// The binder used when fixing up an object return value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="reader">
        /// The data reader whose records are converted.
        /// </param>
        /// <param name="options">
        /// The options used when fixing up an object return value.
        /// </param>
        /// <param name="resultFormat">
        /// The format in which the records are converted into a result.
        /// </param>
        /// <param name="varName">
        /// The name of the variable into which the result is stored; this
        /// parameter may be null.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records to convert; a value of
        /// <see cref="Limits.Unlimited" /> means no limit.
        /// </param>
        /// <param name="nested">
        /// Non-zero to produce a nested list (one sub-list per record).
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="andCount">
        /// Non-zero to also store the record count alongside the result.
        /// </param>
        /// <param name="returnType">
        /// The desired return type used when fixing up an object return value.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when fixing up an object return value.
        /// </param>
        /// <param name="objectName">
        /// The object name used when fixing up an object return value.
        /// </param>
        /// <param name="interpName">
        /// The interpreter name used when fixing up an object return value.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle for an object return
        /// value.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose of the object when its handle is removed.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for an object return value.
        /// </param>
        /// <param name="aliasRaw">
        /// Non-zero to create a raw command alias for an object return value.
        /// </param>
        /// <param name="aliasAll">
        /// Non-zero to create aliases for all members of an object return
        /// value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add an opaque object reference for the created alias.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert an object return value to its string form.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="close">
        /// On input and output, whether the caller should close the data
        /// reader; this may be cleared when the reader is transferred to the
        /// interpreter.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the converted result or the empty string;
        /// upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
#if XML
                case DbResultFormat.DataTable:
                    {
                        IDataTable dataTable = CreateDataTable(
                            reader, interpreter, cultureInfo,
                            blobBehavior, dateTimeBehavior,
                            dateTimeKind, dateTimeFormat,
                            numberFormat, nullValue, dbNullValue,
                            errorValue, ref result);

                        if (dataTable == null)
                            return ReturnCode.Error;

                        ObjectOptionType objectOptionType =
                            ObjectOptionType.SqlExecute |
                            ObjectOps.GetOptionType(aliasRaw, aliasAll);

                        if (MarshalOps.FixupReturnValue(
                                interpreter, binder, cultureInfo,
                                returnType, objectFlags, options,
                                ObjectOps.GetInvokeOptions(objectOptionType),
                                objectOptionType, objectName, interpName,
                                dataTable, create, dispose, alias,
                                aliasReference, toString,
                                ref result) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                        break;
                    }
#endif
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

        /// <summary>
        /// This method converts a database field value into its string form
        /// without performing any value fix up.
        /// </summary>
        /// <param name="value">
        /// The database field value to convert.
        /// </param>
        /// <returns>
        /// The string form of the specified value.
        /// </returns>
        private static string DataValueToString(
            object value /* in */
            )
        {
            return StringOps.GetStringFromObject(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method executes the specified database command and converts its
        /// result into an Eagle result in the requested format.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to store variables and fix up values;
        /// this parameter may be null.
        /// </param>
        /// <param name="binder">
        /// The binder used when fixing up an object return value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="command">
        /// The database command to execute.
        /// </param>
        /// <param name="options">
        /// The options used when fixing up an object return value.
        /// </param>
        /// <param name="executeType">
        /// The kind of execution to perform (non-query, scalar, or reader).
        /// </param>
        /// <param name="commandBehavior">
        /// The command behavior used when executing a reader.
        /// </param>
        /// <param name="resultFormat">
        /// The format in which the result is converted.
        /// </param>
        /// <param name="varName">
        /// The name of the variable into which the result is stored; this
        /// parameter may be null.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records to convert; a value of
        /// <see cref="Limits.Unlimited" /> means no limit.
        /// </param>
        /// <param name="nested">
        /// Non-zero to produce a nested list (one sub-list per record).
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="returnType">
        /// The desired return type used when fixing up an object return value.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when fixing up an object return value.
        /// </param>
        /// <param name="objectName">
        /// The object name used when fixing up an object return value.
        /// </param>
        /// <param name="interpName">
        /// The interpreter name used when fixing up an object return value.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle for an object return
        /// value.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose of the object when its handle is removed.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for an object return value.
        /// </param>
        /// <param name="aliasRaw">
        /// Non-zero to create a raw command alias for an object return value.
        /// </param>
        /// <param name="aliasAll">
        /// Non-zero to create aliases for all members of an object return
        /// value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add an opaque object reference for the created alias.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert an object return value to its string form.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the converted result; upon failure, receives
        /// information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method appends the field names of the specified data record to
        /// the supplied list.
        /// </summary>
        /// <param name="record">
        /// The data record whose field names are gathered; this parameter may
        /// be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the field names are added; it
        /// is created if it is null or being cleared.
        /// </param>
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

        /// <summary>
        /// This method appends the raw field values of the specified data
        /// record to the supplied list of objects.
        /// </summary>
        /// <param name="record">
        /// The data record whose field values are gathered; this parameter may
        /// be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the field values are added;
        /// it is created if it is null or being cleared.
        /// </param>
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

        /// <summary>
        /// This method appends the field data type names of the specified data
        /// record to the supplied list.
        /// </summary>
        /// <param name="record">
        /// The data record whose field data type names are gathered; this
        /// parameter may be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the field data type names are
        /// added; it is created if it is null or being cleared.
        /// </param>
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

        /// <summary>
        /// This method appends the field types of the specified data record to
        /// the supplied list.
        /// </summary>
        /// <param name="record">
        /// The data record whose field types are gathered; this parameter may
        /// be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the field types are added; it
        /// is created if it is null or being cleared.
        /// </param>
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

        /// <summary>
        /// This method appends the converted (string) field values of the
        /// specified data record to the supplied list, optionally including
        /// field names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context; this parameter is not used.
        /// </param>
        /// <param name="record">
        /// The data record whose field values are gathered; this parameter may
        /// be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the field values are added;
        /// it is created if it is null or being cleared.
        /// </param>
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

        /// <summary>
        /// This method creates a detached, in-memory copy of the specified data
        /// record, capturing its field names, values, type names, and types.
        /// </summary>
        /// <param name="record">
        /// The data record to copy.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The detached copy of the data record, or null on failure.
        /// </returns>
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

#if XML
        /// <summary>
        /// This method creates and populates a data table from the records
        /// produced by the specified database data reader.
        /// </summary>
        /// <param name="reader">
        /// The data reader whose records are loaded into the data table.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context used during value conversion; this parameter
        /// may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The populated data table, or null on failure.
        /// </returns>
        public static IDataTable CreateDataTable(
            IDataReader reader,        /* in */
            Interpreter interpreter,   /* in */
            CultureInfo cultureInfo,   /* in */
            BlobBehavior blobBehavior, /* in */
            DateTimeBehavior dateTimeBehavior, /* in */
            DateTimeKind dateTimeKind, /* in */
            string dateTimeFormat,     /* in */
            string numberFormat,       /* in */
            string nullValue,          /* in */
            string dbNullValue,        /* in */
            string errorValue,         /* in */
            ref Result result          /* out */
            )
        {
            if (reader == null)
            {
                result = "invalid data reader";
                return null;
            }

            _DataTable dataTable = new _DataTable(
                interpreter, cultureInfo, blobBehavior,
                dateTimeBehavior, dateTimeKind, dateTimeFormat,
                numberFormat, nullValue, dbNullValue,
                errorValue);

            dataTable.Load(reader);

            return dataTable;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method cannot currently "fail"; however, its
        //          return code should still be checked by the caller.
        //
        /// <summary>
        /// This method converts a single database data record into list form
        /// and appends it to the supplied list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context; this parameter is not used.
        /// </param>
        /// <param name="record">
        /// The data record to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="nested">
        /// Non-zero to append the record as a single nested sub-list; otherwise,
        /// its elements are appended individually.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh row list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the converted record is
        /// appended; it is created if it is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" />; this method does not currently fail.
        /// </returns>
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

        /// <summary>
        /// This method unsets the specified variable, optionally reporting any
        /// failure via the interpreter complaint subsystem.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose variable is unset; this parameter may
        /// be null.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to unset; this parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The optional array element index to unset; this parameter may be
        /// null.
        /// </param>
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

        /// <summary>
        /// This method converts a single database value and stores it into the
        /// specified variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose variable is set.
        /// </param>
        /// <param name="value">
        /// The database value to convert and store.
        /// </param>
        /// <param name="varName">
        /// The name of the variable into which the value is stored; this
        /// parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index used when storing the value; this parameter
        /// may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and store the value as-is.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method converts a single database data record into list form
        /// and stores it into the specified array element variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose variable is set.
        /// </param>
        /// <param name="record">
        /// The data record to convert.
        /// </param>
        /// <param name="varName">
        /// The name of the variable into which the record is stored; this
        /// parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index used when storing the record; this parameter
        /// may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh row list rather than appending to any
        /// existing one.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method converts every record produced by a database data
        /// reader into list form and appends them to the supplied list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context; this parameter is not used.
        /// </param>
        /// <param name="reader">
        /// The data reader whose records are converted.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records to convert; a value of
        /// <see cref="Limits.Unlimited" /> means no limit.
        /// </param>
        /// <param name="nested">
        /// Non-zero to append each record as a single nested sub-list.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh row list for each record.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="list">
        /// On input and output, the list to which the converted records are
        /// appended.
        /// </param>
        /// <param name="count">
        /// On input and output, the running count of records converted, which
        /// is incremented by this method.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method converts every record produced by a database data
        /// reader into elements of the specified array variable, also recording
        /// the field names and the record count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose array variable is populated.
        /// </param>
        /// <param name="reader">
        /// The data reader whose records are converted.
        /// </param>
        /// <param name="varName">
        /// The name of the array variable to populate; this parameter may be
        /// null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records to convert; a value of
        /// <see cref="Limits.Unlimited" /> means no limit.
        /// </param>
        /// <param name="clear">
        /// Non-zero to start with a fresh row list for each record.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias when fixing up an object value.
        /// </param>
        /// <param name="count">
        /// On input and output, the running count of records converted, which
        /// is incremented by this method.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method executes the specified database command and evaluates a
        /// script body once per result (or once for a non-query or scalar
        /// execution), exposing each converted result to the body via a
        /// variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to store variables, convert values, and
        /// evaluate the script body.
        /// </param>
        /// <param name="binder">
        /// The binder used when fixing up an object return value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used during value conversion.
        /// </param>
        /// <param name="command">
        /// The database command to execute.
        /// </param>
        /// <param name="options">
        /// The options used when fixing up an object return value.
        /// </param>
        /// <param name="executeType">
        /// The kind of execution to perform (non-query, scalar, or reader).
        /// </param>
        /// <param name="commandBehavior">
        /// The command behavior used when executing a reader.
        /// </param>
        /// <param name="resultFormat">
        /// The format in which each record is converted before the body is
        /// evaluated.
        /// </param>
        /// <param name="commandName">
        /// The name of the calling command, used in error information.
        /// </param>
        /// <param name="varName">
        /// The name of the variable through which each result is exposed to the
        /// body; this parameter may be null.
        /// </param>
        /// <param name="body">
        /// The script body to evaluate for each result.
        /// </param>
        /// <param name="location">
        /// The script location associated with the body.
        /// </param>
        /// <param name="blobBehavior">
        /// The behavior used when converting binary large object values.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// The behavior used when converting date and time values.
        /// </param>
        /// <param name="dateTimeKind">
        /// The date and time kind used when converting date and time values.
        /// </param>
        /// <param name="dateTimeFormat">
        /// The format used when converting date and time values.
        /// </param>
        /// <param name="numberFormat">
        /// The format used when converting numeric values.
        /// </param>
        /// <param name="nullValue">
        /// The string used to represent a null value.
        /// </param>
        /// <param name="dbNullValue">
        /// The string used to represent a database null value.
        /// </param>
        /// <param name="errorValue">
        /// The string used to represent a value that could not be converted.
        /// </param>
        /// <param name="limit">
        /// The maximum number of records to process; a value of
        /// <see cref="Limits.Unlimited" /> means no limit.
        /// </param>
        /// <param name="nested">
        /// Non-zero to produce a nested list (one sub-list per record).
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to include null and database null field values in the
        /// output.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to emit each field as a name and value pair.
        /// </param>
        /// <param name="names">
        /// Non-zero to include field names in the output.
        /// </param>
        /// <param name="returnType">
        /// The desired return type used when fixing up an object return value.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when fixing up an object return value.
        /// </param>
        /// <param name="objectName">
        /// The object name used when fixing up an object return value.
        /// </param>
        /// <param name="interpName">
        /// The interpreter name used when fixing up an object return value.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an opaque object handle for an object return
        /// value.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose of the object when its handle is removed.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for an object return value.
        /// </param>
        /// <param name="aliasRaw">
        /// Non-zero to create a raw command alias for an object return value.
        /// </param>
        /// <param name="aliasAll">
        /// Non-zero to create aliases for all members of an object return
        /// value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add an opaque object reference for the created alias.
        /// </param>
        /// <param name="toString">
        /// Non-zero to convert an object return value to its string form.
        /// </param>
        /// <param name="noFixup">
        /// Non-zero to skip value fix up and use the raw string form instead.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the empty string or the body break result;
        /// upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, the return code
        /// produced by the failing operation.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        #region DataTable Helper Class
#if XML
        /// <summary>
        /// This class extends the framework data table to support converting
        /// its rows into Eagle lists and dictionaries, applying the same value
        /// formatting as the other <c>[sql execute]</c> result formats.
        /// </summary>
        [ObjectId("2199651c-fd55-4319-bb47-9ea05ea7995d")]
        private sealed class _DataTable : DataTable /* Xml */, IDataTable
        {
            #region Private Data
            /// <summary>
            /// The interpreter context used during value conversion; this field
            /// may be null.
            /// </summary>
            private Interpreter interpreter;

            /// <summary>
            /// The culture used during value conversion.
            /// </summary>
            private CultureInfo cultureInfo;

            /// <summary>
            /// The behavior used when converting binary large object values.
            /// </summary>
            private BlobBehavior blobBehavior;

            /// <summary>
            /// The behavior used when converting date and time values.
            /// </summary>
            private DateTimeBehavior dateTimeBehavior;

            /// <summary>
            /// The date and time kind used when converting date and time
            /// values.
            /// </summary>
            private DateTimeKind dateTimeKind;

            /// <summary>
            /// The format used when converting date and time values.
            /// </summary>
            private string dateTimeFormat;

            /// <summary>
            /// The format used when converting numeric values.
            /// </summary>
            private string numberFormat;

            /// <summary>
            /// The string used to represent a null value.
            /// </summary>
            private string nullValue;

            /// <summary>
            /// The string used to represent a database null value.
            /// </summary>
            private string dbNullValue;

            /// <summary>
            /// The string used to represent a value that could not be
            /// converted.
            /// </summary>
            private string errorValue;
            #endregion

            ///////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs an instance of this class, capturing the value
            /// conversion settings used when its rows are later converted.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context used during value conversion; this
            /// parameter may be null.
            /// </param>
            /// <param name="cultureInfo">
            /// The culture used during value conversion.
            /// </param>
            /// <param name="blobBehavior">
            /// The behavior used when converting binary large object values.
            /// </param>
            /// <param name="dateTimeBehavior">
            /// The behavior used when converting date and time values.
            /// </param>
            /// <param name="dateTimeKind">
            /// The date and time kind used when converting date and time
            /// values.
            /// </param>
            /// <param name="dateTimeFormat">
            /// The format used when converting date and time values.
            /// </param>
            /// <param name="numberFormat">
            /// The format used when converting numeric values.
            /// </param>
            /// <param name="nullValue">
            /// The string used to represent a null value.
            /// </param>
            /// <param name="dbNullValue">
            /// The string used to represent a database null value.
            /// </param>
            /// <param name="errorValue">
            /// The string used to represent a value that could not be
            /// converted.
            /// </param>
            public _DataTable(
                Interpreter interpreter,           /* in */
                CultureInfo cultureInfo,           /* in */
                BlobBehavior blobBehavior,         /* in */
                DateTimeBehavior dateTimeBehavior, /* in */
                DateTimeKind dateTimeKind,         /* in */
                string dateTimeFormat,             /* in */
                string numberFormat,               /* in */
                string nullValue,                  /* in */
                string dbNullValue,                /* in */
                string errorValue                  /* in */
                )
            {
                this.interpreter = interpreter;
                this.cultureInfo = cultureInfo;
                this.blobBehavior = blobBehavior;
                this.dateTimeBehavior = dateTimeBehavior;
                this.dateTimeKind = dateTimeKind;
                this.dateTimeFormat = dateTimeFormat;
                this.numberFormat = numberFormat;
                this.nullValue = nullValue;
                this.dbNullValue = dbNullValue;
                this.errorValue = errorValue;
            }
            #endregion

            ///////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// This method converts the specified data rows into a list,
            /// applying the captured value formatting to each field.
            /// </summary>
            /// <param name="rows">
            /// The data rows to convert.
            /// </param>
            /// <param name="names">
            /// Non-zero to include column names alongside their values.
            /// </param>
            /// <param name="limit">
            /// The maximum number of rows to convert; a value of zero or less
            /// means no limit.
            /// </param>
            /// <returns>
            /// A list containing one sub-list per converted row.
            /// </returns>
            private IStringList RowsToList(
                DataRow[] rows, /* in */
                bool names,     /* in */
                int limit       /* in */
                )
            {
                StringList result = new StringList();
                int count = 0;

                foreach (DataRow row in rows)
                {
                    if (row == null)
                        continue;

                    if ((limit > 0) && (count >= limit))
                        break;

                    StringList rowList = new StringList();

                    foreach (DataColumn column in Columns)
                    {
                        if (column == null)
                            continue;

                        object value = row[column];

                        if (names)
                            rowList.Add(column.ColumnName);

                        rowList.Add(MarshalOps.FixupDataValue(
                            interpreter, value, cultureInfo,
                            blobBehavior, dateTimeBehavior,
                            dateTimeKind, dateTimeFormat,
                            numberFormat, nullValue,
                            dbNullValue, errorValue, false));
                    }

                    result.Add(rowList.ToString());
                    count++;
                }

                return result;
            }
            #endregion

            ///////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method converts all rows of this data table into a list of
            /// values.
            /// </summary>
            /// <returns>
            /// A list containing one sub-list of values per row.
            /// </returns>
            public IStringList ToList()
            {
                return ToList(Limits.Unlimited);
            }

            ///////////////////////////////////////////////////////////

            //
            // NOTE: Converts rows to a StringList, applying the
            //       same value formatting as other [sql execute]
            //       result formats.  This replaces the manual
            //       getRowsFromDataTable pattern.
            //
            /// <summary>
            /// This method converts the rows of this data table into a list of
            /// values, up to the specified limit.
            /// </summary>
            /// <param name="limit">
            /// The maximum number of rows to convert.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of values per row.
            /// </returns>
            public IStringList ToList(
                int limit /* in */
                )
            {
                return RowsToList(Select(null, null), false, limit);
            }

            ///////////////////////////////////////////////////////////

            /// <summary>
            /// This method converts the filtered and/or sorted rows of this
            /// data table into a list of values.
            /// </summary>
            /// <param name="filter">
            /// The optional filter expression selecting the rows; this
            /// parameter may be null.
            /// </param>
            /// <param name="sort">
            /// The optional sort expression ordering the rows; this parameter
            /// may be null.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of values per matching row.
            /// </returns>
            public IStringList ToList(
                string filter, /* in */
                string sort    /* in */
                )
            {
                return ToList(
                    filter, sort, Limits.Unlimited);
            }

            ///////////////////////////////////////////////////////////

            //
            // NOTE: Like ToList but operates on a filtered and/or
            //       sorted subset of rows via DataTable.Select.
            //
            /// <summary>
            /// This method converts the filtered and/or sorted rows of this
            /// data table into a list of values, up to the specified limit.
            /// </summary>
            /// <param name="filter">
            /// The optional filter expression selecting the rows; this
            /// parameter may be null.
            /// </param>
            /// <param name="sort">
            /// The optional sort expression ordering the rows; this parameter
            /// may be null.
            /// </param>
            /// <param name="limit">
            /// The maximum number of rows to convert.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of values per matching row.
            /// </returns>
            public IStringList ToList(
                string filter, /* in */
                string sort,   /* in */
                int limit      /* in */
                )
            {
                return RowsToList(
                    Select(filter, sort), false, limit);
            }

            ///////////////////////////////////////////////////////////

            /// <summary>
            /// This method converts all rows of this data table into a list of
            /// column name and value pairs.
            /// </summary>
            /// <returns>
            /// A list containing one sub-list of name and value pairs per row.
            /// </returns>
            public IStringList ToDictionary()
            {
                return ToDictionary(Limits.Unlimited);
            }

            ///////////////////////////////////////////////////////////

            //
            // NOTE: Like ToList but includes column names as keys,
            //       producing {colName value colName value ...} per
            //       row.
            //
            /// <summary>
            /// This method converts the rows of this data table into a list of
            /// column name and value pairs, up to the specified limit.
            /// </summary>
            /// <param name="limit">
            /// The maximum number of rows to convert.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of name and value pairs per row.
            /// </returns>
            public IStringList ToDictionary(
                int limit /* in */
                )
            {
                return RowsToList(Select(null, null), true, limit);
            }

            ///////////////////////////////////////////////////////////

            /// <summary>
            /// This method converts the filtered and/or sorted rows of this
            /// data table into a list of column name and value pairs.
            /// </summary>
            /// <param name="filter">
            /// The optional filter expression selecting the rows; this
            /// parameter may be null.
            /// </param>
            /// <param name="sort">
            /// The optional sort expression ordering the rows; this parameter
            /// may be null.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of name and value pairs per
            /// matching row.
            /// </returns>
            public IStringList ToDictionary(
                string filter, /* in */
                string sort    /* in */
                )
            {
                return ToDictionary(
                    filter, sort, Limits.Unlimited);
            }

            ///////////////////////////////////////////////////////////

            //
            // NOTE: Like ToDictionary but operates on a filtered
            //       and/or sorted subset of rows.
            //
            /// <summary>
            /// This method converts the filtered and/or sorted rows of this
            /// data table into a list of column name and value pairs, up to the
            /// specified limit.
            /// </summary>
            /// <param name="filter">
            /// The optional filter expression selecting the rows; this
            /// parameter may be null.
            /// </param>
            /// <param name="sort">
            /// The optional sort expression ordering the rows; this parameter
            /// may be null.
            /// </param>
            /// <param name="limit">
            /// The maximum number of rows to convert.
            /// </param>
            /// <returns>
            /// A list containing one sub-list of name and value pairs per
            /// matching row.
            /// </returns>
            public IStringList ToDictionary(
                string filter, /* in */
                string sort,   /* in */
                int limit      /* in */
                )
            {
                return RowsToList(
                    Select(filter, sort), true, limit);
            }

            ///////////////////////////////////////////////////////////

            //
            // NOTE: Returns column names as a StringList.
            //
            /// <summary>
            /// This method returns the names of the columns in this data table
            /// as a list.
            /// </summary>
            /// <returns>
            /// A list containing the name of each column in this data table.
            /// </returns>
            public IStringList GetColumnNames()
            {
                StringList result = new StringList();

                foreach (DataColumn column in Columns)
                {
                    if (column == null)
                        continue;

                    result.Add(column.ColumnName);
                }

                return result;
            }
            #endregion
        }
#endif
        #endregion
    }
}
