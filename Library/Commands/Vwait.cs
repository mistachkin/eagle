/*
 * Vwait.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
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
    [ObjectId("9a58d5a9-85b8-43e1-9136-5667eb2e87bf")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class Vwait : Core
    {
        public Vwait(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                        //
                        // NOTE: Grab the variable and event wait flags from the
                        //       interpreter and use them as the defaults for the
                        //       associated options.
                        //
                        EventWaitFlags eventWaitFlags = interpreter.EventWaitFlags;
                        VariableFlags variableFlags = interpreter.EventVariableFlags;

                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null,
                                OptionFlags.MustHaveObjectValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-handle", null),
                            new Option(typeof(EventWaitFlags),
                                OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-eventwaitflags",
                                new Variant(eventWaitFlags)),
                            new Option(typeof(VariableFlags),
                                OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-variableflags",
                                new Variant(variableFlags)),
                            new Option(null,
                                OptionFlags.MustHaveWideIntegerValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-thread", null),
                            new Option(null,
                                OptionFlags.MustHaveIntegerValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-limit", null),
                            new Option(null,
                                OptionFlags.MustHaveIntegerValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-timeout", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-clear", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-force", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-nocomplain", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-leaveresult", null),
                            new Option(null,
                                OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-locked", null),
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(
                            options, arguments, 0, 1, Index.Invalid, false,
                            ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 1) == arguments.Count))
                            {
                                IVariant value = null;
                                int valueIndex = Index.Invalid;
                                EventWaitHandle @event = null;

                                if (options.IsPresent("-handle", ref value))
                                {
                                    IObject @object = (IObject)value.Value;

                                    if ((@object.Value == null) ||
                                        (@object.Value is EventWaitHandle))
                                    {
                                        @event = (EventWaitHandle)@object.Value;
                                    }
                                    else
                                    {
                                        result = "option value has invalid EventWaitHandle";
                                        code = ReturnCode.Error;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    int timeout = 0; /* milliseconds */

                                    if (options.IsPresent("-timeout", ref value))
                                        timeout = (int)value.Value;

                                    int limit = 0;

                                    if (options.IsPresent("-limit", ref value))
                                        limit = (int)value.Value;

                                    long? threadId = interpreter.GetVariableWaitThreadId();

                                    if (options.IsPresent("-thread", ref value))
                                        threadId = (long)value.Value;

                                    if (options.IsPresent("-eventwaitflags", ref value))
                                        eventWaitFlags = (EventWaitFlags)value.Value;

                                    if (options.IsPresent("-variableflags", ref value))
                                        variableFlags = (VariableFlags)value.Value;

                                    bool clear = false;

                                    if (options.IsPresent("-clear"))
                                        clear = true;

                                    bool force = false;

                                    if (options.IsPresent("-force"))
                                        force = true;

                                    bool noComplain = false;

                                    if (options.IsPresent("-nocomplain"))
                                        noComplain = true;

                                    bool leaveResult = false;

                                    if (options.IsPresent("-leaveresult"))
                                        leaveResult = true;

                                    string locked = null;
                                    IScriptLocation lockedLocation = null;

                                    if (options.IsPresent("-locked", ref value, ref valueIndex))
                                    {
                                        locked = value.ToString();
                                        lockedLocation = arguments[valueIndex + 1];
                                    }

                                    if (clear)
                                    {
                                        //
                                        // NOTE: Reset wait state for the specified variable.
                                        //
                                        code = Interpreter.ClearVariableNameWait(
                                            interpreter, arguments[argumentIndex],
                                            eventWaitFlags, variableFlags, ref result);
                                    }
                                    //
                                    // NOTE: Typically, we do not want to enter a wait state if
                                    //       there are no events queued because there would be
                                    //       no possible way to ever (gracefully) exit the wait;
                                    //       however, there are exceptions to this.
                                    //
                                    else if (force || interpreter.ShouldWaitVariable())
                                    {
                                        ScriptOps.MaybeModifyEventWaitFlags(ref eventWaitFlags);

                                        bool changed = false;

                                        if (locked != null)
                                        {
                                            ReturnCode? lockCode = null;

                                            try
                                            {
                                                lockCode = interpreter.LockVariable(eventWaitFlags,
                                                    variableFlags, arguments[argumentIndex],
                                                    PerformanceOps.GetMicrosecondsFromMilliseconds(timeout),
                                                    @event, ref result);

                                                if ((lockCode != null) &&
                                                    ((ReturnCode)lockCode == ReturnCode.Ok))
                                                {
                                                    string name = StringList.MakeList("vwait -locked",
                                                        arguments[argumentIndex]);

                                                    ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                        name, CallFrameFlags.Evaluate);

                                                    interpreter.PushAutomaticCallFrame(frame);

                                                    code = interpreter.EvaluateScript(
                                                        locked, lockedLocation, ref result);

                                                    if (code == ReturnCode.Error)
                                                    {
                                                        Engine.AddErrorInformation(
                                                            interpreter, result, String.Format(
                                                                "{0}    (\"vwait -locked\" body line {1})",
                                                                Environment.NewLine, Interpreter.GetErrorLine(
                                                                interpreter)));
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
                                            }
                                            finally
                                            {
                                                if ((lockCode != null) &&
                                                    ((ReturnCode)lockCode == ReturnCode.Ok))
                                                {
                                                    ReturnCode unlockCode;
                                                    Result unlockError = null;

                                                    unlockCode = interpreter.UnlockVariable(
                                                        variableFlags, arguments[argumentIndex],
                                                        ref unlockError);

                                                    if (unlockCode != ReturnCode.Ok)
                                                    {
                                                        TraceOps.DebugTrace(String.Format(
                                                            "Execute: could not unlock variable {0}: {1}",
                                                            FormatOps.ErrorVariableName(
                                                                arguments[argumentIndex], null),
                                                            FormatOps.WrapOrNull(unlockError)),
                                                            typeof(Vwait).Name, TracePriority.LockWarning);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            code = interpreter.WaitVariable(eventWaitFlags,
                                                variableFlags, arguments[argumentIndex],
                                                PerformanceOps.GetMicrosecondsFromMilliseconds(timeout),
                                                threadId, limit, @event, ref changed,
                                                ref result);
                                        }

                                        if ((code != ReturnCode.Ok) && noComplain)
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            if (timeout != 0)
                                            {
                                                result = changed;
                                            }
                                            else if (!leaveResult)
                                            {
                                                Engine.ResetResult(interpreter, ref result);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "can't wait for variable \"{0}\": would wait forever",
                                            arguments[argumentIndex]);

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
                                        !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"vwait ?options? varName\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"vwait ?options? varName\"";
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
