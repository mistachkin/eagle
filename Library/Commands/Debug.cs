/*
 * Debug.cs --
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
using System.Diagnostics;
using System.Runtime;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

#if DEBUGGER
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

using _Private = Eagle._Components.Private;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("8d2559ac-e4e4-41c4-8183-52c90008d25f")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("debug")]
    internal sealed class Debug : Core
    {
        public Debug(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "break", "breakpoints", "cacheconfiguration", "callback",
            "cleanup", "collect", "complaint", "emergency", "enable", "eval",
            "exception", "execute", "function", "gcmemory", "halt",
            "history", "icommand", "interactive", "invoke", "iqueue",
            "iresult", "keyring", "levels", "lockloop", "lockvar",
            "log", "memory", "oncancel", "onerror", "onexecute",
            "onexit", "onreturn", "ontest", "ontoken", "operator",
            "output", "paths", "pluginexecute", "pluginflags", "purge",
            "procedureflags", "resume", "restore", "readonly", "ready",
            "refreshautopath", "run", "runtimeoption", "runtimeoverride",
            "secureeval", "self", "setup", "shell", "stack", "status",
            "step", "steps", "subst", "suspend", "sysmemory", "test",
            "testpath", "token", "trace", "types", "undelete",
            "variable", "vout", "watch"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

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
                ArgumentList newArguments = arguments;

            redo:

                if (newArguments != null)
                {
                    if (newArguments.Count >= 2)
                    {
                        string subCommand = newArguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, newArguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            //
                            // NOTE: Programmatically interact with the debugger
                            //       (breakpoint, watch, eval, etc).
                            //
                            switch (subCommand)
                            {
                                case "break":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-ignoreenabled", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-complain", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-nocomplain", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-noerror", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (newArguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, newArguments, 0, 2, Index.Invalid, false,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool noComplain = ScriptOps.HasFlags(
                                                        interpreter, InterpreterFlags.DebugBreakNoComplain,
                                                        true);

                                                    if (options.IsPresent("-complain"))
                                                        noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

#if DEBUGGER
                                                    bool ignoreEnabled = false;

                                                    if (options.IsPresent("-ignoreenabled"))
                                                        ignoreEnabled = true;

                                                    bool noError = false;

                                                    if (options.IsPresent("-noerror"))
                                                        noError = true;
#endif

#if PREVIOUS_RESULT
                                                    //
                                                    // NOTE: At this point, the result of the previous
                                                    //       command may still be untouched and will
                                                    //       be displayed verbatim upon entry into the
                                                    //       interactive loop, if necessary.
                                                    //
                                                    Result previousResult = Result.Copy(
                                                        Interpreter.GetPreviousResult(localInterpreter),
                                                        ResultFlags.CopyObject); /* COPY */
#endif

#if DEBUGGER
                                                    IDebugger debugger = null;
                                                    bool enabled = false;
                                                    HeaderFlags headerFlags = HeaderFlags.None;
                                                    Result error = null;

                                                    if (Engine.CheckDebugger(
                                                            localInterpreter, ignoreEnabled, ref debugger,
                                                            ref enabled, ref headerFlags, ref error))
                                                    {
                                                        //
                                                        // NOTE: If the debugger is disabled, skip breaking
                                                        //       into the nested interactive loop.
                                                        //
                                                        if (enabled && FlagOps.HasFlags(
                                                                debugger.Types, BreakpointType.Demand, true))
                                                        {
#if PREVIOUS_RESULT
                                                            result = previousResult;
#endif

                                                            //
                                                            // NOTE: This will break into the debugger by
                                                            //       starting a nested interactive loop.
                                                            //
                                                            // HACK: Always use the original arguments.
                                                            //
                                                            code = localInterpreter.DebuggerBreak(
                                                                debugger, new InteractiveLoopData(
                                                                (result != null) ?
                                                                    result.ReturnCode : ReturnCode.Ok,
                                                                BreakpointType.Demand, this.Name,
                                                                headerFlags | HeaderFlags.Breakpoint,
                                                                clientData, arguments), ref result);

                                                            //
                                                            // FIXME: If there were no other failures in
                                                            //        the interactive loop, perhaps we
                                                            //        should reflect the previous result?
                                                            //        Better logic here may be needed.
                                                            //
                                                            if ((code == ReturnCode.Ok) && (result != null))
                                                                code = result.ReturnCode;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: Return the previous return code, if any.
                                                            //
                                                            code = (result != null) ?
                                                                result.ReturnCode : ReturnCode.Ok;
                                                        }

                                                        //
                                                        // HACK: When the "no error" option is set, this
                                                        //       sub-command is never allowed to fail.
                                                        //
                                                        if (noError && (code == ReturnCode.Error))
                                                        {
#if PREVIOUS_RESULT
                                                            result = previousResult;
#endif

                                                            code = ReturnCode.Ok;
                                                        }
                                                    }
                                                    else if (noComplain)
                                                    {
#if PREVIOUS_RESULT
                                                        result = previousResult;
#endif

                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = error;
                                                        code = ReturnCode.Error;
                                                    }
#else
                                                    if (noComplain)
                                                    {
#if PREVIOUS_RESULT
                                                        result = previousResult;
#endif

                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = "not implemented";
                                                        code = ReturnCode.Error;
                                                    }
