/*
 * Callback.cs --
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
    [ObjectId("7a66999b-d92f-4884-bf28-1ee7d7aa52ea")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("event")]
    internal sealed class Callback : Core
    {
        public Callback(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "clear", "count", "dequeue", "enqueue", "execute",
            "list"
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
                            //
                            // NOTE: Programmatically interact with the debugger (breakpoint, watch,
                            //       eval, etc).
                            //
                            switch (subCommand)
                            {
                                case "clear":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.ClearCallbackQueue(ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"callback clear\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "count":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            int count = 0;

                                            code = interpreter.CountCallbacks(ref count, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = count;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"callback count\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "dequeue":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetDequeueOptions();

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
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
                                                    // NOTE: Which Tcl interpreter do we want a command alias created
                                                    //       in, if any?
                                                    //
                                                    ICallback callback = null;

                                                    code = interpreter.DequeueCallback(ref callback, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        ObjectOptionType objectOptionType = ObjectOptionType.Dequeue |
                                                            ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                        code = MarshalOps.FixupReturnValue(
                                                            interpreter, interpreter.InternalBinder, interpreter.InternalCultureInfo,
                                                            returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                            objectOptionType), objectOptionType, objectName, interpName,
                                                            callback, create, dispose, alias, aliasReference, toString,
                                                            ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"callback dequeue ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"callback dequeue ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enqueue":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            ICallback callback = CommandCallback.Create(
                                                MarshalFlags.Default, CallbackFlags.Default,
                                                ObjectFlags.Callback, ByRefArgumentFlags.None,
                                                interpreter, clientData, null, new StringList(
                                                arguments, 2), ref result);

                                            if (callback != null)
                                            {
                                                code = interpreter.EnqueueCallback(callback, ref result);

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
                                            result = "wrong # args: should be \"callback enqueue name ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "execute":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.ExecuteCallbackQueue(ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"callback execute\"";
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

                                            StringList list = null;

                                            code = interpreter.ListCallbacks(
                                                pattern, false, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"callback list ?pattern?\"";
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
                        result = "wrong # args: should be \"callback option ?arg ...?\"";
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
