/*
 * Sql.cs --
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
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using IsolationLevel = System.Data.IsolationLevel;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("dbc78d04-325d-4805-a118-3cfeeddfb8fc")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard
#if NATIVE && WINDOWS
        //
        // NOTE: Uses native code indirectly for profiling [sql execute] with
        //       the "-time" option (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        )]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Sql : Core
    {
        #region Private Data
        private readonly EnsembleDictionary transactionSubCommands =
        new EnsembleDictionary(new string[] {
            "begin", "commit", "rollback"
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Sql(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "close", "connection", "execute", "foreach",
            "open", "transaction", "types"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "close":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IDbConnection connection = null;

                                            code = interpreter.GetDbConnection(
                                                arguments[2], LookupFlags.Default, ref connection, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (connection != null)
                                                {
                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(false, ref result))
                                                    {
                                                        if (interpreter.HasDbConnections(ref result))
                                                        {
                                                            try
                                                            {
                                                                connection.Close();
                                                                interpreter.RemoveDbConnection(arguments[2]);

#if NOTIFY
                                                                /* IGNORED */
                                                                interpreter.CheckNotification(
                                                                    NotifyType.Connection, NotifyFlags.Removed,
                                                                    connection, interpreter, null, null, null,
                                                                    ref result);
#endif

                                                                connection = null;
                                                                result = String.Empty;
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                                result = e;
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "invalid connection {0}",
                                                        FormatOps.WrapOrNull(arguments[2]));

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"sql close connection\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "connection":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            if (interpreter.HasDbConnections(ref result))
                                            {
                                                IDbConnection connection = null;

                                                code = interpreter.GetAnyDbConnection(
                                                    arguments[2], LookupFlags.Default, ref connection, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (connection != null)
                                                    {
                                                        try
                                                        {
                                                            result = StringList.MakeList(
                                                                "type", connection.GetType().Name,
                                                                "state", connection.State,
                                                                "database", connection.Database,
                                                                "timeout", connection.ConnectionTimeout,
                                                                "string", connection.ConnectionString);
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "invalid connection {0}",
                                                            FormatOps.WrapOrNull(arguments[2]));

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"sql connection connection\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "execute":
                                    {
                                        int argumentCount = arguments.Count;

                                        if (argumentCount >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetExecuteOptions();

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < argumentCount))
                                                {
                                                    Variant value = null;
                                                    IDbTransaction transaction = null;

                                                    if (options.IsPresent("-transaction", ref value))
                                                    {
                                                        string transactionName = value.ToString();

                                                        if (!String.IsNullOrEmpty(transactionName))
                                                        {
                                                            code = interpreter.GetAnyDbTransaction(
                                                                transactionName, LookupFlags.Default,
                                                                ref transaction, ref result);

                                                            if ((code == ReturnCode.Ok) &&
                                                                (transaction == null))
                                                            {
                                                                result = String.Format(
                                                                    "invalid transaction {0}",
                                                                    FormatOps.WrapOrNull(transactionName));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Type returnType;
                                                        ObjectFlags objectFlags;
                                                        string objectName;
                                                        string interpName;
                                                        bool create;
                                                        bool disposeReader;
                                                        bool alias;
                                                        bool aliasRaw;
                                                        bool aliasAll;
                                                        bool aliasReference;
                                                        bool toString;

                                                        ObjectOps.ProcessFixupReturnValueOptions(
                                                            options, null, out returnType, out objectFlags,
                                                            out objectName, out interpName, out create,
                                                            out disposeReader, out alias, out aliasRaw,
                                                            out aliasAll, out aliasReference, out toString);

                                                        CultureInfo cultureInfo;
                                                        CommandType commandType;
                                                        CommandBehavior commandBehavior;
                                                        DbExecuteType executeType;
                                                        DbResultFormat resultFormat;
                                                        ValueFlags valueFlags;
                                                        DateTimeBehavior dateTimeBehavior;
                                                        DateTimeKind dateTimeKind;
                                                        DateTimeStyles dateTimeStyles;
                                                        string rowsVarName;
                                                        string timeVarName;
                                                        string valueFormat;
                                                        string dateTimeFormat;
                                                        string numberFormat;
                                                        string nullValue;
                                                        string dbNullValue;
                                                        string errorValue;
                                                        int? commandTimeout;
                                                        int limit;
                                                        bool nested;
                                                        bool allowNull;
                                                        bool pairs;
                                                        bool names;
                                                        bool time;
                                                        bool verbatim;
                                                        bool noFixup;

                                                        ObjectOps.ProcessExecuteOptions(
                                                            interpreter, options, null, null, null, null, null,
                                                            null, null, null, out cultureInfo, out commandType,
                                                            out commandBehavior, out executeType, out resultFormat,
                                                            out valueFlags, out dateTimeBehavior, out dateTimeKind,
                                                            out dateTimeStyles, out rowsVarName, out timeVarName,
                                                            out valueFormat, out dateTimeFormat, out numberFormat,
                                                            out nullValue, out dbNullValue, out errorValue,
                                                            out commandTimeout, out limit, out nested,
                                                            out allowNull, out pairs, out names, out time,
                                                            out verbatim, out noFixup);

                                                        if (rowsVarName == null)
                                                            rowsVarName = Vars.ResultSet.Rows;

                                                        if (timeVarName == null)
                                                            timeVarName = Vars.ResultSet.Time;

                                                        //
                                                        // HACK: If the value format option is null, try to
                                                        //       use the "legacy" date time format option.
                                                        //
                                                        if ((valueFormat == null) && (dateTimeFormat != null))
                                                            valueFormat = dateTimeFormat;

                                                        nullValue = StringOps.NullIfEmpty(nullValue);

                                                        if (dbNullValue == null)
                                                            dbNullValue = nullValue; /* COMPAT: Eagle beta. */

                                                        dbNullValue = StringOps.NullIfEmpty(dbNullValue);

                                                        if (errorValue == null)
                                                            errorValue = nullValue; /* COMPAT: Eagle beta. */

                                                        errorValue = StringOps.NullIfEmpty(errorValue);

                                                        if (interpreter.HasDbConnections(ref result))
                                                        {
                                                            IDbConnection connection = null;

                                                            code = interpreter.GetAnyDbConnection(
                                                                arguments[argumentIndex], LookupFlags.Default,
                                                                ref connection, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (connection != null)
                                                                {
                                                                    IDbCommand command = null;

                                                                    try
                                                                    {
                                                                        command = connection.CreateCommand();

                                                                        //
                                                                        // NOTE: Set command text itself to the value of the second
                                                                        //       arguemnt after the options.
                                                                        //
                                                                        command.CommandText = arguments[argumentIndex + 1];

                                                                        //
                                                                        // NOTE: If the timeout was supplied, set the timeout value
                                                                        //       now; otherwise, leave it alone to retain the default
                                                                        //       for the underlying provider.
                                                                        //
                                                                        if (commandTimeout != null)
                                                                            command.CommandTimeout = (int)commandTimeout;

                                                                        //
                                                                        // NOTE: Set the command type to the value specified in the
                                                                        //       option (or the default if none was supplied).
                                                                        //
                                                                        command.CommandType = commandType;

                                                                        //
                                                                        // NOTE: Setup the transaction for this query.  If this is set
                                                                        //       to null, default transaction semantics may be used by
                                                                        //       the underlying data provider.
                                                                        //
                                                                        command.Transaction = transaction; /* throw */

                                                                        //
                                                                        // NOTE: Add any supplied parameters to this command.
                                                                        //
                                                                        if ((argumentIndex + 2) < argumentCount)
                                                                        {
                                                                            code = DataOps.GetParameters(
                                                                                interpreter, cultureInfo, valueFormat, valueFlags,
                                                                                dateTimeKind, dateTimeStyles, command, arguments,
                                                                                argumentIndex + 2, Index.Invalid, verbatim,
                                                                                ref result);
                                                                        }

                                                                        //
                                                                        // NOTE: Make sure we succeeded parsing optional parameters,
                                                                        //       if any were provided.
                                                                        //
                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            //
                                                                            // NOTE: These variables are used to measure performance
                                                                            //       if the -time option is enabled.
                                                                            //
                                                                            IProfilerState profiler = null;
                                                                            bool disposeProfiler = true;

                                                                            try
                                                                            {
                                                                                if (time)
                                                                                {
                                                                                    profiler = ProfilerState.Create(
                                                                                        interpreter, ref disposeProfiler);
                                                                                }

                                                                                //
                                                                                // NOTE: Always prepare the statement, even though
                                                                                //       it may result in a no-op.
                                                                                //
                                                                                if (profiler != null)
                                                                                    profiler.Start();

                                                                                command.Prepare();

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop();

                                                                                    ReturnCode setCode;
                                                                                    Result setError = null;

                                                                                    setCode = interpreter.SetVariableValue2(
                                                                                        VariableFlags.None, timeVarName,
                                                                                        Vars.ResultSet.Prepare,
                                                                                        profiler.ToString(), ref setError);

                                                                                    if (setCode != ReturnCode.Ok)
                                                                                        DebugOps.Complain(interpreter, setCode, setError);

                                                                                    profiler.Start();
                                                                                }

                                                                                code = DataOps.ExecuteCommandAndGetResults(
                                                                                    interpreter, interpreter.InternalBinder,
                                                                                    cultureInfo, command, options, executeType,
                                                                                    commandBehavior, resultFormat, rowsVarName,
                                                                                    dateTimeBehavior, dateTimeKind,
                                                                                    dateTimeFormat, numberFormat, nullValue,
                                                                                    dbNullValue, errorValue, limit, nested,
                                                                                    allowNull, pairs, names, returnType,
                                                                                    objectFlags, objectName, interpName,
                                                                                    create, disposeReader, alias, aliasRaw,
                                                                                    aliasAll, aliasReference, toString,
                                                                                    noFixup, ref result);

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop();

                                                                                    ReturnCode setCode;
                                                                                    Result setError = null;

                                                                                    setCode = interpreter.SetVariableValue2(
                                                                                        VariableFlags.None, timeVarName,
                                                                                        Vars.ResultSet.Execute,
                                                                                        profiler.ToString(), ref setError);

                                                                                    if (setCode != ReturnCode.Ok)
                                                                                        DebugOps.Complain(interpreter, setCode, setError);
                                                                                }
                                                                            }
                                                                            finally
                                                                            {
                                                                                if (profiler != null)
                                                                                {
                                                                                    if (disposeProfiler)
                                                                                    {
                                                                                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                                                                                            interpreter, ref profiler);
                                                                                    }

                                                                                    profiler = null;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                                        result = e;
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                    finally
                                                                    {
                                                                        if (command != null)
                                                                        {
                                                                            command.Dispose();
                                                                            command = null;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "invalid connection {0}",
                                                                        FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? connection string ?{3}paramName ?paramType? ?paramValue? ?paramSize? ?paramValueFlags?{4} ...?\"",
                                                            this.Name, subCommand, null, Characters.OpenBrace, Characters.CloseBrace);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? connection string ?{3}paramName ?paramType? ?paramValue? ?paramSize? ?paramValueFlags?{4} ...?\"",
                                                this.Name, subCommand, null, Characters.OpenBrace, Characters.CloseBrace);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "foreach":
                                    {
                                        int argumentCount = arguments.Count;

                                        if (argumentCount >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetExecuteOptions();

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) < argumentCount))
                                                {
                                                    Variant value = null;
                                                    IDbTransaction transaction = null;

                                                    if (options.IsPresent("-transaction", ref value))
                                                    {
                                                        string transactionName = value.ToString();

                                                        if (!String.IsNullOrEmpty(transactionName))
                                                        {
                                                            code = interpreter.GetDbTransaction(
                                                                transactionName, LookupFlags.Default,
                                                                ref transaction, ref result);

                                                            if ((code == ReturnCode.Ok) &&
                                                                (transaction == null))
                                                            {
                                                                result = String.Format(
                                                                    "invalid transaction {0}",
                                                                    FormatOps.WrapOrNull(transactionName));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Type returnType;
                                                        ObjectFlags objectFlags;
                                                        string objectName;
                                                        string interpName;
                                                        bool create;
                                                        bool disposeReader;
                                                        bool alias;
                                                        bool aliasRaw;
                                                        bool aliasAll;
                                                        bool aliasReference;
                                                        bool toString;

                                                        ObjectOps.ProcessFixupReturnValueOptions(
                                                            options, null, out returnType, out objectFlags,
                                                            out objectName, out interpName, out create,
                                                            out disposeReader, out alias, out aliasRaw,
                                                            out aliasAll, out aliasReference, out toString);

                                                        CultureInfo cultureInfo;
                                                        CommandType commandType;
                                                        CommandBehavior commandBehavior;
                                                        DbExecuteType executeType;
                                                        DbResultFormat resultFormat;
                                                        ValueFlags valueFlags;
                                                        DateTimeBehavior dateTimeBehavior;
                                                        DateTimeKind dateTimeKind;
                                                        DateTimeStyles dateTimeStyles;
                                                        string rowsVarName;
                                                        string timeVarName;
                                                        string valueFormat;
                                                        string dateTimeFormat;
                                                        string numberFormat;
                                                        string nullValue;
                                                        string dbNullValue;
                                                        string errorValue;
                                                        int? commandTimeout;
                                                        int limit;
                                                        bool nested;
                                                        bool allowNull;
                                                        bool pairs;
                                                        bool names;
                                                        bool time;
                                                        bool verbatim;
                                                        bool noFixup;

                                                        ObjectOps.ProcessExecuteOptions(
                                                            interpreter, options, null, null, null, null, null,
                                                            null, null, null, out cultureInfo, out commandType,
                                                            out commandBehavior, out executeType, out resultFormat,
                                                            out valueFlags, out dateTimeBehavior, out dateTimeKind,
                                                            out dateTimeStyles, out rowsVarName, out timeVarName,
                                                            out valueFormat, out dateTimeFormat, out numberFormat,
                                                            out nullValue, out dbNullValue, out errorValue,
                                                            out commandTimeout, out limit, out nested,
                                                            out allowNull, out pairs, out names, out time,
                                                            out verbatim, out noFixup);

                                                        if (rowsVarName == null)
                                                            rowsVarName = Vars.ResultSet.Row;

                                                        if (timeVarName == null)
                                                            timeVarName = Vars.ResultSet.Time;

                                                        //
                                                        // HACK: If the value format option is null, try to
                                                        //       use the "legacy" date time format option.
                                                        //
                                                        if ((valueFormat == null) && (dateTimeFormat != null))
                                                            valueFormat = dateTimeFormat;

                                                        nullValue = StringOps.NullIfEmpty(nullValue);

                                                        if (dbNullValue == null)
                                                            dbNullValue = nullValue; /* COMPAT: Eagle beta. */

                                                        dbNullValue = StringOps.NullIfEmpty(dbNullValue);

                                                        if (errorValue == null)
                                                            errorValue = nullValue; /* COMPAT: Eagle beta. */

                                                        errorValue = StringOps.NullIfEmpty(errorValue);

                                                        if (interpreter.HasDbConnections(ref result))
                                                        {
                                                            IDbConnection connection = null;

                                                            code = interpreter.GetAnyDbConnection(
                                                                arguments[argumentIndex], LookupFlags.Default,
                                                                ref connection, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (connection != null)
                                                                {
                                                                    IDbCommand command = null;

                                                                    try
                                                                    {
                                                                        command = connection.CreateCommand();

                                                                        //
                                                                        // NOTE: Set command text itself to the value of the second
                                                                        //       arguemnt after the options.
                                                                        //
                                                                        command.CommandText = arguments[argumentIndex + 1];

                                                                        //
                                                                        // NOTE: If the timeout was supplied, set the timeout value
                                                                        //       now; otherwise, leave it alone to retain the default
                                                                        //       for the underlying provider.
                                                                        //
                                                                        if (commandTimeout != null)
                                                                            command.CommandTimeout = (int)commandTimeout;

                                                                        //
                                                                        // NOTE: Set the command type to the value specified in the
                                                                        //       option (or the default if none was supplied).
                                                                        //
                                                                        command.CommandType = commandType;

                                                                        //
                                                                        // NOTE: Setup the transaction for this query.  If this is set
                                                                        //       to null, default transaction semantics may be used by
                                                                        //       the underlying data provider.
                                                                        //
                                                                        command.Transaction = transaction; /* throw */

                                                                        //
                                                                        // NOTE: Add any supplied parameters to this command.
                                                                        //
                                                                        if ((argumentIndex + 3) < argumentCount)
                                                                        {
                                                                            code = DataOps.GetParameters(
                                                                                interpreter, cultureInfo, valueFormat, valueFlags,
                                                                                dateTimeKind, dateTimeStyles, command, arguments,
                                                                                argumentIndex + 2, Index.Invalid, verbatim,
                                                                                ref result);
                                                                        }

                                                                        //
                                                                        // NOTE: Make sure we succeeded parsing optional parameters,
                                                                        //       if any were provided.
                                                                        //
                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            //
                                                                            // NOTE: These variables are used to measure performance
                                                                            //       if the -time option is enabled.
                                                                            //
                                                                            IProfilerState profiler = null;
                                                                            bool disposeProfiler = true;

                                                                            try
                                                                            {
                                                                                if (time)
                                                                                {
                                                                                    profiler = ProfilerState.Create(
                                                                                        interpreter, ref disposeProfiler);
                                                                                }

                                                                                //
                                                                                // NOTE: Always prepare the statement, even though
                                                                                //       it may result in a no-op.
                                                                                //
                                                                                if (profiler != null)
                                                                                    profiler.Start();

                                                                                command.Prepare();

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop();

                                                                                    ReturnCode setCode;
                                                                                    Result setError = null;

                                                                                    setCode = interpreter.SetVariableValue2(
                                                                                        VariableFlags.None, timeVarName,
                                                                                        Vars.ResultSet.Prepare,
                                                                                        profiler.ToString(), ref setError);

                                                                                    if (setCode != ReturnCode.Ok)
                                                                                        DebugOps.Complain(interpreter, setCode, setError);

                                                                                    profiler.Start();
                                                                                }

                                                                                Argument body = arguments[argumentCount - 1];

                                                                                code = DataOps.ExecuteCommandAndEvaluateBody(
                                                                                    interpreter, interpreter.InternalBinder,
                                                                                    cultureInfo, command, options, executeType,
                                                                                    commandBehavior, resultFormat, this.Name,
                                                                                    rowsVarName, body, body, dateTimeBehavior,
                                                                                    dateTimeKind, dateTimeFormat, numberFormat,
                                                                                    nullValue, dbNullValue, errorValue, limit,
                                                                                    nested, allowNull, pairs, names, returnType,
                                                                                    objectFlags, objectName, interpName, create,
                                                                                    disposeReader, alias, aliasRaw, aliasAll,
                                                                                    aliasReference, toString, noFixup,
                                                                                    ref result);

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop();

                                                                                    ReturnCode setCode;
                                                                                    Result setError = null;

                                                                                    setCode = interpreter.SetVariableValue2(
                                                                                        VariableFlags.None, timeVarName,
                                                                                        Vars.ResultSet.Execute,
                                                                                        profiler.ToString(), ref setError);

                                                                                    if (setCode != ReturnCode.Ok)
                                                                                        DebugOps.Complain(interpreter, setCode, setError);
                                                                                }
                                                                            }
                                                                            finally
                                                                            {
                                                                                if (profiler != null)
                                                                                {
                                                                                    if (disposeProfiler)
                                                                                    {
                                                                                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                                                                                            interpreter, ref profiler);
                                                                                    }

                                                                                    profiler = null;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                                        result = e;
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                    finally
                                                                    {
                                                                        if (command != null)
                                                                        {
                                                                            command.Dispose();
                                                                            command = null;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "invalid connection {0}",
                                                                        FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? connection string ?{3}paramName ?paramType? ?paramValue? ?paramSize? ?paramValueFlags?{4} ...? body\"",
                                                            this.Name, subCommand, null, Characters.OpenBrace, Characters.CloseBrace);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? connection string ?{3}paramName ?paramType? ?paramValue? ?paramSize? ?paramValueFlags?{4} ...? body\"",
                                                this.Name, subCommand, null, Characters.OpenBrace, Characters.CloseBrace);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "open":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(DbConnectionType), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-type", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-assemblyfilename", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-typename", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-typefullname", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-stricttype", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-verbose", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    DbConnectionType dbConnectionType = DbConnectionType.Default; /* TODO: Good default? */

                                                    if (options.IsPresent("-type", ref value))
                                                        dbConnectionType = (DbConnectionType)value.Value;

                                                    string assemblyFileName = null;

                                                    if (options.IsPresent("-assemblyfilename", ref value))
                                                        assemblyFileName = value.ToString();

                                                    string typeName = null;

                                                    if (options.IsPresent("-typename", ref value))
                                                        typeName = value.ToString();

                                                    string typeFullName = null;

                                                    if (options.IsPresent("-typefullname", ref value))
                                                        typeFullName = value.ToString();

                                                    //
                                                    // NOTE: Perform a case-insensitive search for the type name?
                                                    //
                                                    bool noCase = false;

                                                    if (options.IsPresent("-nocase"))
                                                        noCase = true;

                                                    //
                                                    // NOTE: Prevent any magical type searches (i.e. use their specified
                                                    //       type string verbatim)?
                                                    //
                                                    bool strictType = false;

                                                    if (options.IsPresent("-stricttype"))
                                                        strictType = true;

                                                    //
                                                    // NOTE: Return all Type exception information (this can be very
                                                    //       costly for performance).
                                                    //
                                                    bool verbose = false;

                                                    if (options.IsPresent("-verbose"))
                                                        verbose = true;

                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(true, ref result))
                                                    {
                                                        if (interpreter.HasDbConnections(ref result))
                                                        {
                                                            try
                                                            {
                                                                IDbConnection connection = null;

                                                                code = DataOps.CreateDbConnection(interpreter,
                                                                    dbConnectionType, arguments[argumentIndex],
                                                                    assemblyFileName, typeFullName, typeName,
                                                                    null, Value.GetTypeValueFlags(
                                                                        strictType, verbose, noCase),
                                                                    DataOps.GetOtherDbConnectionTypeNames(true, false),
                                                                    DataOps.GetOtherDbConnectionTypeNames(false, false),
                                                                    ref connection, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (connection != null)
                                                                        connection.Open();

                                                                    result = FormatOps.DatabaseObjectName(connection,
                                                                        dbConnectionType.ToString() + "Connection",
                                                                        interpreter.NextId());

                                                                    interpreter.AddDbConnection(result, connection);

#if NOTIFY
                                                                    /* IGNORED */
                                                                    interpreter.CheckNotification(
                                                                        NotifyType.Connection, NotifyFlags.Added,
                                                                        connection, interpreter, null, null, null,
                                                                        ref result);
#endif
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                                result = e;
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"sql open ?options? connectionString\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"sql open ?options? connectionString\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "transaction":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(IsolationLevel), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-isolation", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    IsolationLevel isolationLevel = IsolationLevel.Unspecified; /* NOTE: Yes, this default is OK, per MSDN. */

                                                    if (options.IsPresent("-isolation", ref value))
                                                        isolationLevel = (IsolationLevel)value.Value;

                                                    string subSubCommand = arguments[argumentIndex];

                                                    code = ScriptOps.SubCommandFromEnsemble(
                                                        interpreter, transactionSubCommands, null,
                                                        true, false, ref subSubCommand, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        switch (subSubCommand)
                                                        {
                                                            case "begin":
                                                                {
                                                                    IDbConnection connection = null;

                                                                    code = interpreter.GetDbConnection(
                                                                        arguments[argumentIndex + 1], LookupFlags.Default,
                                                                        ref connection, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (connection != null)
                                                                        {
                                                                            //
                                                                            // NOTE: We are going to modify the interpreter state, make
                                                                            //       sure it is not set to read-only.  Technically, this
                                                                            //       modifies the interpreter state directly (via the
                                                                            //       transactions dictionary); however, we may need to
                                                                            //       relax or remove this read-only restriction in the
                                                                            //       future.
                                                                            //
                                                                            if (interpreter.IsModifiable(true, ref result))
                                                                            {
                                                                                if (interpreter.HasDbTransactions(ref result))
                                                                                {
                                                                                    try
                                                                                    {
                                                                                        IDbTransaction transaction =
                                                                                            connection.BeginTransaction(isolationLevel);

                                                                                        result = FormatOps.DatabaseObjectName(transaction,
                                                                                            "Transaction", interpreter.NextId());

                                                                                        interpreter.AddDbTransaction(result, transaction);

#if NOTIFY
                                                                                        /* IGNORED */
                                                                                        interpreter.CheckNotification(
                                                                                            NotifyType.Transaction, NotifyFlags.Added,
                                                                                            transaction, interpreter, null, null, null,
                                                                                            ref result);
#endif
                                                                                    }
                                                                                    catch (Exception e)
                                                                                    {
                                                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                                                        result = e;
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "invalid connection {0}",
                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            case "commit":
                                                                {
                                                                    IDbTransaction transaction = null;

                                                                    code = interpreter.GetDbTransaction(
                                                                        arguments[argumentIndex + 1], LookupFlags.Default,
                                                                        ref transaction, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (transaction != null)
                                                                        {
                                                                            //
                                                                            // NOTE: We are going to modify the interpreter state, make
                                                                            //       sure it is not set to read-only.  Technically, this
                                                                            //       modifies the interpreter state directly (via the
                                                                            //       transactions dictionary); however, we may need to
                                                                            //       relax or remove this read-only restriction in the
                                                                            //       future.
                                                                            //
                                                                            if (interpreter.IsModifiable(true, ref result))
                                                                            {
                                                                                if (interpreter.HasDbTransactions(ref result))
                                                                                {
                                                                                    try
                                                                                    {
                                                                                        transaction.Commit();
                                                                                        interpreter.RemoveDbTransaction(arguments[argumentIndex + 1]);

#if NOTIFY
                                                                                        /* IGNORED */
                                                                                        interpreter.CheckNotification(
                                                                                            NotifyType.Transaction, NotifyFlags.Removed,
                                                                                            transaction, interpreter, null, null, null,
                                                                                            ref result);
#endif

                                                                                        transaction = null;
                                                                                    }
                                                                                    catch (Exception e)
                                                                                    {
                                                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                                                        result = e;
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "invalid transaction {0}",
                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            case "rollback":
                                                                {
                                                                    IDbTransaction transaction = null;

                                                                    code = interpreter.GetDbTransaction(
                                                                        arguments[argumentIndex + 1], LookupFlags.Default,
                                                                        ref transaction, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (transaction != null)
                                                                        {
                                                                            //
                                                                            // NOTE: We are going to modify the interpreter state, make
                                                                            //       sure it is not set to read-only.  Technically, this
                                                                            //       modifies the interpreter state directly (via the
                                                                            //       transactions dictionary); however, we may need to
                                                                            //       relax or remove this read-only restriction in the
                                                                            //       future.
                                                                            //
                                                                            if (interpreter.IsModifiable(true, ref result))
                                                                            {
                                                                                if (interpreter.HasDbTransactions(ref result))
                                                                                {
                                                                                    try
                                                                                    {
                                                                                        transaction.Rollback();
                                                                                        interpreter.RemoveDbTransaction(arguments[argumentIndex + 1]);

#if NOTIFY
                                                                                        /* IGNORED */
                                                                                        interpreter.CheckNotification(
                                                                                            NotifyType.Transaction, NotifyFlags.Removed,
                                                                                            transaction, interpreter, null, null, null,
                                                                                            ref result);
#endif

                                                                                        transaction = null;
                                                                                    }
                                                                                    catch (Exception e)
                                                                                    {
                                                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                                                        result = e;
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "invalid transaction {0}",
                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    break;
                                                                }
                                                            default:
                                                                {
                                                                    result = ScriptOps.BadSubCommand(
                                                                        interpreter, null, null, subSubCommand,
                                                                        transactionSubCommands, null, null);

                                                                    code = ReturnCode.Error;
                                                                    break;
                                                                }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? {2} object\"",
                                                            this.Name, subCommand, "action");
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? {2} object\"",
                                                this.Name, subCommand, "action");

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "types":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IStringList list = GenericOps<DbConnectionType, StringPair>.Combine(
                                                true, true, true, DataOps.GetDbConnectionTypeNames(),
                                                DataOps.GetOtherDbConnectionTypeNames(true, false),
                                                DataOps.GetOtherDbConnectionTypeNames(false, false));

                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = list.ToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"sql types ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"sql option ?arg ...?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
