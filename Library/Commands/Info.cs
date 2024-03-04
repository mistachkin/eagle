/*
 * Info.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NATIVE && WINDOWS
using WindowPair = System.Collections.Generic.KeyValuePair<
    Eagle._Components.Public.AnyPair<System.IntPtr, long>,
    Eagle._Components.Public.Pair<string>>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("6fd06e1a-4558-4264-88aa-a3121ed6232e")]
    /*
     * POLICY: We allow certain "safe" sub-commands.
     */
    [CommandFlags(
        CommandFlags.Unsafe | CommandFlags.Standard
#if NATIVE
        //
        // NOTE: Uses native code indirectly for querying various pieces of
        //       system information (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        | CommandFlags.Initialize
        )]
    [ObjectGroup("introspection")]
    internal sealed class Info : Core
    {
        public Info(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        //
        // TODO: Ick.  The mess here demonstrates why a better solution to
        //       ensembles is required.  Ideally, we could use a dictionary
        //       of sub-command(s) to delegates or something.
        //
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "active", "administrator", "appdomain",
            "args", "assembly", "base",
            "binary", "bindertypes", "body",
            "callbacks", "channels", "clr",
            "cmdcount", "cmdline", "cmdtype",
            "commands", "complete", "connections",
            "context",
            "culture", "cultures", "decision", "default",
            "delegates", "engine", "ensembles",
            "exists", "externals", "frame", "framework",
            "frameworkextra", "functions", "globals",
            "hostname", "hwnd", "identifier",
            "interactive", "interps", "lastinput", "level",
            "levelid", "library", "linkedname", "loaded",
            "locals", "modules", "nameofexecutable",
            "newline", "nprocs", "objects", "operands",
            "operators", "os", "patchlevel", "path",
            "pid", "plugin", "pluginflags",
            "policies", "ppid", "previouspid", "processors",
            "procs", "programextension", "ptid",
            "runtime", "runtimeversion", "script",
            "setup", "sharedlibextension", "shelllibrary",
            "source", "subcommands", "syntax", "sysvars",
            "tclversion", "tid", "transactions",
            "user", "varlinks", "vars",
            "whitespace", "windows", "windowtext"
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
            PolicyOps.AllowedInfoSubCommandNames);

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
                                case "active":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = GlobalState.ActiveInterpretersToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info active ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "administrator":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if NATIVE
                                            bool administrator = false;

                                            code = SecurityOps.IsAdministrator(ref administrator, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = administrator;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info administrator\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "appdomain":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = AppDomainOps.GetIdString(interpreter.GetAppDomain());
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info appdomain\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "args":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            bool defaults = false;

                                            if (arguments.Count == 4)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref defaults,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IProcedure procedure = null;

                                                code = interpreter.GetProcedureViaResolvers(
                                                    ScriptOps.MakeCommandName(arguments[2]),
                                                    LookupFlags.Default, ref procedure,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (FlagOps.HasFlags(
                                                            procedure.Flags, ProcedureFlags.Obfuscated,
                                                            true))
                                                    {
                                                        result = String.Format(
                                                            "procedure {0} arguments are unavailable",
                                                            FormatOps.DisplayName(procedure.Name));

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = defaults ?
                                                            procedure.Arguments.ToString(
                                                                ToStringFlags.NameAndDefault) :
                                                            procedure.Arguments.ToString();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info args procName ?defaults?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "assembly":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool entry = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref entry,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                Assembly assembly = entry ?
                                                    GlobalState.GetEntryAssembly() :
                                                    GlobalState.GetAssembly();

                                                if (assembly != null)
                                                    result = StringList.MakeList(
                                                        assembly.FullName, assembly.Location);
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info assembly ?entry?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "base":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = GlobalState.GetBasePath();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info base\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "binary":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool forceDefault = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref forceDefault,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // BUGFIX: If we are in a non-default application domain, we
                                                //         cannot really rely on the BaseDirectory to be what
                                                //         we really want to return here; therefore, use the
                                                //         entry assembly path instead in that case.
                                                //
                                                AppDomain appDomain = interpreter.GetAppDomain();

                                                //
                                                // NOTE: *SPECIAL* If the application domain is null, assume
                                                //       that we must be in the default application domain.
                                                //
                                                if (forceDefault ||
                                                    (appDomain == null) || AppDomainOps.IsDefault(appDomain))
                                                {
                                                    result = GlobalState.InitializeOrGetBinaryPath(false);
                                                }
                                                else
                                                {
                                                    result = GlobalState.GetAnyEntryAssemblyPath();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info binary ?forceDefault?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "bindertypes":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                IScriptBinder scriptBinder = interpreter.InternalBinder as IScriptBinder;

                                                if ((scriptBinder != null) && scriptBinder.HasChangeTypes())
                                                {
                                                    TypeList types = null;

                                                    code = scriptBinder.ListChangeTypes(ref types, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string pattern = null;

                                                        if (arguments.Count == 3)
                                                            pattern = arguments[2];

                                                        //
                                                        // NOTE: Return the list of types we know how to
                                                        //       marshal strings to.
                                                        //
                                                        result = types.ToString(pattern, false);
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: No binder types available.
                                                    //
                                                    result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info bindertypes ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "body":
                                    {
                                        if ((arguments.Count >= 3) && (arguments.Count <= 5))
                                        {
                                            IProcedure procedure = null;

                                            code = interpreter.GetProcedureViaResolvers(
                                                ScriptOps.MakeCommandName(arguments[2]),
                                                LookupFlags.Default, ref procedure,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool showLines = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref showLines,
                                                        ref result);
                                                }

                                                bool useLocation = false;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[4], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref useLocation,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (FlagOps.HasFlags(
                                                            procedure.Flags, ProcedureFlags.Obfuscated,
                                                            true))
                                                    {
                                                        result = String.Format(
                                                            "procedure {0} body is unavailable",
                                                            FormatOps.DisplayName(procedure.Name));

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        IScriptLocation location = useLocation ?
                                                            procedure.Location : null;

                                                        result = FormatOps.ProcedureBody(
                                                            procedure.Body, (location != null) ?
                                                                location.StartLine : Parser.NoLine,
                                                            showLines);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info body procName ?showLines? ?useLocation?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "callbacks":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.CallbacksToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info callbacks ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "channels":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            StringList list = null;

                                            code = interpreter.ListChannels(
                                                pattern, false, false, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info channels ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cmdcount":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            Interpreter childInterpreter = null;

                                            if (arguments.Count >= 3)
                                            {
                                                string path = arguments[2];

                                                code = interpreter.GetNestedChildInterpreter(
                                                    path, LookupFlags.Interpreter, false,
                                                    ref childInterpreter, ref result);
                                            }
                                            else
                                            {
                                                childInterpreter = interpreter;
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    string executeName = arguments[3];
                                                    IExecute execute = null;

                                                    code = childInterpreter.InternalGetIExecuteViaResolvers(
                                                        childInterpreter.GetResolveEngineFlagsNoLock(true),
                                                        executeName, null, LookupFlags.Default,
                                                        ref execute, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IUsageData usageData = execute as IUsageData;

                                                        if (usageData != null)
                                                        {
                                                            long value = 0;

                                                            if (usageData.GetUsage(
                                                                    UsageType.Count, ref value))
                                                            {
                                                                result = value;
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "usage count for \"{0}\" unknown",
                                                                    arguments[2]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "usage data for \"{0}\" not available",
                                                                arguments[2]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = childInterpreter.CommandCount;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info cmdcount ?path? ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cmdline":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Environment.CommandLine;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info cmdline\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cmdtype":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string executeName = arguments[2];
                                            IExecute execute = null;

                                            code = interpreter.InternalGetIExecuteViaResolvers(
                                                interpreter.GetResolveEngineFlagsNoLock(true),
                                                executeName, null, LookupFlags.NoWrapper,
                                                ref execute, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (execute is IProcedure)
                                                {
                                                    result = "proc";
                                                }
                                                else if (execute is IAlias)
                                                {
                                                    IExecute target = ((IAlias)execute).Target;

                                                    if (target is IWrapper)
                                                        target = ((IWrapper)target).Object as IExecute;

                                                    if (target is _Commands.Object)
                                                        result = "object";
                                                    else
                                                        result = "alias";
                                                }
                                                else if (execute is ICommand)
                                                {
                                                    EnsembleDictionary subCommands = PolicyOps.GetSubCommandsUnsafe(
                                                        (ICommand)execute); /* COUNT ONLY */

                                                    if ((subCommands != null) && (subCommands.Count > 0))
                                                        result = "ensemble";
                                                    else
                                                        result = "native";
                                                }
                                                else
                                                {
                                                    result = "native";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info cmdtype commandName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "commands":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(typeof(SdkType), OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-sdk", new Variant(SdkType.Default)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-breakpoint", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-core", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-library", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocore", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nolibrary", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-interactive", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noprocedures", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noexecutes", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noaliases", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-safe", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-unsafe", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-standard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonstandard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-hidden", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-hiddenonly", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-strict", null), // COMPAT: Eagle Beta.
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
                                                    IVariant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool breakpoint = false;

                                                    if (options.IsPresent("-breakpoint"))
                                                        breakpoint = true;

                                                    bool core = false;

                                                    if (options.IsPresent("-core"))
                                                        core = true;

                                                    bool library = false;

                                                    if (options.IsPresent("-library"))
                                                        library = true;

                                                    bool noCore = false;

                                                    if (options.IsPresent("-nocore"))
                                                        noCore = true;

                                                    bool noLibrary = false;

                                                    if (options.IsPresent("-nolibrary"))
                                                        noLibrary = true;

                                                    bool interactive = false;

                                                    if (options.IsPresent("-interactive"))
                                                        interactive = true;

                                                    bool noCommands = false;

                                                    if (options.IsPresent("-nocommands"))
                                                        noCommands = true;

                                                    bool noProcedures = false;

                                                    if (options.IsPresent("-noprocedures"))
                                                        noProcedures = true;

                                                    bool noExecutes = false;

                                                    if (options.IsPresent("-noexecutes"))
                                                        noExecutes = true;

                                                    bool noAliases = false;

                                                    if (options.IsPresent("-noaliases"))
                                                        noAliases = true;

                                                    bool safe = false;

                                                    if (options.IsPresent("-safe"))
                                                        safe = true;

                                                    bool @unsafe = false;

                                                    if (options.IsPresent("-unsafe"))
                                                        @unsafe = true;

                                                    bool standard = false;

                                                    if (options.IsPresent("-standard"))
                                                        standard = true;

                                                    bool nonStandard = false;

                                                    if (options.IsPresent("-nonstandard"))
                                                        nonStandard = true;

                                                    bool hidden = false;

                                                    if (options.IsPresent("-hidden"))
                                                        hidden = true;

                                                    bool hiddenOnly = false;

                                                    if (options.IsPresent("-hiddenonly"))
                                                        hiddenOnly = true;

                                                    SdkType sdkType = SdkType.Default;

                                                    if (options.IsPresent("-sdk", ref value))
                                                        sdkType = (SdkType)value.Value;

                                                    bool strict = false;

                                                    if (options.IsPresent("-strict"))
                                                        strict = true;

                                                    string pattern = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        pattern = arguments[argumentIndex];

                                                    //
                                                    // NOTE: You can never see hidden commands
                                                    //       or hidden procedures from a "safe"
                                                    //       interpreter; otherwise, you can
                                                    //       only see them if the right option
                                                    //       is used.
                                                    //
                                                    // NOTE: *SECURITY* When checking the "safe"
                                                    //       flag here, use calling interpreter
                                                    //       instead of the target interpreter.
                                                    //
                                                    bool listHidden = hidden && !interpreter.InternalIsSafe();

                                                    StringList list = null;

                                                    if ((code == ReturnCode.Ok) && !noCommands)
                                                    {
                                                        CommandFlags hasFlags = CommandFlags.None;

                                                        if (breakpoint)
                                                            hasFlags |= CommandFlags.Breakpoint;

                                                        if (core)
                                                            hasFlags |= CommandFlags.Core;

                                                        if (safe)
                                                            hasFlags |= CommandFlags.Safe;

                                                        if (@unsafe)
                                                            hasFlags |= CommandFlags.Unsafe;

                                                        if (standard)
                                                            hasFlags |= CommandFlags.Standard;

                                                        if (nonStandard)
                                                            hasFlags |= CommandFlags.NonStandard;

                                                        if (FlagOps.HasFlags(sdkType, SdkType.Initialize, true))
                                                            hasFlags |= CommandFlags.Initialize;

                                                        if (FlagOps.HasFlags(sdkType, SdkType.License, true))
                                                            hasFlags |= CommandFlags.LicenseSdk;

                                                        if (FlagOps.HasFlags(sdkType, SdkType.Security, true))
                                                            hasFlags |= CommandFlags.SecuritySdk;

                                                        CommandFlags notHasFlags = CommandFlags.Hidden;

                                                        if (noCore)
                                                            notHasFlags |= CommandFlags.Core;

                                                        if (noAliases)
                                                            notHasFlags |= CommandFlags.Alias;

                                                        if (listHidden)
                                                            notHasFlags &= ~CommandFlags.Hidden;

                                                        if ((code == ReturnCode.Ok) && !hiddenOnly)
                                                        {
                                                            code = localInterpreter.ListCommands(
                                                                hasFlags, notHasFlags, false, false,
                                                                pattern, false, false, false, ref list,
                                                                ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && listHidden)
                                                        {
                                                            code = localInterpreter.ListHiddenCommands(
                                                                hasFlags, notHasFlags, false, false,
                                                                pattern, false, false, false, ref list,
                                                                ref result);
                                                        }
                                                    }

                                                    if ((code == ReturnCode.Ok) && !noProcedures && !strict)
                                                    {
                                                        ProcedureFlags hasFlags = ProcedureFlags.None;

                                                        if (breakpoint)
                                                            hasFlags |= ProcedureFlags.Breakpoint;

                                                        if (core)
                                                            hasFlags |= ProcedureFlags.Core;

                                                        if (library)
                                                            hasFlags |= ProcedureFlags.Library;

                                                        if (interactive)
                                                            hasFlags |= ProcedureFlags.Interactive;

                                                        ProcedureFlags notHasFlags = ProcedureFlags.Hidden;

                                                        if (noCore)
                                                            notHasFlags |= ProcedureFlags.Core;

                                                        if (noLibrary)
                                                            notHasFlags |= ProcedureFlags.Library;

                                                        if (listHidden)
                                                            notHasFlags &= ~ProcedureFlags.Hidden;

                                                        if ((code == ReturnCode.Ok) && !hiddenOnly)
                                                        {
                                                            code = localInterpreter.ListProcedures(
                                                                hasFlags, notHasFlags, false, false, pattern,
                                                                false, false, false, ref list, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && listHidden)
                                                        {
                                                            code = localInterpreter.ListHiddenProcedures(
                                                                ProcedureFlags.None, notHasFlags, false, false,
                                                                pattern, false, false, false, ref list, ref result);
                                                        }
                                                    }

                                                    if ((code == ReturnCode.Ok) && !noExecutes && !strict)
                                                    {
                                                        code = localInterpreter.ListIExecutes(
                                                            pattern, false, false, ref list, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (list != null)
                                                            result = list;
                                                        else
                                                            result = String.Empty;
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
                                                        result = "wrong # args: should be \"info commands ?options? ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info commands ?options? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "complete":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            bool notReady = false; // NOTE: Do not inhibit Interpreter.Ready check.
                                            Result error = null;

                                            if (Parser.IsComplete(
                                                    interpreter, null, Parser.StartLine, arguments[2],
                                                    0, Length.Invalid, interpreter.EngineFlags,
                                                    interpreter.SubstitutionFlags, ref notReady,
                                                    ref error))
                                            {
                                                result = true;
                                            }
                                            else if (notReady)
                                            {
                                                //
                                                // NOTE: This is an exceptional case, the parsing was interrupted
                                                //       prior to being able to actually determine whether or not
                                                //       the script is complete.
                                                //
                                                result = error;
                                                code = ReturnCode.Error;
                                            }
                                            else
                                            {
                                                result = false;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info complete script\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "connections":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DATA
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.DbConnectionsToString(pattern, false);
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info connections ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "context":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.GetContext(ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info context\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "culture":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                try
                                                {
                                                    CultureInfo cultureInfo;

                                                    //
                                                    // HACK: This is to allow assignment to the "null culture" via the
                                                    //       null opaque object handle since we can be relatively certain
                                                    //       that the literal string "null" will not represent a valid
                                                    //       culture name.  We cannot use the empty string here to mean
                                                    //       the "null culture" because it represents the invariant culture.
                                                    //
                                                    object @object = null;

                                                    if (Value.GetObject(interpreter, arguments[2], ref @object) == ReturnCode.Ok)
                                                        cultureInfo = (CultureInfo)@object; /* throw */
                                                    else
                                                        cultureInfo = new CultureInfo(arguments[2]); /* throw */

                                                    interpreter.InternalCultureInfo = cultureInfo;
                                                }
                                                catch (Exception e)
                                                {
                                                    Engine.SetExceptionErrorCode(interpreter, e);

                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = FormatOps.CultureName(interpreter.InternalCultureInfo, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info culture ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cultures":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = new CultureInfoDictionary(CultureInfo.GetCultures(
                                                CultureTypes.AllCultures)).ToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info cultures ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "decision":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            PolicyDecisionType decisionType = PolicyDecisionType.Default;

                                            if (arguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(PolicyDecisionType),
                                                    decisionType.ToString(), arguments[2],
                                                    interpreter.InternalCultureInfo, true, true,
                                                    true, ref result);

                                                if (enumValue is PolicyDecisionType)
                                                    decisionType = (PolicyDecisionType)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                StringList list = null;

                                                code = PolicyOps.QueryDecisions(
                                                    interpreter, decisionType, ref list, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = list;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info decision ?types?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "default":
                                    {
                                        if (arguments.Count == 5)
                                        {
                                            IProcedure procedure = null;

                                            code = interpreter.GetProcedureViaResolvers(
                                                ScriptOps.MakeCommandName(arguments[2]),
                                                LookupFlags.Default, ref procedure,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (FlagOps.HasFlags(
                                                        procedure.Flags, ProcedureFlags.Obfuscated,
                                                        true))
                                                {
                                                    result = String.Format(
                                                        "procedure {0} arguments are unavailable",
                                                        FormatOps.DisplayName(procedure.Name));

                                                    code = ReturnCode.Error;
                                                }
                                                else
                                                {
                                                    if (procedure.Arguments != null)
                                                    {
                                                        bool found = false;
                                                        object @default = null;

                                                        foreach (Argument argument in procedure.Arguments)
                                                        {
                                                            if ((argument != null) &&
                                                                SharedStringOps.SystemEquals(
                                                                    argument.Name, arguments[3]))
                                                            {
                                                                @default = argument.Default;
                                                                found = true;
                                                                break;
                                                            }
                                                        }

                                                        if (found)
                                                        {
                                                            code = interpreter.SetVariableValue(
                                                                VariableFlags.None, arguments[4],
                                                                (@default != null) ? @default.ToString() : null,
                                                                null, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = (@default != null);
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "procedure \"{0}\" doesn't have an argument \"{1}\"",
                                                                arguments[2], arguments[3]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info default procName arg varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "delegates":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if EMIT && NATIVE && LIBRARY
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.DelegatesToString(pattern, false);
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info delegates ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "engine":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            EngineAttribute attribute = EngineAttribute.Default;

                                            if (arguments.Count >= 3)
                                            {
                                                object enumValue = EnumOps.TryParse(
                                                    typeof(EngineAttribute), arguments[2],
                                                    true, true, ref result);

                                                if (enumValue is EngineAttribute)
                                                {
                                                    attribute = (EngineAttribute)enumValue;
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "engine attribute", arguments[2],
                                                        Enum.GetNames(typeof(EngineAttribute)),
                                                        null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool refresh = !interpreter.HavePlatformVariables();

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref refresh,
                                                        ref result);
                                                }

                                                bool all = false;

                                                if (arguments.Count >= 5)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[4], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref all,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    switch (attribute)
                                                    {
                                                        case EngineAttribute.None:
                                                            {
                                                                result = String.Empty;
                                                                break;
                                                            }
                                                        case EngineAttribute.Name:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = Vars.Package.Name;
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, TclVars.Platform.Name,
                                                                        TclVars.Platform.Engine, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Culture:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = FormatOps.CultureName(
                                                                        GlobalState.GetAssemblyCultureInfo(), false);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Culture, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Version:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = FormatOps.MajorMinor(
                                                                        GlobalState.GetAssemblyVersion());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Version, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.PatchLevel:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = GlobalState.GetAssemblyVersion();
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.PatchLevel, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Release:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyRelease(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Release, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.SourceId:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblySourceId(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.SourceId, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.SourceTimeStamp:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblySourceTimeStamp(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.SourceTimeStamp, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.StrongNameTag:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyStrongNameTag(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.StrongNameTag, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Configuration:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AttributeOps.GetAssemblyConfiguration(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Configuration, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Tag:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyTag(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Tag, ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Text:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyText(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Text, ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.TimeStamp:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = FormatOps.Iso8601DateTime(
                                                                        SharedAttributeOps.GetAssemblyDateTime(
                                                                        GlobalState.GetAssembly()), true);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.TimeStamp, ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.CompileOptions:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    StringList options = DefineConstants.OptionList;

                                                                    if (options != null)
                                                                        result = options.ToString(false);
                                                                    else
                                                                        result = String.Empty;
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.CompileOptions, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.CSharpOptions:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = Vars.Platform.CSharpOptionsValue;
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.CSharpOptionsName, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Uri:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyUri(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Uri, ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.PublicKey:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AssemblyOps.GetPublicKey(
                                                                        GlobalState.GetAssemblyName());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.PublicKey, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.PublicKeyToken:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AssemblyOps.GetPublicKeyToken(
                                                                        GlobalState.GetAssemblyName());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.PublicKeyToken, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.ModuleVersionId:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AssemblyOps.GetModuleVersionId(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.ModuleVersionId, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.RuntimeOptions:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = interpreter.RuntimeOptions.ToString();
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.RuntimeOptions, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.ObjectIds:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    StringPairList list = AttributeOps.GetObjectIds(
                                                                        GlobalState.GetAssembly(), all, ref result);

                                                                    if (list != null)
                                                                        result = list;
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.ObjectIds, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.ImageRuntimeVersion:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AssemblyOps.GetImageRuntimeVersion(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.ImageRuntimeVersion, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.StrongName:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
#if CAS_POLICY
                                                                    result = FormatOps.StrongName(
                                                                        GlobalState.GetAssembly(),
                                                                        interpreter.GetStrongName(), true);
#else
                                                                    result = "not implemented";
                                                                    code = ReturnCode.Error;
#endif
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.StrongName, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Hash:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
#if CAS_POLICY
                                                                    result = FormatOps.Hash(interpreter.GetHash());
#else
                                                                    result = "not implemented";
                                                                    code = ReturnCode.Error;
#endif
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Hash, ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Certificate:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = FormatOps.Certificate(interpreter,
                                                                        GlobalState.GetAssemblyLocation(),
                                                                        interpreter.GetCertificate(), true, false,
                                                                        false);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Certificate, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.UpdateBaseUri:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyUpdateBaseUri(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.UpdateBaseUri, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.UpdatePathAndQuery:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = String.Format(
                                                                        Vars.Platform.UpdatePathAndQueryValue,
                                                                        GlobalState.GetAssemblyUpdateVersion(),
                                                                        null);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.UpdatePathAndQueryName,
                                                                        ref result, ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.DownloadBaseUri:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyDownloadBaseUri(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.DownloadBaseUri, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.ScriptBaseUri:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyScriptBaseUri(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.ScriptBaseUri, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.AuxiliaryBaseUri:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = SharedAttributeOps.GetAssemblyAuxiliaryBaseUri(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.AuxiliaryBaseUri, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.TargetFramework:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = AttributeOps.GetAssemblyTargetFramework(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.TargetFramework, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.NativeUtility:
                                                            {
#if NATIVE && NATIVE_UTILITY
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = NativeUtility.GetVersion(interpreter);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.NativeUtility, ref result,
                                                                        ref result);
                                                                }
#else
                                                                result = "not implemented";
                                                                code = ReturnCode.Error;
#endif
                                                                break;
                                                            }
                                                        case EngineAttribute.InterpreterTimeStamp:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = FormatOps.Iso8601DateTime(
                                                                        TimeOps.GetUtcNow(), true);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.InterpreterTimeStamp, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Vendor:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = RuntimeOps.GetVendor(interpreter, true);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Vendor, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Suffix:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = PathOps.GetBaseSuffix(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Suffix, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.TextOrSuffix:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = RuntimeOps.GetAssemblyTextOrSuffix(
                                                                        GlobalState.GetAssembly());
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.TextOrSuffix, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.Timeout:
                                                            {
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = ThreadOps.GetTimeout(
                                                                        interpreter, null, TimeoutType.Start |
                                                                        TimeoutType.MaybeFallback);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.Timeout, ref result,
                                                                        ref result);
                                                                }
                                                                break;
                                                            }
                                                        case EngineAttribute.NetworkTimeout:
                                                            {
#if NETWORK
                                                                if (refresh && !interpreter.InternalIsSafe())
                                                                {
                                                                    result = WebOps.GetTimeoutOrDefault(interpreter);
                                                                }
                                                                else
                                                                {
                                                                    code = interpreter.GetVariableValue2(
                                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                                        Vars.Platform.NetworkTimeout, ref result,
                                                                        ref result);
                                                                }
#else
                                                                result = "not implemented";
                                                                code = ReturnCode.Error;
#endif
                                                                break;
                                                            }
                                                        default:
                                                            {
                                                                result = String.Format(
                                                                    "unsupported engine attribute \"{0}\"",
                                                                    attribute);

                                                                code = ReturnCode.Error;
                                                                break;
                                                            }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info engine ?attribute? ?refresh? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ensembles":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            StringList list = null;

                                            code = interpreter.ListEnsembleCommands(
                                                pattern, false, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info ensembles ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = ConversionOps.ToInt(interpreter.DoesVariableExist(
                                                VariableFlags.NoUsable, arguments[2]) == ReturnCode.Ok);

#if false
                                            TraceOps.DebugTrace(String.Format(
                                                "Execute: interpreter = {0}, " +
                                                "subCommand = {1}, varName = {2}, " +
                                                "result = {3}",
                                                FormatOps.InterpreterNoThrow(interpreter),
                                                FormatOps.WrapOrNull(subCommand),
                                                FormatOps.WrapOrNull(arguments[2]),
                                                FormatOps.WrapOrNull(result)),
                                                typeof(Info).Name, TracePriority.Command);
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info exists varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "externals":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = GlobalState.GetExternalsPath();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info externals\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "frame":
                                    {
                                        //
                                        // NOTE: We do not currently support Tcl TIP #280 style functionality.
                                        //
                                        result = "not implemented";
                                        code = ReturnCode.Error;
                                        break;
                                    }
                                case "clr": // COMPAT: Eagle Beta.
                                case "framework":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    result = CommonOps.Runtime.GetFrameworkVersion();
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue2(
                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                        Vars.Platform.FrameworkVersion, ref result,
                                                        ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?refresh?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "frameworkextra":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
#if !NET_STANDARD_20
                                                    result = CommonOps.Runtime.GetFrameworkExtraVersion();
#else
                                                    result = "not implemented";
                                                    code = ReturnCode.Error;
#endif
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue2(
                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                        Vars.Platform.FrameworkExtraVersion, ref result,
                                                        ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info frameworkextra ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "functions":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-safe", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-unsafe", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-standard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonstandard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-hidden", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool safe = false;

                                                    if (options.IsPresent("-safe"))
                                                        safe = true;

                                                    bool @unsafe = false;

                                                    if (options.IsPresent("-unsafe"))
                                                        @unsafe = true;

                                                    bool standard = false;

                                                    if (options.IsPresent("-standard"))
                                                        standard = true;

                                                    bool nonStandard = false;

                                                    if (options.IsPresent("-nonstandard"))
                                                        nonStandard = true;

                                                    bool hidden = false;

                                                    if (options.IsPresent("-hidden"))
                                                        hidden = true;

                                                    string pattern = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        pattern = arguments[argumentIndex];

                                                    //
                                                    // NOTE: You can never see hidden functions
                                                    //       from a "safe" interpreter; otherwise,
                                                    //       you can only see them if the right
                                                    //       option is used.
                                                    //
                                                    // NOTE: *SECURITY* When checking the "safe"
                                                    //       flag here, use calling interpreter
                                                    //       instead of the target interpreter.
                                                    //
                                                    bool listHidden = hidden && !interpreter.InternalIsSafe();

                                                    //
                                                    // NOTE: Setup the flags used to filter the
                                                    //       list of returned functions.
                                                    //
                                                    FunctionFlags hasFlags = FunctionFlags.None;

                                                    if (safe)
                                                        hasFlags |= FunctionFlags.Safe;

                                                    if (@unsafe)
                                                        hasFlags |= FunctionFlags.Unsafe;

                                                    if (standard)
                                                        hasFlags |= FunctionFlags.Standard;

                                                    if (nonStandard)
                                                        hasFlags |= FunctionFlags.NonStandard;

                                                    //
                                                    // NOTE: By default, skip listing hidden
                                                    //       functions.
                                                    //
                                                    FunctionFlags notHasFlags = FunctionFlags.Hidden;

                                                    if (listHidden)
                                                        notHasFlags &= ~FunctionFlags.Hidden;

                                                    StringList list = null;

                                                    code = localInterpreter.ListFunctions(
                                                        hasFlags, notHasFlags, false, false, pattern,
                                                        false, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (list != null)
                                                            result = list;
                                                        else
                                                            result = String.Empty;
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
                                                        result = "wrong # args: should be \"info functions ?options? ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info functions ?options? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "globals":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.VariablesToList(
                                                VariableFlags.GlobalOnly, pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info globals ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hostname":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    result = Environment.MachineName;
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue2(
                                                        VariableFlags.GlobalOnly, TclVars.Platform.Name,
                                                        TclVars.Platform.Host, ref result, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info hostname ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hwnd":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if NATIVE && WINDOWS
                                            if (PlatformOps.IsWindowsOperatingSystem())
                                            {
                                                IntPtr handle = IntPtr.Zero;

                                                if (IntPtr.Size == sizeof(long))
                                                {
                                                    long value = 0;

                                                    code = Value.GetWideInteger2(
                                                        (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                                        interpreter.InternalCultureInfo, ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        handle = new IntPtr(value);
                                                }
                                                else
                                                {
                                                    int value = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                        interpreter.InternalCultureInfo, ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        handle = new IntPtr(value);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    long processId = 0;
                                                    long threadId = 0;

                                                    code = WindowOps.GetWindowThreadProcessId(
                                                        handle, ref processId, ref threadId, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList("hWnd", handle,
                                                            "processId", processId, "threadId", threadId);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "not supported on this operating system";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info hwnd handle\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "identifier":
                                    {
                                        if ((arguments.Count >= 3) && (arguments.Count <= 5))
                                        {
                                            IdentifierKind kind = IdentifierKind.Command; /* NOTE: Good default? */

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                            {
                                                string kindString = arguments[3];

                                                if (!String.IsNullOrEmpty(kindString))
                                                {
                                                    object enumValue = EnumOps.TryParse(
                                                        typeof(IdentifierKind), kindString,
                                                        true, true);

                                                    if (enumValue is IdentifierKind)
                                                    {
                                                        kind = (IdentifierKind)enumValue;
                                                    }
                                                    else
                                                    {
                                                        result = ScriptOps.BadValue(
                                                            "invalid", "identifier kind", kindString,
                                                            Enum.GetNames(typeof(IdentifierKind)),
                                                            null, null);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }

                                            bool full = false;

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[4], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref full,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IIdentifier identifier = null;

                                                code = interpreter.GetIdentifier(
                                                    kind, arguments[2], arguments, LookupFlags.NoWrapper,
                                                    ref identifier, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (identifier != null)
                                                    {
                                                        if (full)
                                                        {
                                                            ISyntax syntax = identifier as ISyntax;
                                                            IPlugin plugin = EntityOps.GetPluginNoThrow(identifier);

                                                            result = StringList.MakeList(
                                                                "type", FormatOps.RawTypeNameOrFullName(identifier),
                                                                "kind", identifier.Kind, "id", identifier.Id, "name",
                                                                identifier.Name, "group", identifier.Group, "description",
                                                                identifier.Description, "syntax", (syntax != null) ?
                                                                syntax.Syntax : null
#if SHELL && INTERACTIVE_COMMANDS && XML
                                                                , "help", HelpOps.GetHelp(identifier)
#endif
                                                                , "plugin", (plugin != null) ? plugin.ToString() : null,
                                                                "flags", EntityOps.GetFlagsNoThrow(identifier)
                                                            );
                                                        }
                                                        else
                                                        {
                                                            result = AttributeOps.GetObjectId(identifier);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info identifier name ?kind? ?full?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interactive":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = WindowOps.IsInteractive();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info interactive\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interps":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string pattern = null;

                                            if (arguments.Count >= 3)
                                                pattern = arguments[2];

                                            bool all = false;

                                            if (arguments.Count >= 4)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref all,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool safe = interpreter.InternalIsSafe();

                                                if (all)
                                                {
                                                    StringList list = new StringList();

                                                    list.Add(interpreter.ChildInterpretersToString(pattern, false));

                                                    if (!safe)
                                                        list.Add(GlobalState.InterpretersToString(pattern, false));

                                                    result = list;
                                                }
                                                else if (safe)
                                                {
                                                    result = interpreter.ChildInterpretersToString(pattern, false);
                                                }
                                                else
                                                {
                                                    result = GlobalState.InterpretersToString(pattern, false);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info interps ?pattern? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lastinput":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if NATIVE && WINDOWS
                                            if (PlatformOps.IsWindowsOperatingSystem())
                                            {
                                                code = WindowOps.GetLastInputTickCount(ref result);
                                            }
                                            else
                                            {
                                                result = "not supported on this operating system";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info lastinput\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "level":
                                case "levelid":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            int level = 0;

                                            if (arguments.Count >= 3)
                                            {
                                                code = Value.GetInteger2(
                                                    (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                    interpreter.InternalCultureInfo, ref level, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    bool fullName = false;

                                                    if (arguments.Count >= 4)
                                                    {
                                                        code = Value.GetBoolean2(
                                                            arguments[3], ValueFlags.AnyBoolean,
                                                            interpreter.InternalCultureInfo, ref fullName,
                                                            ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.GetInfoLevelArguments(
                                                            subCommand, arguments[2], level,
                                                            fullName, ref result);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = interpreter.GetInfoLevel(
                                                    subCommand, ref level, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = level;
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?number? ?fullName?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "library":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    if (refresh && !interpreter.InternalIsSafe())
                                                    {
                                                        string path = interpreter.InternalInitializedPath;

                                                        //
                                                        // NOTE: An empty path is considered valid here, please
                                                        //       do not change this to !String.IsNullOrEmpty.
                                                        //
                                                        if (path != null)
                                                        {
                                                            result = path;
                                                        }
                                                        else
                                                        {
                                                            result = "no library has been specified for Tcl"; // COMPAT: Tcl.
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = interpreter.GetVariableValue(
                                                            VariableFlags.GlobalOnly,
                                                            TclVars.Core.Library,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (interpreter.InternalIsSafe())
                                                            {
                                                                result = PathOps.ScrubPath(
                                                                    GlobalState.GetBasePath(), result);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "no library has been specified for Tcl"; // COMPAT: Tcl.
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info library ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "linkedname":
                                    {
                                        //
                                        // NOTE: This is designed to conform with TIP #471 (rev 1.1).
                                        //
                                        if (arguments.Count == 3)
                                        {
                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.None;
                                                IVariable variable = null;

                                                code = interpreter.GetVariableViaResolversWithSplit(
                                                    arguments[2], ref flags, ref variable, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                    {
                                                        string linkIndex = variable.LinkIndex;
                                                        Result linkError = null;

                                                        variable = EntityOps.FollowLinks(
                                                            variable, flags, 1, ref linkError);

                                                        if (variable != null)
                                                        {
                                                            if (interpreter.AreNamespacesEnabled())
                                                            {
                                                                result = FormatOps.VariableName(
                                                                    variable.QualifiedName, linkIndex);
                                                            }
                                                            else
                                                            {
                                                                result = FormatOps.VariableName(
                                                                    NamespaceOps.MakeAbsoluteName(
                                                                    variable.Name), linkIndex);
                                                            }
                                                        }
                                                        else if (linkError != null)
                                                        {
                                                            result = linkError;
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "variable \"{0}\" link is invalid",
                                                                arguments[2]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "variable \"{0}\" isn't a link",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info linkedname varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "loaded":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-nocore", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    string path = (argumentIndex != Index.Invalid) ?
                                                        (string)arguments[argumentIndex] : String.Empty;

                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string pattern = null;

                                                        if ((argumentIndex != Index.Invalid) &&
                                                            ((argumentIndex + 1) < arguments.Count))
                                                        {
                                                            pattern = arguments[argumentIndex + 1];
                                                        }

                                                        PluginFlags notHasFlags = PluginFlags.None;

                                                        if (options.IsPresent("-nocore"))
                                                            notHasFlags |= PluginFlags.System;

                                                        StringList list = null;

                                                        code = childInterpreter.ListPlugins(
                                                            PluginFlags.None, notHasFlags,
                                                            false, false, pattern, false,
                                                            true, false, ref list, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = list;
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
                                                        result = "wrong # args: should be \"info loaded ?options? ?interp? ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info loaded ?options? ?interp? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "locals":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                ICallFrame variableFrame = interpreter.CurrentFrame;

                                                if (interpreter.GetVariableFrameViaResolvers(
                                                        LookupFlags.Default, ref variableFrame,
                                                        ref pattern, ref result) == ReturnCode.Ok)
                                                {
                                                    if (variableFrame != null)
                                                    {
                                                        VariableDictionary variables = variableFrame.Variables;

                                                        if (variables != null)
                                                            result = variables.GetLocals(interpreter, pattern);
                                                        else
                                                            result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info locals ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "modules":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if EMIT && NATIVE && LIBRARY
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.ModulesToString(pattern, false);
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info modules ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nameofexecutable":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = PathOps.GetUnixPath(PathOps.GetExecutableName());
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info nameofexecutable\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "newline":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Environment.NewLine;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info newline\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nprocs":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            StringList list = null;

                                            code = interpreter.ListProcedures(
                                                ProcedureFlags.NamedArguments, ProcedureFlags.Hidden,
                                                true, true, pattern, false, false, false, ref list,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (list != null)
                                                    result = list;
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info nprocs ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "objects":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.ObjectsToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info objects ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "operands":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IFunction function = null;

                                            code = interpreter.GetFunction(
                                                arguments[2], LookupFlags.Default,
                                                ref function, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                result = function.Arguments.ToString();
                                            }
                                            else
                                            {
                                                IOperator @operator = null;

                                                code = interpreter.GetOperator(
                                                    arguments[2], LookupFlags.Default,
                                                    ref @operator, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = @operator.Operands;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info operands name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "operators":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-standard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonstandard", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-hidden", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool standard = false;

                                                    if (options.IsPresent("-standard"))
                                                        standard = true;

                                                    bool nonStandard = false;

                                                    if (options.IsPresent("-nonstandard"))
                                                        nonStandard = true;

                                                    bool hidden = false;

                                                    if (options.IsPresent("-hidden"))
                                                        hidden = true;

                                                    string pattern = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        pattern = arguments[argumentIndex];

                                                    //
                                                    // NOTE: You can never see hidden operators
                                                    //       from a "safe" interpreter; otherwise,
                                                    //       you can only see them if the right
                                                    //       option is used.
                                                    //
                                                    // NOTE: *SECURITY* When checking the "safe"
                                                    //       flag here, use calling interpreter
                                                    //       instead of the target interpreter.
                                                    //
                                                    bool listHidden = hidden && !interpreter.InternalIsSafe();

                                                    //
                                                    // NOTE: Setup the flags used to filter the
                                                    //       list of returned operators.
                                                    //
                                                    OperatorFlags hasFlags = OperatorFlags.None;

                                                    if (standard)
                                                        hasFlags |= OperatorFlags.Standard;

                                                    if (nonStandard)
                                                        hasFlags |= OperatorFlags.NonStandard;

                                                    //
                                                    // NOTE: By default, skip listing hidden operators.
                                                    //
                                                    OperatorFlags notHasFlags = OperatorFlags.Hidden;

                                                    if (listHidden)
                                                        notHasFlags &= ~OperatorFlags.Hidden;

                                                    StringList list = null;

                                                    code = localInterpreter.ListOperators(
                                                        hasFlags, notHasFlags, false, false, pattern,
                                                        false, false, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (list != null)
                                                            result = list;
                                                        else
                                                            result = String.Empty;
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
                                                        result = "wrong # args: should be \"info operators ?options? ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info operators ?options? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "os":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    OperatingSystem operatingSystem =
                                                        PlatformOps.GetOperatingSystem();

                                                    result = (operatingSystem != null) ?
                                                        operatingSystem.ToString() : String.Empty;
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue2(
                                                        VariableFlags.GlobalOnly, TclVars.Platform.Name,
                                                        TclVars.Platform.OsString, ref result, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info os ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "patchlevel":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    result = TclVars.Package.PatchLevelValue;
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue(
                                                        VariableFlags.GlobalOnly,
                                                        TclVars.Package.PatchLevelName,
                                                        ref result, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info patchlevel ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "path":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            InfoPathType infoPathType = InfoPathType.Default;

                                            object enumValue = EnumOps.TryParseFlags(
                                                interpreter, typeof(InfoPathType),
                                                infoPathType.ToString(), arguments[2],
                                                interpreter.InternalCultureInfo, true, true,
                                                true, ref result);

                                            if (enumValue is InfoPathType)
                                            {
                                                infoPathType = (InfoPathType)enumValue;

                                                code = PathOps.GetInfoPath(
                                                    interpreter, infoPathType, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info path type\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pid":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = ProcessOps.GetId();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info pid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "plugin":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IPlugin plugin = null;

                                            code = interpreter.GetPlugin(
                                                arguments[2], LookupFlags.Default,
                                                ref plugin, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = plugin.Options(interpreter, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    PluginFlags flags = plugin.Flags;
                                                    Guid id = AttributeOps.GetObjectId(plugin);

                                                    result = StringList.MakeList(
                                                        "kind", plugin.Kind,
                                                        "id", plugin.Id.Equals(Guid.Empty) ? id : plugin.Id,
                                                        "name", plugin.Name,
                                                        "version", (plugin.Version != null) ? plugin.Version : null,
                                                        "uri", (plugin.Uri != null) ? plugin.Uri : null,
                                                        "description", plugin.Description,
                                                        "assemblyName", plugin.AssemblyName,
                                                        "fileName", plugin.FileName, "flags", flags,
                                                        "options", result, "dateTime",
                                                        FormatOps.Iso8601DateTime(plugin.DateTime, true));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info plugin name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pluginflags":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IPlugin plugin = null;

                                            code = interpreter.GetPlugin(
                                                arguments[2], LookupFlags.Default,
                                                ref plugin, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = plugin.Flags;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info pluginflags name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "policies":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.PoliciesToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info policies ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ppid":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if NATIVE
                                            result = NativeOps.GetParentProcessId().ToInt64();
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info ppid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "previouspid":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool reset = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref reset,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    //
                                                    // NOTE: Return the Id of the previously [exec]'d
                                                    //       process and then optionally reset it.
                                                    //
                                                    long processId = interpreter.PreviousProcessId;

                                                    if (reset)
                                                        interpreter.ResetPreviousProcessId();

                                                    result = processId;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info previouspid ?reset?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "processors":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            //
                                            // NOTE: For now, fake it for safe interpreters.
                                            //
                                            if (interpreter.InternalIsSafe())
                                                result = 1;
                                            else
                                                result = Environment.ProcessorCount;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info processors\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "procs":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            StringList list = null;

                                            code = interpreter.ListProcedures(
                                                ProcedureFlags.None, ProcedureFlags.Hidden |
                                                ProcedureFlags.NamedArguments, true, false,
                                                pattern, false, false, false, ref list,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (list != null)
                                                    result = list;
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info procs ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "programextension":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            Assembly assembly = GlobalState.GetEntryAssembly();

                                            if (assembly != null)
                                                result = Path.GetExtension(assembly.Location);
                                            else
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info programextension\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ptid":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool? native = null;

                                            if (arguments.Count == 3)
                                            {
                                                bool boolValue = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref boolValue,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    native = boolValue;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (native == null)
                                                    result = GlobalState.GetPrimaryThreadId();
                                                else if ((bool)native)
                                                    result = GlobalState.GetPrimaryNativeThreadId();
                                                else
                                                    result = GlobalState.GetPrimaryManagedThreadId();
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info ptid ?native?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "runtime":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    result = CommonOps.Runtime.GetRuntimeName();
                                                }
                                                else
                                                {
                                                    code = interpreter.GetVariableValue2(
                                                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                        Vars.Platform.RuntimeName, ref result,
                                                        ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info runtime ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "runtimeversion":
                                    {
                                        if ((arguments.Count >= 2) || (arguments.Count <= 5))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 3))
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            bool build = false;

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref build,
                                                    ref result);
                                            }

                                            bool extra = false;

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[4], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref extra,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    StringList list = new StringList();
                                                    Version version = CommonOps.Runtime.GetRuntimeVersion();

                                                    list.Add(version != null ? version.ToString() : null);

                                                    if (build)
                                                        list.Add(CommonOps.Runtime.GetRuntimeBuild());

                                                    if (extra)
                                                        list.Add(CommonOps.Runtime.GetRuntimeExtraVersion());

                                                    result = list;
                                                }
                                                else
                                                {
                                                    StringList list = new StringList();

                                                    if (code == ReturnCode.Ok) /* REDUNDANT */
                                                    {
                                                        code = interpreter.GetVariableValue2(
                                                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                            Vars.Platform.RuntimeVersion, ref result,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            list.Add(result);
                                                    }

                                                    if ((code == ReturnCode.Ok) && build)
                                                    {
                                                        code = interpreter.GetVariableValue2(
                                                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                            Vars.Platform.RuntimeBuild, ref result,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            list.Add(result);
                                                    }

                                                    if ((code == ReturnCode.Ok) && extra)
                                                    {
                                                        code = interpreter.GetVariableValue2(
                                                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                                                            Vars.Platform.RuntimeExtraVersion, ref result,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            list.Add(result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info runtimeversion ?refresh? ?build? ?extra?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "script":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                if (interpreter.InternalIsSafe())
                                                {
                                                    result = "permission denied: safe interpreter cannot set script";
                                                    code = ReturnCode.Error;
                                                }
                                                //
                                                // NOTE: We intend to modify the interpreter state,
                                                //       make sure this is not forbidden.
                                                //
                                                else if (interpreter.IsModifiable(false, ref result))
                                                {
                                                    //
                                                    // NOTE: Manual override or reset of the current
                                                    //       script location (i.e. file name).
                                                    //
                                                    if (!String.IsNullOrEmpty(arguments[2]))
                                                    {
                                                        interpreter.ScriptLocation = ScriptLocation.Create(
                                                            interpreter, (string)arguments[2], true);
                                                    }
                                                    else
                                                    {
                                                        interpreter.ScriptLocation = null;
                                                    }
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // BUGBUG: It seems Tcl does not bother scrubbing the
                                                //         value returned by [info script] in a naked
                                                //         "safe" interpreter (i.e. one not using the
                                                //         Tcl Safe Base).
                                                //
                                                string fileName = null;

                                                code = ScriptOps.GetLocation(
                                                    interpreter, true, interpreter.InternalIsSafe(),
                                                    ref fileName, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = fileName;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info script ?fileName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "setup":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if !NET_STANDARD_20
                                            bool verbose = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref verbose,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (verbose)
                                                    code = SetupOps.GetInstances(ref result);
                                                else
                                                    result = SetupOps.GetPath();
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info setup ?verbose?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sharedlibextension":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            if (PlatformOps.IsWindowsOperatingSystem())
                                            {
                                                result = FileExtension.Library;
                                            }
                                            else if (PlatformOps.IsMacintoshOperatingSystem())
                                            {
                                                result = FileExtension.DynamicLibrary;
                                            }
                                            else
                                            {
                                                result = FileExtension.SharedObject;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info sharedlibextension\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shelllibrary":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if SHELL
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    if (refresh && !interpreter.InternalIsSafe())
                                                    {
                                                        string path = interpreter.InternalInitializedShellPath;

                                                        //
                                                        // NOTE: An empty path is considered valid here, please
                                                        //       do not change this to !String.IsNullOrEmpty.
                                                        //
                                                        if (path != null)
                                                        {
                                                            result = path;
                                                        }
                                                        else
                                                        {
                                                            result = "no shell library has been specified for Tcl";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = interpreter.GetVariableValue(
                                                            VariableFlags.GlobalOnly,
                                                            TclVars.Core.ShellLibrary,
                                                            ref result);

                                                        if (code != ReturnCode.Ok)
                                                            result = "no shell library has been specified for Tcl";
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
                                            result = "wrong # args: should be \"info shelllibrary ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "source":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                IProcedure procedure = null;

                                                code = interpreter.GetProcedureViaResolvers(
                                                    ScriptOps.MakeCommandName(arguments[2]),
                                                    LookupFlags.Default, ref procedure,
                                                    ref result);

                                                bool full = false;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref full,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    IScriptLocation location = procedure.Location;

                                                    if (location != null)
                                                    {
                                                        if (full)
                                                            result = location.ToList(interpreter.InternalIsSafe());
                                                        else
                                                            result = location.FileName;
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string fileName = null;
                                                int currentLine = Parser.UnknownLine;

                                                code = ScriptOps.GetLocation(
                                                    interpreter, false, interpreter.InternalIsSafe(), ref fileName,
                                                    ref currentLine, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = StringList.MakeList(fileName, currentLine);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info source ?procName? ?full?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subcommands":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-hidden", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    bool? hidden = null;

                                                    if (options.IsPresent("-hidden", ref value))
                                                        hidden = (bool)value.Value;

                                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        ICommand command = null;

                                                        if (hidden != null)
                                                        {
                                                            if ((bool)hidden)
                                                            {
                                                                code = interpreter.GetHiddenCommand(
                                                                    ScriptOps.MakeCommandName(arguments[argumentIndex]),
                                                                    LookupFlags.Default, ref command, ref result);
                                                            }
                                                            else
                                                            {
                                                                code = interpreter.GetCommand(
                                                                    ScriptOps.MakeCommandName(arguments[argumentIndex]),
                                                                    LookupFlags.Default, ref command, ref result);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.GetCommand(
                                                                ScriptOps.MakeCommandName(arguments[argumentIndex]),
                                                                LookupFlags.Default, ref command);

                                                            if ((code != ReturnCode.Ok) && interpreter.InternalIsSafe())
                                                            {
                                                                code = interpreter.GetHiddenCommand(
                                                                    ScriptOps.MakeCommandName(arguments[argumentIndex]),
                                                                    LookupFlags.Default, ref command);
                                                            }

                                                            if (code != ReturnCode.Ok)
                                                            {
                                                                result = String.Format(
                                                                    "invalid command name {0}",
                                                                    FormatOps.DisplayName(arguments[argumentIndex]));
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            string pattern = null;

                                                            if ((argumentIndex + 1) < arguments.Count)
                                                                pattern = arguments[argumentIndex + 1];

                                                            EnsembleDictionary subCommands = PolicyOps.GetSubCommandsSafe(
                                                                interpreter, command);

                                                            result = (subCommands != null) ?
                                                                subCommands.ToString(pattern, false) :
                                                                String.Empty;
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
                                                        result = "wrong # args: should be \"info subcommands ?options? name ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info subcommands ?options? name ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "syntax":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            StringList list = null;

                                            if (arguments.Count == 3)
                                            {
                                                if (SyntaxOps.GetValues(
                                                        interpreter, arguments[2], ref list,
                                                        ref result))
                                                {
                                                    result = list;
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                if (SyntaxOps.GetNames(
                                                        interpreter, ref list, ref result))
                                                {
                                                    result = list;
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info syntax ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sysvars":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.VariablesToList(
                                                VariableFlags.System, pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info sysvars ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "tclversion":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool refresh = !interpreter.HavePlatformVariables();

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref refresh,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (refresh && !interpreter.InternalIsSafe())
                                                {
                                                    result = TclVars.Package.VersionValue;
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: Here, we return our "emulated" version of Tcl,
                                                    //       namely 8.4.
                                                    //
                                                    code = interpreter.GetVariableValue(
                                                        VariableFlags.GlobalOnly,
                                                        TclVars.Package.VersionName,
                                                        ref result, ref result);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info tclversion ?refresh?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "tid":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool? native = null;

                                            if (arguments.Count == 3)
                                            {
                                                bool boolValue = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref boolValue,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    native = boolValue;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (native == null)
                                                    result = GlobalState.GetCurrentSystemThreadId();
                                                else if ((bool)native)
                                                    result = GlobalState.GetCurrentNativeThreadId();
                                                else
                                                    result = GlobalState.GetCurrentManagedThreadId();
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info tid ?native?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "transactions":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DATA
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.DbTransactionsToString(pattern, false);
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info transactions ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "user":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = PlatformOps.GetUserName(true);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info user\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "varlinks":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.VariablesToList(
                                                VariableFlags.Link, pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info varlinks ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vars":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    string pattern = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        pattern = arguments[argumentIndex];

                                                    if (ScriptOps.HasFlags(localInterpreter,
                                                            InterpreterFlags.InfoVarsMayHaveGlobal, true))
                                                    {
                                                        StringList list;

                                                        lock (localInterpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            ICallFrame frame;

                                                            list = localInterpreter.VariablesToList(
                                                                VariableFlags.None, pattern, false,
                                                                out frame);

                                                            /* NO RESULT */
                                                            localInterpreter.MaybeAddGlobals(
                                                                frame, VariableFlags.None, pattern, false,
                                                                ref list); /* COMPAT: Tcl. */
                                                        }

                                                        result = list;
                                                    }
                                                    else
                                                    {
                                                        result = localInterpreter.VariablesToList(
                                                            VariableFlags.None, pattern, false);
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
                                                        result = "wrong # args: should be \"info vars ?options? ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info vars ?options? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "whitespace":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = new string(Characters.WhiteSpaceChars);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info whitespace\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "windows":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
#if NATIVE && WINDOWS
                                            if (PlatformOps.IsWindowsOperatingSystem())
                                            {
                                                string pattern = null;
                                                bool full = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo,
                                                        ref full, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        pattern = arguments[3];
                                                }
                                                else if (arguments.Count >= 3)
                                                {
                                                    pattern = arguments[2];
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Ok;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    WindowOps.WindowEnumerator windowEnumerator =
                                                        new WindowOps.WindowEnumerator();

                                                    bool returnValue = false;

                                                    code = windowEnumerator.Populate(
                                                        ref returnValue, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (returnValue)
                                                        {
                                                            StringList list = new StringList();
                                                            MatchMode mode = StringOps.DefaultMatchMode;

                                                            foreach (WindowPair pair in windowEnumerator.GetWindows())
                                                            {
                                                                //
                                                                // NOTE: Do we have the class name and/or window text
                                                                //       for this window?
                                                                //
                                                                if (pair.Value != null)
                                                                {
                                                                    //
                                                                    // NOTE: Add the information for this window IF
                                                                    //       the pattern is null -OR- the class name
                                                                    //       matches OR the window text matches.
                                                                    //
                                                                    if ((pattern == null) || StringOps.Match(
                                                                            interpreter, mode, pair.Value.X, pattern, false) ||
                                                                        StringOps.Match(
                                                                            interpreter, mode, pair.Value.Y, pattern, false))
                                                                    {
                                                                        list.Add(StringList.MakeList(
                                                                            full ? pair.Key.ToString() : pair.Key.X.ToString(),
                                                                            pair.Value.X, pair.Value.Y));
                                                                    }
                                                                }
                                                                //
                                                                // NOTE: Otherwise, if pattern is null, add all handles.
                                                                //
                                                                else if (pattern == null)
                                                                {
                                                                    list.Add(
                                                                        full ? pair.Key.ToString() : pair.Key.X.ToString());
                                                                }
                                                            }

                                                            result = list;
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
                                                result = "not supported on this operating system";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info windows ?pattern?\" or \"info windows ?full? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "windowtext":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if NATIVE && WINDOWS
                                            if (PlatformOps.IsWindowsOperatingSystem())
                                            {
                                                IntPtr handle = IntPtr.Zero;

                                                if (IntPtr.Size == sizeof(long))
                                                {
                                                    long value = 0;

                                                    code = Value.GetWideInteger2(
                                                        (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                                        interpreter.InternalCultureInfo, ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        handle = new IntPtr(value);
                                                }
                                                else
                                                {
                                                    int value = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                        interpreter.InternalCultureInfo, ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        handle = new IntPtr(value);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    string text;
                                                    Result error = null;

                                                    text = WindowOps.GetWindowText(handle, ref error);

                                                    if (text != null)
                                                    {
                                                        result = text;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        if (error != null)
                                                            result = error;
                                                        else
                                                            result = "failed to get window text";

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "not supported on this operating system";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"info windowtext handle\"";
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
                        result = "wrong # args: should be \"info option ?arg ...?\"";
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
