/*
 * Scope.cs --
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
    [ObjectId("39023a46-960b-48bc-9139-55d6a2416f50")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("variable")]
    internal sealed class Scope : Core
    {
        #region Public Constructors
        public Scope(
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
            "close", "create", "current", "destroy", "eval",
            "exists", "global", "list", "lock", "open", "set",
            "unlock", "unset", "update", "vars"
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
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool all = false;

                                                    if (options.IsPresent("-all"))
                                                        all = true;

                                                    string name = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        name = arguments[argumentIndex];

                                                    if (interpreter.HasScopes(ref result))
                                                    {
                                                        ICallFrame frame = null;

                                                        if (all)
                                                        {
                                                            /* IGNORED */
                                                            interpreter.PopScopeCallFrames(ref frame);

                                                            if (frame == null)
                                                            {
                                                                result = "no scopes are open";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.GetScopeCallFrame(
                                                                name, LookupFlags.Default, true, false,
                                                                ref frame, ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                            result = frame.Name;
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
                                                        result = "wrong # args: should be \"scope close ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope close ?options? ?name?\"";
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
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-args", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-clone", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-byref", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-global", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-open", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-procedure", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-shared", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-strict", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-fast", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool args = false;

                                                    if (options.IsPresent("-args"))
                                                        args = true;

                                                    bool clone = false;

                                                    if (options.IsPresent("-clone"))
                                                        clone = true;

                                                    bool byRef = false;

                                                    if (options.IsPresent("-byref"))
                                                        byRef = true;

                                                    bool global = false;

                                                    if (options.IsPresent("-global"))
                                                        global = true;

                                                    bool open = false;

                                                    if (options.IsPresent("-open"))
                                                        open = true;

                                                    bool procedure = false;

                                                    if (options.IsPresent("-procedure"))
                                                        procedure = true;

                                                    bool shared = false;

                                                    if (options.IsPresent("-shared"))
                                                        shared = true;

                                                    bool strict = false;

                                                    if (options.IsPresent("-strict"))
                                                        strict = true;

                                                    bool fast = false;

                                                    if (options.IsPresent("-fast"))
                                                        fast = true;

                                                    string name = null;

                                                    if (procedure)
                                                    {
                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            result = "cannot specify scope name with -procedure";
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                ICallFrame variableFrame = null;

                                                                code = interpreter.GetVariableFrameViaResolvers(
                                                                    LookupFlags.Default, ref variableFrame, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (CallFrameOps.IsProcedure(variableFrame))
                                                                    {
                                                                        name = CallFrameOps.GetAutomaticScopeName(
                                                                            variableFrame, shared);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "no procedure frame available";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Use the name specified by the caller.
                                                        //
                                                        name = arguments[argumentIndex];
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Use an automatically generated name.
                                                        //
                                                        name = CallFrameOps.GetAutomaticScopeName(interpreter);
                                                    }

                                                    if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(name))
                                                    {
                                                        bool created = false;
                                                        ICallFrame frame = null;

                                                        if (interpreter.GetScope(
                                                                name, LookupFlags.NoVerbose, ref frame) != ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: We intend to modify the interpreter state,
                                                            //       make sure this is not forbidden.
                                                            //
                                                            if (interpreter.IsModifiable(true, ref result))
                                                            {
                                                                //
                                                                // NOTE: Make sure that the scopes collection is available.
                                                                //
                                                                if (interpreter.HasScopes(ref result))
                                                                {
                                                                    ICallFrame newFrame = null;
                                                                    VariableDictionary newVariables = null;

                                                                    //
                                                                    // NOTE: Clone the variables from the current call frame
                                                                    //       or start with no variables?
                                                                    //
                                                                    if (clone)
                                                                    {
                                                                        code = CallFrameOps.CloneToNewScope(interpreter,
                                                                            name, global, byRef, ref newVariables,
                                                                            ref newFrame, ref created, ref result);
                                                                    }
                                                                    else
                                                                    {
                                                                        newVariables = new VariableDictionary();
                                                                    }

                                                                    //
                                                                    // NOTE: Make sure the frame resolution and/or cloing above was
                                                                    //       successful.
                                                                    //
                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        //
                                                                        // NOTE: Create the new scope call frame with the variables
                                                                        //       and an empty list of arguments (which are provided
                                                                        //       separately).
                                                                        //
                                                                        // BUGFIX: This is not done if the new scope call frame was
                                                                        //         already created above during cloning; when that
                                                                        //         happens, just use the already created scope call
                                                                        //         frame.
                                                                        //
                                                                        if (newFrame != null)
                                                                        {
                                                                            frame = newFrame;
                                                                        }
                                                                        else
                                                                        {
                                                                            frame = interpreter.NewScopeCallFrame(
                                                                                name, CallFrameFlags.Scope, newVariables,
                                                                                new ArgumentList());

                                                                            created = true;
                                                                        }

                                                                        //
                                                                        // BUGFIX: First, add the scope to the interpreter.  This
                                                                        //         must be done prior to (possibly) copying any
                                                                        //         procedure arguments to it, just in case any of
                                                                        //         the variable (set) operations fail and cleanup
                                                                        //         of the frame and its contents is required (i.e.
                                                                        //         because it may contain opaque object handle
                                                                        //         references, etc).
                                                                        //
                                                                        code = interpreter.AddScope(frame, clientData, ref result);

                                                                        if ((code == ReturnCode.Ok) && args)
                                                                        {
                                                                            //
                                                                            // NOTE: Setup arguments in scope call frame based on
                                                                            //       those in enclosing procedure frame, if any.
                                                                            //
                                                                            code = interpreter.CopyProcedureArgumentsToFrame(
                                                                                frame, true, ref result);

                                                                            if (code != ReturnCode.Ok)
                                                                            {
                                                                                //
                                                                                // NOTE: Somehow, copying the arguments to the new
                                                                                //       frame failed, so clean it up.
                                                                                //
                                                                                ReturnCode removeCode;
                                                                                Result removeResult = null;

                                                                                removeCode = interpreter.RemoveScope(
                                                                                    name, clientData, ref removeResult);

                                                                                if (removeCode != ReturnCode.Ok)
                                                                                {
                                                                                    DebugOps.Complain(
                                                                                        interpreter, removeCode, removeResult);
                                                                                }
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
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (strict)
                                                        {
                                                            result = String.Format(
                                                                "scope named {0} already exists",
                                                                FormatOps.DisplayName(name));

                                                            code = ReturnCode.Error;
                                                        }
                                                        else if (args)
                                                        {
                                                            //
                                                            // NOTE: Sync up the arguments in this scope call frame
                                                            //       with those in the enclosing procedure frame,
                                                            //       if any.
                                                            //
                                                            code = interpreter.CopyProcedureArgumentsToFrame(
                                                                frame, true, ref result);
                                                        }

                                                        //
                                                        // NOTE: If we succeeded at creating or fetching the scope,
                                                        //       continue.
                                                        //
                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: If required, make the named [possibly new] scope
                                                            //       the current scope.
                                                            //
                                                            if (open)
                                                                interpreter.PushCallFrame(frame);

                                                            //
                                                            // NOTE: Enable or disable "fast" local variable access
                                                            //       for the new scope.
                                                            //
                                                            if (created)
                                                                CallFrameOps.SetFast(frame, fast);

                                                            //
                                                            // NOTE: Return the newly created scope name.
                                                            //
                                                            result = frame.Name;
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Ok)
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope create ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope create ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "current":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                ICallFrame frame = null;

                                                code = interpreter.GetScopeCallFrame(
                                                    null, LookupFlags.Default, false, false,
                                                    ref frame, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = frame.Name;
                                                }
                                                else
                                                {
                                                    result = String.Empty;
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
                                            result = "wrong # args: should be \"scope current\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "destroy":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string name = arguments[2];

                                            code = interpreter.RemoveScope(name, clientData, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope destroy name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            EventWaitFlags eventWaitFlags = interpreter.EventWaitFlags;

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(EventWaitFlags),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-eventwaitflags",
                                                    new Variant(eventWaitFlags)),
                                                new Option(null,
                                                    OptionFlags.MustHaveBooleanValue,
                                                    Index.Invalid, Index.Invalid, "-lock", null),
                                                new Option(null,
                                                    OptionFlags.MustHaveIntegerValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-timeout", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool @lock = false;

                                                    if (options.IsPresent("-lock"))
                                                        @lock = true;

                                                    Variant value = null;
                                                    int timeout = 0; /* milliseconds */

                                                    if (options.IsPresent("-timeout", ref value))
                                                        timeout = (int)value.Value;

                                                    if (interpreter.HasScopes(ref result))
                                                    {
                                                        string name = arguments[argumentIndex];

                                                        if (!String.IsNullOrEmpty(name))
                                                        {
                                                            ICallFrame unlockFrame = null;

                                                            try
                                                            {
                                                                if (!@lock || (interpreter.InternalLockScope(
                                                                        eventWaitFlags, name, timeout, null, ref unlockFrame,
                                                                        ref result) == ReturnCode.Ok))
                                                                {
                                                                    ICallFrame frame = null;

                                                                    if (unlockFrame != null)
                                                                    {
                                                                        //
                                                                        // NOTE: Use the frame we successfully locked.
                                                                        //
                                                                        frame = unlockFrame;
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // NOTE: Use the specified frame, unless locked.
                                                                        //
                                                                        code = interpreter.GetScope(
                                                                            name, LookupFlags.Default, ref frame, ref result);
                                                                    }

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        interpreter.PushAutomaticCallFrame(frame); /* scope <name> */

                                                                        try
                                                                        {
                                                                            string frameName = StringList.MakeList("scope eval", name);

                                                                            frame = interpreter.NewTrackingCallFrame(frameName,
                                                                                CallFrameFlags.Scope | CallFrameFlags.Evaluate);

                                                                            interpreter.PushAutomaticCallFrame(frame); /* scope eval */

                                                                            if (((argumentIndex + 2) == arguments.Count))
                                                                            {
                                                                                code = interpreter.EvaluateScript(
                                                                                    arguments[argumentIndex + 1], ref result);
                                                                            }
                                                                            else
                                                                            {
                                                                                code = interpreter.EvaluateScript(
                                                                                    arguments, argumentIndex + 1, ref result);
                                                                            }

                                                                            if (code == ReturnCode.Error)
                                                                            {
                                                                                Engine.AddErrorInformation(
                                                                                    interpreter, result, String.Format(
                                                                                        "{0}    (in scope eval \"{1}\" script line {2})",
                                                                                        Environment.NewLine, arguments[argumentIndex],
                                                                                        Interpreter.GetErrorLine(interpreter)));
                                                                            }
                                                                        }
                                                                        finally
                                                                        {
                                                                            //
                                                                            // NOTE: Pop the original call frame that we pushed
                                                                            //       above and any intervening scope call frames
                                                                            //       that may be leftover (i.e. they were not
                                                                            //       explicitly closed).
                                                                            //
                                                                            // BUGFIX: *SPECIAL* In this particular case [only],
                                                                            //         the original call frame WAS ALSO a scope
                                                                            //         call frame; therefore, only pop (all the)
                                                                            //         scope call frames.
                                                                            //
                                                                            /* IGNORED */
                                                                            interpreter.PopScopeCallFrames();
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                if (@lock && (unlockFrame != null))
                                                                {
                                                                    ReturnCode unlockCode;
                                                                    Result unlockError = null;

                                                                    unlockCode = interpreter.InternalUnlockScope(
                                                                        unlockFrame, ref unlockError);

                                                                    if (unlockCode != ReturnCode.Ok)
                                                                    {
                                                                        DebugOps.Complain(
                                                                            interpreter, unlockCode, unlockError);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope eval ?options? name arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope eval ?options? name arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.NoVerbose, ref frame);

                                                    if (code == ReturnCode.Ok)
                                                        result = true;
                                                    else
                                                        result = false;

                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope exists name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "global":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-unset", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-force", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, false,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool force = false;

                                                    if (options.IsPresent("-force"))
                                                        force = true;

                                                    bool unset = false;

                                                    if (options.IsPresent("-unset"))
                                                        unset = true;

                                                    if (!unset || (argumentIndex == Index.Invalid))
                                                    {
                                                        if (interpreter.IsModifiable(false, ref result))
                                                        {
                                                            if (interpreter.HasScopes(ref result))
                                                            {
                                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                                {
                                                                    if (unset)
                                                                    {
                                                                        code = interpreter.UnsetGlobalScopeCallFrame(
                                                                            !force, ref result);
                                                                    }
                                                                    else if (argumentIndex != Index.Invalid)
                                                                    {
                                                                        ICallFrame globalScopeFrame =
                                                                            interpreter.GlobalScopeFrame;

                                                                        if (force || (globalScopeFrame == null))
                                                                        {
                                                                            code = interpreter.SetGlobalScopeCallFrame(
                                                                                arguments[argumentIndex], ref result);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "global scope call frame already set";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        result = StringOps.GetStringFromObject(
                                                                            interpreter.GlobalScopeFrame);
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
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "cannot specify scope name with -unset option";
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
                                                        result = "wrong # args: should be \"scope global ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope global ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "list":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.ScopesToString(pattern, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope list ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lock":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-nocomplain", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid,
                                                false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

                                                    string name = arguments[argumentIndex];

                                                    if (!String.IsNullOrEmpty(name))
                                                    {
                                                        ICallFrame frame = null;
                                                        Result error = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame,
                                                            ref error);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (frame != null)
                                                            {
                                                                if (frame.Lock(ref result))
                                                                    result = String.Empty;
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
                                                                result = "invalid call frame";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (noComplain)
                                                        {
                                                            code = ReturnCode.Ok;
                                                        }
                                                        else
                                                        {
                                                            result = error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope lock ?options? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope lock ?options? name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "open":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-procedure", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-shared", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-args", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool args = false;

                                                    if (options.IsPresent("-args"))
                                                        args = true;

                                                    bool procedure = false;

                                                    if (options.IsPresent("-procedure"))
                                                        procedure = true;

                                                    bool shared = false;

                                                    if (options.IsPresent("-shared"))
                                                        shared = true;

                                                    string name = null;

                                                    if (procedure)
                                                    {
                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            result = "cannot specify scope name with -procedure";
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                ICallFrame variableFrame = null;

                                                                code = interpreter.GetVariableFrameViaResolvers(
                                                                    LookupFlags.Default, ref variableFrame, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (CallFrameOps.IsProcedure(variableFrame))
                                                                    {
                                                                        name = CallFrameOps.GetAutomaticScopeName(
                                                                            variableFrame, shared);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "no procedure frame available";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Use the name specified by the caller.
                                                        //
                                                        name = arguments[argumentIndex];
                                                    }
                                                    else
                                                    {
                                                        result = "must specify scope name or -procedure";
                                                        code = ReturnCode.Error;
                                                    }

                                                    if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(name))
                                                    {
                                                        ICallFrame frame = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (args)
                                                            {
                                                                code = interpreter.CopyProcedureArgumentsToFrame(
                                                                    frame, true, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                interpreter.PushCallFrame(frame);

                                                                result = String.Empty;
                                                            }
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Ok)
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope open ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope open ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.Default, ref frame, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // TRICKY: Need to get/set variables from a specific
                                                        //         call frame that is not the current one.
                                                        //
                                                        if (arguments.Count == 4)
                                                        {
                                                            code = interpreter.GetVariableValue2(VariableFlags.None,
                                                                frame, arguments[3], null, ref result, ref result);
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.SetVariableValue2(VariableFlags.None,
                                                                frame, arguments[3], null, arguments[4], null, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = arguments[4];
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope set name varName ?value?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unlock":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-nocomplain", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid,
                                                false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

                                                    string name = arguments[argumentIndex];

                                                    if (!String.IsNullOrEmpty(name))
                                                    {
                                                        ICallFrame frame = null;
                                                        Result error = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame,
                                                            ref error);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (frame != null)
                                                            {
                                                                if (frame.Unlock(ref result))
                                                                    result = String.Empty;
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
                                                                result = "invalid call frame";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (noComplain)
                                                        {
                                                            code = ReturnCode.Ok;
                                                        }
                                                        else
                                                        {
                                                            result = error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope unlock ?options? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope unlock ?options? name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.Default, ref frame, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // TRICKY: Need to unset variables from a specific
                                                        //         call frame that is not the current one.
                                                        //
                                                        code = interpreter.UnsetVariable2(VariableFlags.Purge,
                                                            frame, arguments[3], null, null, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope unset name varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "update":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-global", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid,
                                                    false, ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool global = false;

                                                    if (options.IsPresent("-global"))
                                                        global = true;

                                                    string name = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        name = arguments[argumentIndex];

                                                    ICallFrame frame = null;

                                                    if (name != null)
                                                    {
                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame,
                                                            ref result);
                                                    }
                                                    else
                                                    {
                                                        code = interpreter.GetScopeCallFrame(
                                                            null, LookupFlags.Default, false, false,
                                                            ref frame, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = CallFrameOps.CloneToExistingScope(
                                                            interpreter, frame, global, ref result);
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
                                                        result = "wrong # args: should be \"scope update ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope update ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vars":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        ICallFrame frame = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            string pattern = null;

                                                            if (arguments.Count == 4)
                                                            {
                                                                //
                                                                // NOTE: *OK* Cannot have namespaces within a scope.
                                                                //
                                                                pattern = ScriptOps.MakeVariableName(arguments[3]);
                                                            }

                                                            if (frame != null)
                                                            {
                                                                VariableDictionary variables = frame.Variables;

                                                                if (variables != null)
                                                                    result = variables.GetDefined(interpreter, pattern);
                                                                else
                                                                    result = String.Empty;
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
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope vars name ?pattern?\"";
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
                        result = "wrong # args: should be \"scope option ?arg ...?\"";
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
