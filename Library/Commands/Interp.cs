/*
 * Interp.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("f83b2063-cf1f-428f-9cb9-7a1862a69960")]
    //
    // TODO: Make this command "safe".  The main thing that needs to be done is
    //       to audit the code for security and make sure any state changes are
    //       isolated to the current interpreter or one of its children.
    //
    [CommandFlags(
        CommandFlags.Unsafe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Interp : Core
    {
        public Interp(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "addcommands",
            "alias", "aliases", "bgerror", "callbacklimit", "cancel", "childlimit", "children",
            "create", "delete", "enabled", "eval", "eventlimit", "exists", "expose", "exposed",
            "expr", "finallytimeout", "hide", "hidden", "immutable", "invokehidden",
            "isolated", "issafe", "issdk", "isstandard", "makesafe", "makestandard",
            "marktrusted", "namespacelimit", "nopolicy", "parent", "policy", "proclimit",
            "queue", "readonly", "readorgetscriptfile", "readylimit", "recursionlimit",
            "rename", "resetcancel", "resultlimit", "scopelimit", "service", "set",
            "shareinterp", "shareobject", "sleeptime", "source", "stub",
            "subcommand", "subst", "target", "timeout", "unset", "varlimit",
            "watchdog"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary allowedSubCommands = new EnsembleDictionary(
            PolicyOps.AllowedInterpSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
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
                                case "addcommands":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            //
                                            // TODO: All of these options are flagged as "Unsafe" just
                                            //       in case this sub-command ends up being exposed to
                                            //       "Safe" interpreters in the future (e.g. to allow
                                            //       them to restore their own core command set, etc).
                                            //
                                            // NOTE: Both default values for flag enumeration options
                                            //       to this sub-command are null because they would
                                            //       be based on the child interpreter flags; however,
                                            //       the child interpreter has not been looked up at
                                            //       this point.  Unfortunately, this means that flag
                                            //       operators (e.g. "+") will not work as expected
                                            //       for these sub-command options, due to lack of the
                                            //       old (original) values.
                                            //
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(CreateFlags),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-createflags", null),
                                                new Option(typeof(InterpreterFlags),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-interpreterflags", null),
                                                new Option(null, OptionFlags.MustHaveRuleSetValue |
                                                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                                    "-ruleset", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                                    Index.Invalid, "-safetyoverride", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                                    Index.Invalid, "-repopulate", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.InternalIsSafe())
                                                        {
                                                            lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                IPlugin plugin = childInterpreter.GetCorePlugin(
                                                                    ref result);

                                                                if (plugin != null)
                                                                {
                                                                    bool haveOverride = false;
                                                                    Variant value = null;

                                                                    CreateFlags createFlags =
                                                                        childInterpreter.CreateFlagsNoLock;

                                                                    if (options.IsPresent("-createflags", ref value))
                                                                    {
                                                                        createFlags = (CreateFlags)value.Value;
                                                                        haveOverride = true;
                                                                    }

                                                                    InterpreterFlags interpreterFlags =
                                                                        childInterpreter.InterpreterFlagsNoLock;

                                                                    if (options.IsPresent("-interpreterflags", ref value))
                                                                    {
                                                                        interpreterFlags = (InterpreterFlags)value.Value;
                                                                        haveOverride = true;
                                                                    }

                                                                    IRuleSet ruleSet = null;

                                                                    if (options.IsPresent("-ruleset", ref value))
                                                                        ruleSet = (IRuleSet)value.Value;

                                                                    bool safetyOverride = false;

                                                                    if (options.IsPresent("-safetyoverride", ref value))
                                                                        safetyOverride = true;

                                                                    bool repopulate = false;

                                                                    if (options.IsPresent("-repopulate", ref value))
                                                                        repopulate = true;

                                                                    createFlags &= CreateFlags.CoreCommandSetMask;
                                                                    interpreterFlags &= InterpreterFlags.UseCultureForOperators;

                                                                    AddEntityClientData addEntityClientData;

                                                                    addEntityClientData = haveOverride ?
                                                                        new AddEntityClientData(
                                                                            null, createFlags, interpreterFlags) :
                                                                        new AddEntityClientData(
                                                                            null, childInterpreter);

                                                                    if (!haveOverride)
                                                                    {
                                                                        if (addEntityClientData.CreateSafe)
                                                                            addEntityClientData.HideUnsafe = true;

                                                                        if (addEntityClientData.CreateStandard)
                                                                            addEntityClientData.HideNonStandard = true;
                                                                    }

                                                                    if (safetyOverride ||
                                                                        (addEntityClientData.HasMatchingCreateFlags(childInterpreter) &&
                                                                        (addEntityClientData.HasMatchingCreateAndHideFlags())))
                                                                    {
                                                                        if (repopulate)
                                                                        {
                                                                            //
                                                                            // TODO: The noCommands and noPolicies parameters are
                                                                            //       hard-coded to false here.  Rethink?
                                                                            //
                                                                            code = RuntimeOps.PopulatePluginEntities(
                                                                                childInterpreter, plugin, null, ruleSet,
                                                                                childInterpreter.PluginFlags, null,
                                                                                Interpreter.ShouldUseBuiltIns(
                                                                                    IdentifierKind.Command), false,
                                                                                false, ref result);
                                                                        }

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            string pattern = arguments[argumentIndex + 1];

                                                                            if (String.IsNullOrEmpty(pattern))
                                                                                pattern = null;

                                                                            int addCount = 0;

                                                                            code = childInterpreter.AddCommands(
                                                                                addEntityClientData, plugin,
                                                                                clientData, CommandFlags.None,
                                                                                pattern, false, ref addCount,
                                                                                ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                if (addEntityClientData.IsHidingAnything())
                                                                                {
                                                                                    int moveCount = 0;

                                                                                    code = childInterpreter.MoveExposedAndHiddenCommands(
                                                                                        plugin.Flags, ref moveCount, ref result);

                                                                                    if (code == ReturnCode.Ok)
                                                                                    {
                                                                                        result = StringList.MakeList(
                                                                                            "added", addCount, "moved", moveCount);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = StringList.MakeList(
                                                                                        "added", addCount);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "permission denied: one or more safety checks failed";
                                                                        code = ReturnCode.Error;
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
                                                            result = "permission denied: safe interpreter cannot add commands";
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
                                                        result = "wrong # args: should be \"interp addcommands ?options? path pattern\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp addcommands ?options? path pattern\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "alias":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            Interpreter sourceInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                arguments[2], LookupFlags.Interpreter, false,
                                                ref sourceInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string sourceName = arguments[3];
                                                string sourceCommandName = ScriptOps.MakeCommandName(sourceName);

                                                if (arguments.Count == 4)
                                                {
                                                    //
                                                    // NOTE: Return the alias definition.
                                                    //
                                                    IAlias alias = null;

                                                    code = sourceInterpreter.GetAlias(
                                                        sourceName, LookupFlags.Default,
                                                        ref alias, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = alias.ToString();
                                                }
                                                else if ((arguments.Count == 5) &&
                                                    String.IsNullOrEmpty(arguments[4]))
                                                {
                                                    //
                                                    // NOTE: Delete the alias definition.
                                                    //
                                                    code = sourceInterpreter.RemoveAliasAndCommand(
                                                        sourceName, clientData, false, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else if (arguments.Count > 5)
                                                {
                                                    Interpreter targetInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        arguments[4], LookupFlags.Interpreter, false,
                                                        ref targetInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string targetName = arguments[5];

                                                        if ((arguments.Count == 6) &&
                                                            String.IsNullOrEmpty(targetName))
                                                        {
                                                            //
                                                            // NOTE: Delete the alias definition.
                                                            //
                                                            code = sourceInterpreter.RemoveAliasAndCommand(
                                                                sourceName, clientData, false, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: Create the alias definition.
                                                            //
                                                            if ((sourceInterpreter.DoesIExecuteExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                (sourceInterpreter.RemoveIExecute(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                            {
                                                                if ((sourceInterpreter.DoesProcedureExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                    (sourceInterpreter.RemoveProcedure(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                                {
                                                                    if ((sourceInterpreter.DoesCommandExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                        (sourceInterpreter.RemoveCommand(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                                    {
                                                                        ArgumentList targetArguments = new ArgumentList(targetName);

                                                                        if (arguments.Count > 6)
                                                                            targetArguments.AddRange(ArgumentList.GetRange(arguments, 6));

                                                                        IAlias alias = null;

                                                                        code = sourceInterpreter.AddAlias(
                                                                            sourceName, CommandFlags.None, AliasFlags.SkipSourceName | AliasFlags.CrossCommandAlias,
                                                                            clientData, targetInterpreter, null, targetArguments, null, 0, ref alias, ref result);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    goto aliasArgs;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            goto aliasArgs;
                                        }
                                        break;

                                    aliasArgs:
                                        result = "wrong # args: should be \"interp alias childPath childCmd ?parentPath parentCmd? ?arg ...?\"";
                                        code = ReturnCode.Error;
                                        break;
                                    }
                                case "aliases":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            string pattern = (arguments.Count >= 4) ?
                                                (string)arguments[3] : null;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool all = false; /* TODO: Good default? */

                                                if (arguments.Count >= 5)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[4], ValueFlags.AnyBoolean,
                                                        childInterpreter.InternalCultureInfo, ref all,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    StringList list = null;

                                                    code = childInterpreter.ListAliases(
                                                        pattern, false, all, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp aliases ?path? ?pattern? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "bgerror":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    if (arguments.Count == 4)
                                                    {
                                                        StringList list = null;

                                                        code = ListOps.GetOrCopyOrSplitList(
                                                            childInterpreter, arguments[3], true,
                                                            ref list, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            childInterpreter.BackgroundError = list.ToString();
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = childInterpreter.BackgroundError;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp bgerror path ?cmdPrefix?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "callbacklimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if CALLBACK_QUEUE
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int callbackLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref callbackLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            childInterpreter.InternalCallbackLimit = callbackLimit;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "callback", childInterpreter.InternalCallbackLimit);
                                                    }
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp callbacklimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cancel":
                                    {
                                        OptionDictionary options = new OptionDictionary(
                                            new IOption[] {
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-global", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nolocal", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-unwind", null),
                                            Option.CreateEndOfOptions()
                                        });

                                        int argumentIndex = Index.Invalid;

                                        if (arguments.Count > 2)
                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                        else
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            string path = null;
                                            Result cancelResult = null;

                                            if (argumentIndex != Index.Invalid)
                                            {
                                                if ((argumentIndex + 2) >= arguments.Count)
                                                {
                                                    //
                                                    // NOTE: Grab the name of the interpreter.
                                                    //
                                                    path = arguments[argumentIndex];

                                                    //
                                                    // NOTE: The cancel result is just after the interpreter.
                                                    //
                                                    if ((argumentIndex + 1) < arguments.Count)
                                                        cancelResult = arguments[argumentIndex + 1];
                                                }
                                                else
                                                {
                                                    if (Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp cancel ?-unwind? ?--? ?path? ?result?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                path = String.Empty;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                Interpreter childInterpreter = null;

                                                code = interpreter.GetNestedChildInterpreter(
                                                    path, LookupFlags.Interpreter, false,
                                                    ref childInterpreter, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    CancelFlags cancelFlags = CancelFlags.InterpCancel;

                                                    if (options.IsPresent("-unwind"))
                                                        cancelFlags |= CancelFlags.Unwind;

                                                    if (options.IsPresent("-global"))
                                                        cancelFlags |= CancelFlags.Global;

                                                    if (options.IsPresent("-nolocal"))
                                                        cancelFlags &= ~CancelFlags.Local;

                                                    code = Engine.CancelEvaluate(
                                                        childInterpreter, cancelResult, cancelFlags,
                                                        ref result);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case "childlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int childLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref childLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            childInterpreter.InternalChildLimit = childLimit;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "child", childInterpreter.InternalChildLimit);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp childlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "children":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    if (childInterpreter.HasChildInterpreters(ref result))
                                                    {
                                                        result = childInterpreter.ChildInterpretersToString(null, false);
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?path?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        OptionDictionary options = new OptionDictionary(
                                            new IOption[] {
                                            new Option(typeof(CreationFlagTypes), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue,
                                                Index.Invalid, Index.Invalid, "-creationflagtypes", new Variant(Defaults.CreationFlagTypes)),
                                            new Option(null, OptionFlags.MustHaveRuleSetValue, Index.Invalid, Index.Invalid, "-ruleset", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-namespaces", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nofunctions", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonamespaces", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noinitialize", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-alias", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-safe", null),
#if DEBUG
                                            new Option(typeof(SdkType), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue,
                                                Index.Invalid, Index.Invalid, "-sdk", new Variant(SdkType.Default)),
#else
                                            new Option(typeof(SdkType), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue |
                                                OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-sdk", new Variant(SdkType.Default)),
#endif
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nohidden", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-standard", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-unsafeinitialize", null),
#if APPDOMAINS && ISOLATED_INTERPRETERS
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-isolated", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-isolated", null),
#endif
#if DEBUGGER
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-debug", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-debug", null),
#endif
#if TEST_PLUGIN || DEBUG
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-test", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-test", null),
#endif
#if NOTIFY && NOTIFY_ARGUMENTS
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-monitor", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-monitor", null),
#endif
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-probing", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noprobing", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-probing", null),
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-noprobing", null),
#endif
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-security", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nosecurity", null),
                                            Option.CreateEndOfOptions()
                                        });

                                        int argumentIndex = Index.Invalid;

                                        if (arguments.Count > 2)
                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                        else
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                            {
                                                if (interpreter.HasChildInterpreters(ref result))
                                                {
                                                    string path = null;
                                                    string name = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        path = arguments[argumentIndex];

                                                    if ((path != null) &&
                                                        (interpreter.DoesChildInterpreterExist(path, true, ref name) == ReturnCode.Ok))
                                                    {
                                                        result = String.Format(
                                                            "interpreter named \"{0}\" already exists, cannot create",
                                                            name);

                                                        code = ReturnCode.Error;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Variant value = null;
                                                        IRuleSet ruleSet = null;

                                                        if (options.IsPresent("-ruleset", ref value))
                                                            ruleSet = (IRuleSet)value.Value;

                                                        CreationFlagTypes creationFlagTypes = Defaults.CreationFlagTypes;

                                                        if (options.IsPresent("-creationflagtypes", ref value))
                                                            creationFlagTypes = (CreationFlagTypes)value.Value;

                                                        //
                                                        // HACK: Inherit the default "use namespaces" setting
                                                        //       from the parent interpreter.
                                                        //
                                                        bool namespaces = interpreter.AreNamespacesEnabled();

                                                        if (options.IsPresent("-namespaces"))
                                                            namespaces = true;

                                                        if (options.IsPresent("-nonamespaces"))
                                                            namespaces = false;

                                                        bool initialize = true;

                                                        if (options.IsPresent("-noinitialize"))
                                                            initialize = false;

                                                        bool noCommands = false;

                                                        if (options.IsPresent("-nocommands"))
                                                            noCommands = true;

                                                        bool noFunctions = false;

                                                        if (options.IsPresent("-nofunctions"))
                                                            noFunctions = true;

                                                        bool variables = true;

                                                        if (options.IsPresent("-novariables"))
                                                            variables = false;

                                                        bool safe = interpreter.InternalIsSafe();

                                                        if (options.IsPresent("-safe"))
                                                            safe = true;

                                                        SdkType sdkType = SdkType.Default;

                                                        if (options.IsPresent("-sdk", ref value))
                                                            sdkType = (SdkType)value.Value;

                                                        bool noHidden = false;

                                                        if (options.IsPresent("-nohidden"))
                                                            noHidden = true;

                                                        bool alias = false;

                                                        if (options.IsPresent("-alias"))
                                                            alias = true;

                                                        bool standard = false;

                                                        if (options.IsPresent("-standard"))
                                                            standard = true;

                                                        bool unsafeInitialize = false;

                                                        if (options.IsPresent("-unsafeinitialize"))
                                                            unsafeInitialize = true;

                                                        bool isolated = false;

#if APPDOMAINS && ISOLATED_INTERPRETERS
                                                        if (options.IsPresent("-isolated"))
                                                            isolated = true;
#endif

#if DEBUGGER
                                                        bool debug = false;

                                                        if (options.IsPresent("-debug"))
                                                            debug = true;
#endif

#if TEST_PLUGIN || DEBUG
                                                        bool test = false;

                                                        if (options.IsPresent("-test"))
                                                            test = true;
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
                                                        bool monitor = false;

                                                        if (options.IsPresent("-monitor"))
                                                            monitor = true;
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                        bool probing = interpreter.HasProbePlugins();

                                                        if (options.IsPresent("-probing"))
                                                            probing = true;

                                                        if (options.IsPresent("-noprobing"))
                                                            probing = false;
#endif

                                                        bool security = interpreter.HasSecurity();

                                                        if (options.IsPresent("-security"))
                                                            security = true;

                                                        if (options.IsPresent("-nosecurity"))
                                                            security = false;

                                                        CreateFlags createFlags;
                                                        HostCreateFlags hostCreateFlags;
                                                        InitializeFlags initializeFlags;
                                                        ScriptFlags scriptFlags;
                                                        InterpreterFlags interpreterFlags;
                                                        PluginFlags pluginFlags;

                                                        ScriptOps.ExtractInterpreterCreationFlags(
                                                            interpreter, creationFlagTypes,
                                                            CreateFlags.NestedUse,
                                                            HostCreateFlags.NestedUse,
                                                            null, null, null, null, out createFlags,
                                                            out hostCreateFlags, out initializeFlags,
                                                            out scriptFlags, out interpreterFlags,
                                                            out pluginFlags);

                                                        //
                                                        // NOTE: Enable full namespace support?
                                                        //
                                                        if (namespaces)
                                                            createFlags |= CreateFlags.UseNamespaces;
                                                        else
                                                            createFlags &= ~CreateFlags.UseNamespaces;

                                                        //
                                                        // NOTE: Initialize the script library?
                                                        //
                                                        createFlags &= ~CreateFlags.Initialize;

                                                        //
                                                        // NOTE: Do not initialize the script library
                                                        //       unless requested -AND- there are (at
                                                        //       least some?) core commands present.
                                                        //
                                                        if (initialize && !noCommands)
                                                        {
                                                            //
                                                            // NOTE: We cannot initialize the script
                                                            //       library when created as "safe"
                                                            //       with no hidden commands (i.e.
                                                            //       all "unsafe" commands would be
                                                            //       missing, e.g. [info], some of
                                                            //       which are necessary for script
                                                            //       library initialization).
                                                            //
                                                            if (!safe || !noHidden)
                                                                createFlags |= CreateFlags.Initialize;
                                                        }

                                                        //
                                                        // NOTE: Are we creating a safe interpreter?  If so, make
                                                        //       sure the "full initialize" option is not present,
                                                        //       then disable evaluating "init.eagle" and evaluate
                                                        //       "safe.eagle" instead.
                                                        //
                                                        if (safe)
                                                        {
                                                            if (noHidden)
                                                                createFlags |= CreateFlags.Safe;
                                                            else
                                                                createFlags |= CreateFlags.SafeAndHideUnsafe;

                                                            if (!unsafeInitialize)
                                                            {
                                                                initializeFlags &= ~InitializeFlags.Initialization;
                                                                initializeFlags |= InitializeFlags.Safe;
                                                            }

                                                            interpreterFlags &= ~InterpreterFlags.UnsafeMask;
                                                        }

                                                        if (standard)
                                                        {
                                                            if (noHidden)
                                                                createFlags |= CreateFlags.Standard;
                                                            else
                                                                createFlags |= CreateFlags.StandardAndHideNonStandard;
                                                        }

#if DEBUGGER
                                                        //
                                                        // NOTE: Do we want a script debugger?
                                                        //
                                                        if (debug)
                                                        {
                                                            createFlags |= CreateFlags.DebuggerUse;
                                                            hostCreateFlags |= HostCreateFlags.DebuggerUse;
                                                        }
#endif

#if TEST_PLUGIN || DEBUG
                                                        //
                                                        // NOTE: Do we want to enable the test plugin?
                                                        //
                                                        if (test)
                                                            createFlags &= ~CreateFlags.NoTestPlugin;
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
                                                        //
                                                        // NOTE: Do we want to enable the trace plugin?
                                                        //
                                                        if (monitor)
                                                            createFlags &= ~CreateFlags.NoMonitorPlugin;
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                                                        //
                                                        // NOTE: Do we want to probe possible plugins
                                                        //       for additional packages, etc?
                                                        //
                                                        if (probing)
                                                            createFlags |= CreateFlags.ProbePlugins;
                                                        else
                                                            createFlags &= ~CreateFlags.ProbePlugins;
#endif

                                                        //
                                                        // NOTE: Set the built-in variables?
                                                        //
                                                        createFlags &= ~CreateFlags.NoVariablesMask;

                                                        if (!variables)
                                                        {
                                                            if (FlagOps.HasFlags(
                                                                    createFlags, CreateFlags.Initialize, true))
                                                            {
                                                                //
                                                                // HACK: Some portions of the script library
                                                                //       require pre-set variables for their
                                                                //       initialization.
                                                                //
                                                                createFlags |= CreateFlags.MinimumVariables;
                                                            }
                                                            else
                                                            {
                                                                createFlags |= CreateFlags.NoVariables;
                                                            }
                                                        }

                                                        createFlags &= ~CreateFlags.NoCommands;

                                                        if (noCommands)
                                                            createFlags |= CreateFlags.NoCommands;

                                                        createFlags &= ~CreateFlags.NoFunctions;

                                                        if (noFunctions)
                                                            createFlags |= CreateFlags.NoFunctions;

                                                        if (FlagOps.HasFlags(sdkType, SdkType.Security, true))
                                                            createFlags |= CreateFlags.SecuritySdk;

                                                        if (FlagOps.HasFlags(sdkType, SdkType.License, true))
                                                            createFlags |= CreateFlags.LicenseSdk;

                                                        if (noCommands || FlagOps.HasFlags(
                                                                createFlags, CreateFlags.SdkMask, false))
                                                        {
                                                            //
                                                            // HACK: Various package index script files in the
                                                            //       wild contain commands that are simply not
                                                            //       available in SDK enabled interpreters.
                                                            //
                                                            initializeFlags |= InitializeFlags.NoTraceAutoPath;
                                                        }

                                                        code = interpreter.CreateChildInterpreter(path,
                                                            clientData, ruleSet, createFlags, hostCreateFlags,
                                                            initializeFlags, scriptFlags, interpreterFlags,
                                                            pluginFlags, isolated, security, alias, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"interp create ?-safe? ?--? ?path?\"";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        break;
                                    }
                                case "delete":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            if (interpreter.HasChildInterpreters(ref result))
                                            {
                                                for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                                {
                                                    code = interpreter.DeleteChildInterpreter(
                                                        arguments[argumentIndex], clientData,
                                                        ObjectOps.GetDefaultSynchronous(),
                                                        ref result);

                                                    if (code != ReturnCode.Ok)
                                                        break;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp delete ?path ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enabled":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool enabled = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        childInterpreter.InternalCultureInfo, ref enabled,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.Enabled = enabled;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.Enabled;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp enabled ?path? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string name = StringList.MakeList("interp eval", path);

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Evaluate | CallFrameFlags.Restricted);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                if (arguments.Count == 4)
                                                    code = childInterpreter.EvaluateScript(arguments[3], ref result);
                                                else
                                                    code = childInterpreter.EvaluateScript(arguments, 3, ref result);

                                                if (code == ReturnCode.Error)
                                                {
                                                    Engine.CopyErrorInformation(childInterpreter, interpreter, result);

                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in interp eval \"{1}\" script line {2})",
                                                            Environment.NewLine, path, Interpreter.GetErrorLine(childInterpreter)));
                                                }

                                                //
                                                // NOTE: Pop the original call frame that we pushed above and
                                                //       any intervening scope call frames that may be leftover
                                                //       (i.e. they were not explicitly closed).
                                                //
                                                /* IGNORED */
                                                interpreter.PopScopeCallFramesAndOneMore();
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp eval path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eventlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int eventLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref eventLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            childInterpreter.InternalEventLimit = eventLimit;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "event", childInterpreter.InternalEventLimit);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp eventlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            result = (interpreter.DoesChildInterpreterExist(path) == ReturnCode.Ok);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp exists ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "expose":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    code = childInterpreter.ExposeCommand(arguments[3], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot expose commands";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp expose path hiddenCmdName ?cmdName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exposed":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    StringList list = null;

                                                    code = childInterpreter.CommandsToList(
                                                        CommandFlags.None, CommandFlags.Hidden, false,
                                                        false, null, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        code = childInterpreter.ProceduresToList(
                                                            ProcedureFlags.None, ProcedureFlags.Hidden, false,
                                                            false, null, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp exposed path\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "expr":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string name = StringList.MakeList("interp expr", path);

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Expression | CallFrameFlags.Restricted);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                //
                                                // FIXME: The expression parser does not know the line where
                                                //        the error happened unless it evaluates a command
                                                //        contained within the expression.
                                                //
                                                Interpreter.SetErrorLine(childInterpreter, 0);

                                                if (arguments.Count == 4)
                                                    code = childInterpreter.EvaluateExpression(arguments[3], ref result);
                                                else
                                                    code = childInterpreter.EvaluateExpression(arguments, 3, ref result);

                                                if (code == ReturnCode.Error)
                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in interp expr \"{1}\" script line {2})",
                                                            Environment.NewLine, path, Interpreter.GetErrorLine(childInterpreter)));

                                                //
                                                // NOTE: Pop the original call frame that we pushed above and
                                                //       any intervening scope call frames that may be leftover
                                                //       (i.e. they were not explicitly closed).
                                                //
                                                /* IGNORED */
                                                interpreter.PopScopeCallFramesAndOneMore();
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp expr path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "finallytimeout":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int timeout = _Timeout.None;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        childInterpreter.InternalCultureInfo, ref timeout,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!childInterpreter.InternalSetOrUnsetTimeout(
                                                                TimeoutType.Finally, timeout, ref result))
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int? timeout = childInterpreter.InternalGetTimeout(
                                                        TimeoutType.Finally, ref result);

                                                    if (timeout != null)
                                                    {
                                                        result = (int)timeout;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp finallytimeout ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hide":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    code = childInterpreter.HideCommand(arguments[3], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot hide commands";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp hide path cmdName ?hiddenCmdName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hidden":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    StringList list = null;

                                                    code = childInterpreter.HiddenCommandsToList(
                                                        CommandFlags.Hidden, CommandFlags.None, false,
                                                        false, null, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        code = childInterpreter.HiddenProceduresToList(
                                                            ProcedureFlags.Hidden, ProcedureFlags.None, false,
                                                            false, null, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp hidden path\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "immutable":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool immutable = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        childInterpreter.InternalCultureInfo, ref immutable, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.Immutable = immutable;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.Immutable;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp immutable ?path? ?immutable?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invokehidden":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid,
                                                        Index.Invalid, "-global", null),
                                                    new Option(null, OptionFlags.MustHaveAbsoluteNamespaceValue,
                                                        Index.Invalid, Index.Invalid, "-namespace", null),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = childInterpreter.GetOptions(
                                                    options, arguments, 0, 3, Index.Invalid, true,
                                                    ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        bool global = false;

                                                        if (options.IsPresent("-global"))
                                                            global = true;

                                                        Variant value = null;
                                                        INamespace @namespace = null;

                                                        if (options.IsPresent("-namespace", ref value))
                                                            @namespace = (INamespace)value.Value;

                                                        string executeName = arguments[argumentIndex];
                                                        IExecute execute = null;

                                                        code = childInterpreter.GetIExecuteViaResolvers(
                                                            childInterpreter.GetResolveEngineFlagsNoLock(true) |
                                                            EngineFlags.UseHidden, executeName, null,
                                                            LookupFlags.Default, ref execute, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Figure out the arguments for the command to
                                                            //       be executed.
                                                            //
                                                            ArgumentList executeArguments =
                                                                ArgumentList.GetRange(arguments, argumentIndex);

                                                            //
                                                            // NOTE: Create and push a new call frame to track the
                                                            //       activation of this alias.
                                                            //
                                                            string name = StringList.MakeList(
                                                                "interp invokehidden", executeName);

                                                            ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                                name, CallFrameFlags.Interpreter);

                                                            interpreter.PushAutomaticCallFrame(frame);

                                                            ICallFrame nestedFrame = null;

                                                            if (!global && (@namespace != null))
                                                            {
                                                                string nestedName = StringList.MakeList(
                                                                    "namespace eval", @namespace.QualifiedName);

                                                                nestedFrame = childInterpreter.NewNamespaceCallFrame(
                                                                    nestedName, CallFrameFlags.Evaluate |
                                                                    CallFrameFlags.UseNamespace, null, @namespace,
                                                                    false);

                                                                childInterpreter.PushNamespaceCallFrame(nestedFrame);
                                                            }

                                                            //
                                                            // NOTE: Execute the command in the global scope?
                                                            //
                                                            if (global)
                                                                childInterpreter.PushGlobalCallFrame(true);
                                                            else if (nestedFrame != null)
                                                                childInterpreter.PushNamespaceCallFrame(nestedFrame);

                                                            try
                                                            {
                                                                //
                                                                // NOTE: Save the current engine flags and then enable
                                                                //       the external execution flags.
                                                                //
                                                                EngineFlags savedEngineFlags =
                                                                    childInterpreter.BeginExternalExecution();

                                                                try
                                                                {
                                                                    //
                                                                    // NOTE: Execute the hidden command now.
                                                                    //
                                                                    code = childInterpreter.ExecuteHidden(
                                                                        executeName, execute, clientData, executeArguments,
                                                                        ref result);
                                                                }
                                                                finally
                                                                {
                                                                    //
                                                                    // NOTE: Restore the saved engine flags, masking off the
                                                                    //       external execution flags as necessary.
                                                                    //
                                                                    /* IGNORED */
                                                                    childInterpreter.EndAndCleanupExternalExecution(
                                                                        savedEngineFlags);
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                //
                                                                // NOTE: If we previously pushed the global call frame
                                                                //       (above), we also need to pop any leftover scope
                                                                //       call frames now; otherwise, the call stack will
                                                                //       be imbalanced.
                                                                //
                                                                if (global)
                                                                    childInterpreter.PopGlobalCallFrame(true);
                                                                else if (nestedFrame != null)
                                                                    /* IGNORED */
                                                                    childInterpreter.PopNamespaceCallFrame(nestedFrame);
                                                            }

                                                            //
                                                            // NOTE: Pop the original call frame that we pushed above
                                                            //       and any intervening scope call frames that may be
                                                            //       leftover (i.e. they were not explicitly closed).
                                                            //
                                                            /* IGNORED */
                                                            interpreter.PopScopeCallFramesAndOneMore();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp invokehidden path ?options? cmd ?arg ..?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp invokehidden path ?options? cmd ?arg ..?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isolated":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    result = !AppDomainOps.IsCurrent(childInterpreter);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp isolated ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "issafe":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    result = childInterpreter.InternalIsSafe();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp issafe ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "issdk":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                SdkType sdkType = SdkType.AnySdkMask;

                                                if (arguments.Count >= 4)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        childInterpreter, typeof(SdkType),
                                                        sdkType.ToString(), arguments[3],
                                                        childInterpreter.InternalCultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is SdkType)
                                                        sdkType = (SdkType)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        result = childInterpreter.InternalIsSdk(sdkType, false);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp issdk ?path? ?sdkType?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isstandard":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    result = childInterpreter.InternalIsStandard();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp isstandard ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "makesafe":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    bool safe = true;

                                                    if (arguments.Count >= 4)
                                                    {
                                                        code = Value.GetBoolean2(
                                                            arguments[3], ValueFlags.AnyBoolean,
                                                            childInterpreter.InternalCultureInfo,
                                                            ref safe, ref result);
                                                    }

                                                    MakeFlags makeFlags = MakeFlags.SafeLibrary;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (arguments.Count >= 5)
                                                        {
                                                            object enumValue = EnumOps.TryParseFlags(
                                                                childInterpreter, typeof(MakeFlags),
                                                                makeFlags.ToString(), arguments[4],
                                                                childInterpreter.InternalCultureInfo, true, true,
                                                                true, ref result);

                                                            if (enumValue is MakeFlags)
                                                                makeFlags = (MakeFlags)enumValue;
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            if (safe != childInterpreter.InternalIsSafe())
                                                            {
                                                                code = childInterpreter.InternalMakeSafe(
                                                                    makeFlags, safe, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "interpreter is already marked as \"{0}\"",
                                                                    childInterpreter.InternalIsSafe() ?
                                                                        "safe" : "unsafe");

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot modify safety";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp makesafe ?path? ?safe? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "makestandard":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    bool standard = true;

                                                    if (arguments.Count >= 4)
                                                    {
                                                        code = Value.GetBoolean2(
                                                            arguments[3], ValueFlags.AnyBoolean,
                                                            childInterpreter.InternalCultureInfo,
                                                            ref standard, ref result);
                                                    }

                                                    MakeFlags makeFlags = MakeFlags.StandardLibrary;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (arguments.Count >= 5)
                                                        {
                                                            object enumValue = EnumOps.TryParseFlags(
                                                                childInterpreter, typeof(MakeFlags),
                                                                makeFlags.ToString(), arguments[4],
                                                                childInterpreter.InternalCultureInfo, true, true,
                                                                true, ref result);

                                                            if (enumValue is MakeFlags)
                                                                makeFlags = (MakeFlags)enumValue;
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            if (standard != childInterpreter.InternalIsStandard())
                                                            {
                                                                code = childInterpreter.MakeStandard(
                                                                    makeFlags, standard, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "interpreter is already marked as \"{0}\"",
                                                                    childInterpreter.InternalIsStandard() ?
                                                                        "standard" : "non-standard");

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot modify standardization";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp makestandard ?path? ?standard? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "marktrusted":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    code = childInterpreter.MarkTrusted(ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot mark trusted";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp marktrusted path\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "namespacelimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int namespaceLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref namespaceLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            childInterpreter.InternalNamespaceLimit = namespaceLimit;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "namespace", childInterpreter.InternalNamespaceLimit);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp namespacelimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nopolicy":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    code = childInterpreter.RemovePolicy(
                                                        arguments[3], clientData, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot remove policy";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp nopolicy path name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "parent":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Interpreter parentInterpreter =
                                                    childInterpreter.ParentInterpreter;

                                                if (parentInterpreter != null)
                                                    result = parentInterpreter.IdNoThrow;
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp parent ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "policy":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid, Index.Invalid, "-type", null),
                                                new Option(null, OptionFlags.MustHaveWideIntegerValue, Index.Invalid, Index.Invalid, "-token", null),
                                                new Option(typeof(PolicyFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(PolicyFlags.Script)),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    Type type = null;

                                                    if (options.IsPresent("-type", ref value))
                                                        type = (Type)value.Value;

                                                    long token = 0;

                                                    if (options.IsPresent("-token", ref value))
                                                        token = (long)value.Value;

                                                    PolicyFlags flags = PolicyFlags.Script;

                                                    if (options.IsPresent("-flags", ref value))
                                                        flags = (PolicyFlags)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.InternalIsSafe())
                                                        {
                                                            if ((type != null) || (token != 0))
                                                            {
                                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                                {
                                                                    IPlugin plugin = childInterpreter.GetCorePlugin(ref result);

                                                                    if (plugin != null)
                                                                    {
                                                                        code = childInterpreter.AddScriptPolicy(
                                                                            flags, type, token, interpreter,
                                                                            arguments[argumentIndex + 1],
                                                                            plugin, clientData, ref result);
                                                                    }
                                                                    else
                                                                    {
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "option \"-type\" or \"-token\" must be specified";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "permission denied: safe interpreter cannot add policy";
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
                                                        result = "wrong # args: should be \"interp policy ?options? path script\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp policy ?options? path script\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "proclimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int procedureLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref procedureLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            childInterpreter.InternalProcedureLimit = procedureLimit;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "procedure", childInterpreter.InternalProcedureLimit);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp proclimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "queue":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-when", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    DateTime dateTime = TimeOps.GetUtcNow();

                                                    if (options.IsPresent("-when", ref value))
                                                        dateTime = (DateTime)value.Value;

                                                    string path = arguments[2];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string text = null;

                                                        if ((argumentIndex + 1) == arguments.Count)
                                                            text = arguments[argumentIndex];
                                                        else
                                                            text = ListOps.Concat(arguments, argumentIndex);

                                                        code = childInterpreter.QueueScript(dateTime, text, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"interp queue path ?options? arg ?arg ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp queue path ?options? arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readonly":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool readOnly = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        childInterpreter.InternalCultureInfo, ref readOnly, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.ReadOnly = readOnly;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.ReadOnly;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readonly ?path? ?readonly?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readorgetscriptfile":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            IOption scriptFlagsOption = new Option(
                                                typeof(ScriptFlags), OptionFlags.MustHaveEnumValue |
                                                OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                                "-scriptflags", null);

                                            IOption engineFlagsOption = new Option(
                                                typeof(EngineFlags), OptionFlags.MustHaveEnumValue |
                                                OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                                "-engineflags", null);

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                                    Index.Invalid, Index.Invalid, "-encoding", null),
                                                scriptFlagsOption,
                                                engineFlagsOption,
                                                Option.CreateEndOfOptions()
                                            });

                                            int scanArgumentIndex = Index.Invalid;

                                            code = interpreter.ScanOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref scanArgumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((scanArgumentIndex != Index.Invalid) &&
                                                    ((scanArgumentIndex + 2) == arguments.Count))
                                                {
                                                    string path = arguments[scanArgumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            ScriptFlags oldScriptFlags = ScriptOps.GetFlags(
                                                                childInterpreter, childInterpreter.ScriptFlags,
                                                                true, false);

                                                            EngineFlags oldEngineFlags = childInterpreter.EngineFlags;

                                                            scriptFlagsOption.Value = new Variant(oldScriptFlags);
                                                            engineFlagsOption.Value = new Variant(oldEngineFlags);

                                                            int getArgumentIndex = Index.Invalid;

                                                            code = interpreter.GetOptions(
                                                                options, arguments, 0, 2, Index.Invalid, false,
                                                                ref getArgumentIndex, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (getArgumentIndex == scanArgumentIndex)
                                                                {
                                                                    Variant value = null;
                                                                    Encoding encoding = null;

                                                                    if (options.IsPresent("-encoding", ref value))
                                                                        encoding = (Encoding)value.Value;

                                                                    ScriptFlags newScriptFlags = oldScriptFlags;

                                                                    if (options.IsPresent("-scriptflags", ref value))
                                                                        newScriptFlags = (ScriptFlags)value.Value;

                                                                    EngineFlags newEngineFlags = oldEngineFlags;

                                                                    if (options.IsPresent("-engineflags", ref value))
                                                                        newEngineFlags = (EngineFlags)value.Value;

                                                                    string fileName = arguments[getArgumentIndex + 1];

                                                                    if (!String.IsNullOrEmpty(fileName))
                                                                    {
                                                                        EngineFlags engineFlags = Engine.CombineFlagsWithMasks(
                                                                            oldEngineFlags, newEngineFlags,
                                                                            EngineFlags.ReadOrGetScriptFileMask,
                                                                            EngineFlags.ReadOrGetScriptFileMask
                                                                        );

                                                                        SubstitutionFlags substitutionFlags = childInterpreter.SubstitutionFlags;
                                                                        EventFlags eventFlags = childInterpreter.EngineEventFlags;
                                                                        ExpressionFlags expressionFlags = childInterpreter.ExpressionFlags;
                                                                        string text = null;

                                                                        code = Engine.ReadOrGetScriptFile(
                                                                            childInterpreter, encoding, ref newScriptFlags, ref fileName,
                                                                            ref engineFlags, ref substitutionFlags, ref eventFlags,
                                                                            ref expressionFlags, ref text, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                            result = text;
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "invalid file name";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // HACK: This error should never happen.
                                                                    //
                                                                    result = String.Format(
                                                                        "mismatch of first non-option argument index " +
                                                                        "between \"get\" mode and \"scan\" mode: {0} " +
                                                                        "versus {1}", getArgumentIndex, scanArgumentIndex);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((scanArgumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[scanArgumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[scanArgumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp readorgetscriptfile ?options? path fileName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readorgetscriptfile ?options? path fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readylimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                int readyLimit = 0;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        childInterpreter.InternalCultureInfo, ref readyLimit,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.ReadyLimit = readyLimit;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.ReadyLimit;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readylimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "recursionlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                int recursionLimit = 0;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        childInterpreter.InternalCultureInfo, ref recursionLimit,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.RecursionLimit = recursionLimit;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.RecursionLimit;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp recursionlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rename":
                                    {
                                        if (arguments.Count >= 5)
                                        {
                                            OptionDictionary options = new OptionDictionary(new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-nodelete", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                                    Index.Invalid, "-hidden", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                                    Index.Invalid, "-hiddenonly", null),
                                                new Option(typeof(IdentifierKind),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-kind",
                                                    new Variant(IdentifierKind.None)),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                                    Index.Invalid, "-newnamevar", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 3) == arguments.Count))
                                                {
                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Variant value = null;
                                                        IdentifierKind kind = IdentifierKind.None;

                                                        if (options.IsPresent("-kind", ref value))
                                                            kind = (IdentifierKind)value.Value;

                                                        bool delete = true;

                                                        if (options.IsPresent("-nodelete"))
                                                            delete = false;

                                                        bool hidden = false;

                                                        if (options.IsPresent("-hidden"))
                                                            hidden = true;

                                                        bool hiddenOnly = false;

                                                        if (options.IsPresent("-hiddenonly"))
                                                            hiddenOnly = true;

                                                        string varName = null;

                                                        if (options.IsPresent("-newnamevar", ref value))
                                                            varName = value.ToString();

                                                        string oldName = arguments[argumentIndex + 1];
                                                        string newName = arguments[argumentIndex + 2];
                                                        Result localResult = null;

                                                        if (kind == IdentifierKind.Object)
                                                        {
                                                            if (childInterpreter.RenameObject(
                                                                    oldName, newName, false, false, false,
                                                                    ref localResult) == ReturnCode.Ok)
                                                            {
                                                                result = String.Empty;
                                                                return ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = localResult;
                                                                return ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (childInterpreter.RenameAnyIExecute(
                                                                    oldName, newName, varName, kind,
                                                                    false, delete, hidden, hiddenOnly,
                                                                    ref localResult) == ReturnCode.Ok)
                                                            {
                                                                result = String.Empty;
                                                                return ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = localResult;
                                                                return ReturnCode.Error;
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
                                                        result = "wrong # args: should be \"interp rename ?options? path oldName newName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp rename ?options? path oldName newName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resetcancel":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-global", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nolocal", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 3)
                                                code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    CancelFlags cancelFlags = CancelFlags.InterpResetCancel;

                                                    if (options.IsPresent("-global"))
                                                        cancelFlags |= CancelFlags.Global;

                                                    if (options.IsPresent("-force"))
                                                        cancelFlags |= CancelFlags.IgnorePending;

                                                    if (options.IsPresent("-nolocal"))
                                                        cancelFlags &= ~CancelFlags.Local;

                                                    string path = arguments[2];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: This really only works for the current interpreter
                                                        //       if the cancel was not specified with the unwind
                                                        //       flag; otherwise, this command will never actually
                                                        //       get a chance to execute.  For interpreters other
                                                        //       than the current interpreter, this will always
                                                        //       "just work".
                                                        //
                                                        bool reset = false;

                                                        code = Engine.ResetCancel(
                                                            childInterpreter, cancelFlags, ref reset, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = reset;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"interp resetcancel path ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp resetcancel path ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resultlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if RESULT_LIMITS
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int resultLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref resultLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                childInterpreter.InternalExecuteResultLimit = resultLimit;
                                                                childInterpreter.InternalNestedResultLimit = resultLimit;
                                                            }
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            result = StringList.MakeList(
                                                                "execute", childInterpreter.InternalExecuteResultLimit,
                                                                "nested", childInterpreter.InternalNestedResultLimit);
                                                        }
                                                    }
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp resultlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scopelimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int scopeLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref scopeLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            childInterpreter.InternalScopeLimit = scopeLimit;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "scope", childInterpreter.InternalScopeLimit);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp scopelimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "service":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-dedicated", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocancel", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noglobalcancel", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-erroronempty", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-userinterface", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                    new Option(null, OptionFlags.MustHaveWideIntegerValue, Index.Invalid, Index.Invalid, "-thread", null),
                                                    new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-limit", null),
                                                    new Option(typeof(EventFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-eventflags",
                                                        new Variant(childInterpreter.ServiceEventFlags)),
                                                    new Option(typeof(EventPriority), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-priority",
                                                        new Variant(EventPriority.Service)),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                if (arguments.Count > 3)
                                                    code = childInterpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                                                else
                                                    code = ReturnCode.Ok;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex == Index.Invalid)
                                                    {
                                                        bool dedicated = false;

                                                        if (options.IsPresent("-dedicated"))
                                                            dedicated = true;

                                                        bool noCancel = false;

                                                        if (options.IsPresent("-nocancel"))
                                                            noCancel = true;

                                                        bool noGlobalCancel = false;

                                                        if (options.IsPresent("-noglobalcancel"))
                                                            noGlobalCancel = true;

                                                        bool stopOnError = true;

                                                        if (options.IsPresent("-nocomplain"))
                                                            stopOnError = false;

                                                        bool errorOnEmpty = false;

                                                        if (options.IsPresent("-erroronempty"))
                                                            errorOnEmpty = true;

                                                        bool userInterface = false;

                                                        if (options.IsPresent("-userinterface"))
                                                            userInterface = true;

                                                        Variant value = null;
                                                        int limit = 0;

                                                        if (options.IsPresent("-limit", ref value))
                                                            limit = (int)value.Value;

                                                        long? threadId = null;

                                                        if (options.IsPresent("-thread", ref value))
                                                            threadId = (long)value.Value;

                                                        EventFlags eventFlags = childInterpreter.ServiceEventFlags;

                                                        if (options.IsPresent("-eventflags", ref value))
                                                            eventFlags = (EventFlags)value.Value;

                                                        EventPriority priority = EventPriority.Service;

                                                        if (options.IsPresent("-priority", ref value))
                                                            priority = (EventPriority)value.Value;

                                                        if (dedicated)
                                                        {
                                                            try
                                                            {
                                                                ServiceEventClientData serviceEventClientData =
                                                                    new ServiceEventClientData(
                                                                        clientData, interpreter, eventFlags,
                                                                        priority, threadId, limit, noCancel,
                                                                        noGlobalCancel, stopOnError,
                                                                        errorOnEmpty, userInterface
                                                                    );

                                                                if (Engine.QueueWorkItem(childInterpreter,
                                                                        EventManager.ServiceEventsThreadStart,
                                                                        serviceEventClientData))
                                                                {
                                                                    result = String.Empty;
                                                                    code = ReturnCode.Ok;
                                                                }
                                                                else
                                                                {
                                                                    result = "failed to queue event servicing work item";
                                                                    code = ReturnCode.Error;
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
                                                            IEventManager eventManager = childInterpreter.EventManager;

                                                            if (EventOps.ManagerIsOk(eventManager))
                                                            {
                                                                code = eventManager.ServiceEvents(
                                                                    eventFlags, priority, threadId, limit, noCancel,
                                                                    noGlobalCancel, stopOnError, errorOnEmpty,
                                                                    userInterface, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "event manager not available";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp service path ?options?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp service path ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Result value;

                                                if (arguments.Count == 4)
                                                {
                                                    value = null;

                                                    code = childInterpreter.GetVariableValue(
                                                        VariableFlags.None, arguments[3], ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = value;
                                                }
                                                else if (arguments.Count == 5)
                                                {
                                                    value = arguments[4];

                                                    code = childInterpreter.SetVariableValue(
                                                        VariableFlags.None, arguments[3], value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = value;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp set interp varName ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shareinterp":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[3], LookupFlags.Default, ref @object,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Interpreter otherInterpreter = (@object != null) ?
                                                            @object.Value as Interpreter : null;

                                                        if (otherInterpreter != null)
                                                        {
                                                            //
                                                            // NOTE: The interpreter is not owned by us and
                                                            //       the opaque object handle should really
                                                            //       be marked as NoDispose, if it has not
                                                            //       been already.
                                                            //
                                                            @object.ObjectFlags |= ObjectFlags.NoDispose;

                                                            //
                                                            // NOTE: Also, mark the interpreter itself as
                                                            //       shared, to prevent its eventual disposal
                                                            //       in the DisposeChildInterpreters method.
                                                            //       This flag will NOT prevent any other
                                                            //       code from disposing of this interpreter,
                                                            //       including from within the interpreter
                                                            //       itself.
                                                            //
                                                            otherInterpreter.SetShared();

                                                            //
                                                            // NOTE: Add the other (now shared) interpreter
                                                            //       to the specified interpreter as a child.
                                                            //
                                                            string otherId =
                                                                GlobalState.NextInterpreterId().ToString();

                                                            code = childInterpreter.AddChildInterpreter(
                                                                otherId, otherInterpreter, clientData,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = otherId;
                                                        }
                                                        else
                                                        {
                                                            result = "invalid interpreter";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot share interpreters";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp shareinterp interp objectName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shareobject":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.InternalIsSafe())
                                                {
                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[3], LookupFlags.Default, ref @object,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // HACK: Add the object handle verbatim to the "safe"
                                                        //       child interpreter.  It should be noted that
                                                        //       the object flags are NOT changed by this
                                                        //       sub-command; therefore, it will be unusable
                                                        //       by the "safe" interpreter until its flags are
                                                        //       manually adjusted in the parent interpreter.
                                                        //
                                                        long token = 0;

                                                        code = childInterpreter.AddSharedObject(
                                                            ObjectData.CreateForSharing(interpreter,
                                                            childInterpreter, @object
#if DEBUGGER && DEBUGGER_ARGUMENTS
                                                            , new ArgumentList(arguments)
#endif
                                                            ), clientData, null, @object.Value,
                                                            ref token, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = token;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot share objects";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp shareobject interp objectName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sleeptime":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int sleepTime = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        childInterpreter.InternalCultureInfo, ref sleepTime,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.SleepTime = sleepTime;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.SleepTime;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp sleeptime ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "source":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = childInterpreter.EvaluateFile(
                                                            arguments[argumentIndex + 1], ref result);
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
                                                        result = "wrong # args: should be \"interp source ?options? path fileName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp source ?options? path fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "stub":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-ensemble", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-external", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    bool ensemble = false;

                                                    if (options.IsPresent("-ensemble"))
                                                        ensemble = true;

                                                    bool external = false;

                                                    if (options.IsPresent("-external"))
                                                        external = true;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.InternalIsSafe())
                                                        {
                                                            ICommand command;
                                                            Result error = null;

                                                            if (external)
                                                            {
                                                                command = ScriptOps.NewExternalCommand(
                                                                    interpreter, arguments[argumentIndex + 1],
                                                                    _ClientData.Empty, this.Plugin, ref error);
                                                            }
                                                            else if (ensemble)
                                                            {
                                                                command = ScriptOps.NewEnsembleCommand(
                                                                    arguments[argumentIndex + 1],
                                                                    _ClientData.Empty, this.Plugin);
                                                            }
                                                            else
                                                            {
                                                                command = ScriptOps.NewStubCommand(
                                                                    arguments[argumentIndex + 1],
                                                                    _ClientData.Empty, this.Plugin,
                                                                    true);
                                                            }

                                                            if (command != null)
                                                            {
                                                                code = interpreter.AddCommand(
                                                                    command, clientData, ref result);
                                                            }
                                                            else if (error != null)
                                                            {
                                                                result = error;
                                                                code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
                                                                result = "could not create stub command";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "permission denied: safe interpreter cannot add stub commands";
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
                                                        result = "wrong # args: should be \"interp stub ?options? path name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp stub ?options? path name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subcommand":
                                    {
                                        if (arguments.Count >= 5)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(SubCommandFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(SubCommandFlags.Default)),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 3) <= arguments.Count) &&
                                                    ((argumentIndex + 4) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    SubCommandFlags subCommandFlags = SubCommandFlags.Default;

                                                    if (options.IsPresent("-flags", ref value))
                                                        subCommandFlags = (SubCommandFlags)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.InternalIsSafe())
                                                        {
                                                            lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                StringList list = null;

                                                                if ((argumentIndex + 4) == arguments.Count)
                                                                {
                                                                    code = ListOps.GetOrCopyOrSplitList(
                                                                        childInterpreter, arguments[argumentIndex + 3],
                                                                        true, ref list, ref result);
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    string commandName = ScriptOps.MakeCommandName(
                                                                        arguments[argumentIndex + 1]);

                                                                    ICommand command = null;

                                                                    code = childInterpreter.GetCommand(
                                                                        commandName, LookupFlags.Default,
                                                                        ref command, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        EnsembleDictionary subCommands = PolicyOps.GetSubCommandsUnsafe(
                                                                            command); /* ALREADY UNSAFE */

                                                                        if (subCommands != null)
                                                                        {
                                                                            string subCommandName = arguments[argumentIndex + 2];
                                                                            ISubCommand localSubCommand;

                                                                            if (!FlagOps.HasFlags(subCommandFlags,
                                                                                    SubCommandFlags.ForceQuery, true) &&
                                                                                (arguments.Count >= 6))
                                                                            {
                                                                                bool exists = subCommands.ContainsKey(subCommandName); /* EXEMPT */

                                                                                if ((list != null) && (list.Count > 0))
                                                                                {
                                                                                    if (!exists || !FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.ForceNew, true))
                                                                                    {
                                                                                        localSubCommand = ScriptOps.NewCommandSubCommand(
                                                                                            subCommandName, null, command, list,
                                                                                            ScriptOps.GetSubCommandNameIndex(),
                                                                                            subCommandFlags);

                                                                                        subCommands[subCommandName] = localSubCommand;
                                                                                        result = localSubCommand.ToString();
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't add new sub-command: already exists";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                                else if (FlagOps.HasFlags(subCommandFlags,
                                                                                        SubCommandFlags.ForceDelete, true))
                                                                                {
                                                                                    if (exists && subCommands.Remove(subCommandName))
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't remove sub-command: doesn't exist";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (exists || FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.ForceReset, true))
                                                                                    {
                                                                                        subCommands[subCommandName] = null;
                                                                                        result = String.Empty;
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't reset sub-command: doesn't exist";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                            }
                                                                            else if (subCommands.TryGetValue(
                                                                                    subCommandName, out localSubCommand))
                                                                            {
                                                                                result = (localSubCommand != null) ?
                                                                                    localSubCommand.ToString() :
                                                                                    String.Empty;
                                                                            }
                                                                            else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                    SubCommandFlags.NoComplain, true))
                                                                            {
                                                                                result = "can't query sub-command: doesn't exist";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                            else
                                                                            {
                                                                                result = String.Empty;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "sub-commands not available";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "permission denied: safe interpreter cannot manage sub-commands";
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
                                                        result = "wrong # args: should be \"interp subcommand ?options? path cmdName subCmdName ?command?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp subcommand ?options? path cmdName subCmdName ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subst":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nobackslashes", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;

                                                    if (options.IsPresent("-nobackslashes"))
                                                        substitutionFlags &= ~SubstitutionFlags.Backslashes;

                                                    if (options.IsPresent("-nocommands"))
                                                        substitutionFlags &= ~SubstitutionFlags.Commands;

                                                    if (options.IsPresent("-novariables"))
                                                        substitutionFlags &= ~SubstitutionFlags.Variables;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string name = StringList.MakeList("interp subst", path);

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Substitute | CallFrameFlags.Restricted);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        code = childInterpreter.SubstituteString(
                                                            arguments[argumentIndex + 1], substitutionFlags, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in interp subst \"{1}\" script line {2})",
                                                                    Environment.NewLine, path, Interpreter.GetErrorLine(childInterpreter)));

                                                        //
                                                        // NOTE: Pop the original call frame that we pushed above and
                                                        //       any intervening scope call frames that may be leftover
                                                        //       (i.e. they were not explicitly closed).
                                                        //
                                                        /* IGNORED */
                                                        interpreter.PopScopeCallFramesAndOneMore();
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
                                                        result = "wrong # args: should be \"interp subst ?options? path string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp subst ?options? path string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "target":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    string sourceName = arguments[3];
                                                    IAlias alias = null;

                                                    code = childInterpreter.GetAlias(
                                                        sourceName, LookupFlags.Default,
                                                        ref alias, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.GetInterpreterPath(
                                                            alias.TargetInterpreter, ref result);

                                                        if (code != ReturnCode.Ok)
                                                            result = String.Format(
                                                                "target interpreter for alias \"{0}\" " +
                                                                "in path \"{1}\" is not my descendant",
                                                                sourceName, path);
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Modify the error message (COMPAT: Tcl).
                                                        //
                                                        result = String.Format(
                                                            "alias \"{0}\" in path \"{1}\" not found",
                                                            sourceName, path);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp target path alias\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "timeout":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int timeout = _Timeout.None;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        childInterpreter.InternalCultureInfo, ref timeout,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        childInterpreter.InternalFallbackTimeout = timeout;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = childInterpreter.InternalFallbackTimeout;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp timeout ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = childInterpreter.UnsetVariable(
                                                    VariableFlags.NoRemove, arguments[3], ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp unset interp varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "varlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int variableLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            childInterpreter.InternalCultureInfo, ref variableLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                childInterpreter.InternalVariableLimit = variableLimit;
                                                                childInterpreter.InternalArrayElementLimit = variableLimit;
                                                            }
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (childInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            result = StringList.MakeList(
                                                                "variable", childInterpreter.InternalVariableLimit,
                                                                "arrayElement", childInterpreter.InternalArrayElementLimit);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp varlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "watchdog":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 6))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter childInterpreter = null;

                                            code = interpreter.GetNestedChildInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref childInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool? boolValue = null;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                {
                                                    code = Value.GetNullableBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        childInterpreter.InternalCultureInfo,
                                                        ref boolValue, ref result);
                                                }

                                                TimeoutFlags timeoutFlags = TimeoutFlags.Timeout;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        childInterpreter, typeof(TimeoutFlags),
                                                        timeoutFlags.ToString(), arguments[4],
                                                        childInterpreter.InternalCultureInfo,
                                                        true, false, true, ref result);

                                                    if (enumValue is TimeoutFlags)
                                                        timeoutFlags = (TimeoutFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                WatchdogType watchdogType = WatchdogType.Default;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 6))
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        childInterpreter, typeof(WatchdogType),
                                                        watchdogType.ToString(), arguments[5],
                                                        childInterpreter.InternalCultureInfo,
                                                        true, false, true, ref result);

                                                    if (enumValue is WatchdogType)
                                                        watchdogType = (WatchdogType)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    WatchdogOperation watchdogOperation;

                                                    if (boolValue == null)
                                                        watchdogOperation = WatchdogOperation.CheckAndFlags;
                                                    else if ((bool)boolValue)
                                                        watchdogOperation = WatchdogOperation.StartAndFlags;
                                                    else
                                                        watchdogOperation = WatchdogOperation.StopAndFlags;

                                                    if (childInterpreter.InternalNoThreadAbort)
                                                        watchdogOperation |= WatchdogOperation.NoAbort;

                                                    code = childInterpreter.InternalWatchdogControl(
                                                        watchdogType, watchdogOperation, timeoutFlags,
                                                        null, null, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp watchdog ?path? ?enabled? ?flags? ?type?\"";
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
                        result = "wrong # args: should be \"interp cmd ?arg ...?\"";
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
