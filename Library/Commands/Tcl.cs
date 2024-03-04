/*
 * Tcl.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Private.Tcl;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private.Tcl;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("74ca173d-5378-4eb2-9d7c-4952ce598b33")]
    [CommandFlags(CommandFlags.NativeCode | CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Tcl : Core
    {
        #region Private Data
        private readonly EnsembleDictionary commandSubCommands =
        new EnsembleDictionary(new string[] {
            "create", "delete", "exists", "list"
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Tcl(
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
            "active", "available", "build", "cancel", "canceled",
            "command", "complete", "convert", "create", "delete",
            "errorline", "eval", "exceptions", "exists", "expr",
            "find", "interps", "load", "module",
            "preserve", "primary", "queue", "ready", "recordandeval", "release",
            "resetcancel", "result", "select", "set", "source", "subst",
            "threads", "types", "unload", "unset", "update",
            "versionrange"
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
            ReturnCode code;

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
                                        //
                                        // NOTE: We support the full TIP #335 semantics.  Please refer to
                                        //       "http://tip.tcl.tk/335" for more information.
                                        //
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default,
                                                ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                    result = TclWrapper.GetInterpActive(tclApi, interp);
                                                else
                                                    code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl active interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "build":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                            if (TclApi.CheckModule(tclApi, ref result))
                                            {
                                                TclBuild build = tclApi.Build;

                                                if (build != null)
                                                {
                                                    result = build.ToString();
                                                }
                                                else
                                                {
                                                    result = "no Tcl library build available";
                                                    code = ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl build\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cancel":
                                    {
                                        //
                                        // NOTE: We support the full TIP #285 semantics.  Please refer to
                                        //       "http://tip.tcl.tk/285" for more information.
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            if (interpreter.InternalHasTclInterpreters(ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-time", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-unwind", null),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    IntPtr interp = IntPtr.Zero;
                                                    Result cancelResult = null;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        if ((argumentIndex + 2) >= arguments.Count)
                                                        {
                                                            code = interpreter.GetTclInterpreter(
                                                                arguments[argumentIndex], LookupFlags.Default,
                                                                ref interp, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if ((argumentIndex + 1) < arguments.Count)
                                                                {
                                                                    //
                                                                    // NOTE: The cancel result is just after the interpreter.
                                                                    //
                                                                    cancelResult = arguments[argumentIndex + 1];
                                                                }
                                                            }
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
                                                                result = "wrong # args: should be \"tcl cancel ?options? path ?result?\"";
                                                            }

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Interpreter path is REQUIRED for the Tcl command, since we do
                                                        //       not have the concept of "current interpreter" on the Tcl side.
                                                        //
                                                        result = "wrong # args: should be \"tcl cancel ?options? path ?result?\"";
                                                        code = ReturnCode.Error;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                        if (TclApi.CheckModule(tclApi, ref result))
                                                        {
                                                            bool time = false;

                                                            if (options.IsPresent("-time"))
                                                                time = true;

                                                            bool unwind = false;

                                                            if (options.IsPresent("-unwind"))
                                                                unwind = true;

                                                            IClientData performanceClientData = time ?
                                                                new PerformanceClientData(unwind ?
                                                                    "UnwindTclEvaluate" : "CancelTclEvaluate",
                                                                    false) : null;

                                                            code = TclWrapper.CancelEvaluate(
                                                                tclApi, interp, cancelResult,
                                                                TclWrapper.GetCancelEvaluateFlags(unwind),
                                                                ref performanceClientData, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Empty;
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
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl cancel ?options? path ?result?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "canceled":
                                    {
                                        //
                                        // NOTE: We support the full TIP #285 semantics.  Please refer to
                                        //       "http://tip.tcl.tk/285" for more information.
                                        //
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    //
                                                    // NOTE: This can (and should) legitimately return
                                                    //       errors.
                                                    //
                                                    code = TclWrapper.Canceled(tclApi, interp,
                                                        TclWrapper.GetCanceledFlags(false, true),
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl canceled interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "command":
                                    {
                                        //
                                        // NOTE: Sub-commands are "create", "delete", "exists", and "list".
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            string subSubCommand = arguments[2];

                                            code = ScriptOps.SubCommandFromEnsemble(
                                                interpreter, commandSubCommands, null, true,
                                                false, ref subSubCommand, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                switch (subSubCommand)
                                                {
                                                    case "create":
                                                        {
                                                            if (arguments.Count >= 6)
                                                            {
                                                                OptionDictionary options = new OptionDictionary(
                                                                    new IOption[] {
                                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noforcedelete", null),
                                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                                    Option.CreateEndOfOptions()
                                                                });

                                                                int argumentIndex = Index.Invalid;

                                                                code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, false, ref argumentIndex, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if ((argumentIndex != Index.Invalid) &&
                                                                        ((argumentIndex + 3) == arguments.Count))
                                                                    {
                                                                        bool forceDelete = true;

                                                                        if (options.IsPresent("-noforcedelete"))
                                                                            forceDelete = false;

                                                                        bool noComplain = false;

                                                                        if (options.IsPresent("-nocomplain"))
                                                                            noComplain = true;

                                                                        //
                                                                        // NOTE: Get the IExecute interface.  We do not actually care
                                                                        //       whether this is a procedure or command.
                                                                        //
                                                                        string executeName = arguments[argumentIndex];
                                                                        IExecute execute = null;

                                                                        code = interpreter.InternalGetIExecuteViaResolvers(
                                                                            interpreter.GetResolveEngineFlagsNoLock(true),
                                                                            executeName, null, LookupFlags.Default,
                                                                            ref execute, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            code = interpreter.AddTclBridge(
                                                                                execute, arguments[argumentIndex + 1],
                                                                                arguments[argumentIndex + 2], null,
                                                                                forceDelete, noComplain, ref result);
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
                                                                            result = "wrong # args: should be \"tcl command create ?options? srcCmd interp targetCmd\"";
                                                                        }

                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"tcl command create ?options? srcCmd interp targetCmd\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case "delete":
                                                        {
                                                            //
                                                            // NOTE: Syntax is: "tcl command delete interp targetCmd".
                                                            //
                                                            if (arguments.Count == 5)
                                                            {
                                                                code = interpreter.RemoveTclBridge(arguments[3], arguments[4], null, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"tcl command delete interp targetCmd\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case "exists":
                                                        {
                                                            if (arguments.Count == 5)
                                                            {
                                                                string bridgeName =
                                                                    FormatOps.TclBridgeName(arguments[3], arguments[4]);

                                                                TclBridge tclBridge = null;

                                                                code = interpreter.GetTclBridge(
                                                                    bridgeName, LookupFlags.NoVerbose, ref tclBridge);

                                                                if ((code == ReturnCode.Ok) && (tclBridge != null))
                                                                    result = true;
                                                                else
                                                                    result = false;

                                                                code = ReturnCode.Ok; // NOTE: Force result to Ok.
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"tcl command exists interp targetCmd\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case "list":
                                                        {
                                                            if ((arguments.Count == 3) || (arguments.Count == 4))
                                                            {
                                                                string pattern = null;

                                                                if (arguments.Count == 4)
                                                                    pattern = arguments[3];

                                                                result = interpreter.TclBridgesToString(pattern, false);
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"tcl command list ?pattern?\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    default:
                                                        {
                                                            result = ScriptOps.BadSubCommand(
                                                                interpreter, null, null, subSubCommand,
                                                                commandSubCommands, null, null);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} {2} ?arg ...?\"",
                                                this.Name, subCommand, "arg");

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "complete":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                            if (TclApi.CheckModule(tclApi, ref result))
                                            {
                                                bool complete = false;

                                                code = TclWrapper.IsCommandComplete(
                                                    tclApi, arguments[2], ref complete, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = complete;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl complete command\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "convert":
                                    {
                                        if (arguments.Count == 5)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    code = TclWrapper.ConvertToType(
                                                        tclApi, interp, arguments[3], arguments[4], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl convert interp string type\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-alias", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noinitialize", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-memory", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-safe", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nobridge", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noforcedelete", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    bool alias = false;

                                                    if (options.IsPresent("-alias"))
                                                        alias = true;

                                                    bool initialize = true;

                                                    if (options.IsPresent("-noinitialize"))
                                                        initialize = false;

                                                    bool memory = false;

                                                    if (options.IsPresent("-memory"))
                                                        memory = true;

                                                    bool safe = false;

                                                    if (options.IsPresent("-safe"))
                                                        safe = true;

                                                    bool bridge = true;

                                                    if (options.IsPresent("-nobridge"))
                                                        bridge = false;

                                                    bool forceDelete = true;

                                                    if (options.IsPresent("-noforcedelete"))
                                                        forceDelete = false;

                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

                                                    if (!bridge || interpreter.InternalHasTclBridges(ref result))
                                                    {
                                                        string interpName = null;

                                                        code = interpreter.CreateTclInterpreter(initialize, memory, safe, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Grab the Tcl interpreter name, we will need it below.
                                                            //
                                                            interpName = result;

                                                            //
                                                            // NOTE: Create an alias for the new Tcl interpreter handle?
                                                            //
                                                            if ((code == ReturnCode.Ok) && alias)
                                                            {
                                                                code = interpreter.AddTclAlias(
                                                                    interpName, ObjectOps.GetEvaluateOptions(),
                                                                    ObjectOptionType.Evaluate, ref result);
                                                            }

                                                            //
                                                            // NOTE: Add a bridged eval command to the Tcl interpreter?
                                                            //
                                                            if ((code == ReturnCode.Ok) && bridge)
                                                            {
                                                                code = interpreter.AddStandardTclBridge(
                                                                    interpName, null, null, forceDelete, noComplain,
                                                                    ref result);
                                                            }

                                                            //
                                                            // NOTE: If we succeeded, return interpreter name as the result.
                                                            //
                                                            if (code == ReturnCode.Ok)
                                                                result = interpName;
                                                        }

                                                        //
                                                        // NOTE: If we failed above, cleanup the Tcl interpreter now.
                                                        //
                                                        if ((code != ReturnCode.Ok) && (interpName != null))
                                                        {
                                                            Result deleteResult = null;

                                                            if (interpreter.DeleteTclInterpreter(
                                                                    interpName, ref deleteResult) != ReturnCode.Ok)
                                                            {
                                                                ResultList errors = new ResultList();

                                                                errors.Add(result);
                                                                errors.Add(deleteResult);

                                                                result = errors;
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
                                                    result = "wrong # args: should be \"tcl create ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl create ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "delete":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string interpName = arguments[2];

                                            if ((interpreter.DoesAliasExist(interpName) != ReturnCode.Ok) ||
                                                (interpreter.RemoveAliasAndCommand(interpName, clientData, false, ref result) == ReturnCode.Ok))
                                            {
                                                code = interpreter.DeleteTclInterpreter(interpName, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl delete interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "errorline":
                                    {
                                        //
                                        // NOTE: We support the full TIP #336 semantics.  Please refer to
                                        //       "http://tip.tcl.tk/336" for more information.
                                        //
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    if (arguments.Count == 4)
                                                    {
                                                        int line = 0;

                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            interpreter.InternalCultureInfo, ref line, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = TclWrapper.SetErrorLine(
                                                                tclApi, interp, line, ref result);
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = TclWrapper.GetErrorLine(tclApi, interp);
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl errorline interp ?line?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetEvaluateOptions();

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool time = false;

                                                    if (options.IsPresent("-time"))
                                                        time = true;

                                                    IVariant value = null;
                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    IClientData performanceClientData = time ?
                                                        new PerformanceClientData(
                                                            "EvaluateTclScript", false) : null;

                                                    //
                                                    // NOTE: Perform a concat operation if more than one script
                                                    //       argument was specified.
                                                    //
                                                    if (((argumentIndex + 2) == arguments.Count))
                                                        code = interpreter.EvaluateTclScript(
                                                            arguments[argumentIndex], arguments[argumentIndex + 1],
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            ref performanceClientData, ref result);
                                                    else
                                                        code = interpreter.EvaluateTclScript(
                                                            arguments[argumentIndex],
                                                            ListOps.Concat(arguments, argumentIndex + 1),
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            ref performanceClientData, ref result);
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
                                                        result = "wrong # args: should be \"tcl eval ?options? interp arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl eval ?options? interp arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exceptions":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                            if (TclApi.CheckModule(tclApi, ref result))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    bool exceptions = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref exceptions,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        tclApi.Exceptions = exceptions;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = tclApi.Exceptions;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl exceptions ?exceptions?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.NoVerbose, ref interp);

                                            if ((code == ReturnCode.Ok) && (interp != IntPtr.Zero))
                                                result = true;
                                            else
                                                result = false;

                                            code = ReturnCode.Ok; // NOTE: Force result to Ok.
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl exists interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "expr":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-time", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-exceptions", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool time = false;

                                                    if (options.IsPresent("-time"))
                                                        time = true;

                                                    IVariant value = null;
                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    IClientData performanceClientData = time ?
                                                        new PerformanceClientData(
                                                            "EvaluateTclExpression", false) : null;

                                                    //
                                                    // NOTE: Perform a concat operation if more than one
                                                    //       argument was specified.
                                                    //
                                                    if (((argumentIndex + 2) == arguments.Count))
                                                        code = interpreter.EvaluateTclExpression(
                                                            arguments[argumentIndex],
                                                            arguments[argumentIndex + 1], exceptions,
                                                            ref performanceClientData, ref result);
                                                    else
                                                        code = interpreter.EvaluateTclExpression(
                                                            arguments[argumentIndex],
                                                            ListOps.Concat(arguments, argumentIndex + 1),
                                                            exceptions, ref performanceClientData,
                                                            ref result);
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
                                                        result = "wrong # args: should be \"tcl expr ?options? interp arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl expr ?options? interp arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "available":
                                case "find":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            bool available = SharedStringOps.SystemEquals(subCommand, "available");

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(FindFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(interpreter.TclFindFlags)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-robustify", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-architecture", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-minimumversion", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-maximumversion", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-unknownversion", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-verbose", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-eval", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-full", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-errorsvar", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid,
                                                    true, ref argumentIndex, ref result);
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
                                                    IVariant value = null;
                                                    FindFlags flags = interpreter.TclFindFlags;

                                                    if (options.IsPresent("-flags", ref value))
                                                        flags = (FindFlags)value.Value;

                                                    //
                                                    // HACK: Avoid using any ActiveTcl "BaseKits" here in case they
                                                    //       do not embed Tk correctly.  Also, make sure the Win32
                                                    //       SetDllDirectory API gets called prior to attempting to
                                                    //       load the native Tcl library; otherwise, the inability
                                                    //       of "tcl*.dll" to load "zlib1.dll" could cause issues.
                                                    //
                                                    if (options.IsPresent("-robustify"))
                                                        flags &= ~FindFlags.OtherNamePatternList;

                                                    if (options.IsPresent("-architecture"))
                                                        flags |= FindFlags.FindArchitecture | FindFlags.GetArchitecture;

                                                    if (options.IsPresent("-trusted"))
                                                        flags |= FindFlags.TrustedOnly;

                                                    if (options.IsPresent("-verbose"))
                                                        flags |= FindFlags.VerboseMask;

                                                    string text = null;

                                                    if (options.IsPresent("-eval", ref value))
                                                    {
                                                        text = value.ToString();

                                                        if (String.IsNullOrEmpty(text))
                                                            text = null;

                                                        flags |= FindFlags.EvaluateScript;
                                                    }

                                                    bool full = false;

                                                    if (options.IsPresent("-full"))
                                                        full = true;

                                                    Version minimumVersion = TclWrapper.GetDefaultMinimumVersion(flags);

                                                    if (options.IsPresent("-minimumversion", ref value))
                                                        minimumVersion = (Version)value.Value;

                                                    Version maximumVersion = TclWrapper.GetDefaultMaximumVersion(flags);

                                                    if (options.IsPresent("-maximumversion", ref value))
                                                        maximumVersion = (Version)value.Value;

                                                    Version unknownVersion = TclWrapper.GetDefaultUnknownVersion(flags);

                                                    if (options.IsPresent("-unknownversion", ref value))
                                                        unknownVersion = (Version)value.Value;

                                                    string errorsVarName = null;

                                                    if (options.IsPresent("-errorsvar", ref value))
                                                        errorsVarName = value.ToString();

                                                    string path = null;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        path = arguments[argumentIndex];

                                                        if (!String.IsNullOrEmpty(path))
                                                        {
                                                            path = PathOps.ResolveFullPath(interpreter, path);

                                                            if (path == null)
                                                            {
                                                                result = String.Format(
                                                                    "cannot resolve full path for \"{0}\"",
                                                                    arguments[argumentIndex]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            path = null; /* HACK: Avoid spurious errors. */
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        TclBuildDictionary builds = null;
                                                        ResultList errors = new ResultList(ResultFlags.FullListMask);

                                                        code = TclWrapper.Find(
                                                            interpreter, flags, null,
                                                            (path != null) ? new StringList(path) : null,
                                                            text, minimumVersion, maximumVersion,
                                                            unknownVersion, clientData, ref builds,
                                                            ref errors);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (errorsVarName != null)
                                                            {
                                                                code = interpreter.SetVariableValue(
                                                                    VariableFlags.None, errorsVarName,
                                                                    (Result)errors, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                string pattern = null;

                                                                if ((argumentIndex != Index.Invalid) &&
                                                                    ((argumentIndex + 1) < arguments.Count))
                                                                {
                                                                    pattern = arguments[argumentIndex + 1];
                                                                }

                                                                if (full)
                                                                {
                                                                    StringList list = new StringList();

                                                                    foreach (KeyValuePair<string, TclBuild> pair in builds)
                                                                    {
                                                                        TclBuild build = pair.Value;

                                                                        if (build == null)
                                                                            continue;

                                                                        //
                                                                        // NOTE: *NOCASE* File names are not case-sensitive
                                                                        //       on Windows.
                                                                        //
                                                                        if ((pattern == null) || StringOps.Match(
                                                                                interpreter, StringOps.DefaultMatchMode,
                                                                                pair.Key, pattern, true))
                                                                        {
                                                                            list.Add(build.ToString());
                                                                        }
                                                                    }

                                                                    if (available)
                                                                        result = (list.Count > 0);
                                                                    else
                                                                        result = list;
                                                                }
                                                                else
                                                                {
                                                                    if (available)
                                                                    {
                                                                        if (pattern != null)
                                                                        {
                                                                            StringList list = new StringList();

                                                                            code = GenericOps<string>.FilterList(
                                                                                builds.GetKeysInOrder(false), list,
                                                                                Index.Invalid, Index.Invalid,
                                                                                ToStringFlags.None, pattern, false,
                                                                                ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                                result = (list.Count > 0);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = (builds.Count > 0);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // NOTE: *NOCASE* File names are not case-sensitive
                                                                        //       on Windows.
                                                                        //
                                                                        result = builds.ToString(pattern, true);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else if (available)
                                                        {
                                                            result = false;
                                                            code = ReturnCode.Ok;
                                                        }
                                                        else if (errorsVarName != null)
                                                        {
                                                            code = interpreter.SetVariableValue(
                                                                VariableFlags.None, errorsVarName, (Result)errors,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = "find Tcl library builds failed";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = ListOps.Concat(errors, 0, Environment.NewLine);
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
                                                            "wrong # args: should be \"{0} {1} ?options? ?path? ?pattern?\"",
                                                            this.Name, subCommand);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? ?path? ?pattern?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interps":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.TclSyncRoot)
                                            {
                                                if (interpreter.InternalHasTclInterpreters())
                                                {
                                                    string pattern = null;

                                                    if (arguments.Count == 3)
                                                        pattern = arguments[2];

                                                    IntPtrDictionary interps = null;

                                                    code = interpreter.GetTclInterpreters(
                                                        pattern, LookupFlags.Default, false, false,
                                                        ref interps, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = interps.ToString();
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl interps ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "load":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            if (interpreter.InternalHasTclInterpreters(ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(typeof(FindFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-findflags",
                                                        new Variant(interpreter.TclFindFlags)),
                                                    new Option(typeof(LoadFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-loadflags",
                                                        new Variant(interpreter.TclLoadFlags)),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-robustify", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                    new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-eval", null),
                                                    new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-minimumversion", null),
                                                    new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-maximumversion", null),
                                                    new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-unknownversion", null),
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
                                                        FindFlags findFlags = interpreter.TclFindFlags;

                                                        if (options.IsPresent("-findflags", ref value))
                                                            findFlags = (FindFlags)value.Value;

                                                        LoadFlags loadFlags = interpreter.TclLoadFlags;

                                                        if (options.IsPresent("-loadflags", ref value))
                                                            loadFlags = (LoadFlags)value.Value;

                                                        //
                                                        // HACK: Avoid using any ActiveTcl "BaseKits" here in case they
                                                        //       do not embed Tk correctly.  Also, make sure the Win32
                                                        //       SetDllDirectory API gets called prior to attempting to
                                                        //       load the native Tcl library; otherwise, the inability
                                                        //       of "tcl*.dll" to load "zlib1.dll" could cause issues.
                                                        //
                                                        if (options.IsPresent("-robustify"))
                                                        {
                                                            findFlags &= ~FindFlags.OtherNamePatternList;
                                                            loadFlags |= LoadFlags.SetDllDirectory;
                                                        }

                                                        if (options.IsPresent("-trusted"))
                                                            findFlags |= FindFlags.TrustedOnly;

                                                        string text = null;

                                                        if (options.IsPresent("-eval", ref value))
                                                        {
                                                            text = value.ToString();

                                                            if (String.IsNullOrEmpty(text))
                                                                text = null;

                                                            findFlags |= FindFlags.EvaluateScript;
                                                        }

                                                        Version minimumVersion = TclWrapper.GetDefaultMinimumVersion(findFlags);

                                                        if (options.IsPresent("-minimumversion", ref value))
                                                            minimumVersion = (Version)value.Value;

                                                        Version maximumVersion = TclWrapper.GetDefaultMaximumVersion(findFlags);

                                                        if (options.IsPresent("-maximumversion", ref value))
                                                            maximumVersion = (Version)value.Value;

                                                        Version unknownVersion = TclWrapper.GetDefaultUnknownVersion(findFlags);

                                                        if (options.IsPresent("-unknownversion", ref value))
                                                            unknownVersion = (Version)value.Value;

                                                        string path = null;

                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            //
                                                            // NOTE: Ok, they want to look at a specific file or
                                                            //       directory [only].
                                                            //
                                                            path = arguments[argumentIndex];

                                                            if (!String.IsNullOrEmpty(path))
                                                            {
                                                                path = PathOps.ResolveFullPath(interpreter, path);

                                                                if (path != null)
                                                                {
                                                                    //
                                                                    // NOTE: Prohibit the loader from attempting to
                                                                    //       search anywhere else.
                                                                    //
                                                                    findFlags &= ~FindFlags.LocationMask;
                                                                    findFlags |= FindFlags.SpecificPath;
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "cannot resolve full path for \"{0}\"",
                                                                        arguments[argumentIndex]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                path = null; /* HACK: Avoid spurious errors. */
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = interpreter.LoadTcl(
                                                                findFlags, loadFlags,
                                                                (path != null) ? new StringList(path) : null,
                                                                text, minimumVersion, maximumVersion, unknownVersion,
                                                                clientData, ref result);
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
                                                            result = "wrong # args: should be \"tcl load ?options? ?path?\"";
                                                        }

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
                                            result = "wrong # args: should be \"tcl load ?options? ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "module":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool full = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref full,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    string fileName = tclApi.FileName;

                                                    if (full)
                                                    {
                                                        TclModule module = null;

                                                        if (TclWrapper.TryCopyModule(
                                                                fileName, ref module,
                                                                ref result))
                                                        {
                                                            result = module.ToString();
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = fileName;
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
                                            result = "wrong # args: should be \"tcl module ?full?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "preserve":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    code = TclWrapper.Preserve(tclApi, interp, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl preserve interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "primary":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            string name = null;
                                            IntPtr interp = IntPtr.Zero; /* NOT USED */

                                            code = interpreter.GetAnyTclParentInterpreter(
                                                LookupFlags.Default, ref name, ref interp,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                                result = name;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl primary\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "queue":
                                    {
                                        if (arguments.Count >= 4)
                                        {
#if TCL_THREADS
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(EventType), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-eventtype",
                                                    new Variant(EventType.Evaluate)),
                                                new Option(typeof(EventFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-eventflags",
                                                    new Variant(EventFlags.None)),
                                                new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-data", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-exceptions", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-synchronous", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    EventType eventType = EventType.Evaluate;

                                                    if (options.IsPresent("-eventtype", ref value))
                                                        eventType = (EventType)value.Value;

                                                    EventFlags eventFlags = EventFlags.None;

                                                    if (options.IsPresent("-eventflags", ref value))
                                                        eventFlags = (EventFlags)value.Value;

                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    bool synchronous = false;

                                                    if (options.IsPresent("-synchronous", ref value))
                                                        synchronous = (bool)value.Value;

                                                    object data;

                                                    if (((argumentIndex + 2) == arguments.Count))
                                                    {
                                                        data = new AnyTriplet<Tcl_EvalFlags, bool, string>(
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            arguments[argumentIndex + 1]);
                                                    }
                                                    else
                                                    {
                                                        data = new AnyTriplet<Tcl_EvalFlags, bool, string>(
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            ListOps.Concat(arguments, argumentIndex + 1));
                                                    }

                                                    if (options.IsPresent("-data", ref value))
                                                    {
                                                        IObject @object = (IObject)value.Value;

                                                        if (@object != null)
                                                        {
                                                            data = @object.Value;
                                                        }
                                                        else
                                                        {
                                                            result = "option value has invalid data";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.QueueTclThreadEvent(
                                                            arguments[argumentIndex], eventType,
                                                            eventFlags, data, synchronous,
                                                            ref result);
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
                                                        result = "wrong # args: should be \"tcl queue ?options? interp arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl queue ?options? interp arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ready":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                //
                                                // NOTE: Perform a full check of the Tcl API object
                                                //       and the specified Tcl interpreter, including
                                                //       the thread affinity.
                                                //
                                                result = interpreter.IsTclInterpreterReady(arguments[2]);
                                            }
                                            else
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                //
                                                // NOTE: Just see if the Tcl API object is Ok (no Tcl
                                                //       interpreter name was supplied).
                                                //
                                                if (TclApi.CheckModule(tclApi))
                                                    result = true;
                                                else
                                                    result = false;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl ready ?interp?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "recordandeval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-time", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-exceptions", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool time = false;

                                                    if (options.IsPresent("-time"))
                                                        time = true;

                                                    IVariant value = null;
                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    IClientData performanceClientData = time ?
                                                        new PerformanceClientData(
                                                            "RecordAndEvaluateTclScript", false) : null;

                                                    //
                                                    // NOTE: Perform a concat operation if more than one script
                                                    //       argument was specified.
                                                    //
                                                    if (((argumentIndex + 2) == arguments.Count))
                                                        code = interpreter.RecordAndEvaluateTclScript(
                                                            arguments[argumentIndex], arguments[argumentIndex + 1],
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            ref performanceClientData, ref result);
                                                    else
                                                        code = interpreter.RecordAndEvaluateTclScript(
                                                            arguments[argumentIndex],
                                                            ListOps.Concat(arguments, argumentIndex + 1),
                                                            Tcl_EvalFlags.TCL_EVAL_NONE, exceptions,
                                                            ref performanceClientData, ref result);
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
                                                        result = "wrong # args: should be \"tcl recordandeval ?options? interp arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl recordandeval ?options? interp arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "release":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    code = TclWrapper.Release(tclApi, interp, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl release interp\"";
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
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-children", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                    if (TclApi.CheckModule(tclApi, ref result))
                                                    {
                                                        bool children = false;

                                                        if (options.IsPresent("-children"))
                                                            children = true;

                                                        bool force = false;

                                                        if (options.IsPresent("-force"))
                                                            force = true;

                                                        IntPtr interp = IntPtr.Zero;

                                                        code = interpreter.GetTclInterpreter(
                                                            arguments[argumentIndex], LookupFlags.Default, ref interp, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            code = TclWrapper.ResetCancellation(tclApi, interp, force, ref result);

                                                        if ((code == ReturnCode.Ok) && children)
                                                            code = TclWrapper.SetInterpCancelFlags(tclApi, interp,
                                                                Tcl_EvalFlags.TCL_EVAL_NONE, force, ref result);

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
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"tcl resetcancel ?options? interp\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl resetcancel ?options? interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "result":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    result = StringList.MakeList(
                                                        ReturnCode.Invalid, TclWrapper.GetResultAsString(
                                                        tclApi, interp));

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
                                            result = "wrong # args: should be \"tcl result interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "select":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(FindFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(interpreter.TclFindFlags)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-robustify", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-architecture", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-verbose", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-eval", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-allerrors", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-minimumversion", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-maximumversion", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-unknownversion", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-errorsvar", null),
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
                                                    FindFlags flags = interpreter.TclFindFlags;

                                                    if (options.IsPresent("-flags", ref value))
                                                        flags = (FindFlags)value.Value;

                                                    //
                                                    // HACK: Avoid using any ActiveTcl "BaseKits" here in case they
                                                    //       do not embed Tk correctly.  Also, make sure the Win32
                                                    //       SetDllDirectory API gets called prior to attempting to
                                                    //       load the native Tcl library; otherwise, the inability
                                                    //       of "tcl*.dll" to load "zlib1.dll" could cause issues.
                                                    //
                                                    if (options.IsPresent("-robustify"))
                                                        flags &= ~FindFlags.OtherNamePatternList;

                                                    if (options.IsPresent("-architecture"))
                                                        flags |= FindFlags.Architecture;

                                                    if (options.IsPresent("-trusted"))
                                                        flags |= FindFlags.TrustedOnly;

                                                    if (options.IsPresent("-verbose"))
                                                        flags |= FindFlags.VerboseMask;

                                                    string text = null;

                                                    if (options.IsPresent("-eval"))
                                                    {
                                                        text = value.ToString();

                                                        if (String.IsNullOrEmpty(text))
                                                            text = null;

                                                        flags |= FindFlags.EvaluateScript;
                                                    }

                                                    Version minimumVersion = TclWrapper.GetDefaultMinimumVersion(flags);

                                                    if (options.IsPresent("-minimumversion", ref value))
                                                        minimumVersion = (Version)value.Value;

                                                    Version maximumVersion = TclWrapper.GetDefaultMaximumVersion(flags);

                                                    if (options.IsPresent("-maximumversion", ref value))
                                                        maximumVersion = (Version)value.Value;

                                                    Version unknownVersion = TclWrapper.GetDefaultUnknownVersion(flags);

                                                    if (options.IsPresent("-unknownversion", ref value))
                                                        unknownVersion = (Version)value.Value;

                                                    string errorsVarName = null;

                                                    if (options.IsPresent("-errorsvar", ref value))
                                                        errorsVarName = value.ToString();

                                                    bool allErrors = false;

                                                    if (options.IsPresent("-allerrors"))
                                                        allErrors = true;

                                                    string path = null;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Ok, they want to look at a specific file or
                                                        //       directory [only].
                                                        //
                                                        path = arguments[argumentIndex];

                                                        if (!String.IsNullOrEmpty(path))
                                                        {
                                                            path = PathOps.ResolveFullPath(interpreter, path);

                                                            if (path != null)
                                                            {
                                                                //
                                                                // NOTE: Allow the Tcl detection logic to use
                                                                //       the specific file or directory.
                                                                //
                                                                flags |= FindFlags.SpecificPath;
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "cannot resolve full path for \"{0}\"",
                                                                    arguments[argumentIndex]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            path = null; /* HACK: Avoid spurious errors. */
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        TclBuildDictionary builds = null;
                                                        ResultList errors; /* REUSED */

                                                        errors = new ResultList(ResultFlags.FullListMask);

                                                        code = TclWrapper.Find(
                                                            interpreter, flags, null,
                                                            (path != null) ? new StringList(path) : null,
                                                            text, minimumVersion, maximumVersion,
                                                            unknownVersion, clientData, ref builds,
                                                            ref errors);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            TclBuild build = null;

                                                            if (!allErrors)
                                                                errors = new ResultList(ResultFlags.FullListMask);

                                                            code = TclWrapper.Select(interpreter,
                                                                flags, builds, minimumVersion, maximumVersion,
                                                                ref build, ref errors);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (errorsVarName != null)
                                                                {
                                                                    code = interpreter.SetVariableValue(
                                                                        VariableFlags.None, errorsVarName,
                                                                        (Result)errors, ref result);
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    result = build.ToString();
                                                            }
                                                            else if (errorsVarName != null)
                                                            {
                                                                code = interpreter.SetVariableValue(
                                                                    VariableFlags.None, errorsVarName, (Result)errors,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    result = "select Tcl library build failed";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = ListOps.Concat(errors, 0, Environment.NewLine);
                                                            }
                                                        }
                                                        else if (errorsVarName != null)
                                                        {
                                                            code = interpreter.SetVariableValue(
                                                                VariableFlags.None, errorsVarName, (Result)errors,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = "find Tcl library builds failed";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = ListOps.Concat(errors, 0, Environment.NewLine);
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
                                                        result = "wrong # args: should be \"tcl select ?options? ?path?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl select ?options? ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    if (arguments.Count == 4)
                                                    {
                                                        code = TclWrapper.GetVariable(
                                                            tclApi, interp, Tcl_VarFlags.TCL_LEAVE_ERR_MSG,
                                                            arguments[3], ref result, ref result);
                                                    }
                                                    else if (arguments.Count == 5)
                                                    {
                                                        Result value = arguments[4];

                                                        code = TclWrapper.SetVariable(
                                                            tclApi, interp, Tcl_VarFlags.TCL_LEAVE_ERR_MSG,
                                                            arguments[3], ref value, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = value;
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
                                            result = "wrong # args: should be \"tcl set interp varName ?newValue?\"";
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
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-time", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-exceptions", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    bool time = false;

                                                    if (options.IsPresent("-time"))
                                                        time = true;

                                                    IVariant value = null;
                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    IClientData performanceClientData = time ?
                                                        new PerformanceClientData(
                                                            "EvaluateTclFile", false) : null;

                                                    code = interpreter.EvaluateTclFile(
                                                        arguments[argumentIndex], arguments[argumentIndex + 1],
                                                        exceptions, ref performanceClientData, ref result);
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
                                                        result = "wrong # args: should be \"tcl source ?options? interp fileName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl source ?options? interp fileName\"";
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
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-time", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-exceptions", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Tcl_SubstFlags flags = Tcl_SubstFlags.TCL_SUBST_ALL;

                                                    if (options.IsPresent("-nobackslashes"))
                                                        flags &= ~Tcl_SubstFlags.TCL_SUBST_BACKSLASHES;

                                                    if (options.IsPresent("-nocommands"))
                                                        flags &= ~Tcl_SubstFlags.TCL_SUBST_COMMANDS;

                                                    if (options.IsPresent("-novariables"))
                                                        flags &= ~Tcl_SubstFlags.TCL_SUBST_VARIABLES;

                                                    bool time = false;

                                                    if (options.IsPresent("-time"))
                                                        time = true;

                                                    IVariant value = null;
                                                    bool exceptions = interpreter.TclExceptions;

                                                    if (options.IsPresent("-exceptions", ref value))
                                                        exceptions = (bool)value.Value;

                                                    IClientData performanceClientData = time ?
                                                        new PerformanceClientData(
                                                            "SubstituteTclString", false) : null;

                                                    code = interpreter.SubstituteTclString(
                                                        arguments[argumentIndex], arguments[argumentIndex + 1],
                                                        flags, exceptions, ref performanceClientData, ref result);
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
                                                        result = "wrong # args: should be \"tcl subst ?options? interp string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl subst ?options? interp string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "threads":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if TCL_THREADS
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            result = interpreter.TclThreadsToString(pattern, false);
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl threads ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "types":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    code = TclWrapper.GetAllObjectTypes(
                                                        tclApi, interp, ref result);
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl types interp\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unload":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.UnloadTcl(
                                                interpreter.TclCommandUnloadFlags, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl unload\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            IntPtr interp = IntPtr.Zero;

                                            code = interpreter.GetTclInterpreter(
                                                arguments[2], LookupFlags.Default, ref interp, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                if (TclApi.CheckModule(tclApi, ref result))
                                                {
                                                    code = TclWrapper.UnsetVariable(
                                                        tclApi, interp, Tcl_VarFlags.TCL_LEAVE_ERR_MSG,
                                                        arguments[3], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl unset interp varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "update":
                                    {
                                        OptionDictionary options = new OptionDictionary(
                                            new IOption[] {
                                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-timeout", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-wait", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                            Option.CreateEndOfOptions()
                                        });

                                        int argumentIndex = Index.Invalid;

                                        if (arguments.Count > 2)
                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                        else
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            if (argumentIndex == Index.Invalid)
                                            {
                                                IVariant value = null;
                                                int timeout = _Timeout.Infinite;

                                                if (options.IsPresent("-timeout", ref value))
                                                    timeout = (int)value.Value;

                                                bool wait = false;

                                                if (options.IsPresent("-wait"))
                                                    wait = true;

                                                bool all = false;

                                                if (options.IsPresent("-all"))
                                                    all = true;

                                                bool noComplain = false;

                                                if (options.IsPresent("-nocomplain"))
                                                    noComplain = true;

                                                int eventCount = 0;
                                                int sleepCount = 0;
                                                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                                                code = TclWrapper.DoOneEvent(
                                                    interpreter, timeout, wait, all,
                                                    noComplain, ref eventCount,
                                                    ref sleepCount, ref tclApi,
                                                    ref result);

                                                TclApi.SetTclApi(interpreter, tclApi);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = StringList.MakeList(
                                                        "eventCount", eventCount,
                                                        "sleepCount", sleepCount);
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"tcl update ?options?\"";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        break;
                                    }
                                case "versionrange":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(FindFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(interpreter.TclFindFlags)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-robustify", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-minimumversion", null),
                                                new Option(null, OptionFlags.MustHaveVersionValue, Index.Invalid, Index.Invalid, "-maximumversion", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-majorincrement", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-minorincrement", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-intermediateminimum", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-intermediatemaximum", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IVariant value = null;
                                                    FindFlags flags = interpreter.TclFindFlags;

                                                    if (options.IsPresent("-flags", ref value))
                                                        flags = (FindFlags)value.Value;

                                                    //
                                                    // HACK: Avoid using any ActiveTcl "BaseKits" here in case they
                                                    //       do not embed Tk correctly.  Also, make sure the Win32
                                                    //       SetDllDirectory API gets called prior to attempting to
                                                    //       load the native Tcl library; otherwise, the inability
                                                    //       of "tcl*.dll" to load "zlib1.dll" could cause issues.
                                                    //
                                                    if (options.IsPresent("-robustify"))
                                                        flags &= ~FindFlags.OtherNamePatternList;

                                                    if (options.IsPresent("-trusted"))
                                                        flags |= FindFlags.TrustedOnly;

                                                    Version minimumVersion = TclWrapper.GetDefaultMinimumVersion(flags);

                                                    if (options.IsPresent("-minimumversion", ref value))
                                                        minimumVersion = (Version)value.Value;

                                                    Version maximumVersion = TclWrapper.GetDefaultMaximumVersion(flags);

                                                    if (options.IsPresent("-maximumversion", ref value))
                                                        maximumVersion = (Version)value.Value;

                                                    int? majorIncrement = TclWrapper.GetDefaultMajorIncrement(flags);

                                                    if (options.IsPresent("-majorincrement", ref value))
                                                        majorIncrement = (int)value.Value;

                                                    int? minorIncrement = TclWrapper.GetDefaultMinorIncrement(flags);

                                                    if (options.IsPresent("-minorincrement", ref value))
                                                        minorIncrement = (int)value.Value;

                                                    int? intermediateMinimum = TclWrapper.GetDefaultIntermediateMinimum(flags);

                                                    if (options.IsPresent("-intermediateminimum", ref value))
                                                        intermediateMinimum = (int)value.Value;

                                                    int? intermediateMaximum = TclWrapper.GetDefaultIntermediateMaximum(flags);

                                                    if (options.IsPresent("-intermediatemaximum", ref value))
                                                        intermediateMaximum = (int)value.Value;

                                                    code = TclWrapper.GetVersionRange(
                                                        flags, minimumVersion, maximumVersion, majorIncrement,
                                                        minorIncrement, intermediateMinimum, intermediateMaximum,
                                                        ref result);
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"tcl versionrange ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"tcl versionrange ?options?\"";
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
                        result = "wrong # args: should be \"tcl arg ?arg ...?\"";
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
