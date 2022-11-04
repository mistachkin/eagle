/*
 * Namespace2.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("3f2b8b9a-c7c6-4eae-b88b-2a99b3c97591")]
    [CommandFlags(
        CommandFlags.NoAdd | CommandFlags.Safe |
        CommandFlags.Standard | CommandFlags.Initialize)]
    [ObjectGroup("scriptEnvironment")]
    [ObjectName("namespace")]
    internal sealed class Namespace2 : Core
    {
        #region Private Data
        //
        // HACK: This is purposely not read-only to allow for ad-hoc
        //       "customization" (i.e. via a script using something
        //       like [object invoke -flags +NonPublic]).
        //
        private bool RenameGlobalOk = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only to allow for ad-hoc
        //       customization (i.e. via a script using something
        //       like [object invoke -flags +NonPublic]).
        //
        private bool RenameInUseOk = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public Namespace2(
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

                                            string pattern = null;

                                            if (arguments.Count >= 4)
                                                pattern = arguments[3];

                                            IEnumerable<INamespace> children = NamespaceOps.Children(
                                                interpreter, name, pattern, false, ref result);

                                            if (children != null)
                                            {
                                                StringList list = new StringList();

                                                foreach (INamespace child in children)
                                                    list.Add(child.QualifiedName);

                                                result = list;
                                            }
                                            else
                                            {
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
                                            string text = arguments[2];

                                            if (!NamespaceOps.IsSubCommand(interpreter, text, "inscope"))
                                            {
                                                INamespace currentNamespace = null;

                                                code = interpreter.GetCurrentNamespaceViaResolvers(
                                                    null, LookupFlags.Default, ref currentNamespace,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    StringList list = new StringList();

                                                    list.Add(NamespaceOps.MakeAbsoluteName(
                                                        this.Name));

                                                    list.Add("inscope");

                                                    list.Add(NamespaceOps.MakeAbsoluteName(
                                                        currentNamespace.QualifiedName));

                                                    list.Add(text);

                                                    result = list;
                                                }
                                            }
                                            else
                                            {
                                                result = text; /* COMPAT: Tcl. */
                                            }
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
                                            INamespace currentNamespace = null;

                                            code = interpreter.GetCurrentNamespaceViaResolvers(
                                                null, LookupFlags.Default, ref currentNamespace,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (currentNamespace != null)
                                                {
                                                    result = currentNamespace.QualifiedName;
                                                }
                                                else
                                                {
                                                    result = "current namespace is invalid";
                                                    code = ReturnCode.Error;
                                                }
                                            }
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
                                            for (int index = 2; index < arguments.Count; index++)
                                            {
                                                code = interpreter.DeleteNamespace(
                                                    VariableFlags.None, arguments[index], false,
                                                    ref result);

                                                if (code != ReturnCode.Ok)
                                                    break;
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

                                            string pattern = null;

                                            if (arguments.Count >= 4)
                                                pattern = arguments[3];

                                            IEnumerable<INamespace> descendants = NamespaceOps.Descendants(
                                                interpreter, name, pattern, false, ref result);

                                            if (descendants != null)
                                            {
                                                StringList list = new StringList();

                                                foreach (INamespace descendant in descendants)
                                                    list.Add(descendant.QualifiedName);

                                                result = list;
                                            }
                                            else
                                            {
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
                                            string namespaceName = NamespaceOps.MapName(
                                                interpreter, arguments[2]);

                                            INamespace @namespace = NamespaceOps.Lookup(
                                                interpreter, namespaceName, false, true,
                                                ref result);

                                            if (@namespace != null)
                                            {
                                                string name = StringList.MakeList("namespace eval", @namespace.QualifiedName);

                                                ICallFrame frame = interpreter.NewNamespaceCallFrame(
                                                    name, CallFrameFlags.Evaluate | CallFrameFlags.UseNamespace,
                                                    arguments, @namespace, false);

                                                interpreter.PushNamespaceCallFrame(frame);

                                                if (arguments.Count == 4)
                                                    code = interpreter.EvaluateScript(arguments[3], ref result);
                                                else
                                                    code = interpreter.EvaluateScript(arguments, 3, ref result);

                                                if (code == ReturnCode.Error)
                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in namespace eval \"{1}\" script line {2})",
                                                            Environment.NewLine, NamespaceOps.MaybeQualifiedName(@namespace,
                                                            true), Interpreter.GetErrorLine(interpreter)));

                                                /* IGNORED */
                                                interpreter.PopNamespaceCallFrame(frame);

                                                /* NO RESULT */
                                                Engine.CleanupNamespacesOrComplain(interpreter);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
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
                                            result = ConversionOps.ToInt(NamespaceOps.Lookup(
                                                interpreter, arguments[2], false, false) != null);
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

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool clear = false;

                                                if (options.IsPresent("-clear"))
                                                    clear = true;

                                                StringList patterns = new StringList();

                                                if (argumentIndex != Index.Invalid)
                                                    patterns.AddObjects(ArgumentList.GetRange(arguments, argumentIndex));

                                                code = NamespaceOps.Export(interpreter, null, patterns, clear, ref result);
                                            }
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
                                            if (arguments.Count >= 3)
                                            {
                                                code = NamespaceOps.Forget(interpreter,
                                                    new StringList(ArgumentList.GetRange(arguments, 2)),
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
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

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool force = false;

                                                if (options.IsPresent("-force"))
                                                    force = true;

                                                StringList patterns = new StringList();

                                                if (argumentIndex != Index.Invalid)
                                                    patterns.AddObjects(ArgumentList.GetRange(arguments, argumentIndex));

                                                code = NamespaceOps.Import(interpreter, patterns, force, ref result);
                                            }
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
                                            string namespaceName = NamespaceOps.MapName(
                                                interpreter, arguments[2]);

                                            INamespace @namespace = NamespaceOps.Lookup(
                                                interpreter, namespaceName, false, false,
                                                ref result);

                                            if (@namespace != null)
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
                                                        string name = StringList.MakeList("namespace inscope", @namespace.QualifiedName);

                                                        ICallFrame frame = interpreter.NewNamespaceCallFrame(
                                                            name, CallFrameFlags.InScope | CallFrameFlags.UseNamespace,
                                                            arguments, @namespace, false);

                                                        interpreter.PushNamespaceCallFrame(frame);

                                                        StringList list = new StringList(arguments, 4);

                                                        code = interpreter.EvaluateScript(
                                                            ListOps.Concat(arguments[3], list.ToString()),
                                                            location, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in namespace inscope \"{1}\" script line {2})",
                                                                    Environment.NewLine, NamespaceOps.MaybeQualifiedName(@namespace,
                                                                    true), Interpreter.GetErrorLine(interpreter)));

                                                        /* IGNORED */
                                                        interpreter.PopNamespaceCallFrame(frame);

                                                        /* NO RESULT */
                                                        Engine.CleanupNamespacesOrComplain(interpreter);
                                                    }
                                                }
                                                else
                                                {
                                                    string name = StringList.MakeList("namespace inscope", @namespace.QualifiedName);

                                                    ICallFrame frame = interpreter.NewNamespaceCallFrame(
                                                        name, CallFrameFlags.InScope | CallFrameFlags.UseNamespace,
                                                        arguments, @namespace, false);

                                                    interpreter.PushNamespaceCallFrame(frame);

                                                    code = interpreter.EvaluateScript(arguments[3], ref result);

                                                    if (code == ReturnCode.Error)
                                                        Engine.AddErrorInformation(interpreter, result,
                                                            String.Format("{0}    (in namespace inscope \"{1}\" script line {2})",
                                                                Environment.NewLine, NamespaceOps.MaybeQualifiedName(@namespace,
                                                                true), Interpreter.GetErrorLine(interpreter)));

                                                    /* IGNORED */
                                                    interpreter.PopNamespaceCallFrame(frame);

                                                    /* NO RESULT */
                                                    Engine.CleanupNamespacesOrComplain(interpreter);
                                                }
                                            }
                                            else
                                            {
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
                                            string name = arguments[2];

                                            if (!NamespaceOps.IsQualifiedName(name))
                                            {
                                                result = NamespaceOps.MakeQualifiedName(interpreter, name, true);
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
                                            code = NamespaceOps.Origin(interpreter, null, arguments[2], ref result);
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
                                            code = NamespaceOps.Parent(
                                                interpreter, (arguments.Count == 3) ? arguments[2] : null,
                                                ref result);
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
                                            code = interpreter.RenameNamespace(
                                                arguments[2], arguments[3], RenameGlobalOk,
                                                RenameInUseOk, ref result);
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
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            INamespace currentNamespace = null;

                                            code = interpreter.GetCurrentNamespaceViaResolvers(
                                                null, LookupFlags.Default, ref currentNamespace,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (currentNamespace != null)
                                                {
                                                    if (arguments.Count == 3)
                                                    {
                                                        string unknown = StringOps.NullIfEmpty(arguments[2]);

                                                        if (String.IsNullOrEmpty(unknown) &&
                                                            NamespaceOps.IsGlobal(interpreter, currentNamespace))
                                                        {
                                                            currentNamespace.Unknown = interpreter.GlobalUnknown;
                                                        }
                                                        else
                                                        {
                                                            currentNamespace.Unknown = unknown;
                                                        }

                                                        result = unknown;
                                                    }
                                                    else
                                                    {
                                                        result = currentNamespace.Unknown;
                                                    }
                                                }
                                                else
                                                {
                                                    if (arguments.Count == 3)
                                                    {
                                                        string unknown = StringOps.NullIfEmpty(arguments[2]);

                                                        if (String.IsNullOrEmpty(unknown))
                                                            interpreter.GlobalUnknown = TclVars.Command.Unknown;
                                                        else
                                                            interpreter.GlobalUnknown = unknown;

                                                        result = unknown;
                                                    }
                                                    else
                                                    {
                                                        result = interpreter.GlobalUnknown;
                                                    }
                                                }
                                            }
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
                                                        NamespaceFlags flags = NamespaceFlags.None;

                                                        if (isCommand)
                                                            flags |= NamespaceFlags.Command;
                                                        else if (isVariable)
                                                            flags |= NamespaceFlags.Variable;
                                                        else
                                                            flags |= NamespaceFlags.Command;

                                                        code = NamespaceOps.Which(interpreter, null, name, flags, ref result);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"namespace which ?-command? ?-variable? name\"";
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