#endif
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug break ?options?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug break ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "breakpoints":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                string pattern = null;

                                                if (newArguments.Count == 3)
                                                    pattern = newArguments[2];

                                                IStringList list = null;

                                                code = debugger.GetBreakpointList(
                                                    interpreter, pattern, false, ref list,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = list.ToString(); /* EXEMPT */
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug breakpoints ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cacheconfiguration":
                                    {
                                        if ((newArguments.Count >= 2) && (newArguments.Count <= 4))
                                        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                                            if (newArguments.Count >= 3)
                                            {
                                                string text = newArguments[2];

                                                if (String.IsNullOrEmpty(text))
                                                    text = null;

                                                int level = Level.Invalid;

                                                if (newArguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)newArguments[3], ValueFlags.AnyInteger,
                                                        interpreter.InternalCultureInfo, ref level,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    /* IGNORED */
                                                    interpreter.InitializeAndPreSetupCaches(
                                                        text, level, true);
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                result = CacheConfiguration.GetStateAndOrSettings(
                                                    CacheInformationFlags.Debug);
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug cacheconfiguration ?settings? ?level?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "callback":
                                    {
                                        //
                                        // debug callback ?{}|arg ...?
                                        //
                                        if (newArguments.Count >= 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (newArguments.Count >= 3)
                                                {
                                                    //
                                                    // NOTE: If there is only one argument and it's empty,
                                                    //       null out the callback arguments; otherwise,
                                                    //       use them verbatim.
                                                    //
                                                    ArgumentList list = ArgumentList.NullIfEmpty(
                                                        newArguments, 2);

                                                    debugger.CallbackArguments =
                                                        (list != null) ? new StringList(list) : null;

                                                    debugger.CheckCallbacks(interpreter);

                                                    result = String.Empty;
                                                }
                                                else if (newArguments.Count == 2)
                                                {
                                                    result = debugger.CallbackArguments;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug callback ?{}|arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cleanup":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                                            try
                                            {
                                                CacheFlags cacheFlags = CacheFlags.Default;

                                                if (newArguments.Count == 3)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        interpreter, typeof(CacheFlags),
                                                        cacheFlags.ToString(), newArguments[2],
                                                        interpreter.InternalCultureInfo, true, true,
                                                        true, ref result);

                                                    if (enumValue is CacheFlags)
                                                        cacheFlags = (CacheFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    ICallFrame variableFrame = interpreter.CurrentFrame;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.GetVariableFrameViaResolvers(
                                                            LookupFlags.Default, ref variableFrame,
                                                            ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(CallFrameOps.Cleanup(
                                                            interpreter.CurrentFrame, variableFrame, false),
                                                            interpreter.ClearCaches(cacheFlags, true),
                                                            GC.GetTotalMemory(true));
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug cleanup ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "collect":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            GarbageFlags flags = GarbageFlags.ForCommand;

                                            if (newArguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(GarbageFlags),
                                                    flags.ToString(), newArguments[2],
                                                    interpreter.InternalCultureInfo, true, true,
                                                    true, ref result);

                                                if (enumValue is GarbageFlags)
                                                    flags = (GarbageFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                /* NO RESULT */
                                                ObjectOps.CollectGarbage(flags); /* throw */

                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug collect ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "complaint":
                                    {
                                        if (newArguments.Count == 2)
                                        {
                                            result = DebugOps.SafeGetComplaint(interpreter);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug complaint\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "emergency":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                    new Option(null, OptionFlags.MustHaveInterpreterValue,
                                                        Index.Invalid, Index.Invalid, "-interpreter", null),
                                                    new Option(null, OptionFlags.None,
                                                        Index.Invalid, Index.Invalid, "-ignoreenabled", null),
                                                    new Option(null, OptionFlags.None,
                                                        Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                    new Option(null, OptionFlags.None,
                                                        Index.Invalid, Index.Invalid, "-noerror", null),
                                                    Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (newArguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, newArguments, 0, 2, Index.Invalid, true,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == newArguments.Count))
                                                {
                                                    Variant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

#if DEBUGGER
                                                    bool ignoreEnabled = false;

                                                    if (options.IsPresent("-ignoreenabled"))
                                                        ignoreEnabled = true;

                                                    bool noError = false;

                                                    if (options.IsPresent("-noerror"))
                                                        noError = true;
#endif

                                                    DebugEmergencyLevel emergencyLevel =
                                                        DebugEmergencyLevel.Default;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        object enumValue = EnumOps.TryParseFlags(
                                                            localInterpreter, typeof(DebugEmergencyLevel),
                                                            emergencyLevel.ToString(), newArguments[argumentIndex],
                                                            localInterpreter.InternalCultureInfo, true,
                                                            false, true, ref result);

                                                        if (enumValue is DebugEmergencyLevel)
                                                            emergencyLevel = (DebugEmergencyLevel)enumValue;
                                                        else
                                                            code = ReturnCode.Error;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        bool locked = false;

                                                        try
                                                        {
                                                            localInterpreter.InternalHardTryLock(
                                                                ref locked); /* TRANSACTIONAL */

                                                            if (locked)
                                                            {
                                                                StringBuilder builder = StringOps.NewStringBuilder();
                                                                bool? enabled = null;

                                                                if (FlagOps.HasFlags(
                                                                        emergencyLevel, DebugEmergencyLevel.Enabled,
                                                                        true))
                                                                {
                                                                    enabled = true;
                                                                }
                                                                else if (FlagOps.HasFlags(
                                                                        emergencyLevel, DebugEmergencyLevel.Disabled,
                                                                        true))
                                                                {
                                                                    enabled = false;
                                                                }

                                                                bool @break = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Break,
                                                                    true);

                                                                bool tokens = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Tokens,
                                                                    true);

                                                                bool scriptArguments = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.ScriptArguments,
                                                                    true);

                                                                bool verbose = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Verbose,
                                                                    true);

#if DEBUGGER
                                                                IDebugger debugger = null;

                                                                bool created = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Created,
                                                                    true);

                                                                bool ignoreModifiable = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.IgnoreModifiable,
                                                                    true);

                                                                bool isolated = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Isolated,
                                                                    true);

                                                                bool disposed = FlagOps.HasFlags(
                                                                    emergencyLevel, DebugEmergencyLevel.Disposed,
                                                                    true);

                                                                bool wasModified = false;

                                                                if ((enabled != null) && (created || disposed))
                                                                {
                                                                    if (Engine.SetupDebugger( /* per-thread */
                                                                            localInterpreter,
                                                                            localInterpreter.CultureName,
                                                                            localInterpreter.CreateFlags,
                                                                            localInterpreter.HostCreateFlags,
                                                                            localInterpreter.InitializeFlags,
                                                                            localInterpreter.ScriptFlags,
                                                                            localInterpreter.InterpreterFlags,
                                                                            localInterpreter.PluginFlags,
                                                                            localInterpreter.GetAppDomain(),
                                                                            localInterpreter.Host,
                                                                            DebuggerOps.GetLibraryPath(
                                                                                localInterpreter),
                                                                            DebuggerOps.GetAutoPathList(
                                                                                localInterpreter), ignoreModifiable,
                                                                            (bool)enabled, isolated, ref debugger,
                                                                            ref wasModified, ref result))
                                                                    {
                                                                        if (wasModified)
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "debugger {0}", (bool)enabled ?
                                                                                "created" : "disposed"));
                                                                        }
                                                                        else if (verbose)
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "debugger {0}", (bool)enabled ?
                                                                                "setup" : "unsetup"));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    debugger = localInterpreter.Debugger; /* per-thread */

                                                                    if (verbose || (enabled == null))
                                                                    {
                                                                        builder.AppendLine(String.Format(
                                                                            "debugger is {0}", (debugger != null) ?
                                                                            "valid" : "invalid"));
                                                                    }
                                                                }
#else
                                                                builder.AppendLine("debugger is not available");
#endif

                                                                if (code == ReturnCode.Ok)
                                                                {
#if DEBUGGER
                                                                    if (debugger != null)
                                                                    {
                                                                        BreakpointType wasTypes = debugger.Types; /* per-thread */
                                                                        BreakpointType types = _Private.Debugger.GetDefaultTypes(tokens);

                                                                        if (!DebuggerOps.MatchBreakpointTypes(
                                                                                wasTypes, types, enabled, verbose, builder))
                                                                        {
                                                                            if (enabled != null)
                                                                            {
                                                                                if ((bool)enabled)
                                                                                    debugger.Types |= types; /* per-thread */
                                                                                else if (types != BreakpointType.None)
                                                                                    debugger.Types &= ~types; /* per-thread */
                                                                                else
                                                                                    debugger.Types = types; /* per-thread */

                                                                                builder.AppendLine(String.Format(
                                                                                    "debugger types {0}", (bool)enabled ?
                                                                                    "added" : "removed"));
                                                                            }
                                                                        }

                                                                        bool wasEnabled = debugger.Enabled; /* per-thread */

                                                                        if (enabled != null)
                                                                        {
                                                                            if ((bool)enabled != wasEnabled)
                                                                            {
                                                                                debugger.Enabled = (bool)enabled; /* per-thread */

                                                                                builder.AppendLine(String.Format(
                                                                                    "debugger {0}", (bool)enabled ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                            else if (verbose)
                                                                            {
                                                                                builder.AppendLine(String.Format(
                                                                                    "debugger is {0}", wasEnabled ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "debugger is {0}", wasEnabled ?
                                                                                "enabled" : "disabled"));
                                                                        }

#if DEBUGGER_BREAKPOINTS
                                                                        if (tokens)
                                                                        {
                                                                            bool wasBreakOnToken = debugger.BreakOnToken;

                                                                            if (enabled != null)
                                                                            {
                                                                                if ((bool)enabled != wasBreakOnToken)
                                                                                {
                                                                                    debugger.BreakOnToken = (bool)enabled; /* per-thread */

                                                                                    builder.AppendLine(String.Format(
                                                                                        "debugger break-on-token {0}",
                                                                                        (bool)enabled ? "enabled" :
                                                                                        "disabled"));
                                                                                }
                                                                                else if (verbose)
                                                                                {
                                                                                    builder.AppendLine(String.Format(
                                                                                        "debugger break-on-token is {0}",
                                                                                        wasBreakOnToken ? "enabled" :
                                                                                        "disabled"));
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                builder.AppendLine(String.Format(
                                                                                    "debugger break-on-token is {0}",
                                                                                    wasBreakOnToken ? "enabled" :
                                                                                    "disabled"));
                                                                            }
                                                                        }
#else
                                                                        builder.AppendLine(
                                                                            "debugger break-on-token is not available");
#endif
                                                                    }
#endif

                                                                    bool wasInteractive = localInterpreter.InternalInteractive; /* per-thread */

                                                                    if (enabled != null)
                                                                    {
                                                                        if ((bool)enabled != wasInteractive)
                                                                        {
                                                                            localInterpreter.InternalInteractive = (bool)enabled; /* per-thread */

                                                                            builder.AppendLine(String.Format(
                                                                                "interactive mode {0}", (bool)enabled ?
                                                                                "enabled" : "disabled"));
                                                                        }
                                                                        else if (verbose)
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "interactive mode is {0}", wasInteractive ?
                                                                                "enabled" : "disabled"));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        builder.AppendLine(String.Format(
                                                                            "interactive mode is {0}", wasInteractive ?
                                                                            "enabled" : "disabled"));
                                                                    }

                                                                    if (tokens)
                                                                    {
                                                                        bool wasScriptLocation = localInterpreter.HasScriptLocation();

                                                                        if (enabled != null)
                                                                        {
                                                                            if ((bool)enabled != wasScriptLocation)
                                                                            {
                                                                                localInterpreter.EnableScriptLocation((bool)enabled);

                                                                                builder.AppendLine(String.Format(
                                                                                    "script locations {0}", (bool)enabled ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                            else if (verbose)
                                                                            {
                                                                                builder.AppendLine(String.Format(
                                                                                    "script locations are {0}", wasScriptLocation ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "script locations are {0}", wasScriptLocation ?
                                                                                "enabled" : "disabled"));
                                                                        }

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                                                        bool wasArgumentLocation = localInterpreter.HasArgumentLocation();

                                                                        if (enabled != null)
                                                                        {
                                                                            if ((bool)enabled != wasArgumentLocation)
                                                                            {
                                                                                localInterpreter.EnableArgumentLocation(
                                                                                    enabled, true);

                                                                                builder.AppendLine(String.Format(
                                                                                    "argument locations {0}", (bool)enabled ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                            else if (verbose)
                                                                            {
                                                                                builder.AppendLine(String.Format(
                                                                                    "argument locations are {0}", wasArgumentLocation ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "argument locations are {0}", wasArgumentLocation ?
                                                                                "enabled" : "disabled"));
                                                                        }
#else
                                                                        builder.AppendLine(
                                                                            "argument locations are not available");
#endif
                                                                    }

                                                                    if (scriptArguments)
                                                                    {
#if SCRIPT_ARGUMENTS
                                                                        bool wasScriptArguments = localInterpreter.HasScriptArguments();

                                                                        if (enabled != null)
                                                                        {
                                                                            if ((bool)enabled != wasScriptArguments)
                                                                            {
                                                                                localInterpreter.EnableScriptArguments((bool)enabled);

                                                                                builder.AppendLine(String.Format(
                                                                                    "script arguments {0}", (bool)enabled ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                            else if (verbose)
                                                                            {
                                                                                builder.AppendLine(String.Format(
                                                                                    "script arguments are {0}", wasScriptArguments ?
                                                                                    "enabled" : "disabled"));
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "script arguments are {0}", wasScriptArguments ?
                                                                                "enabled" : "disabled"));
                                                                        }
#else
                                                                        builder.AppendLine(
                                                                            "script arguments are not available");
#endif
                                                                    }

#if DEBUGGER && TEST
                                                                    if (_Tests.Default.TestSetDebugInteractiveLoopCallback(
                                                                            localInterpreter, enabled)) /* per-thread */
                                                                    {
                                                                        if (enabled != null)
                                                                        {
                                                                            builder.AppendLine(String.Format(
                                                                                "interactive loop {0}", (bool)enabled ?
                                                                                "hooked" : "unhooked"));
                                                                        }
                                                                        else
                                                                        {
                                                                            builder.AppendLine(
                                                                                "interactive loop is hooked");
                                                                        }
                                                                    }
                                                                    else if (verbose || (enabled == null))
                                                                    {
                                                                        builder.AppendLine(
                                                                            "interactive loop is unhooked");
                                                                    }
#else
                                                                    builder.AppendLine(
                                                                        "interactive loop hook is not available");
#endif

                                                                    if ((enabled != null) &&
                                                                        (verbose || (builder.Length > 0)))
                                                                    {
                                                                        builder.Insert(0, String.Format(
                                                                            "emergency mode now {0}{1}", (bool)enabled ?
                                                                            "enabled" : "disabled", Environment.NewLine));
                                                                    }

                                                                    if (@break)
                                                                    {
                                                                        if (builder.Length > 0)
                                                                        {
                                                                            IDebugHost debugHost = interpreter.Host;

                                                                            if (debugHost != null)
                                                                            {
                                                                                /* IGNORED */
                                                                                debugHost.WriteResult(
                                                                                    ReturnCode.Ok, builder.ToString(),
                                                                                    false);
                                                                            }
                                                                        }

                                                                        //
                                                                        // HACK: Break right now without leaving
                                                                        //       this method.
                                                                        //
                                                                        newArguments = new ArgumentList();
                                                                        newArguments.Add(this.Name);
                                                                        newArguments.Add("break");

                                                                        if (!Object.ReferenceEquals(
                                                                                localInterpreter, interpreter))
                                                                        {
                                                                            newArguments.Add("-interpreter");

                                                                            newArguments.Add(
                                                                                localInterpreter.IdNoThrow.ToString());
                                                                        }

                                                                        if (noComplain)
                                                                            newArguments.Add("-nocomplain");

#if DEBUGGER
                                                                        if (ignoreEnabled)
                                                                            newArguments.Add("-ignoreenabled");

                                                                        if (noError)
                                                                            newArguments.Add("-noerror");
#endif

                                                                        newArguments.Add(Option.EndOfOptions);
                                                                        goto redo;
                                                                    }

                                                                    result = builder;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "unable to acquire lock";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            localInterpreter.InternalExitLock(
                                                                ref locked); /* TRANSACTIONAL */
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug emergency ?options? ?level?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "Execute: sub-command \"{0}\" success: {1}",
                                                    subCommand, FormatOps.WrapOrNull(code, result)),
                                                    typeof(Debug).Name, TracePriority.CommandDebug);
                                            }
                                            else
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "Execute: sub-command \"{0}\" failure: {1}",
                                                    subCommand, FormatOps.WrapOrNull(code, result)),
                                                    typeof(Debug).Name, TracePriority.CommandError);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug emergency ?options? ?level?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enable":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;
                                            bool enabled = false;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref enabled, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                enabled = !enabled;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.Enabled = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "debugger " +
                                                            ConversionOps.ToEnabled(debugger.Enabled));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug enable ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (newArguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                string name = StringList.MakeList("debug eval");

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Evaluate | CallFrameFlags.Debugger);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                if (newArguments.Count == 3)
                                                    code = debugInterpreter.EvaluateScript(
                                                        newArguments[2], ref result);
                                                else
                                                    code = debugInterpreter.EvaluateScript(
                                                        newArguments, 2, ref result);

                                                if (code == ReturnCode.Error)
                                                {
                                                    Engine.CopyErrorInformation(debugInterpreter, interpreter, result);

                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in debug eval script line {1})",
                                                            Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));
                                                }

                                                //
                                                // NOTE: Pop the original call frame that we pushed above and
                                                //       any intervening scope call frames that may be leftover
                                                //       (i.e. they were not explicitly closed).
                                                //
                                                /* IGNORED */
                                                interpreter.PopScopeCallFramesAndOneMore();
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug eval arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exception":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
#if PREVIOUS_RESULT
                                            OptionDictionary options = ObjectOps.GetExceptionOptions();

                                            int argumentIndex = Index.Invalid;

                                            if (newArguments.Count > 2)
                                                code = interpreter.GetOptions(options, newArguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
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

                                                    ObjectOps.ProcessFixupReturnValueOptions(
                                                        options, null, out returnType, out objectFlags,
                                                        out objectName, out interpName, out create,
                                                        out dispose, out alias, out aliasRaw, out aliasAll,
                                                        out aliasReference, out toString);

                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(true, ref result))
                                                    {
                                                        Result previousResult = Interpreter.GetPreviousResult(
                                                            interpreter);

                                                        if (previousResult != null)
                                                        {
                                                            Exception exception = previousResult.Exception;

                                                            //
                                                            // NOTE: Create an opaque object handle
                                                            //       for the exception from the
                                                            //       previous result.
                                                            //
                                                            ObjectOptionType objectOptionType = ObjectOptionType.Exception |
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            code = MarshalOps.FixupReturnValue(
                                                                interpreter, interpreter.InternalBinder,
                                                                interpreter.InternalCultureInfo, returnType, objectFlags,
                                                                ObjectOps.GetInvokeOptions(objectOptionType),
                                                                objectOptionType, objectName, interpName, exception,
                                                                create, dispose, alias, aliasReference, toString,
                                                                ref result);
                                                        }
                                                        else
                                                        {
                                                            result = "no previous result";
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
                                                    result = "wrong # args: should be \"debug exception ?options?\"";
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
                                            result = "wrong # args: should be \"debug exception ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "execute":
                                    {
                                        if ((newArguments.Count == 3) || (newArguments.Count == 4))
                                        {
#if DEBUGGER
                                            string executeName = newArguments[2];
                                            IExecute execute = null;

                                            code = interpreter.GetIExecuteViaResolvers(
                                                interpreter.GetResolveEngineFlagsNoLock(true),
                                                executeName, null, LookupFlags.Default,
                                                ref execute, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (newArguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        newArguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteBreakpoint(
                                                                execute, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "execute \"{0}\" breakpoint is now {1}",
                                                                execute, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} execute \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), execute, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "execute \"{0}\" breakpoint is {1}",
                                                        execute, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteBreakpoint(execute)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug execute name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "function":
                                    {
                                        if ((newArguments.Count == 3) || (newArguments.Count == 4))
                                        {
#if DEBUGGER
                                            string functionName = newArguments[2];
                                            IFunction function = null;

                                            code = interpreter.GetFunction(
                                                functionName, LookupFlags.Default,
                                                ref function, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (newArguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        newArguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteArgumentBreakpoint(
                                                                function, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "function \"{0}\" breakpoint is now {1}",
                                                                function, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} function \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), function, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "function \"{0}\" breakpoint is {1}",
                                                        function, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteArgumentBreakpoint(function)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug function name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "gcmemory":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            bool collect = false;

                                            if (newArguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref collect, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = GC.GetTotalMemory(collect);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug gcmemory ?collect?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "halt":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            Result haltResult = null;

                                            if (newArguments.Count == 3)
                                                haltResult = newArguments[2];

                                            code = Engine.HaltEvaluate(
                                                interpreter, haltResult, CancelFlags.DebugHalt,
                                                ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug halt ?result?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "history":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if HISTORY
                                            if (newArguments.Count == 3)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                    interpreter.History = enabled;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.History;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug history ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "icommand":
                                    {
                                        //
                                        // debug icommand ?command?
                                        //
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (newArguments.Count == 3)
                                                {
                                                    debugger.Command = StringOps.NullIfEmpty(newArguments[2]);
                                                    result = String.Empty;
                                                }
                                                else if (newArguments.Count == 2)
                                                {
                                                    result = debugger.Command;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug icommand ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interactive":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            if (newArguments.Count == 3)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref enabled,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    interpreter.InternalInteractive = enabled;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.InternalInteractive;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug interactive ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invoke":
                                    {
                                        if (newArguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                int currentLevel = 0;

                                                code = interpreter.GetInfoLevel(
                                                    CallFrameOps.InfoLevelSubCommand, ref currentLevel,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    bool mark = false;
                                                    bool absolute = false;
                                                    bool super = false;
                                                    int level = 0;
                                                    ICallFrame currentFrame = null;
                                                    ICallFrame otherFrame = null;

                                                    FrameResult frameResult = debugInterpreter.GetCallFrame(
                                                        newArguments[2], ref mark, ref absolute, ref super,
                                                        ref level, ref currentFrame, ref otherFrame,
                                                        ref result);

                                                    if (frameResult != FrameResult.Invalid)
                                                    {
                                                        int argumentIndex = ((int)frameResult + 2);

                                                        //
                                                        // BUGFIX: The argument count needs to be checked
                                                        //         again here.
                                                        //
                                                        if (argumentIndex < newArguments.Count)
                                                        {
                                                            if (mark)
                                                            {
                                                                code = CallFrameOps.MarkMatching(
                                                                    debugInterpreter.CallStack,
                                                                    debugInterpreter.CurrentFrame,
                                                                    absolute, level,
                                                                    CallFrameFlags.Variables,
                                                                    CallFrameFlags.Invisible |
                                                                        CallFrameFlags.NoVariables,
                                                                    CallFrameFlags.Invisible, false,
                                                                    false, true, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                try
                                                                {
                                                                    string name = StringList.MakeList("debug invoke",
                                                                        newArguments[2], newArguments[argumentIndex]);

                                                                    ICallFrame newFrame = debugInterpreter.NewUplevelCallFrame(
                                                                        name, currentLevel, CallFrameFlags.Debugger, mark,
                                                                        currentFrame, otherFrame);

                                                                    ICallFrame savedFrame = null;

                                                                    debugInterpreter.PushUplevelCallFrame(
                                                                        currentFrame, newFrame, true, ref savedFrame);

                                                                    code = debugInterpreter.Invoke(
                                                                        newArguments[argumentIndex], clientData,
                                                                        ArgumentList.GetRange(newArguments, argumentIndex),
                                                                        ref result);

                                                                    if (code == ReturnCode.Error)
                                                                        Engine.AddErrorInformation(interpreter, result,
                                                                            String.Format("{0}    (\"debug invoke\" body line {1})",
                                                                                Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));

                                                                    //
                                                                    // NOTE: Pop the original call frame
                                                                    //       that we pushed above and any
                                                                    //       intervening scope call frames
                                                                    //       that may be leftover (i.e. they
                                                                    //       were not explicitly closed).
                                                                    //
                                                                    /* IGNORED */
                                                                    debugInterpreter.PopUplevelCallFrame(
                                                                        currentFrame, newFrame, ref savedFrame);
                                                                }
                                                                finally
                                                                {
                                                                    if (mark)
                                                                    {
                                                                        //
                                                                        // NOTE: We should not get an error at
                                                                        //       this point from unmarking the
                                                                        //       call frames; however, if we do
                                                                        //       get one, we need to complain
                                                                        //       loudly about it because that
                                                                        //       means the interpreter state
                                                                        //       has probably been corrupted
                                                                        //       somehow.
                                                                        //
                                                                        ReturnCode markCode;
                                                                        Result markResult = null;

                                                                        markCode = CallFrameOps.MarkMatching(
                                                                            debugInterpreter.CallStack,
                                                                            debugInterpreter.CurrentFrame,
                                                                            absolute, level,
                                                                            CallFrameFlags.Variables,
                                                                            CallFrameFlags.NoVariables,
                                                                            CallFrameFlags.Invisible, false,
                                                                            false, false, ref markResult);

                                                                        if (markCode != ReturnCode.Ok)
                                                                        {
                                                                            DebugOps.Complain(debugInterpreter,
                                                                                markCode, markResult);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "wrong # args: should be \"debug invoke ?level? cmd ?arg ...?\"";
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
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug invoke ?level? cmd ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "iqueue":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false, ref debugger, ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-dump", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-clear", null),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                if (newArguments.Count > 2)
                                                {
                                                    code = interpreter.GetOptions(
                                                        options, newArguments, 0, 2, Index.Invalid, true,
                                                        ref argumentIndex, ref result);
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Ok;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex == Index.Invalid) ||
                                                        ((argumentIndex + 1) == newArguments.Count))
                                                    {
                                                        bool dump = false;

                                                        if (options.IsPresent("-dump"))
                                                            dump = true;

                                                        bool clear = false;

                                                        if (options.IsPresent("-clear"))
                                                            clear = true;

                                                        if ((code == ReturnCode.Ok) && dump)
                                                            code = debugger.DumpCommands(ref result);

                                                        if ((code == ReturnCode.Ok) && clear)
                                                            code = debugger.ClearCommands(ref result);

                                                        if ((code == ReturnCode.Ok) &&
                                                            (argumentIndex != Index.Invalid))
                                                        {
                                                            code = debugger.EnqueueCommand(
                                                                newArguments[argumentIndex], ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && !dump)
                                                            result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        if ((argumentIndex != Index.Invalid) &&
                                                            Option.LooksLikeOption(newArguments[argumentIndex]))
                                                        {
                                                            result = OptionDictionary.BadOption(
                                                                options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                        }
                                                        else
                                                        {
                                                            result = "wrong # args: should be \"debug iqueue ?options? ?command?\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug iqueue ?options? ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "iresult":
                                    {
                                        //
                                        // debug iresult ?result?
                                        //
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (newArguments.Count == 3)
                                                {
                                                    debugger.Result = StringOps.NullIfEmpty(newArguments[2]);
                                                    result = String.Empty;
                                                }
                                                else if (newArguments.Count == 2)
                                                {
                                                    result = debugger.Result;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug iresult ?result?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "keyring":
                                    {
                                        if (newArguments.Count == 2)
                                        {
                                            code = ScriptOps.FetchAndMergeKeyRing(
                                                interpreter, false, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug keyring\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "levels":
                                    {
                                        if (newArguments.Count == 2)
                                        {
                                            result = StringList.MakeList(
                                                "maximumLevels", interpreter.MaximumLevels,
                                                "maximumScriptLevels", interpreter.MaximumScriptLevels,
                                                "maximumParserLevels", interpreter.MaximumParserLevels,
                                                "maximumExpressionLevels", interpreter.MaximumExpressionLevels
                                            );

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug levels\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lockloop":
                                    {
                                        if (newArguments.Count == 3)
                                        {
#if SHELL
                                            bool enabled = false;

                                            code = Value.GetBoolean2(
                                                newArguments[2], ValueFlags.AnyBoolean,
                                                interpreter.InternalCultureInfo, ref enabled, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (enabled)
                                                {
                                                    IDebugHost debugHost = interpreter.Host;

                                                    if (debugHost != null)
                                                    {
                                                        bool success = false;
                                                        int count = 0;

                                                        int retries = ThreadOps.GetDefaultRetries(
                                                            interpreter, TimeoutType.FirmLock);

                                                        int timeout = ThreadOps.GetDefaultTimeout(
                                                            interpreter, TimeoutType.FirmLock);

                                                        do
                                                        {
                                                            //
                                                            // NOTE: Break out of any pending method calls
                                                            //       to read interactive input.  Then, try
                                                            //       to enter the associated semaphore.
                                                            //
#if CONSOLE
                                                            int savedCancelViaConsole = 0;

                                                            Interpreter.BeginNoConsoleCancelEventHandler(
                                                                ref savedCancelViaConsole);

                                                            try
                                                            {
#endif
                                                                Result error = null;

                                                                if (debugHost.Cancel(
                                                                        false, ref error) != ReturnCode.Ok)
                                                                {
                                                                    TraceOps.DebugTrace(String.Format(
                                                                        "Execute: host cancel error: {0}",
                                                                        error), typeof(Debug).Name,
                                                                        TracePriority.CommandError2);

                                                                    error = null;

                                                                    if (HostOps.Sleep(
                                                                            debugHost as IThreadHost,
                                                                            timeout,
                                                                            ref error) != ReturnCode.Ok)
                                                                    {
                                                                        TraceOps.DebugTrace(String.Format(
                                                                            "Execute: host sleep error: {0}",
                                                                            error), typeof(Debug).Name,
                                                                            TracePriority.CommandError2);
                                                                    }

                                                                    continue;
                                                                }
#if CONSOLE
                                                            }
                                                            finally
                                                            {
                                                                Interpreter.EndNoConsoleCancelEventHandler(
                                                                    ref savedCancelViaConsole);
                                                            }
#endif

                                                            //
                                                            // NOTE: Attempt to enter the interactive loop
                                                            //       semaphore.  Upon success, this should
                                                            //       prevent other threads from obtaining
                                                            //       interactive input.  This must be done
                                                            //       after canceling out of pending calls
                                                            //       to read interactive input because the
                                                            //       interactive loop semaphore is held by
                                                            //       the [other] thread during such reads.
                                                            //
                                                            if (Interpreter.TryEnterInteractiveLoopSemaphore(
                                                                    interpreter, timeout, false))
                                                            {
                                                                //
                                                                // NOTE: Null out per-thread property value
                                                                //       for the interactive loop semaphore
                                                                //       because this thread holds the lock
                                                                //       and does not need to acquire it
                                                                //       again.
                                                                //
                                                                /* NO RESULT */
                                                                interpreter.ResetInteractiveLoopSemaphore();

                                                                //
                                                                // NOTE: Indicate to the calling script that
                                                                //       we succeeded.
                                                                //
                                                                success = true;

                                                                //
                                                                // NOTE: Success, stop retrying now.
                                                                //
                                                                break;
                                                            }
                                                        } while (count++ < retries);

                                                        //
                                                        // NOTE: Return non-zero to the caller if the lock
                                                        //       was actually obtained.
                                                        //
                                                        result = success;
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: First, restore the per-thread property
                                                    //       value for the interactive loop semaphore.
                                                    //       This must be done first because exiting
                                                    //       it (below) will refer to the per-thread
                                                    //       property value.
                                                    //
                                                    /* NO RESULT */
                                                    interpreter.RestoreInteractiveLoopSemaphore();

                                                    //
                                                    // NOTE: Exit the interactive loop semaphore.
                                                    //       This will permit other threads to obtain
                                                    //       interactive input.
                                                    //
                                                    result = Interpreter.ExitInteractiveLoopSemaphore(
                                                        interpreter);
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug lockloop enabled\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lockvar":
                                    {
                                        if (newArguments.Count == 4)
                                        {
                                            bool? enabled = null;

                                            code = Value.GetNullableBoolean2(
                                                newArguments[2], ValueFlags.AnyBoolean,
                                                interpreter.InternalCultureInfo, ref enabled, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    VariableFlags flags = VariableFlags.NoElement;
                                                    IVariable variable = null;

                                                    code = interpreter.GetVariableViaResolversWithSplit(
                                                        newArguments[3], ref flags, ref variable, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (variable != null)
                                                        {
                                                            if (enabled != null)
                                                            {
                                                                if ((bool)enabled)
                                                                {
                                                                    if (variable.Lock(ref result))
                                                                        result = String.Empty;
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                                else
                                                                {
                                                                    if (variable.Unlock(ref result))
                                                                        result = String.Empty;
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                long? threadId = variable.ThreadId;

                                                                result = StringList.MakeList(
                                                                    (threadId != null), threadId);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid variable";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug lockvar enabled name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "log":
                                    {
                                        if (newArguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-level", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-category", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, newArguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == newArguments.Count))
                                                {
                                                    Variant value = null;
                                                    int level = 0;

                                                    if (options.IsPresent("-level", ref value))
                                                        level = (int)value.Value;

                                                    string category = DebugOps.DefaultCategory;

                                                    if (options.IsPresent("-category", ref value))
                                                        category = value.ToString();

                                                    DebugOps.Log(level, category, newArguments[argumentIndex]);

                                                    result = String.Empty;
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug log ?options? message\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug log ?options? message\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "memory":
                                    {
                                        if (newArguments.Count == 2)
                                        {
                                            StringList list = new StringList(
                                                "gcTotalMemory", GC.GetTotalMemory(false).ToString());

                                            int maxGeneration = GC.MaxGeneration;

                                            list.Add("gcMaxGeneration", maxGeneration.ToString());

                                            for (int generation = 0; generation <= maxGeneration; generation++)
                                            {
                                                list.Add(String.Format(
                                                    "gcCollectionCount({0})", generation),
                                                    GC.CollectionCount(generation).ToString());
                                            }

                                            list.Add("isServerGC",
                                                GCSettings.IsServerGC.ToString());

#if NET_35 || NET_40 || NET_STANDARD_20
                                            list.Add("gcLatencyMode",
                                                GCSettings.LatencyMode.ToString());
#endif

#if NATIVE
                                            Result error = null;

                                            if (NativeOps.GetMemoryStatus(
                                                    ref list, ref error) != ReturnCode.Ok)
                                            {
                                                list.Add("nativeMemory");
                                                list.Add(error);
                                            }
#endif

                                            result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug memory\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "oncancel":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnCancel;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnCancel = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on cancel " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnCancel));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug oncancel ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onerror":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnError;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnError = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on error " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnError));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onerror ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onexecute":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnExecute;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnExecute = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on execute " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnExecute));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onexecute ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onexit":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnExit;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnExit = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on exit " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnExit));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onexit ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onreturn":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnReturn;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnReturn = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on return " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnReturn));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onreturn ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ontest":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnTest;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnTest = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on test " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnTest));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ontest ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ontoken":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnToken;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnToken = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on token " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnToken));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ontoken ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "operator":
                                    {
                                        if ((newArguments.Count == 3) || (newArguments.Count == 4))
                                        {
#if DEBUGGER
                                            string operatorName = newArguments[2];
                                            IOperator @operator = null;

                                            code = interpreter.GetOperator(
                                                operatorName, LookupFlags.Default,
                                                ref @operator, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (newArguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        newArguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteArgumentBreakpoint(
                                                                @operator, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "operator \"{0}\" breakpoint is now {1}",
                                                                @operator, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} operator \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), @operator, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "operator \"{0}\" breakpoint is {1}",
                                                        @operator, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteArgumentBreakpoint(@operator)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug operator name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "output":
                                    {
                                        if (newArguments.Count == 3)
                                        {
#if NATIVE
                                            DebugOps.Output(newArguments[2]);

                                            result = String.Empty;
                                            code = ReturnCode.Ok;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug output message\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "paths":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            DebugPathFlags flags = DebugPathFlags.Default;

                                            if (newArguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(DebugPathFlags),
                                                    flags.ToString(), newArguments[2],
                                                    interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is DebugPathFlags)
                                                    flags = (DebugPathFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                PathClientDataDictionary paths = null;

                                                GlobalState.GetPaths(
                                                    interpreter, FlagOps.HasFlags(
                                                    flags, DebugPathFlags.GetAll,
                                                    true), ref paths);

                                                if (FlagOps.HasFlags(
                                                        flags, DebugPathFlags.UseFilter,
                                                        true))
                                                {
                                                    GlobalState.FilterPaths(
                                                        FlagOps.HasFlags(flags,
                                                            DebugPathFlags.ExistingOnly,
                                                            true),
                                                        FlagOps.HasFlags(flags,
                                                            DebugPathFlags.UniqueOnly,
                                                            true),
                                                        ref paths);
                                                }

                                                if (paths != null)
                                                {
                                                    IStringList list = paths.ToList();

                                                    if (list != null)
                                                        result = list.ToString();
                                                    else
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug paths ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pluginexecute":
                                    {
                                        if (newArguments.Count == 4)
                                        {
                                            IPlugin plugin = null;

                                            code = interpreter.GetPlugin(
                                                newArguments[2], LookupFlags.Default, ref plugin,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                StringList list = null;

                                                code = ListOps.GetOrCopyOrSplitList(
                                                    interpreter, newArguments[3], false, ref list,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // HACK: Convert empty list elements to a null
                                                    //       string.
                                                    //
                                                    for (int index = 0; index < list.Count; index++)
                                                        if (String.IsNullOrEmpty(list[index]))
                                                            list[index] = null;

                                                    //
                                                    // NOTE: The IExecuteRequest.Execute method is
                                                    //       always passed a string array here, not
                                                    //       a StringList object.  Upon success,
                                                    //       the response is always converted to a
                                                    //       string and used as the command result;
                                                    //       otherwise, the error is used as the
                                                    //       command result.
                                                    //
                                                    object response = null;

                                                    code = plugin.Execute(
                                                        interpreter, clientData, list.ToArray(),
                                                        ref response, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringOps.GetStringFromObject(
                                                            response);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug pluginexecute name request\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pluginflags":
                                    {
                                        //
                                        // debug pluginflags ?flags?
                                        //
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            if (newArguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(PluginFlags),
                                                    interpreter.PluginFlags.ToString(),
                                                    newArguments[2], interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is PluginFlags)
                                                    interpreter.PluginFlags = (PluginFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.PluginFlags;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug pluginflags ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "procedureflags":
                                    {
                                        //
                                        // debug procedureflags procName ?flags?
                                        //
                                        if ((newArguments.Count == 3) || (newArguments.Count == 4))
                                        {
                                            IProcedure procedure = null;

                                            code = interpreter.GetProcedureViaResolvers(
                                                ScriptOps.MakeCommandName(newArguments[2]),
                                                LookupFlags.Default, ref procedure,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (newArguments.Count == 4)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        interpreter, typeof(ProcedureFlags),
                                                        procedure.Flags.ToString(),
                                                        newArguments[3], interpreter.InternalCultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is ProcedureFlags)
                                                        procedure.Flags = (ProcedureFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = procedure.Flags;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug procedureflags procName ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "purge":
                                    {
                                        if (newArguments.Count == 2)
                                        {
                                            code = CallFrameOps.Purge(interpreter, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug purge\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resume":
                                    {
                                        if (newArguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Resume(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = "debugger resumed";
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug resume\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "restore":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            bool strict = false;

                                            if (newArguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref strict, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = interpreter.RestoreCorePlugin(strict, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug restore ?strict?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ready":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            bool isolated = false; /* NOTE: Require isolated interpreter? */

                                            if (newArguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref isolated, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                IDebugger debugger = null;

                                                if (isolated)
                                                {
                                                    Interpreter debugInterpreter = null;

                                                    result = Engine.CheckDebugger(
                                                        interpreter, false, ref debugger,
                                                        ref debugInterpreter, ref result);
                                                }
                                                else
                                                {
                                                    result = Engine.CheckDebugger(
                                                        interpreter, false, ref debugger,
                                                        ref result);
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ready ?isolated?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "refreshautopath":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            bool verbose = false;

                                            if (newArguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref verbose,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                /* NO RESULT */
                                                GlobalState.RefreshAutoPathList(verbose);

                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug refreshautopath ?verbose?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readonly":
                                    {
                                        if ((newArguments.Count >= 4) && (newArguments.Count <= 5))
                                        {
                                            IdentifierKind kind = IdentifierKind.None;

                                            object enumValue = EnumOps.TryParseFlags(
                                                interpreter, typeof(IdentifierKind), kind.ToString(),
                                                newArguments[2], interpreter.InternalCultureInfo,
                                                true, true, true, ref result);

                                            if (enumValue is IdentifierKind)
                                            {
                                                kind = (IdentifierKind)enumValue;

                                                bool? enabled = null;

                                                code = Value.GetNullableBoolean2(
                                                    newArguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref enabled,
                                                    ref result);

                                                string pattern = null;

                                                if (newArguments.Count == 5)
                                                    pattern = newArguments[4];

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int count;

                                                    switch (kind)
                                                    {
                                                        case IdentifierKind.Command:
                                                            {
                                                                if (pattern != null)
                                                                    pattern = ScriptOps.MakeCommandName(pattern);

                                                                if (enabled != null)
                                                                {
                                                                    count = interpreter.SetCommandsReadOnly(
                                                                        pattern, false, (bool)enabled);

                                                                    result = String.Format(
                                                                        "{0} {1} {2}", (bool)enabled ? "locked" : "unlocked",
                                                                        count, (count != 1) ? "commands" : "command");
                                                                }
                                                                else
                                                                {
                                                                    result = interpreter.GetCommandsReadOnly(
                                                                        pattern, false, true);
                                                                }
                                                                break;
                                                            }
                                                        case IdentifierKind.Procedure:
                                                            {
                                                                if (pattern != null)
                                                                    pattern = ScriptOps.MakeCommandName(pattern);

                                                                if (enabled != null)
                                                                {
                                                                    count = interpreter.SetProceduresReadOnly(
                                                                        pattern, false, (bool)enabled);

                                                                    result = String.Format(
                                                                        "{0} {1} {2}", (bool)enabled ? "locked" : "unlocked",
                                                                        count, (count != 1) ? "procedures" : "procedure");
                                                                }
                                                                else
                                                                {
                                                                    result = interpreter.GetProceduresReadOnly(
                                                                        pattern, false, true);
                                                                }
                                                                break;
                                                            }
                                                        case IdentifierKind.Variable:
                                                            {
                                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                                {
                                                                    ICallFrame variableFrame = null;

                                                                    code = interpreter.GetVariableFrameViaResolvers(
                                                                        LookupFlags.Default, ref variableFrame,
                                                                        ref pattern, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (variableFrame != null)
                                                                        {
                                                                            VariableDictionary variables = variableFrame.Variables;

                                                                            if (variables != null)
                                                                            {
                                                                                if (enabled != null)
                                                                                {
                                                                                    count = variables.SetReadOnly(
                                                                                        interpreter, pattern, (bool)enabled);

                                                                                    result = String.Format(
                                                                                        "{0} {1} {2} in call frame {3}", (bool)enabled ?
                                                                                        "locked" : "unlocked", count, (count != 1) ?
                                                                                        "variables" : "variable", variableFrame.Name);
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = variables.GetReadOnly(
                                                                                        interpreter, pattern, true);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "call frame does not support variables";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "invalid call frame";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                            }
                                                        default:
                                                            {
                                                                result = "unsupported identifier kind";
                                                                code = ReturnCode.Error;
                                                                break;
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
                                            result = "wrong # args: should be \"debug readonly kind enabled ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "run":
                                    {
                                        //
                                        // NOTE: Think of this as "eval without debugging"
                                        //       or "run this at full speed".
                                        //
                                        if (newArguments.Count >= 3)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Suspend(ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        if (newArguments.Count == 3)
                                                        {
                                                            code = interpreter.EvaluateScript(
                                                                newArguments[2], ref result);
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.EvaluateScript(
                                                                newArguments, 2, ref result);
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        //
                                                        // NOTE: At this point, if we fail to resume
                                                        //       debugging for some reason, there is
                                                        //       not really much we can do about it.
                                                        //
                                                        ReturnCode resumeCode;
                                                        Result resumeResult = null;

                                                        resumeCode = debugger.Resume(ref resumeResult);

                                                        if (resumeCode != ReturnCode.Ok)
                                                        {
                                                            DebugOps.Complain(
                                                                interpreter, resumeCode, resumeResult);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug run arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "runtimeoption":
                                    {
                                        if ((newArguments.Count == 3) || (newArguments.Count == 4))
                                        {
                                            object enumValue = EnumOps.TryParse(
                                                typeof(RuntimeOptionOperation), newArguments[2],
                                                true, true, ref result);

                                            if (enumValue is RuntimeOptionOperation)
                                            {
                                                RuntimeOptionOperation operation =
                                                    (RuntimeOptionOperation)enumValue;

                                                switch (operation)
                                                {
                                                    case RuntimeOptionOperation.Has:
                                                        {
                                                            if (newArguments.Count == 4)
                                                            {
                                                                result = interpreter.HasRuntimeOption(
                                                                    newArguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption has name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Get:
                                                        {
                                                            if (newArguments.Count == 3)
                                                            {
                                                                result = interpreter.RuntimeOptions;
                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption get\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Clear:
                                                        {
                                                            if (newArguments.Count == 3)
                                                            {
                                                                result = interpreter.ClearRuntimeOptions();
                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption clear\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Add:
                                                        {
                                                            if (newArguments.Count == 4)
                                                            {
                                                                result = interpreter.AddRuntimeOption(
                                                                    newArguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption add name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Remove:
                                                        {
                                                            if (newArguments.Count == 4)
                                                            {
                                                                result = interpreter.RemoveRuntimeOption(
                                                                    newArguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption remove name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Set:
                                                        {
                                                            if (newArguments.Count == 4)
                                                            {
                                                                StringList list = null;

                                                                code = ListOps.GetOrCopyOrSplitList(
                                                                    interpreter, newArguments[3], true, ref list,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                                    {
                                                                        ClientDataDictionary runtimeOptions =
                                                                            new ClientDataDictionary();

                                                                        foreach (string element in list)
                                                                            runtimeOptions[element] = null;

                                                                        interpreter.RuntimeOptions = runtimeOptions;
                                                                        result = interpreter.RuntimeOptions;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption set list\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    default:
                                                        {
                                                            result = String.Format(
                                                                "unsupported runtime option operation \"{0}\"",
                                                                operation);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                }
                                            }
                                            else
                                            {
                                                result = ScriptOps.BadValue(
                                                    null, "runtime option operation", newArguments[2],
                                                    Enum.GetNames(typeof(RuntimeOptionOperation)),
                                                    null, null);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug runtimeoption operation ?arg?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "runtimeoverride":
                                    {
                                        if (newArguments.Count == 3)
                                        {
                                            object enumValue = EnumOps.TryParse(
                                                typeof(RuntimeName), newArguments[2],
                                                true, true, ref result);

                                            if (enumValue is RuntimeName)
                                            {
                                                if (!CommonOps.Runtime.SetManualOverride(
                                                        (RuntimeName)enumValue, ref result))
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = ScriptOps.BadValue(
                                                    null, "runtime name", newArguments[2],
                                                    Enum.GetNames(typeof(RuntimeName)),
                                                    null, null);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug runtimeoverride name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "secureeval":
                                    {
                                        if (newArguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-timeout", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-nocancel", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-globalcancel", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-stoponerror", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-file", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-events", null),
#if ISOLATED_PLUGINS
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-noisolatedplugins", null),
#else
                                                new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-noisolatedplugins", null),
#endif
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, newArguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < newArguments.Count))
                                                {
                                                    Variant value = null;
                                                    int timeout = _Timeout.Infinite;

                                                    if (options.IsPresent("-timeout", ref value))
                                                        timeout = (int)value.Value;

                                                    bool noCancel = false;

                                                    if (options.IsPresent("-nocancel", ref value))
                                                        noCancel = (bool)value.Value;

                                                    bool globalCancel = false;

                                                    if (options.IsPresent("-globalcancel", ref value))
                                                        globalCancel = (bool)value.Value;

                                                    bool stopOnError = false;

                                                    if (options.IsPresent("-stoponerror", ref value))
                                                        stopOnError = (bool)value.Value;

                                                    bool file = false;

                                                    if (options.IsPresent("-file", ref value))
                                                        file = (bool)value.Value;

                                                    bool trusted = false;

                                                    if (options.IsPresent("-trusted", ref value))
                                                        trusted = (bool)value.Value;

                                                    bool events = !trusted;

                                                    if (options.IsPresent("-events", ref value))
                                                        events = (bool)value.Value;

#if ISOLATED_PLUGINS
                                                    bool noIsolatedPlugins = false;

                                                    if (options.IsPresent("-noisolatedplugins", ref value))
                                                        noIsolatedPlugins = (bool)value.Value;
#endif

                                                    string path = newArguments[argumentIndex];
                                                    Interpreter childInterpreter = null;

                                                    code = interpreter.GetNestedChildInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref childInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        bool haveTimeout = (timeout >= 0);

                                                        string name = StringList.MakeList(
                                                            "debug secure eval", path,
                                                            noCancel ? "(with reset-cancel)" : "(without reset-cancel)",
                                                            trusted ? "(trusted)" : "(untrusted)",
                                                            events ? "(with events)" : "(without events)",
                                                            stopOnError ? "(with stop-on-error)" : "(without stop-on-error)",
                                                            haveTimeout ? "(with timeout)" : "(without timeout)"
#if ISOLATED_PLUGINS
                                                            , String.Format("(with isolated plugins {0})",
                                                                noIsolatedPlugins ? "unavailable" : "available")
#endif
                                                            );

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Evaluate | CallFrameFlags.Debugger |
                                                            CallFrameFlags.Restricted);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        bool locked = false;

                                                        try
                                                        {
                                                            childInterpreter.InternalHardTryLock(
                                                                ref locked); /* TRANSACTIONAL */

                                                            if (locked)
                                                            {
                                                                try
                                                                {
                                                                    int savedEnabled = 0;

                                                                    if (!events)
                                                                    {
                                                                        if (!EventOps.SaveEnabledAndForceDisabled(
                                                                                childInterpreter, true, ref savedEnabled))
                                                                        {
                                                                            result = "failed to forcibly disable events";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }

                                                                    try
                                                                    {
                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            EventWaitFlags? savedEventWaitFlags = null;

                                                                            if (stopOnError)
                                                                            {
                                                                                savedEventWaitFlags = childInterpreter.EventWaitFlags;
                                                                                childInterpreter.EventWaitFlags |= EventWaitFlags.StopOnError;
                                                                            }

                                                                            try
                                                                            {
                                                                                if (trusted)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Use of this flag is currently limited to "safe" child
                                                                                    //       interpreters only.  Just ignore the flag if called for
                                                                                    //       an "unsafe" interpreter.
                                                                                    //
                                                                                    if (childInterpreter.InternalIsSafe())
                                                                                        childInterpreter.InternalMarkTrusted();
                                                                                    else
                                                                                        trusted = false;
                                                                                }

                                                                                try
                                                                                {
                                                                                    //
                                                                                    // HACK: If necessary, add the "IgnoreHidden" engine flag to the
                                                                                    //       per-thread engine flags for this interpreter so that the
                                                                                    //       script specified by the caller can run with full trust.
                                                                                    //       The per-thread engine flags must be used in this case;
                                                                                    //       otherwise, other scripts being evaluated on other threads
                                                                                    //       in this interpreter would also gain full trust.
                                                                                    //
                                                                                    bool added = false;

                                                                                    if (trusted &&
                                                                                        !EngineFlagOps.HasIgnoreHidden(childInterpreter.ContextEngineFlags))
                                                                                    {
                                                                                        added = true;
                                                                                        childInterpreter.ContextEngineFlags |= EngineFlags.IgnoreHidden;
                                                                                    }

                                                                                    try
                                                                                    {
                                                                                        Thread timeoutThread = null;

                                                                                        try
                                                                                        {
                                                                                            if (haveTimeout)
                                                                                            {
                                                                                                code = RuntimeOps.QueueScriptTimeout(
                                                                                                    childInterpreter, null, timeout, ref timeoutThread,
                                                                                                    ref result);
                                                                                            }

                                                                                            if (code == ReturnCode.Ok)
                                                                                            {
#if ISOLATED_PLUGINS
                                                                                                PluginFlags savedPluginFlags = PluginFlags.None;

                                                                                                if (noIsolatedPlugins)
                                                                                                    childInterpreter.BeginNoIsolatedPlugins(ref savedPluginFlags);

                                                                                                try
                                                                                                {
#endif
                                                                                                    if (((argumentIndex + 2) == newArguments.Count))
                                                                                                    {
                                                                                                        if (file)
                                                                                                        {
                                                                                                            code = childInterpreter.EvaluateFile(
                                                                                                                newArguments[argumentIndex + 1], ref result);
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            code = childInterpreter.EvaluateScript(
                                                                                                                newArguments[argumentIndex + 1], ref result);
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        if (file)
                                                                                                        {
                                                                                                            result = String.Format(
                                                                                                                "wrong # args: should be \"{0} {1} " + /* SKIP */
                                                                                                                "-file true ?options? path fileName\"",
                                                                                                                this.Name, subCommand);

                                                                                                            code = ReturnCode.Error;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            code = childInterpreter.EvaluateScript(
                                                                                                                newArguments, argumentIndex + 1, ref result);
                                                                                                        }
                                                                                                    }
#if ISOLATED_PLUGINS
                                                                                                }
                                                                                                finally
                                                                                                {
                                                                                                    if (noIsolatedPlugins)
                                                                                                        childInterpreter.EndNoIsolatedPlugins(ref savedPluginFlags);
                                                                                                }
#endif

                                                                                                if (code == ReturnCode.Error)
                                                                                                {
                                                                                                    Engine.CopyErrorInformation(childInterpreter, interpreter, result);

                                                                                                    Engine.AddErrorInformation(interpreter, result,
                                                                                                        String.Format("{0}    (in debug secureeval \"{1}\" script line {2})",
                                                                                                            Environment.NewLine, path, Interpreter.GetErrorLine(childInterpreter)));
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                        finally
                                                                                        {
                                                                                            ThreadOps.MaybeShutdown(
                                                                                                childInterpreter, null, ShutdownFlags.ScriptTimeout,
                                                                                                ref timeoutThread);
                                                                                        }
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        if (added)
                                                                                        {
                                                                                            childInterpreter.ContextEngineFlags &= ~EngineFlags.IgnoreHidden;
                                                                                            added = false;
                                                                                        }
                                                                                    }
                                                                                }
                                                                                finally
                                                                                {
                                                                                    if (trusted)
                                                                                        childInterpreter.InternalMarkSafe();
                                                                                }
                                                                            }
                                                                            finally
                                                                            {
                                                                                if (savedEventWaitFlags != null)
                                                                                {
                                                                                    childInterpreter.EventWaitFlags =
                                                                                        (EventWaitFlags)savedEventWaitFlags;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    finally
                                                                    {
                                                                        if (!events)
                                                                        {
                                                                            /* IGNORED */
                                                                            EventOps.RestoreEnabled(
                                                                                childInterpreter, savedEnabled);

                                                                            savedEnabled = 0;
                                                                        }
                                                                    }
                                                                }
                                                                finally
                                                                {
                                                                    if (noCancel)
                                                                    {
                                                                        CancelFlags cancelFlags = CancelFlags.DebugSecureEval;

                                                                        if (globalCancel)
                                                                            cancelFlags |= CancelFlags.Global;

                                                                        /* IGNORED */
                                                                        Engine.ResetCancel(childInterpreter, cancelFlags);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "could not lock interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            childInterpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
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
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug secureeval ?options? path arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug secureeval ?options? path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "self": /* SKIP */
                                    {
                                        //
                                        // NOTE: The default is "break when debugger is attached".
                                        //
                                        bool debug = DebugOps.IsAttached();

                                        //
                                        // NOTE: Purposely relaxed argument count checking...
                                        //
                                        if (newArguments.Count >= 3)
                                            code = Value.GetBoolean2(
                                                newArguments[2], ValueFlags.AnyBoolean,
                                                interpreter.InternalCultureInfo, ref debug, ref result);

                                        bool force = false; // NOTE: Break for release builds?

                                        //
                                        // NOTE: Purposely relaxed argument count checking...
                                        //
                                        if ((code == ReturnCode.Ok) && (newArguments.Count >= 4))
                                            code = Value.GetBoolean2(
                                                newArguments[3], ValueFlags.AnyBoolean,
                                                interpreter.InternalCultureInfo, ref force, ref result);

                                        if ((code == ReturnCode.Ok) && debug)
                                            DebugOps.Break(interpreter, null, force);

                                        break;
                                    }
                                case "setup":
                                    {
                                        if ((newArguments.Count >= 2) && (newArguments.Count <= 8))
                                        {
#if DEBUGGER
                                            bool setup = !Engine.CheckDebugger(interpreter, true);

                                            if (newArguments.Count >= 3)
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref setup, ref result);

                                            bool isolated = false; /* TODO: Good default? */

                                            if ((code == ReturnCode.Ok) && (newArguments.Count >= 4))
                                                code = Value.GetBoolean2(
                                                    newArguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref isolated, ref result);

                                            CreateFlags createFlags = interpreter.CreateFlags; /* TODO: Good default? */
                                            InitializeFlags initializeFlags = interpreter.InitializeFlags; /* TODO: Good default? */
                                            ScriptFlags scriptFlags = interpreter.ScriptFlags; /* TODO: Good default? */
                                            InterpreterFlags interpreterFlags = interpreter.InterpreterFlags; /* TODO: Good default? */
                                            PluginFlags pluginFlags = interpreter.PluginFlags; /* TODO: Good default? */

                                            //
                                            // NOTE: Remove flags that we are handling specially
                                            //       -OR- that we know will cause problems and
                                            //       add the ones we know are generally required.
                                            //
                                            createFlags &= ~CreateFlags.ThrowOnError;
                                            createFlags &= ~CreateFlags.DebuggerInterpreter;
                                            createFlags |= CreateFlags.Initialize;

                                            if (isolated)
                                                createFlags |= CreateFlags.DebuggerInterpreter;

                                            if ((code == ReturnCode.Ok) && (newArguments.Count >= 5))
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(CreateFlags),
                                                    createFlags.ToString(),
                                                    newArguments[4], interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is CreateFlags)
                                                    createFlags = (CreateFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (newArguments.Count >= 6))
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(InitializeFlags),
                                                    initializeFlags.ToString(),
                                                    newArguments[5], interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is InitializeFlags)
                                                    initializeFlags = (InitializeFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (newArguments.Count >= 7))
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(ScriptFlags),
                                                    scriptFlags.ToString(),
                                                    newArguments[6], interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is ScriptFlags)
                                                    scriptFlags = (ScriptFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (newArguments.Count >= 8))
                                            {
                                                object enumValue = EnumOps.TryParseFlags(
                                                    interpreter, typeof(InterpreterFlags),
                                                    interpreterFlags.ToString(),
                                                    newArguments[7], interpreter.InternalCultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is InterpreterFlags)
                                                    interpreterFlags = (InterpreterFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // TODO: Make it possible to disable copying the library
                                                //       path and/or the auto-path here?
                                                //
                                                if (Engine.SetupDebugger(
                                                        interpreter, interpreter.CultureName,
                                                        createFlags, interpreter.HostCreateFlags,
                                                        initializeFlags, scriptFlags, interpreterFlags,
                                                        pluginFlags, interpreter.GetAppDomain(),
                                                        interpreter.Host,
                                                        DebuggerOps.GetLibraryPath(interpreter),
                                                        DebuggerOps.GetAutoPathList(interpreter),
                                                        false, setup, isolated, ref result))
                                                {
                                                    result = String.Empty;
                                                }
                                                else
                                                {
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
                                            result = "wrong # args: should be \"debug setup ?create? ?isolated? ?createFlags? ?initializeFlags? ?scriptFlags? ?interpreterFlags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shell":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
#if SHELL
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue, Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-initialize", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-loop", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-asynchronous", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (newArguments.Count > 2)
                                                code = interpreter.GetOptions(options, newArguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                Variant value = null;
                                                Interpreter localInterpreter = null;

                                                if (options.IsPresent("-interpreter", ref value))
                                                    localInterpreter = (Interpreter)value.Value;

                                                bool initialize = false;

                                                if (options.IsPresent("-initialize", ref value))
                                                    initialize = (bool)value.Value;

                                                bool loop = false;

                                                if (options.IsPresent("-loop", ref value))
                                                    loop = (bool)value.Value;

                                                bool asynchronous = false;

                                                if (options.IsPresent("-asynchronous", ref value))
                                                    asynchronous = (bool)value.Value;

                                                //
                                                // NOTE: Pass the remaining newArguments, if any, to
                                                //       the [nested] shell.  If there are no more
                                                //       newArguments, pass null.
                                                //
                                                IEnumerable<string> args = (argumentIndex != Index.Invalid) ?
                                                    ArgumentList.GetRangeAsStringList(newArguments, argumentIndex) : null;

                                                if (asynchronous)
                                                {
                                                    //
                                                    // TODO: Perhaps consider returning an opaque object handle
                                                    //       for this Thread object here?  That would make it
                                                    //       much easier for the calling script to control the
                                                    //       sub-shell.
                                                    //
                                                    Thread thread;

                                                    if (localInterpreter != null)
                                                    {
                                                        thread = ShellOps.CreateInteractiveLoopThread(
                                                            localInterpreter, new InteractiveLoopData(args),
                                                            true, ref result);
                                                    }
                                                    else
                                                    {
                                                        thread = ShellOps.CreateShellMainThread(args, true);
                                                    }

                                                    if (thread != null)
                                                    {
                                                        result = String.Empty;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = "failed to create shell thread";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    code = ResultOps.ExitCodeToReturnCode(
                                                        Interpreter.ShellMainCore(
                                                            (localInterpreter != null) ?
                                                                localInterpreter : interpreter,
                                                        null, clientData, args, initialize, loop,
                                                        ref result));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug shell ?options? ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "stack":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            bool force = false;

                                            if (newArguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    newArguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref force, ref result);

#if NATIVE
                                            if ((code == ReturnCode.Ok) && force)
                                            {
                                                /* NO RESULT */
                                                RuntimeOps.RefreshNativeStackPointers(true);

                                                code = RuntimeOps.CheckForStackSpace(
                                                    interpreter);

                                                if (code != ReturnCode.Ok)
                                                    result = "stack check failed";
                                            }
#endif

                                            if (code == ReturnCode.Ok)
                                            {
                                                UIntPtr used = UIntPtr.Zero;
                                                UIntPtr allocated = UIntPtr.Zero;
                                                UIntPtr extra = UIntPtr.Zero;
                                                UIntPtr margin = UIntPtr.Zero;
                                                UIntPtr maximum = UIntPtr.Zero;
                                                UIntPtr reserve = UIntPtr.Zero;
                                                UIntPtr commit = UIntPtr.Zero;

                                                code = RuntimeOps.GetStackSize(
                                                    ref used, ref allocated,
                                                    ref extra, ref margin,
                                                    ref maximum, ref reserve,
                                                    ref commit, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = StringList.MakeList("threadId",
                                                        GlobalState.GetCurrentNativeThreadId(),
                                                        "used", used, "allocated", allocated,
                                                        "extra", extra, "margin", margin,
                                                        "maximum", maximum, "reserve", reserve,
                                                        "commit", commit);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug stack ?force?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "status":
                                    {
                                        if (newArguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;
                                            bool enabled = false;
                                            HeaderFlags headerFlags = HeaderFlags.None;
                                            Interpreter debugInterpreter = null;

                                            /* IGNORED */
                                            Engine.CheckDebugger(interpreter, true, ref debugger,
                                                ref enabled, ref headerFlags, ref debugInterpreter);

                                            StringList list = new StringList();

                                            if (debugger != null)
                                                list.Add("debugger available");
                                            else
                                                list.Add("debugger not available");

                                            if ((debugger != null) && enabled)
                                                list.Add("debugger enabled");
                                            else
                                                list.Add("debugger not enabled");

                                            list.Add(String.Format(
                                                "header flags are \"{0}\"",
                                                headerFlags));

                                            if (debugInterpreter != null)
                                                list.Add("debugger interpreter available");
                                            else
                                                list.Add("debugger interpreter not available");

                                            result = list;
                                            code = ReturnCode.Ok;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug status\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "step":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.SingleStep;

                                                if (newArguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        newArguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (interpreter.InternalInteractive)
                                                    {
                                                        debugger.SingleStep = enabled;

                                                        IInteractiveHost interactiveHost = interpreter.Host;

                                                        if (interactiveHost != null)
                                                            /* IGNORED */
                                                            interactiveHost.WriteResultLine(
                                                                ReturnCode.Ok, "single step " +
                                                                ConversionOps.ToEnabled(debugger.SingleStep));

                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "cannot {0} single step",
                                                            ConversionOps.ToEnable(enabled));

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug step ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "steps":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (newArguments.Count == 3)
                                                {
                                                    long steps = 0;

                                                    code = Value.GetWideInteger2(
                                                        (IGetValue)newArguments[2], ValueFlags.AnyWideInteger,
                                                        interpreter.InternalCultureInfo, ref steps, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (interpreter.InternalInteractive)
                                                        {
                                                            debugger.Steps = steps;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "cannot break after {0} steps",
                                                                steps);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = debugger.Steps;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug steps ?integer?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subst":
                                    {
                                        if (newArguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nobackslashes", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, newArguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 1) == newArguments.Count))
                                                    {
                                                        SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;

                                                        if (options.IsPresent("-nobackslashes"))
                                                            substitutionFlags &= ~SubstitutionFlags.Backslashes;

                                                        if (options.IsPresent("-nocommands"))
                                                            substitutionFlags &= ~SubstitutionFlags.Commands;

                                                        if (options.IsPresent("-novariables"))
                                                            substitutionFlags &= ~SubstitutionFlags.Variables;

                                                        string name = StringList.MakeList("debug subst");

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Substitute | CallFrameFlags.Debugger);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        code = debugInterpreter.SubstituteString(
                                                            newArguments[argumentIndex], substitutionFlags, ref result);

                                                        if (code == ReturnCode.Error)
                                                        {
                                                            Engine.CopyErrorInformation(debugInterpreter, interpreter, result);

                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in debug subst script line {1})",
                                                                    Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));
                                                        }

                                                        //
                                                        // NOTE: Pop the original call frame that we pushed above and
                                                        //       any intervening scope call frames that may be leftover
                                                        //       (i.e. they were not explicitly closed).
                                                        //
                                                        /* IGNORED */
                                                        interpreter.PopScopeCallFramesAndOneMore();
                                                    }
                                                    else
                                                    {
                                                        if ((argumentIndex != Index.Invalid) &&
                                                            Option.LooksLikeOption(newArguments[argumentIndex]))
                                                        {
                                                            result = OptionDictionary.BadOption(
                                                                options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                        }
                                                        else
                                                        {
                                                            result = "wrong # args: should be \"debug subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "suspend":
                                    {
                                        if (newArguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Suspend(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = "debugger suspended";
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug suspend\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sysmemory":
                                    {
                                        if (newArguments.Count == 2)
                                        {
#if NATIVE
                                            StringList list = null;

                                            if (NativeOps.GetMemoryStatus(
                                                    ref list, ref result) == ReturnCode.Ok)
                                            {
                                                result = list;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug sysmemory\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "test":
                                    {
                                        if ((newArguments.Count >= 2) && (newArguments.Count <= 4))
                                        {
#if DEBUGGER
                                            if (newArguments.Count >= 3)
                                            {
                                                string name = newArguments[2];

                                                if (newArguments.Count >= 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        newArguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (interpreter.SetTestBreakpoint(
                                                                name, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "test \"{0}\" breakpoint is now {1}",
                                                                name, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} test \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), name, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "test \"{0}\" breakpoint is {1}",
                                                        name, ConversionOps.ToEnabled(
                                                            interpreter.HasTestBreakpoint(name)));

                                                    code = ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                code = interpreter.TestBreakpointsToString(ref result);
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug test ?name? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "testpath":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            if (newArguments.Count == 3)
                                                interpreter.TestPath = newArguments[2];

                                            result = interpreter.TestPath;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug testpath ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "token":
                                    {
                                        if ((newArguments.Count == 5) || (newArguments.Count == 6))
                                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                int startLine = 0;

                                                code = Value.GetInteger2(
                                                    (IGetValue)newArguments[3], ValueFlags.AnyInteger,
                                                    interpreter.InternalCultureInfo, ref startLine, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int endLine = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)newArguments[4], ValueFlags.AnyInteger,
                                                        interpreter.InternalCultureInfo, ref endLine, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IScriptLocation location = ScriptLocation.Create(
                                                            interpreter, newArguments[2], startLine, endLine, false);

                                                        if (newArguments.Count == 6)
                                                        {
                                                            bool enabled = false;

                                                            code = Value.GetBoolean2(
                                                                newArguments[5], ValueFlags.AnyBoolean,
                                                                interpreter.InternalCultureInfo, ref enabled, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                bool match = false;

                                                                if (enabled)
                                                                {
                                                                    code = debugger.SetBreakpoint(
                                                                        interpreter, location, ref match, ref result);
                                                                }
                                                                else
                                                                {
                                                                    code = debugger.ClearBreakpoint(
                                                                        interpreter, location, ref match, ref result);
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    result = String.Format(
                                                                        "token \"{0}\" breakpoint {1} {2}",
                                                                        location, match ? "was already" : "is now",
                                                                        ConversionOps.ToEnabled(enabled));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            bool match = false;

                                                            code = debugger.MatchBreakpoint(
                                                                interpreter, location, ref match, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Format(
                                                                    "token \"{0}\" breakpoint is {1}",
                                                                    location, ConversionOps.ToEnabled(match));
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug token fileName startLine endLine ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "trace":
                                    {
                                        if (newArguments.Count >= 2)
                                        {
                                            IOption prioritiesOption = new Option(
                                                typeof(TracePriority), OptionFlags.MustHaveEnumValue,
                                                Index.Invalid, Index.Invalid, "-priorities",
                                                new Variant(TraceOps.GetTracePriorities()));

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-noresult", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-default", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-console", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-native", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-debug", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-raw", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-log", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-resetsystem", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-resetlisteners", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-forceenabled", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-overrideenvironment", null),
                                                new Option(null, OptionFlags.MustHaveListValue,
                                                    Index.Invalid, Index.Invalid, "-enabledcategories", null),
                                                new Option(null, OptionFlags.MustHaveListValue,
                                                    Index.Invalid, Index.Invalid, "-disabledcategories", null),
                                                new Option(null, OptionFlags.MustHaveListValue,
                                                    Index.Invalid, Index.Invalid, "-penaltycategories", null),
                                                new Option(null, OptionFlags.MustHaveListValue,
                                                    Index.Invalid, Index.Invalid, "-bonuscategories", null),
                                                new Option(typeof(TraceStateType),
                                                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                                                    Index.Invalid, "-statetypes",
                                                    new Variant(TraceStateType.TraceCommand)),
                                                new Option(typeof(TracePriority),
                                                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                                                    Index.Invalid, "-priority",
                                                    new Variant(TraceOps.GetTracePriority())),
                                                prioritiesOption,
                                                new Option(null, OptionFlags.MustHaveValue,
                                                    Index.Invalid, Index.Invalid, "-category", null),
#if TEST
                                                new Option(null, OptionFlags.MustHaveValue,
                                                    Index.Invalid, Index.Invalid, "-logname", null),
                                                new Option(null, OptionFlags.MustHaveValue,
                                                    Index.Invalid, Index.Invalid, "-logfilename", null),
#else
                                                new Option(null,
                                                    OptionFlags.MustHaveValue | OptionFlags.Unsupported,
                                                    Index.Invalid, Index.Invalid, "-logname", null),
                                                new Option(null,
                                                    OptionFlags.MustHaveValue | OptionFlags.Unsupported,
                                                    Index.Invalid, Index.Invalid, "-logfilename", null),
#endif
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (newArguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, newArguments, 0, 2, Index.Invalid, true,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == newArguments.Count))
                                                {
                                                    Variant value = null;
                                                    TracePriority priority = TraceOps.GetTracePriority();

                                                    if (options.IsPresent("-priority", ref value))
                                                        priority = (TracePriority)value.Value;

                                                    TracePriority? priorities = null;

                                                    if (options.IsPresent("-priorities", ref value))
                                                        priorities = (TracePriority)value.Value;

                                                    TraceStateType stateType = TraceStateType.TraceCommand;

                                                    if (options.IsPresent("-statetypes", ref value))
                                                        stateType = (TraceStateType)value.Value;

                                                    bool overrideEnvironment = false;

                                                    if (options.IsPresent("-overrideenvironment", ref value))
                                                        overrideEnvironment = (bool)value.Value;

                                                    if (overrideEnvironment)
                                                        stateType |= TraceStateType.OverrideEnvironment;

                                                    bool noResult = false;

                                                    if (options.IsPresent("-noresult", ref value))
                                                        noResult = true;

                                                    bool? @default = null;

                                                    if (options.IsPresent("-default", ref value))
                                                        @default = (bool)value.Value;

                                                    bool? console = null;

                                                    if (options.IsPresent("-console", ref value))
                                                        console = (bool)value.Value;

                                                    bool? native = null;

                                                    if (options.IsPresent("-native", ref value))
                                                        native = (bool)value.Value;

                                                    bool debug = false;

                                                    if (options.IsPresent("-debug", ref value))
                                                        debug = (bool)value.Value;

                                                    bool raw = false;

                                                    if (options.IsPresent("-raw", ref value))
                                                        raw = (bool)value.Value;

                                                    bool? log = null;

                                                    if (options.IsPresent("-log", ref value))
                                                        log = (bool)value.Value;

                                                    bool resetSystem = false;

                                                    if (options.IsPresent("-resetsystem", ref value))
                                                        resetSystem = (bool)value.Value;

                                                    StringList enabledCategories = null;

                                                    if (options.IsPresent("-enabledcategories", ref value))
                                                        enabledCategories = (StringList)value.Value;

                                                    StringList disabledCategories = null;

                                                    if (options.IsPresent("-disabledcategories", ref value))
                                                        disabledCategories = (StringList)value.Value;

                                                    StringList penaltyCategories = null;

                                                    if (options.IsPresent("-penaltycategories", ref value))
                                                        penaltyCategories = (StringList)value.Value;

                                                    StringList bonusCategories = null;

                                                    if (options.IsPresent("-bonuscategories", ref value))
                                                        bonusCategories = (StringList)value.Value;

                                                    bool resetListeners = false;

                                                    if (options.IsPresent("-resetlisteners", ref value))
                                                        resetListeners = (bool)value.Value;

                                                    bool? forceEnabled = null;

                                                    if (options.IsPresent("-forceenabled", ref value))
                                                        forceEnabled = (bool)value.Value;

                                                    string category = DebugOps.DefaultCategory;

                                                    if (options.IsPresent("-category", ref value))
                                                        category = value.ToString();

                                                    string logName = null;

                                                    if (options.IsPresent("-logname", ref value))
                                                        logName = value.ToString();

                                                    string logFileName = null;

                                                    if (options.IsPresent("-logfilename", ref value))
                                                        logFileName = value.ToString();

                                                    if (resetSystem)
                                                    {
                                                        /* NO RESULT */
                                                        TraceOps.ResetStatus(
                                                            interpreter, overrideEnvironment);
                                                    }

                                                    TraceStateType? resultStateType = null;

                                                    if (forceEnabled != null)
                                                    {
                                                        resultStateType = TraceOps.ForceEnabledOrDisabled(
                                                            interpreter, stateType, (bool)forceEnabled);

                                                        //
                                                        // HACK: If present, must re-process "-priorities"
                                                        //       option now because the basis for its value
                                                        //       was changed by the ForceEnabledOrDisabled
                                                        //       method.
                                                        //
                                                        int nameIndex = Index.Invalid; /* NOT USED */
                                                        int valueIndex = Index.Invalid;

                                                        if (prioritiesOption.IsPresent(
                                                                options, ref nameIndex, ref valueIndex))
                                                        {
                                                            object enumValue = EnumOps.TryParseFlags(
                                                                interpreter, typeof(TracePriority),
                                                                TraceOps.GetTracePriorities().ToString(),
                                                                newArguments[valueIndex],
                                                                interpreter.InternalCultureInfo, true,
                                                                true, true, ref result);

                                                            if (enumValue is TracePriority)
                                                            {
                                                                /* NO RESULT */
                                                                TraceOps.SetTracePriorities(
                                                                    (TracePriority)enumValue);
                                                            }
                                                            else /* IMPOSSIBLE? */
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else if (priorities != null)
                                                    {
                                                        /* NO RESULT */
                                                        TraceOps.SetTracePriorities(
                                                            (TracePriority)priorities);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (enabledCategories != null)
                                                        {
                                                            /* NO RESULT */
                                                            TraceOps.SetTraceCategories(
                                                                TraceCategoryType.Enabled, enabledCategories, 1);
                                                        }

                                                        if (disabledCategories != null)
                                                        {
                                                            /* NO RESULT */
                                                            TraceOps.SetTraceCategories(
                                                                TraceCategoryType.Disabled, disabledCategories, 1);
                                                        }

                                                        if (penaltyCategories != null)
                                                        {
                                                            /* NO RESULT */
                                                            TraceOps.SetTraceCategories(
                                                                TraceCategoryType.Penalty, penaltyCategories, 1);
                                                        }

                                                        if (bonusCategories != null)
                                                        {
                                                            /* NO RESULT */
                                                            TraceOps.SetTraceCategories(
                                                                TraceCategoryType.Bonus, bonusCategories, 1);
                                                        }

                                                        TraceListenerCollection listeners; /* REUSED */

#if TEST
                                                        TraceListener listener; /* REUSED */
#endif

                                                        bool useDefault = (@default != null) && (bool)@default;
                                                        bool useConsole = (console != null) && (bool)console;
                                                        bool useNative = (native != null) && (bool)native;

                                                        if (debug)
                                                        {
#if !NET_STANDARD_20
                                                            listeners = DebugOps.GetDebugListeners();
                                                            code = ReturnCode.Ok; /* REDUNDANT */
#else
                                                            listeners = null;
                                                            result = "not implemented";
                                                            code = ReturnCode.Error;
#endif
                                                        }
                                                        else
                                                        {
                                                            listeners = DebugOps.GetTraceListeners();
                                                            code = ReturnCode.Ok; /* REDUNDANT */
                                                        }

                                                        if ((code == ReturnCode.Ok) && resetListeners)
                                                        {
                                                            code = DebugOps.ClearTraceListeners(
                                                                listeners, debug, useConsole, false,
                                                                ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && useDefault)
                                                        {
                                                            code = DebugOps.AddTraceListener(
                                                                listeners, TraceListenerType.Default,
                                                                clientData, resetListeners, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && useConsole)
                                                        {
                                                            code = DebugOps.AddTraceListener(
                                                                listeners, TraceListenerType.Console,
                                                                clientData, resetListeners, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && useNative)
                                                        {
                                                            code = DebugOps.AddTraceListener(
                                                                listeners, TraceListenerType.Native,
                                                                clientData, resetListeners, ref result);
                                                        }

#if TEST
                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (log != null)
                                                            {
                                                                if ((bool)log)
                                                                {
                                                                    if (logFileName == null)
                                                                    {
                                                                        logFileName = DebugOps.GetTraceLogFileName(
                                                                            interpreter, logName, ref result);
                                                                    }

                                                                    if (logFileName != null)
                                                                    {
                                                                        listener = null; /* NOT USED */

                                                                        code = DebugOps.SetupTraceLogFile(
                                                                            ShellOps.GetTraceListenerName(null,
                                                                                GlobalState.GetCurrentSystemThreadId()),
                                                                            logFileName, null, !debug, debug, useConsole,
                                                                            false, false, ref listener, ref result);
                                                                    }
                                                                    else
                                                                    {
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    code = DebugOps.RemoveTraceListener(
                                                                        listeners, TraceListenerType.TestLogFile,
                                                                        true, ref result);
                                                                }
                                                            }
                                                        }
#endif

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (argumentIndex != Index.Invalid)
                                                            {
                                                                if (debug)
                                                                {
                                                                    if (raw)
                                                                    {
                                                                        /* EXEMPT */
                                                                        DebugOps.DebugWrite(
                                                                            newArguments[argumentIndex], category);
                                                                    }
                                                                    else
                                                                    {
                                                                        /* NO RESULT */
                                                                        TraceOps.DebugWriteToAlways(
                                                                            interpreter, newArguments[argumentIndex],
                                                                            true);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (raw)
                                                                    {
                                                                        /* IGNORED */
                                                                        DebugOps.TraceWrite(
                                                                            interpreter, newArguments[argumentIndex],
                                                                            category); /* EXEMPT */
                                                                    }
                                                                    else
                                                                    {
                                                                        /* NO RESULT */
                                                                        TraceOps.DebugTraceAlways(
                                                                            newArguments[argumentIndex], category,
                                                                            priority);
                                                                    }
                                                                }

                                                                if (!noResult && (resultStateType != null))
                                                                    result = resultStateType;
                                                                else
                                                                    result = String.Empty;
                                                            }
                                                            else
                                                            {
                                                                StringPairList list = null;

                                                                code = TraceOps.QueryStatus(
                                                                    interpreter, ref list, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
#if TEST
#if NATIVE
                                                                    list.Insert(0,
                                                                        new StringPair("hasNativeListener",
                                                                        DebugOps.HasNativeTraceListener(
                                                                            listeners).ToString()));
#endif

                                                                    list.Insert(0,
                                                                        new StringPair("hasBufferedListener",
                                                                        DebugOps.HasBufferedTraceListener(
                                                                            listeners).ToString()));

                                                                    list.Insert(0,
                                                                        new StringPair("hasTestListener",
                                                                        DebugOps.HasTestTraceListener(
                                                                            listeners).ToString()));
#endif

#if CONSOLE
                                                                    list.Insert(0,
                                                                        new StringPair("hasConsoleListener",
                                                                        DebugOps.HasTraceListener(
                                                                            listeners, TraceListenerType.Console,
                                                                            clientData).ToString()));
#endif

                                                                    list.Insert(0,
                                                                        new StringPair("hasDefaultListener",
                                                                        DebugOps.HasTraceListener(
                                                                            listeners, TraceListenerType.Default,
                                                                            clientData).ToString()));

                                                                    /* NO RESULT */
                                                                    TraceOps.MaybeAddResultStateType(
                                                                        resultStateType, forceEnabled,
                                                                        ref list);

                                                                    if (noResult)
                                                                        result = String.Empty;
                                                                    else
                                                                        result = list;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug trace ?options? ?message?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug trace ?options? ?message?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "types":
                                    {
                                        //
                                        // debug types ?types?
                                        //
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (newArguments.Count == 3)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        interpreter, typeof(BreakpointType),
                                                        debugger.Types.ToString(),
                                                        newArguments[2], interpreter.InternalCultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is BreakpointType)
                                                        debugger.Types = (BreakpointType)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = debugger.Types;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug types ?types?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "undelete":
                                    {
                                        if ((newArguments.Count == 2) || (newArguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (newArguments.Count == 3)
                                                pattern = newArguments[2];

                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                ICallFrame variableFrame = null;

                                                code = interpreter.GetVariableFrameViaResolvers(
                                                    LookupFlags.Default, ref variableFrame,
                                                    ref pattern, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (variableFrame != null)
                                                    {
                                                        VariableDictionary variables = variableFrame.Variables;

                                                        if (variables != null)
                                                        {
                                                            int count = variables.SetUndefined(
                                                                interpreter, pattern, false);

                                                            result = String.Format(
                                                                "undeleted {0} {1} in call frame {2}",
                                                                count, (count != 1) ? "variables" :
                                                                "variable", variableFrame.Name);
                                                        }
                                                        else
                                                        {
                                                            result = "call frame does not support variables";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid call frame";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug undelete ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "variable":
                                    {
                                        if (newArguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-searches", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-elements", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-links", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-empty", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, newArguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == newArguments.Count))
                                                {
                                                    IHost host = interpreter.Host;

                                                    if (host != null)
                                                    {
                                                        _Hosts.Default defaultHost = host as _Hosts.Default;

                                                        if (defaultHost != null)
                                                        {
                                                            DetailFlags detailFlags = DetailFlags.Default;

                                                            if (options.IsPresent("-searches"))
                                                                detailFlags |= DetailFlags.VariableSearches;

                                                            if (options.IsPresent("-elements"))
                                                                detailFlags |= DetailFlags.VariableElements;

                                                            if (options.IsPresent("-links"))
                                                                detailFlags |= DetailFlags.VariableLinks;

                                                            if (options.IsPresent("-empty"))
                                                                detailFlags |= DetailFlags.EmptyContent;

                                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                VariableFlags flags = VariableFlags.NoElement;
                                                                IVariable variable = null;

                                                                code = interpreter.GetVariableViaResolversWithSplit(
                                                                    newArguments[argumentIndex], ref flags, ref variable,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    StringPairList list = null;

                                                                    if (defaultHost.BuildLinkedVariableInfoList(
                                                                            interpreter, variable, detailFlags,
                                                                            ref list))
                                                                    {
                                                                        result = list;
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "could not introspect variable";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "interpreter host does not have variable support";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(newArguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, newArguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"debug variable ?options? varName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug variable ?options? varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vout":
                                    {
                                        if ((newArguments.Count >= 2) && (newArguments.Count <= 4))
                                        {
                                            string channelId = StandardChannel.Output;

                                            if ((newArguments.Count >= 3) &&
                                                !String.IsNullOrEmpty(newArguments[2]))
                                            {
                                                channelId = newArguments[2];
                                            }

                                            if (newArguments.Count >= 4)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    newArguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                    code = interpreter.SetChannelVirtualOutput(
                                                        channelId, enabled, ref result);
                                            }
                                            else
                                            {
                                                StringBuilder builder = null;

                                                code = interpreter.GetChannelVirtualOutput(
                                                    channelId, true, ref builder, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = builder;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug vout ?channelId? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "watch":
                                    {
                                        //
                                        // debug watch ?varName? ?types?
                                        //
                                        //       Also, new syntax will be either 2 or 3 newArguments exactly
                                        //       (just like "debug types").
                                        //
                                        if ((newArguments.Count >= 2) && (newArguments.Count <= 4))
                                        {
                                            //
                                            // BUGFIX: The debugger is not required to setup variable watches;
                                            //         however, they will not actually fire if the debugger is
                                            //         not available.
                                            //
                                            if (newArguments.Count == 2)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    //
                                                    // NOTE: Return a list of all watched variables for
                                                    //       the current call frame.
                                                    //
                                                    ICallFrame variableFrame = null;
                                                    VariableFlags flags = VariableFlags.None;

                                                    if (interpreter.GetVariableFrameViaResolvers(
                                                            LookupFlags.Default, ref variableFrame,
                                                            ref flags, ref result) == ReturnCode.Ok)
                                                    {
                                                        if (variableFrame != null)
                                                        {
                                                            VariableDictionary variables = variableFrame.Variables;

                                                            if (variables != null)
                                                                result = variables.GetWatchpoints();
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
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    VariableFlags flags = VariableFlags.NoElement;
                                                    IVariable variable = null;

                                                    code = interpreter.GetVariableViaResolversWithSplit(
                                                        newArguments[2], ref flags, ref variable, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Result linkError = null;

                                                        if (EntityOps.IsLink(variable))
                                                            variable = EntityOps.FollowLinks(variable, flags, ref linkError);

                                                        if (variable != null)
                                                        {
                                                            if (newArguments.Count == 4)
                                                            {
                                                                object enumValue = EnumOps.TryParseFlags(
                                                                    interpreter, typeof(VariableFlags),
                                                                    EntityOps.GetWatchpointFlags(variable.Flags).ToString(),
                                                                    newArguments[3], interpreter.InternalCultureInfo, true, true, true,
                                                                    ref result);

                                                                if (enumValue is VariableFlags)
                                                                {
                                                                    VariableFlags watchFlags = (VariableFlags)enumValue;

                                                                    //
                                                                    // NOTE: Next, reset all the watch related variable
                                                                    //       flags for this variable, masking off any
                                                                    //       variable flags that are not watch related
                                                                    //       from the newly supplied variable flags.
                                                                    //
                                                                    variable.Flags = EntityOps.SetWatchpointFlags(
                                                                        variable.Flags, watchFlags);
                                                                }
                                                                else
                                                                {
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                //
                                                                // NOTE: Finally, return the [potentially changed]
                                                                //       watch flags on the variable.
                                                                //
                                                                StringList list = new StringList();

                                                                if (EntityOps.IsBreakOnGet(variable))
                                                                    list.Add(VariableFlags.BreakOnGet.ToString());

                                                                if (EntityOps.IsBreakOnSet(variable))
                                                                    list.Add(VariableFlags.BreakOnSet.ToString());

                                                                if (EntityOps.IsBreakOnUnset(variable))
                                                                    list.Add(VariableFlags.BreakOnUnset.ToString());

                                                                if (EntityOps.IsMutable(variable))
                                                                    list.Add(VariableFlags.Mutable.ToString());

                                                                if (list.Count == 0)
                                                                    list.Add(VariableFlags.None.ToString());

                                                                result = list;
                                                            }
                                                        }
                                                        else if (linkError != null)
                                                        {
                                                            result = linkError;
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            result = "invalid variable";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug watch ?varName? ?types?\"";
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
                        result = "wrong # args: should be \"debug option ?arg ...?\"";
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
