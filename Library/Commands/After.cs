/*
 * After.cs --
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
    [ObjectId("4f867f2c-e65f-48d8-bf81-e794d10f7466")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class After : Core
    {
        public After(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "active", "cancel", "clear", "counts",
            "dump", "enable", "flags", "idle", "info"
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
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, false,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "active":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                result = eventManager.Active;
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after active\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cancel":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            string text;

                                            if (arguments.Count == 3)
                                                text = arguments[2];
                                            else
                                                text = ListOps.Concat(arguments, 2);

                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                code = eventManager.CancelEvents(
                                                    text, false, false, ref result);
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after cancel arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "clear":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                code = eventManager.ClearEvents(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after clear\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "counts":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                result = StringList.MakeList(
                                                    "enabled", eventManager.Enabled,
                                                    "active", eventManager.Active,
                                                    "noNotify", eventManager.NoNotify,
                                                    "createEventCount", Event.CreateCount,
                                                    "disposeEventCount", Event.DisposeCount,
                                                    "queueEventCount", eventManager.QueueEventCount,
                                                    "queueIdleEventCount", eventManager.QueueIdleEventCount,
                                                    "eventCount", eventManager.EventCount,
                                                    "idleEventCount", eventManager.IdleEventCount,
                                                    "totalEventCount", eventManager.TotalEventCount,
                                                    "maximumEventCount", eventManager.MaximumEventCount,
                                                    "maximumIdleEventCount", eventManager.MaximumIdleEventCount,
                                                    "maybeDisposeEventCount", eventManager.MaybeDisposeEventCount,
                                                    "reallyDisposeEventCount", eventManager.ReallyDisposeEventCount,
                                                    "interpreterEventCount", interpreter.EventCount,
#if NATIVE && TCL
                                                    "interpreterTclEventCount", interpreter.TclEventCount,
#endif
                                                    "interpreterWaitCount", interpreter.WaitCount,
                                                    "interpreterWaitSpinCount", interpreter.WaitSpinCount);

                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after counts\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "dump":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                code = eventManager.Dump(ref result);
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after dump\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enable":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        eventManager.Enabled = enabled;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = eventManager.Enabled;
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after enable ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "flags":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    object enumValue = EnumOps.TryParseFlags(
                                                        interpreter, typeof(EventFlags),
                                                        interpreter.AfterEventFlags.ToString(),
                                                        arguments[2], interpreter.InternalCultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is EventFlags)
                                                        interpreter.AfterEventFlags = (EventFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = interpreter.AfterEventFlags;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after flags ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "idle":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(new IOption[] {
                                                new Option(null, OptionFlags.MustHaveWideIntegerValue |
                                                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                                    "-thread", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    IVariant value = null;
                                                    long? threadId = null;

                                                    if (options.IsPresent("-thread", ref value))
                                                        threadId = (long)value.Value;

                                                    IEventManager eventManager = interpreter.EventManager;

                                                    if (EventOps.ManagerIsOk(eventManager))
                                                    {
                                                        //
                                                        // FIXME: PRI 5: Somewhat arbitrary, we cannot be "idle" with no
                                                        //        windows message loop.
                                                        //
                                                        string name = FormatOps.Id(this.Name, null, interpreter.NextId());
                                                        DateTime now = TimeOps.GetUtcNow();
                                                        DateTime dateTime = now.AddMilliseconds(EventManager.MinimumIdleWaitTime);
                                                        string text;

                                                        if ((argumentIndex + 1) == arguments.Count)
                                                            text = arguments[argumentIndex];
                                                        else
                                                            text = ListOps.Concat(arguments, argumentIndex);

                                                        IScript script = interpreter.CreateAfterScript(
                                                            name, null, null, ScriptTypes.Idle, text,
                                                            now, EngineMode.EvaluateScript, ScriptFlags.None,
                                                            clientData, true);

                                                        code = eventManager.QueueScript(
                                                            name, dateTime, script, script.EventFlags,
                                                            EventPriority.Idle, threadId,
                                                            interpreter.InternalEventLimit, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = name;
                                                    }
                                                    else
                                                    {
                                                        result = "event manager not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"after idle ?options? arg ?arg ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after idle ?options? arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "info":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IEventManager eventManager = interpreter.EventManager;

                                            if (EventOps.ManagerIsOk(eventManager))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    IEvent @event = null;

                                                    code = eventManager.GetEvent(
                                                        arguments[2], ref @event, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (EventManager.IsScriptEvent(@event))
                                                        {
                                                            if (@event.ClientData != null)
                                                            {
                                                                IScript script = @event.ClientData.Data as IScript;

                                                                if (script != null)
                                                                {
                                                                    result = script.ToString();
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "event \"{0}\" clientData is not a script",
                                                                        arguments[2]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "event \"{0}\" has invalid clientData",
                                                                    arguments[2]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "event \"{0}\" is not an after event",
                                                                arguments[2]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    StringList list = null;

                                                    code = eventManager.ListEvents(null, false, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                            else
                                            {
                                                result = "event manager not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"after info ?id?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        long milliseconds = 0; // for idle, execute the script right now.

                                        code = Value.GetWideInteger2(
                                            subCommand, ValueFlags.AnyWideInteger,
                                            interpreter.InternalCultureInfo, ref milliseconds, ref result);

                                        if (code == ReturnCode.Ok)
                                        {
                                            if (arguments.Count == 2)
                                            {
                                                //
                                                // BUGBUG: This call will never timeout if we cannot obtain
                                                //         the interpreter lock.
                                                //
                                                EventWaitFlags eventWaitFlags = interpreter.EventWaitFlags;

                                                bool noCancel = FlagOps.HasFlags(
                                                    eventWaitFlags, EventWaitFlags.NoCancel, true);

                                                bool noGlobalCancel = FlagOps.HasFlags(
                                                    eventWaitFlags, EventWaitFlags.NoGlobalCancel, true);

                                                code = EventOps.Wait(interpreter, null,
                                                    PerformanceOps.GetMicrosecondsFromMilliseconds(
                                                    milliseconds), null, false, false, noCancel,
                                                    noGlobalCancel, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else if (arguments.Count >= 3)
                                            {
                                                OptionDictionary options = new OptionDictionary(new IOption[] {
                                                    new Option(null, OptionFlags.MustHaveWideIntegerValue |
                                                        OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                                        "-thread", null),
                                                    Option.CreateEndOfOptions()
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, false,
                                                    ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        IVariant value = null;
                                                        long? threadId = null;

                                                        if (options.IsPresent("-thread", ref value))
                                                            threadId = (long)value.Value;

                                                        IEventManager eventManager = interpreter.EventManager;

                                                        if (EventOps.ManagerIsOk(eventManager))
                                                        {
                                                            string name = FormatOps.Id(this.Name, null, interpreter.NextId());
                                                            DateTime now = TimeOps.GetUtcNow();
                                                            DateTime dateTime = now.AddMilliseconds(milliseconds);
                                                            string text;

                                                            if ((argumentIndex + 1) == arguments.Count)
                                                                text = arguments[argumentIndex];
                                                            else
                                                                text = ListOps.Concat(arguments, argumentIndex);

                                                            IScript script = interpreter.CreateAfterScript(
                                                                name, null, null, ScriptTypes.Timer, text,
                                                                now, EngineMode.EvaluateScript, ScriptFlags.None,
                                                                clientData, false);

                                                            code = eventManager.QueueScript(
                                                                name, dateTime, script, script.EventFlags,
                                                                EventPriority.After, threadId,
                                                                interpreter.InternalEventLimit, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = name;
                                                        }
                                                        else
                                                        {
                                                            result = "event manager not available";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"after milliseconds ?options? arg ?arg ...?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"after milliseconds ?options? arg ?arg ...?\"";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = ScriptOps.BadSubCommand(
                                                interpreter, null, "argument", subCommand, this, null, ", or a number");
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"after option ?arg ...?\"";
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
