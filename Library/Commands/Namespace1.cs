/*
 * Namespace1.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("dbf1b8e2-0eb9-4246-ba2c-cfef01861d1d")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("scriptEnvironment")]
    [ObjectName("namespace")]
    internal sealed class Namespace1 : Core
    {
        public Namespace1(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "children", "code", "current", "delete", "descendants",
            "enable", "eval", "exists", "export", "forget", "import",
            "info", "inscope", "mappings", "name", "origin",
            "parent", "qualifiers", "rename", "tail", "unknown",
            "which"
        });

        ///////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;

            //
            // NOTE: *WARNING* We do NOT actually support namespaces.  This
            //       command exists for the sole purpose of improving source
            //       code compatibility with simple stand alone scripts that
            //       may simply wrap themselves in a "namespace eval" block,
            //       etc.  Any other (more involved) use may not work at all
            //       or may cause undesired and/or unpredictable results.
            //
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
                                case "children":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string name = null;

                                            if (arguments.Count >= 3)
                                                name = arguments[2];

                                            if ((arguments.Count < 3) ||
                                                NamespaceOps.IsGlobalName(name))
                                            {
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                if (NamespaceOps.IsAbsoluteName(name))
                                                    result = String.Format("namespace \"{0}\" not found", name);
                                                else
                                                    result = String.Format("namespace \"{0}\" not found in \"{1}\"",
                                                        name, TclVars.Namespace.Global);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace children ?name? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "code":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            //
                                            // NOTE: We are always in the global namespace, fake it.
                                            //
                                            result = new StringList(
                                                TclVars.Namespace.Global + this.Name, "inscope",
                                                TclVars.Namespace.Global, arguments[2]);

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace code script\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "current":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = TclVars.Namespace.Global;
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace current\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "delete":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            code = ReturnCode.Ok;

                                            for (int index = 2; index < arguments.Count; index++)
                                            {
                                                if (!NamespaceOps.IsGlobalName(arguments[index]))
                                                {
                                                    //
                                                    // NOTE: We only know about the global namespace; an attempt
                                                    //       to delete any other namespace is an error.
                                                    //
                                                    result = String.Format(
                                                        "unknown namespace \"{0}\" in namespace delete command",
                                                        arguments[index]);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                for (int index = 2; index < arguments.Count; /* index++ */)
                                                {
                                                    //
                                                    // NOTE: Delete the one-and-only namespace (global) now (actually,
                                                    //       as soon as the evaluation stack unwinds).
                                                    //
                                                    code = interpreter.DeleteNamespace(
                                                        VariableFlags.None, arguments[index], false, ref result);

                                                    //
                                                    // NOTE: Since we only know about the global namespace there
                                                    //       is no point in attempting to delete it multiple times.
                                                    //
                                                    break;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace delete ?name name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "descendants":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string name = null;

                                            if (arguments.Count >= 3)
                                                name = arguments[2];

                                            if ((arguments.Count < 3) ||
                                                NamespaceOps.IsGlobalName(name))
                                            {
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                if (NamespaceOps.IsAbsoluteName(name))
                                                    result = String.Format("namespace \"{0}\" not found", name);
                                                else
                                                    result = String.Format("namespace \"{0}\" not found in \"{1}\"",
                                                        name, TclVars.Namespace.Global);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace descendants ?name? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enable":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            if (arguments.Count >= 3)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo, ref enabled, ref result);

                                                bool force = false;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref force, ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = NamespaceOps.Enable(
                                                        interpreter, clientData, enabled, force, ref result);
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.AreNamespacesEnabled();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace enable ?enabled? ?force?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string name = StringList.MakeList("namespace eval", arguments[2]);

                                            ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                CallFrameFlags.Namespace | CallFrameFlags.Evaluate);

                                            interpreter.PushAutomaticCallFrame(frame);

                                            if (arguments.Count == 4)
                                                code = interpreter.EvaluateScript(arguments[3], ref result);
                                            else
                                                code = interpreter.EvaluateScript(arguments, 3, ref result);

                                            if (code == ReturnCode.Error)
                                                Engine.AddErrorInformation(interpreter, result,
                                                    String.Format("{0}    (in namespace eval \"{1}\" script line {2})",
                                                        Environment.NewLine, arguments[2], Interpreter.GetErrorLine(interpreter)));

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
                                            result = "wrong # args: should be \"namespace eval name arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            if (NamespaceOps.IsGlobalName(arguments[2]))
                                                result = true;
                                            else
                                                result = false;

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace exists name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "export":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-clear", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            //
                                            // NOTE: We do not support importing or exporting of namespace commands
                                            //       because we do not really support namespaces; therefore, we do
                                            //       nothing.
                                            //
                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace export ?-clear? ?pattern pattern ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "forget":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            //
                                            // NOTE: We do not support importing or exporting of namespace commands
                                            //       because we do not really support namespaces; therefore, we do
                                            //       nothing.
                                            //
                                            result = String.Empty;
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace forget ?pattern pattern ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "import":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            //
                                            // NOTE: We do not support importing or exporting of namespace commands
                                            //       because we do not really support namespaces; therefore, we do
                                            //       nothing.
                                            //
                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace import ?-force? ?pattern pattern ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "info":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = NamespaceOps.InfoSubCommand(
                                                interpreter, arguments[2], ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace info name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "inscope":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            if (NamespaceOps.IsGlobalName(arguments[2]))
                                            {
                                                if (arguments.Count > 4)
                                                {
                                                    IScriptLocation location = null;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                                    code = ScriptOps.GetLocation(
                                                        interpreter, arguments, 3, ref location,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
#endif
                                                    {
                                                        string name = StringList.MakeList("namespace inscope", arguments[2]);

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Namespace | CallFrameFlags.InScope);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        StringList list = new StringList(arguments, 4);

                                                        code = interpreter.EvaluateScript(
                                                            ListOps.Concat(arguments[3], list.ToString()),
                                                            location, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in namespace inscope \"{1}\" script line {2})",
                                                                    Environment.NewLine, arguments[2], Interpreter.GetErrorLine(interpreter)));

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
                                                    string name = StringList.MakeList("namespace inscope", arguments[2]);

                                                    ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                        CallFrameFlags.Namespace | CallFrameFlags.InScope);

                                                    interpreter.PushAutomaticCallFrame(frame);

                                                    code = interpreter.EvaluateScript(arguments[3], ref result);

                                                    if (code == ReturnCode.Error)
                                                        Engine.AddErrorInformation(interpreter, result,
                                                            String.Format("{0}    (in namespace inscope \"{1}\" script line {2})",
                                                                Environment.NewLine, arguments[2], Interpreter.GetErrorLine(interpreter)));

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
                                                result = String.Format(
                                                    "unknown namespace \"{0}\" in inscope namespace command",
                                                    arguments[2]);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace inscope name arg ?arg...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "mappings":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                StringDictionary namespaceMappings = interpreter.NamespaceMappings;

                                                if (namespaceMappings != null)
                                                {
                                                    result = namespaceMappings.KeysAndValuesToString(null, false);
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = "namespace mappings not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace mappings\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "name":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            //
                                            // NOTE: We are always in the global namespace, fake it.
                                            //
                                            string name = arguments[2];

                                            if (!NamespaceOps.IsQualifiedName(name))
                                            {
                                                result = NamespaceOps.MakeAbsoluteName(name);
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = "only non-qualified names are allowed";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace name name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "origin":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string executeName = arguments[2];

                                            code = interpreter.DoesIExecuteExistViaResolvers(
                                                executeName, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = TclVars.Namespace.Global + executeName;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace origin name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "parent":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            //
                                            // NOTE: Either they did not specify a namespace argument (use current
                                            //       namespace, which is always global and always exists) or they
                                            //       specified a namespace which should be the global one; otherwise,
                                            //       an error is reported because we do not really support namespaces.
                                            //
                                            if ((arguments.Count == 2) ||
                                                NamespaceOps.IsGlobalName(arguments[2]))
                                            {
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: See if they prefixed the argument with "::" to figure out
                                                //       the appropriate error message (Tcl emulation).
                                                //
                                                if (NamespaceOps.IsAbsoluteName(arguments[2]))
                                                    result = String.Format("namespace \"{0}\" not found", arguments[2]);
                                                else
                                                    result = String.Format("namespace \"{0}\" not found in \"{1}\"",
                                                        arguments[2], TclVars.Namespace.Global);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace parent ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "qualifiers":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string qualifiers = null;
                                            string tail = null;

                                            code = NamespaceOps.SplitName(
                                                arguments[2], ref qualifiers, ref tail, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = qualifiers;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace qualifiers string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rename":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            result = "not implemented";
                                            code = ReturnCode.Error;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace rename oldName newName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "tail":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string qualifiers = null;
                                            string tail = null;

                                            code = NamespaceOps.SplitName(
                                                arguments[2], ref qualifiers, ref tail, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = tail;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace tail string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unknown":
                                    {
                                        //
                                        // NOTE: The string is currently used as the name of the command
                                        //       or procedure to execute when an unknown command is
                                        //       encountered by the engine.
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                string unknown = StringOps.NullIfEmpty(arguments[2]);

                                                if (String.IsNullOrEmpty(unknown))
                                                    interpreter.NamespaceUnknown = interpreter.GlobalUnknown;
                                                else
                                                    interpreter.NamespaceUnknown = unknown;

                                                result = unknown;
                                            }
                                            else
                                            {
                                                result = interpreter.NamespaceUnknown;
                                            }

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace unknown ?script?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "which":
                                    {
                                        //
                                        // TODO: *FIXME* Only the global namespace is supported here.
                                        //
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-command", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-variable", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, OptionBehaviorFlags.LastIsNonOption, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    string name = arguments[argumentIndex];

                                                    bool isCommand = false;

                                                    if (options.IsPresent("-command"))
                                                        isCommand = true;

                                                    bool isVariable = false;

                                                    if (options.IsPresent("-variable"))
                                                        isVariable = true;

                                                    if (!isCommand || !isVariable)
                                                    {
                                                        if (!isCommand && !isVariable)
                                                            isCommand = true;

                                                        if (isCommand)
                                                        {
                                                            code = interpreter.DoesIExecuteExistViaResolvers(
                                                                name, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = TclVars.Namespace.Global +
                                                                    ScriptOps.MakeCommandName(name);
                                                            }
                                                            else
                                                            {
                                                                result = String.Empty;
                                                                code = ReturnCode.Ok;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            VariableFlags flags = VariableFlags.NamespaceWhichMask;
                                                            IVariable variable = null;

                                                            code = interpreter.GetVariableViaResolversWithSplit(
                                                                name, ref flags, ref variable, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = TclVars.Namespace.Global +
                                                                    ScriptOps.MakeVariableName(name);
                                                            }
                                                            else
                                                            {
                                                                result = String.Empty;
                                                                code = ReturnCode.Ok;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"namespace which ?-command? ?-variable? name\""; /* COMPAT: Tcl */
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
                                                        result = "wrong # args: should be \"namespace which ?-command? ?-variable? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"namespace which ?-command? ?-variable? name\"";
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
                        result = "wrong # args: should be \"namespace subcommand ?arg ...?\"";
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
