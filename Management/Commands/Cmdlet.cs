/*
 * Cmdlet.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>cmdlet</c> command, which serves as
    /// the bridge between the interpreter and the Windows PowerShell hosting
    /// environment.  Its sub-commands let scripts query the hosting plugin,
    /// write debug, error, verbose, and progress records to the PowerShell
    /// host, invoke PowerShell pipelines, inspect the script cmdlet state, and
    /// remove the meta-command.  The script cmdlet context that backs these
    /// operations is supplied through the per-command client data.
    /// </summary>
    [ObjectId("b62ba209-838c-46ad-8782-2269e470cf7a")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Cmdlet : Default
    {
        #region Public Constructor
        /// <summary>
        /// Constructs an instance of the <c>cmdlet</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Cmdlet(
            ICommandData commandData
            )
            : base(commandData)
        {
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this);
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region Private Methods
        #region Pseudo-Plugin Helper Methods
        /// <summary>
        /// This method extracts the script cmdlet context that backs this
        /// command from the supplied client data, unwrapping any nested client
        /// data as necessary and verifying that the contained script cmdlet has
        /// not been disposed.
        /// </summary>
        /// <param name="clientData">
        /// The client data supplied to this command, expected to contain (or
        /// wrap) the script cmdlet context.  This parameter may be null, in
        /// which case the lookup fails.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message describing
        /// why the script cmdlet could not be obtained.
        /// </param>
        /// <returns>
        /// The script cmdlet context contained in the client data, or null if
        /// it could not be obtained, in which case the <paramref name="error" />
        /// parameter is set.
        /// </returns>
        private static _Cmdlets.Script GetScriptCmdlet(
            IClientData clientData,
            ref Result error
            )
        {
            if (clientData == null)
            {
                error = "invalid clientData";
                return null;
            }

            _Cmdlets.Script result = null;

            object data = null;

            /* IGNORED */
            clientData = _Public.ClientData.UnwrapOrReturn(
                clientData, ref data);

            result = data as _Cmdlets.Script;

            if (result == null)
            {
                error = "clientData does not contain script cmdlet";
                return null;
            }

            if (result.Disposed)
            {
                error = "script cmdlet is disposed";
                return null;
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the binder to use when marshalling values
        /// returned from a PowerShell pipeline back into the interpreter.  The
        /// interpreter binder cannot be used when the plugin has been loaded
        /// into an application domain different from the interpreter, or when
        /// there is no interpreter from which to obtain it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter from which to obtain the binder.  This parameter may
        /// be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data used to determine whether this plugin resides in an
        /// application domain different from the interpreter.
        /// </param>
        /// <returns>
        /// The interpreter binder, or null if no suitable binder is available.
        /// </returns>
        private static IBinder GetBinder(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
            //
            // BUGFIX: We cannot use the ScriptBinder if this plugin has been
            //         loaded into an AppDomain different from the interpreter
            //         -OR- there is no interpreter to obtain it from.
            //
            if (interpreter != null)
            {
                if (Utility.IsCrossAppDomain(interpreter, pluginData))
                    return null;

                return interpreter.Binder;
            }
            else
            {
                return null;
            }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the list of compile-time define constants that
        /// describe how this library was built.  It is used as a fallback for
        /// the <c>options</c> sub-command when there is no plugin context to
        /// provide its own option information.
        /// </summary>
        /// <param name="result">
        /// Upon success, this contains the list of define constants.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="result" /> parameter.
        /// </returns>
        private static ReturnCode GetDefineConstants(
            ref Result result
            )
        {
            StringList list = DefineConstants.OptionList;

            if (list != null)
            {
                result = new StringList(list, false);
                return ReturnCode.Ok;
            }
            else
            {
                result = "define constants not available";
                return ReturnCode.Error;
            }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region PowerShell Helper Methods
        /// <summary>
        /// This method creates a nested PowerShell pipeline, using the default
        /// runspace, for the specified command text.
        /// </summary>
        /// <param name="command">
        /// The PowerShell command text used to populate the pipeline.
        /// </param>
        /// <param name="addToHistory">
        /// Non-zero to add the command to the PowerShell command history.
        /// </param>
        /// <returns>
        /// The newly created nested pipeline, or null if there is no default
        /// runspace available.
        /// </returns>
        private static Pipeline CreatePipeline(
            string command,
            bool addToHistory
            ) /* throw */
        {
            Runspace runspace = Runspace.DefaultRunspace;

            return (runspace != null) ?
                runspace.CreateNestedPipeline(command, addToHistory) : null;
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a nested PowerShell pipeline for the specified
        /// command text and synchronously invokes it, collecting any objects it
        /// produces.  Currently, this requires the default runspace and always
        /// creates a nested pipeline.
        /// </summary>
        /// <param name="command">
        /// The PowerShell command text to invoke.
        /// </param>
        /// <param name="addToHistory">
        /// Non-zero to add the command to the PowerShell command history.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this contains the collection of objects produced by
        /// the invoked pipeline.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        private static ReturnCode InvokePipeline(
            string command,
            bool addToHistory,
            ref Collection<PSObject> returnValue,
            ref Result error
            ) /* throw */
        {
            //
            // HACK: Currently, this requires use of the default runspace and
            //       it will always create a nested pipeline.
            //
            using (Pipeline pipeline = CreatePipeline(command, addToHistory))
            {
                if (pipeline != null)
                {
                    returnValue = pipeline.Invoke();
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "could not create nested pipeline";
                    return ReturnCode.Error;
                }
            }
        }
        #endregion
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The set of sub-command names supported by the <c>cmdlet</c> command
        /// ensemble.
        /// </summary>
        private EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "about", "debug", "error", "invoke", "options",
            "progress", "remove", "status", "verbose"
        });

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the dictionary of sub-command names supported by the
        /// <c>cmdlet</c> command ensemble.
        /// </summary>
        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
            set { subCommands = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>cmdlet</c> command.  It dispatches to one
        /// of the ensemble sub-commands (for example <c>about</c>, <c>debug</c>,
        /// <c>error</c>, <c>invoke</c>, <c>options</c>, <c>progress</c>,
        /// <c>remove</c>, <c>status</c>, or <c>verbose</c>), most of which act
        /// upon the script cmdlet context obtained from the client data, the
        /// hosting PowerShell environment, or the command plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created; this is expected to contain (or wrap) the script cmdlet
        /// context.  This parameter should not be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; element one is the sub-command name; the remaining
        /// elements are the arguments for the selected sub-command.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the selected
        /// sub-command, such as the script cmdlet status, the plugin or define
        /// constant information, the value produced by an invoked pipeline, or
        /// an empty string.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            _Cmdlets.Script script = GetScriptCmdlet(clientData, ref result);

            if (script == null)
                return ReturnCode.Error;

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            string subCommand = arguments[1];
            bool tried = false;

            code = Utility.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, true,
                null, ref subCommand, ref tried, ref result);

            if ((code == ReturnCode.Ok) && !tried)
            {
                switch (subCommand)
                {
                    case "about":
                        {
                            if (arguments.Count == 2)
                            {
                                IPlugin plugin = this.Plugin;

                                if (plugin != null)
                                {
                                    code = plugin.About(
                                        interpreter, ref result);
                                }
                                else
                                {
                                    result = "invalid command plugin";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1}\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "debug":
                        {
                            if (arguments.Count == 3)
                            {
                                try
                                {
                                    script.WriteDebug(arguments[2]); /* throw */
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
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} text\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "error":
                        {
                            if (arguments.Count == 4)
                            {
                                object enumValue = Utility.TryParseEnum(
                                    typeof(ReturnCode), arguments[2],
                                    true, true, ref result);

                                if (enumValue is ReturnCode)
                                {
                                    try
                                    {
                                        script.WriteErrorRecord(
                                            (ReturnCode)enumValue,
                                            arguments[3]); /* throw */

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
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} code result\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "invoke":
                        {
                            if (arguments.Count >= 3)
                            {
                                OptionDictionary options = new OptionDictionary(
                                    new IOption[] {
                                    new Option(null, OptionFlags.NoCase, Index.Invalid,
                                        Index.Invalid, "-addToHistory", null)
                                }, Utility.GetFixupReturnValueOptions().Values);

                                int argumentIndex = Index.Invalid;

                                code = interpreter.GetOptions(options, arguments, 0, 2,
                                    Index.Invalid, true, ref argumentIndex, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        ((argumentIndex + 1) == arguments.Count))
                                    {
                                        Type returnType;
                                        ObjectFlags objectFlags;
                                        string objectName;
                                        string interpName;
                                        bool create;
                                        bool dispose;
                                        bool alias;
                                        bool aliasRaw;
                                        bool aliasAll;
                                        bool aliasReference;
                                        bool toString;

                                        Utility.ProcessFixupReturnValueOptions(
                                            options, null, out returnType, out objectFlags,
                                            out objectName, out interpName, out create,
                                            out dispose, out alias, out aliasRaw,
                                            out aliasAll, out aliasReference, out toString);

                                        bool addToHistory = false;

                                        if (options.IsPresent("-addToHistory"))
                                            addToHistory = true;

                                        Collection<PSObject> returnValue = null;

                                        try
                                        {
                                            code = InvokePipeline(
                                                arguments[argumentIndex], addToHistory,
                                                ref returnValue, ref result);
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = e;
                                            code = ReturnCode.Error;
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            ObjectOptionType objectOptionType =
                                                Utility.GetOptionType(aliasRaw, aliasAll);

                                            code = Utility.FixupReturnValue(interpreter,
                                                GetBinder(interpreter, this.Plugin),
                                                interpreter.CultureInfo, returnType,
                                                objectFlags, Utility.GetInvokeOptions(
                                                objectOptionType), objectOptionType,
                                                objectName, interpName, returnValue,
                                                create, dispose, alias, aliasReference,
                                                toString, ref result);
                                        }
                                    }
                                    else
                                    {
                                        if ((argumentIndex != Index.Invalid) &&
                                            Option.LooksLikeOption(arguments[argumentIndex]))
                                        {
                                            result = OptionDictionary.BadOption(
                                                options, arguments[argumentIndex],
                                                !interpreter.IsSafe());
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? script\"",
                                                this.Name, subCommand);
                                        }

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} ?options? script\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "options":
                        {
                            if (arguments.Count == 2)
                            {
                                IPlugin plugin = this.Plugin;

                                if (plugin != null)
                                {
                                    code = plugin.Options(
                                        interpreter, ref result);
                                }
                                else
                                {
                                    //
                                    // NOTE: There is (normally) no plugin
                                    //       context for this library.
                                    //
                                    code = GetDefineConstants(ref result);
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1}\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "progress":
                        {
                            if (arguments.Count >= 5)
                            {
                                OptionDictionary options = new OptionDictionary(
                                    new IOption[] {
                                    new Option(null, OptionFlags.MustHaveValue |
                                        OptionFlags.NoCase, Index.Invalid, Index.Invalid,
                                        "-currentOperation", null),
                                    new Option(null, OptionFlags.MustHaveIntegerValue |
                                        OptionFlags.NoCase, Index.Invalid, Index.Invalid,
                                        "-parentActivityId", null),
                                    new Option(null, OptionFlags.MustHaveIntegerValue |
                                        OptionFlags.NoCase, Index.Invalid, Index.Invalid,
                                        "-percentComplete", null),
                                    new Option(typeof(ProgressRecordType),
                                        OptionFlags.MustHaveIntegerValue | OptionFlags.NoCase,
                                        Index.Invalid, Index.Invalid, "-recordType",
                                        new Variant((ProgressRecordType)(-1))),
                                    new Option(null, OptionFlags.MustHaveIntegerValue |
                                        OptionFlags.NoCase, Index.Invalid, Index.Invalid,
                                        "-secondsRemaining", null),
                                    Option.CreateEndOfOptions()
                                });

                                int argumentIndex = Index.Invalid;

                                code = interpreter.GetOptions(options, arguments, 0, 2,
                                    Index.Invalid, true, ref argumentIndex, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        ((argumentIndex + 3) == arguments.Count))
                                    {
                                        IVariant value = null;
                                        string currentOperation = null;

                                        if (options.IsPresent("-currentOperation", true, ref value))
                                            currentOperation = value.ToString();

                                        int parentActivityId = Identifier.Invalid;

                                        if (options.IsPresent("-parentActivityId", true, ref value))
                                            parentActivityId = (int)value.Value;

                                        int percentComplete = Percent.Invalid;

                                        if (options.IsPresent("-percentComplete", true, ref value))
                                            percentComplete = (int)value.Value;

                                        ProgressRecordType recordType = (ProgressRecordType)(-1);

                                        if (options.IsPresent("-recordType", true, ref value))
                                            recordType = (ProgressRecordType)value.Value;

                                        int secondsRemaining = Count.Invalid;

                                        if (options.IsPresent("-secondsRemaining", true, ref value))
                                            secondsRemaining = (int)value.Value;

                                        int activityId = Identifier.Invalid;

                                        code = Value.GetInteger2(
                                            (IGetValue)arguments[argumentIndex], ValueFlags.AnyInteger,
                                            interpreter.CultureInfo, ref activityId, ref result);

                                        if (code == ReturnCode.Ok)
                                        {
                                            try
                                            {
                                                ProgressRecord progressRecord = new ProgressRecord(
                                                    activityId, arguments[argumentIndex + 1],
                                                    arguments[argumentIndex + 2]); /* throw */

                                                if (currentOperation != null)
                                                    progressRecord.CurrentOperation = currentOperation;

                                                if (parentActivityId != Identifier.Invalid)
                                                    progressRecord.ParentActivityId = parentActivityId; /* throw */

                                                if (percentComplete != Percent.Invalid)
                                                    progressRecord.PercentComplete = percentComplete; /* throw */

                                                if (recordType != (ProgressRecordType)(-1))
                                                    progressRecord.RecordType = recordType; /* throw */

                                                if (secondsRemaining != Count.Invalid)
                                                    progressRecord.SecondsRemaining = secondsRemaining;

                                                script.WriteProgress(progressRecord); /* throw */

                                                result = String.Empty;
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
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
                                                options, arguments[argumentIndex],
                                                !interpreter.IsSafe());
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? activityId activity statusDescription\"",
                                                this.Name, subCommand);
                                        }

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} ?options? activityId activity statusDescription\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "remove":
                        {
                            if (arguments.Count == 2)
                            {
                                code = script.RemoveMetaCommand(interpreter, ref result);

                                if (code == ReturnCode.Ok)
                                    result = String.Empty;
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1}\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "status":
                        {
                            if (arguments.Count == 2)
                            {
                                result = StringList.MakeList(
                                    "Disposed", script.Disposed, /* PEDANTIC */
                                    "FlagsCallback", script.FlagsCallback,
                                    "StateCallback", script.StateCallback,
                                    "ParameterCallback", script.ParameterCallback,
                                    "Listener", script.Listener,
                                    "PreInitialize", script.PreInitialize,
                                    "CreateFlags", script.CreateFlags,
                                    "EngineFlags", script.EngineFlags,
                                    "SubstitutionFlags", script.SubstitutionFlags,
                                    "EventFlags", script.EventFlags,
                                    "ExpressionFlags", script.ExpressionFlags,
                                    "Console", script.Console,
                                    "Unsafe", script.Unsafe,
                                    "Standard", script.Standard,
                                    "Force", script.Force,
                                    "Exceptions", script.Exceptions,
                                    "Policies", script.Policies,
                                    "Deny", script.Deny,
                                    "MetaCommand", script.MetaCommand,
                                    "Text", script.Text,
                                    "Interpreter", script.Interpreter,
                                    "Tokens", script.Tokens,
                                    "CommandRuntime", script.CommandRuntime,
                                    "Stopping", script.Stopping); /* throw */
                            }
                            else
                            {
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1}\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    case "verbose":
                        {
                            if (arguments.Count == 3)
                            {
                                try
                                {
                                    script.WriteVerbose(arguments[2]); /* throw */
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
                                result = String.Format(
                                    "wrong # args: should be \"{0} {1} text\"",
                                    this.Name, subCommand);

                                code = ReturnCode.Error;
                            }
                            break;
                        }
                    default:
                        {
                            result = Utility.BadSubCommand(
                                interpreter, null, null, subCommand, this, null, null);

                            code = ReturnCode.Error;
                            break;
                        }
                }
            }

            return code;
        }
        #endregion
    }
}
