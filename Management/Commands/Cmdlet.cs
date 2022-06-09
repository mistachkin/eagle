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
    [ObjectId("b62ba209-838c-46ad-8782-2269e470cf7a")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Cmdlet : Default
    {
        #region Public Constructor
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
        private EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "about", "debug", "error", "invoke", "options",
            "progress", "remove", "status", "verbose"
        });

        ////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
            set { subCommands = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecute Members
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
                false, ref subCommand, ref tried, ref result);

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
                                    new Option(null, OptionFlags.None, Index.Invalid,
                                        Index.Invalid, "-addtohistory", null)
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

                                        if (options.IsPresent("-addtohistory"))
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
                                        Variant value = null;
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
