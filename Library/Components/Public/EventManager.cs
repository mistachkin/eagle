/*
 * EventManager.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using EventQueueKey = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Components.Public.EventPriority, System.DateTime, long>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    [ObjectId("ac231a31-e777-41e3-89de-e74cb4092467")]
    public class EventManager :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IEventManager, IHaveInterpreter, IDisposable
    {
        #region Private Constants
        //
        // NOTE: All values are in milliseconds unless otherwise noted.
        //
        internal static readonly int DefaultSleepTime = 0;
        internal static readonly int MinimumSleepTime = 50;

        ///////////////////////////////////////////////////////////////////////

        internal static readonly int MinimumEventTime = 1;
        internal static readonly int MinimumIdleWaitTime = 1000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();

        private int maximumCount;
        private int maximumIdleCount;

        private int queueCount;
        private int queueIdleCount;

        private int maybeDisposeCount;
        private int reallyDisposeCount;

        private int waitForEmptyQueueTotalCount;
        private int waitForEmptyQueueErrorCount;

        private int waitForEventEnqueuedTotalCount;
        private int waitForEventEnqueuedErrorCount;

        private DateTime lastNow;

        private EventQueue events;
        private EventQueue idleEvents;

        private EventWaitHandle emptyEvent;
        private EventWaitHandle enqueueEvent;
        private EventWaitHandle idleEmptyEvent;
        private EventWaitHandle idleEnqueueEvent;
        private EventWaitHandle[] userEvents;

        private SleepTypeIntDictionary sleepTimes;
        private SleepTypeIntDictionary minimumSleepTimes;

        private int enabled;
        private int levels;
        private int noNotify;

        private DateTimeNowCallback nowCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private EventManager()
        {
            maximumCount = 0;
            maximumIdleCount = 0;

            queueCount = 0;
            queueIdleCount = 0;

            Interlocked.Exchange(ref maybeDisposeCount, 0);
            Interlocked.Exchange(ref reallyDisposeCount, 0);

            Interlocked.Exchange(ref waitForEmptyQueueTotalCount, 0);
            Interlocked.Exchange(ref waitForEmptyQueueErrorCount, 0);

            Interlocked.Exchange(ref waitForEventEnqueuedTotalCount, 0);
            Interlocked.Exchange(ref waitForEventEnqueuedErrorCount, 0);

            lastNow = DateTime.MinValue;

            events = new EventQueue();
            idleEvents = new EventQueue();

            emptyEvent = ThreadOps.CreateEvent(true);
            enqueueEvent = ThreadOps.CreateEvent(true);
            idleEmptyEvent = ThreadOps.CreateEvent(true);
            idleEnqueueEvent = ThreadOps.CreateEvent(true);
            userEvents = null;

            sleepTimes = new SleepTypeIntDictionary();
            minimumSleepTimes = new SleepTypeIntDictionary();

            Interlocked.Exchange(ref enabled, 1);
            Interlocked.Exchange(ref levels, 0);
            Interlocked.Exchange(ref noNotify, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public EventManager(
            Interpreter interpreter
            )
            : this()
        {
            this.interpreter = interpreter;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        #region Event Formatting Methods
        private static string EventToList(
            int index,
            IEvent @event
            )
        {
            if (@event == null)
                return null;

            StringPairList result = new StringPairList();

            result.Add("dateTime",
                FormatOps.Iso8601FullDateTime(@event.DateTime));

            result.Add("index", index.ToString());

            Guid id = @event.Id;

            if (!id.Equals(Guid.Empty))
                result.Add("id", id.ToString());

            result.Add("priority", @event.Priority.ToString());
            result.Add("flags", @event.Flags.ToString());
            result.Add("name", @event.Name);

            if (IsScriptEvent(@event))
            {
                IClientData clientData = @event.ClientData;

                IScript script = (clientData != null) ?
                    clientData.Data as IScript : null;

                result.Add("script",
                    (script != null) ? script.Text : null);
            }
            else
            {
                result.Add("callback",
                    FormatOps.DelegateName(@event.Callback));

                IClientData clientData = @event.ClientData;

                result.Add("clientData", (clientData != null) ?
                    clientData.ToString() : String.Empty);
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Checking Methods
        private static bool IsEventPriorityReady(
            IEvent @event,
            EventPriority priority
            )
        {
            if (@event != null)
            {
                EventPriority eventPriority = GetReadyEventPriority(
                    @event.Flags, @event.Priority);

                //
                // NOTE: This code assumes that lower numbers indicate higher
                //       relative priority.
                //
                return eventPriority <= priority;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsEventDateTimeReady(
            IEvent @event,
            DateTime dateTime
            )
        {
            if (@event != null)
            {
                DateTime eventDateTime = @event.DateTime;

                return (eventDateTime == DateTime.MinValue) ||
                    (dateTime >= eventDateTime);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsEventThreadReady(
            IEvent @event,
            EventFlags eventFlags,
            long? threadId
            )
        {
            if (@event != null)
            {
                //
                // NOTE: If the caller is waiting for events on a
                //       specific thread, only consider events that
                //       match its thread; otherwise, if the greedy
                //       flag is set, consider all events; otherwise,
                //       only consider events that are not targeted
                //       at a specific thread.
                //
                long? eventThreadId = @event.ThreadId;

                if (threadId != null)
                {
                    return (eventThreadId != null) &&
                        ((long)eventThreadId == (long)threadId);
                }
                else if (FlagOps.HasFlags(
                        eventFlags, EventFlags.GreedyThread, true))
                {
                    return true;
                }
                else
                {
                    return (eventThreadId == null);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AreEventFlagsReady(
            IEvent @event,
            EventFlags eventFlags,
            bool notHas,
            bool all
            )
        {
            if (@event != null)
            {
                EventFlags localEventFlags = @event.Flags;

                return (!notHas == FlagOps.HasFlags(
                    localEventFlags, eventFlags, all));
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsIdleEvent(
            EventFlags eventFlags
            )
        {
            return FlagOps.HasFlags(eventFlags, EventFlags.Idle, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsIdleEvent(
            IEvent @event
            )
        {
            return (@event != null) ? IsIdleEvent(@event.Flags) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static bool IsScriptEvent(
            IEvent @event
            )
        {
            return (@event != null) ?
                (@event.Callback == ScriptEventCallback) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchEventName(
            IEvent @event,
            string pattern
            )
        {
            return (@event != null) ?
                SharedStringOps.SystemEquals(@event.Name, pattern) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchScriptText(
            IScript script,
            string pattern
            )
        {
            return (script != null) ?
                StringOps.UserEquals(script.Text, pattern) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchEvent(
            IEvent @event,
            EventMatchCallback callback,
            IClientData clientData
            )
        {
            if (callback == null)
                return true;

            try
            {
                bool match = false;
                Result error = null;

                if (callback(
                        clientData, @event, ref match,
                        ref error) == ReturnCode.Ok) /* throw */
                {
                    return match;
                }
                else
                {
                    TraceOps.DebugTrace(
                        error, typeof(EventManager).Name,
                        TracePriority.EventError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EventManager).Name,
                    TracePriority.EventError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldDequeueIdleEvent(
            EventFlags eventFlags,
            int nonIdleCount
            )
        {
            if (FlagOps.HasFlags(eventFlags, EventFlags.NoIdle, true))
                return false;

            if ((nonIdleCount != 0) &&
                FlagOps.HasFlags(eventFlags, EventFlags.IdleIfEmpty, true))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static EventPriority GetAutomaticEventPriority(
            EventFlags eventFlags,
            EventPriority priority
            )
        {
            //
            // NOTE: If the caller specified any priority that is not
            //       "Automatic", simply return it verbatim.
            //
            if (priority != EventPriority.Automatic)
                return priority;

            //
            // NOTE: If the caller specified the "Idle" event flag then the
            //       priority for this event is "Idle".  This must be checked
            //       first because all idle events currently also include the
            //       "After" flag.
            //
            if (FlagOps.HasFlags(eventFlags, EventFlags.Idle, true))
            {
                priority = EventPriority.Idle;
            }
            //
            // NOTE: Otherwise, if the caller specified the "After" event flag
            //       then the priority for this event is "After".
            //
            else if (FlagOps.HasFlags(eventFlags, EventFlags.After, true))
            {
                priority = EventPriority.After;
            }
            //
            // NOTE: Otherwise, if the caller specified the "Immediate" event
            //       flag then the priority for this event is "Immediate".
            //
            else if (FlagOps.HasFlags(eventFlags, EventFlags.Immediate, true))
            {
                priority = EventPriority.Immediate;
            }
            //
            // NOTE: Otherwise, just return the "Normal" priority.
            //
            else
            {
                priority = EventPriority.Normal;
            }

            return priority;
        }

        ///////////////////////////////////////////////////////////////////////

        private static EventPriority GetReadyEventPriority(
            EventFlags eventFlags,
            EventPriority priority
            )
        {
            //
            // NOTE: This code assumes that lower numbers indicate higher
            //       relative priority.  If the caller is accepting a event
            //       priorities "Immediate" and [relatively] lower and this
            //       event has an "Immediate" priority flag then this event
            //       should be considered to be of "Immediate" priority.
            //
            if ((priority >= EventPriority.Immediate) &&
                FlagOps.HasFlags(eventFlags, EventFlags.Immediate, true))
            {
                return EventPriority.Immediate;
            }
            //
            // NOTE: This code assumes that lower numbers indicate higher
            //       relative priority.  Otherwise, if the caller is accepting
            //       a event priorities "After" and [relatively] lower and this
            //       event has an "After" priority flag then this event should
            //       be considered to be of "After" priority.
            //
            else if ((priority >= EventPriority.After) &&
                FlagOps.HasFlags(eventFlags, EventFlags.After, true))
            {
                return EventPriority.After;
            }
            //
            // NOTE: This code assumes that lower numbers indicate higher
            //       relative priority.  Otherwise, if the caller is accepting
            //       a event priorities "Idle" and [relatively] lower and this
            //       event has an "Idle" priority flag then this event should
            //       be considered to be of "Idle" priority.
            //
            else if ((priority >= EventPriority.Idle) &&
                FlagOps.HasFlags(eventFlags, EventFlags.Idle, true))
            {
                return EventPriority.Idle;
            }

            //
            // NOTE: Just return the "Normal" priority.
            //
            return EventPriority.Normal;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long? GetAutomaticEventThread(
            long? threadId
            )
        {
            if (threadId == null) /* NOTE: Consume all? */
                return null;

            long localThreadId = (long)threadId;

            if (localThreadId != 0) /* NOTE: Consume specific? */
                return localThreadId;

            return GlobalState.GetCurrentSystemThreadId();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ParameterizedThreadStart Methods
#if SHELL && INTERACTIVE_COMMANDS
        internal static void QueueEventThreadStart(
            object obj
            )
        {
            try
            {
                IAnyPair<Interpreter, IScript> anyPair = obj as
                    IAnyPair<Interpreter, IScript>;

                if (anyPair != null)
                {
                    Interpreter interpreter = anyPair.X;
                    IScript script = anyPair.Y;

                    if ((interpreter != null) && (script != null))
                    {
                        IEventManager eventManager = interpreter.EventManager;

                        if (EventOps.ManagerIsOk(eventManager))
                        {
                            ReturnCode code;
                            Result result = null;

                            code = eventManager.QueueEvent(
                                script.Name, DateTime.MinValue,
                                ScriptEventCallback, new ClientData(script),
                                interpreter.QueueEventFlags,
                                EventPriority.QueueEvent, null, 0,
                                ref result);

                            IInteractiveHost interactiveHost =
                                interpreter.GetInteractiveHost();

                            if (interactiveHost != null)
                            {
                                if (code == ReturnCode.Ok)
                                {
                                    interactiveHost.WriteLine("event queued");
                                }
                                else
                                {
                                    interactiveHost.WriteLine(String.Format(
                                        "failed to queue event{0}{1}: {2}",
                                        Environment.NewLine, code.ToString(),
                                        result));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EventManager).Name,
                    TracePriority.EventError);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        internal static void ServiceEventsThreadStart(
            object obj
            )
        {
            try
            {
                ServiceEventClientData clientData =
                    obj as ServiceEventClientData;

                if (clientData == null)
                    return;

                Interpreter interpreter = clientData.Interpreter;

                if (interpreter == null)
                    return;

                IEventManager eventManager = interpreter.EventManager;

                if (EventOps.ManagerIsOk(eventManager))
                {
                    ReturnCode code;
                    int eventCount = 0;
                    Result result = null;

                    //
                    // NOTE: Service events for this interpreter until an
                    //       error occurs.
                    //
                    code = eventManager.ServiceEvents(
                        clientData.EventFlags, clientData.Priority,
                        clientData.ThreadId, clientData.Limit,
                        clientData.NoCancel, clientData.NoGlobalCancel,
                        clientData.StopOnError, clientData.ErrorOnEmpty,
                        clientData.UserInterface, ref eventCount,
                        ref result);

                    IInteractiveHost interactiveHost =
                        interpreter.GetInteractiveHost();

                    if (interactiveHost != null)
                    {
                        if (code == ReturnCode.Ok)
                        {
                            interactiveHost.WriteLine(String.Format(
                                "serviced {0} event(s), overall success",
                                eventCount));
                        }
                        else
                        {
                            interactiveHost.WriteLine(String.Format(
                                "serviced {0} event(s), overall failure" +
                                "{1}{2}: {3}", eventCount,
                                Environment.NewLine, code.ToString(),
                                result));
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    "ServiceEventsThreadStart: caught thread abort",
                    typeof(EventManager).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException)
            {
                TraceOps.DebugTrace(
                    "ServiceEventsThreadStart: caught thread interrupt",
                    typeof(EventManager).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EventManager).Name,
                    TracePriority.EventError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ScriptEventCallback (ExecuteCallback) Methods
        private static void ScriptEventPrologue(
            Interpreter interpreter,
            string name,
            EngineMode engineMode,
            ref bool useNamespaces,
            ref ICallFrame frame
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Skip performing the call frame management for this
                //       script callback if the interpreter is disposed.  In
                //       that case, the actual script will not be evaluated
                //       anyhow.
                //
                if (!interpreter.Disposed)
                {
                    //
                    // NOTE: Are namespaces currently enabled for this
                    //       interpreter?
                    //
                    useNamespaces = interpreter.AreNamespacesEnabled();

                    //
                    // NOTE: If namespaces are enabled, create a global
                    //       namespaces call frame; otherwise, a tracking
                    //       call frame is created.
                    //
                    CallFrameFlags flags = CallFrameOps.GetFlags(
                        CallFrameFlags.After, engineMode, useNamespaces);

                    if (useNamespaces)
                    {
                        INamespace @namespace = interpreter.GlobalNamespace;

                        frame = interpreter.NewNamespaceCallFrame(
                            name, flags, null, @namespace, false);

                        interpreter.PushNamespaceCallFrame(frame);
                    }
                    else
                    {
                        frame = interpreter.NewTrackingCallFrame(
                            name, flags);

                        interpreter.PushAutomaticCallFrame(frame);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ScriptEventEpilogue(
            Interpreter interpreter,
            bool useNamespaces,
            ICallFrame frame
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter has been disposed, we cannot
                //       deal with popping the call frame because the call
                //       stack itself is gone.  However, we do NOT care if
                //       the interpreter is simply marked as "deleted".
                //
                if (!interpreter.Disposed)
                {
                    if (useNamespaces)
                    {
                        //
                        // NOTE: Pop the namespace call frame, which will
                        //       restore the original "current namespace"
                        //       for the interpreter on this thread.
                        //
                        /* IGNORED */
                        interpreter.PopNamespaceCallFrame(frame);
                    }
                    else
                    {
                        //
                        // NOTE: Pop the original call frame that we pushed
                        //       above and any intervening scope call frames
                        //       that may be leftover (i.e. they were not
                        //       explicitly closed).
                        //
                        /* IGNORED */
                        interpreter.PopScopeCallFramesAndOneMore();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ScriptEventTrace(
            string prefix,
            Interpreter interpreter,
            string scriptName,
            EngineMode engineMode,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags combinedEventFlags,
            ExpressionFlags expressionFlags,
            ReturnCode code,
            string text,
            Result result
            )
        {
            TraceOps.DebugTrace(interpreter, String.Format(
                "ScriptEventTrace: {0}, interpreter = {1}, " +
                "scriptName = {2}, engineMode = {3}, " +
                "engineFlags = {4}, substitutionFlags = {5}, " +
                "combinedEventFlags = {6}, expressionFlags = {7}, " +
                "text = {8}, code = {9}, result = {10}", prefix,
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(scriptName),
                FormatOps.WrapOrNull(engineMode),
                FormatOps.WrapOrNull(engineFlags),
                FormatOps.WrapOrNull(substitutionFlags),
                FormatOps.WrapOrNull(combinedEventFlags),
                FormatOps.WrapOrNull(expressionFlags),
                FormatOps.WrapOrNull(true, true, text),
                code, FormatOps.WrapOrNull(true, true,
                result)), typeof(EventManager).Name,
                TracePriority.EventDebug, 1);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ScriptEventCore(
            Interpreter interpreter,
            EngineMode engineMode,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            string text,
            ref Result result
            )
        {
            switch (engineMode)
            {
                case EngineMode.None:
                    {
                        //
                        // NOTE: Do nothing.  Mostly, this
                        //       always succeeds at doing
                        //       nothing unless something
                        //       is done.  The result is
                        //       not changed.
                        //
                        return ReturnCode.Ok;
                    }
                case EngineMode.EvaluateExpression:
                    {
                        return Engine.EvaluateExpression(
                            interpreter, text, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref result);
                    }
                case EngineMode.EvaluateScript:
                    {
                        return Engine.EvaluateScript(
                            interpreter, text, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref result);
                    }
                case EngineMode.EvaluateFile:
                    {
                        return Engine.EvaluateFile(
                            interpreter, text, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref result);
                    }
                case EngineMode.SubstituteString:
                    {
                        return Engine.SubstituteString(
                            interpreter, text, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref result);
                    }
                case EngineMode.SubstituteFile:
                    {
                        return Engine.SubstituteFile(
                            interpreter, text, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref result);
                    }
                default:
                    {
                        result = String.Format(
                            "unsupported engine mode {0}",
                            engineMode);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ScriptEventCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            //
            // NOTE: An interpreter context is required in order to evaluate
            //       a script.
            //
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: We must have a clientData object since it contains the
            //       script to evaluate.
            //
            if (clientData == null)
            {
                result = "invalid event clientData";
                return ReturnCode.Error;
            }

            //
            // NOTE: We expect (and require) that the client data for this
            //       event contains an IScript.
            //
            IScript script = clientData.Data as IScript;

            if (script == null)
            {
                result = "event clientData is not a script";
                return ReturnCode.Error;
            }

            //
            // NOTE: Grab all the necessary information from the script object
            //       now because it should not change during our processing.
            //
            string scriptName;
            StringList texts;
            EngineMode engineMode;
            EngineFlags engineFlags;
            SubstitutionFlags substitutionFlags;
            EventFlags combinedEventFlags;
            ExpressionFlags expressionFlags;

            //
            // NOTE: We want "snapshot" semantics for accessing the script
            //       object; therefore, lock it.
            //
            lock (script.SyncRoot) /* TRANSACTIONAL */
            {
                scriptName = script.Name;
                texts = new StringList(script);
                engineMode = script.EngineMode;
                engineFlags = script.EngineFlags;
                substitutionFlags = script.SubstitutionFlags;
                expressionFlags = script.ExpressionFlags;

                //
                // BUGFIX: For the vast majority of cases, we need to prefer
                //         the dequeue-related engine event flags (i.e. After,
                //         Immediate, etc) from the interpreter event flags
                //         field over those same flags from the script itself;
                //         otherwise, events will not be processed in the
                //         correct order by the [vwait] machinery (which this
                //         callback method is technically a party of).
                //
                combinedEventFlags = interpreter.CombineEngineEventFlags(
                    script.EventFlags); /* NO-LOCK */
            }

            //
            // NOTE: Create and push the call frame for this callback.
            //
            bool useNamespaces = false;
            ICallFrame frame = null;

            ScriptEventPrologue(
                interpreter, scriptName, engineMode, ref useNamespaces,
                ref frame);

            //
            // NOTE: The initial result is Ok (i.e. if the are no scripts,
            //       that will be the final result as well).
            //
            ReturnCode code = ReturnCode.Ok;

            try
            {
                //
                // NOTE: Do we need to emit trace messages for this event?
                //
                bool eventDebug = FlagOps.HasFlags(combinedEventFlags,
                    EventFlags.Debug, true);

                //
                // NOTE: Process each section of the provided script.  If
                //       a section raises a script error, any remaining
                //       sections are not processed.
                //
                foreach (string text in texts)
                {
                    if (eventDebug)
                    {
                        ScriptEventTrace("starting script",
                            interpreter, scriptName, engineMode,
                            engineFlags, substitutionFlags,
                            combinedEventFlags, expressionFlags,
                            code, text, result);
                    }

                    code = ScriptEventCore(
                        interpreter, engineMode, engineFlags,
                        substitutionFlags, combinedEventFlags,
                        expressionFlags, text, ref result);

                    if (eventDebug)
                    {
                        ScriptEventTrace("completed script",
                            interpreter, scriptName, engineMode,
                            engineFlags, substitutionFlags,
                            combinedEventFlags, expressionFlags,
                            code, text, result);
                    }

                    if (code != ReturnCode.Ok)
                    {
                        if (code == ReturnCode.Error)
                        {
                            Engine.AddErrorInformation(
                                interpreter, result, String.Format(
                                    "{0}    (\"after\" script line {1})",
                                    Environment.NewLine,
                                    Interpreter.GetErrorLine(interpreter)));
                        }

                        break;
                    }
                }
            }
            finally
            {
                //
                // NOTE: Pop (and maybe dispose?) the call frame.
                //
                ScriptEventEpilogue(interpreter, useNamespaces, frame);
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool IsEnabled()
        {
            return Interlocked.CompareExchange(ref enabled, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool IsActive()
        {
            return Interlocked.CompareExchange(ref levels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool IsNoNotify()
        {
            return Interlocked.CompareExchange(ref noNotify, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private int EnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        private int ExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime GetNow()
        {
            //
            // HACK: Make sure that time moves forward by some minimal amount
            //       because the resolution of the system clock is limited to
            //       10 milliseconds (i.e. if you call it faster than that, it
            //       may not increment (?)).
            //
            DateTime result;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                result = (nowCallback != null) ?
                    nowCallback() : TimeOps.GetUtcNow();

                TimeSpan timeSpan = result.Subtract(lastNow);
                double milliseconds = timeSpan.TotalMilliseconds;

                if (milliseconds < MinimumEventTime)
                    result = result.AddMilliseconds(MinimumEventTime);

                lastNow = result;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private EventQueue GetEventQueue(
            bool idle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return idle ? idleEvents : events;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int GetEventCount(
            bool idle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                EventQueue eventQueue = GetEventQueue(idle);

                if (eventQueue == null)
                    return 0;

                return eventQueue.Count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool HaveAnyEventQueue()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((events != null) || (idleEvents != null));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int GetTotalEventCount()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                result += GetEventCount(false);
                result += GetEventCount(true);

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool IsIdleEventQueue(
            EventQueue eventQueue
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return Object.ReferenceEquals(eventQueue, idleEvents);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventQueueKey CreateEventQueueKey(
            EventPriority priority,
            DateTime dateTime
            )
        {
            return new AnyTriplet<EventPriority, DateTime, long>(
                priority, dateTime, GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeDispose(
            EventFlags eventFlags, /* in */
            ref IEvent @event      /* in, out */
            )
        {
            if (@event != null)
            {
                if (FlagOps.HasFlags(
                        eventFlags, EventFlags.FireAndForget, true) ||
                    FlagOps.HasFlags(
                        @event.Flags, EventFlags.FireAndForget, true))
                {
                    if (Event.Dispose(@event))
                        Interlocked.Increment(ref reallyDisposeCount);
                }

                @event = null;
            }

            Interlocked.Increment(ref maybeDisposeCount);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool TryGetSleepTime(
            SleepType sleepType,
            bool minimum,
            out int sleepTime
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                SleepTypeIntDictionary localSleepTimes = minimum ?
                    minimumSleepTimes : sleepTimes;

                if ((localSleepTimes != null) &&
                    localSleepTimes.TryGetValue(sleepType, out sleepTime))
                {
                    return true;
                }

                sleepTime = 0; /* NOT USED */
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool TrySetSleepTime(
            SleepType sleepType,
            bool minimum,
            int? sleepTime
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                SleepTypeIntDictionary localSleepTimes = minimum ?
                    minimumSleepTimes : sleepTimes;

                if (localSleepTimes != null)
                {
                    if (sleepTime != null)
                    {
                        int newSleepTime = (int)sleepTime;
                        int oldSleepTime;

                        if (localSleepTimes.TryGetValue(
                                sleepType, out oldSleepTime))
                        {
                            if (newSleepTime != oldSleepTime)
                            {
                                localSleepTimes[sleepType] = newSleepTime;
                                return true;
                            }
                        }
                        else
                        {
                            localSleepTimes.Add(sleepType, newSleepTime);
                            return true;
                        }
                    }
                    else
                    {
                        return localSleepTimes.Remove(sleepType);
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode DequeueAnyReadyEvent(
            DateTime dateTime,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            bool strict,
            ref IEvent @event
            )
        {
            Result error = null;

            return DequeueAnyReadyEvent(
                dateTime, eventFlags, priority, threadId, strict,
                ref @event, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode DequeueAnyReadyEvent(
            EventQueue eventQueue,
            DateTime dateTime,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            bool strict,
            ref IEvent @event,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (eventQueue != null)
                {
                    //
                    // NOTE: Are we dealing with idle events now?
                    //
                    bool idle = IsIdleEventQueue(eventQueue);

                    //
                    // NOTE: Grab the event count now.
                    //
                    int eventCount = eventQueue.Count;

                    if (eventCount > 0)
                    {
                        priority = GetAutomaticEventPriority(
                            eventFlags, priority);

                        threadId = GetAutomaticEventThread(
                            threadId);

                        for (int index = 0; index < eventCount; index++)
                        {
                            //
                            // NOTE: Grab the Nth event from the specified
                            //       event queue.
                            //
                            IEvent localEvent = eventQueue[index];

                            //
                            // NOTE: The events in the queue should never be
                            //       null; however, if there are null events,
                            //       just skip over them.
                            //
                            if (localEvent == null)
                                continue;

                            //
                            // HACK: Events are sorted in highest priority
                            //       order first; therefore, if the priority of
                            //       this event is not high enough, none of
                            //       them will be and we can bail out early.
                            //
                            if (!IsEventPriorityReady(localEvent, priority))
                                break;

                            //
                            // HACK: Events are sorted soonest first; therefore,
                            //       if the time for this event has not arrived
                            //       yet then none of the events after this are
                            //       ready yet either.
                            //
                            if (!IsEventDateTimeReady(localEvent, dateTime))
                                break;

                            //
                            // HACK: Is this event targeted at *this* or *any*
                            //       thread; if so, cool; otherwise, skip it.
                            //
                            // BUGFIX: Since there is no sorting of the events
                            //         based on this property, do not stop the
                            //         loop here.  Just skip over this event.
                            //
                            if (!IsEventThreadReady(
                                    localEvent, eventFlags, threadId))
                            {
                                continue;
                            }

                            //
                            // NOTE: See if the event flags for this event match
                            //       the ones we are looking for.
                            //
                            if (AreEventFlagsReady(localEvent, eventFlags &
                                    EventFlags.DequeueMask, false, false))
                            {
                                Event.MarkDequeued(localEvent);
                                @event = localEvent;

                                eventQueue.RemoveAt(index);

                                //
                                // NOTE: Check if the event queue is empty at
                                //       this point and raise a signal if so.
                                //
                                if (CheckForEmptyQueue(idle))
                                    SignalEmptyQueue(idle);

#if NOTIFY
                                if (!IsNoNotify() && (interpreter != null))
                                {
                                    /* IGNORED */
                                    interpreter.CheckNotification(
                                        (idle ? NotifyType.Idle : NotifyType.None) |
                                            NotifyType.Event, NotifyFlags.Dequeued,
                                        new ObjectList(dateTime, eventFlags,
                                            priority, threadId, @event),
                                        interpreter, null, null, null, ref error);
                                }
#endif

                                return ReturnCode.Ok;
                            }
                        }

                        error = "no events are ready";
                    }
                    else
                    {
                        if (strict)
                        {
                            error = "no events";
                        }
                        else
                        {
                            @event = null;
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    if (strict)
                    {
                        error = "not accepting events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeComplain(
            ReturnCode code,
            Result error
            )
        {
            if (code != ReturnCode.Ok)
            {
                Interpreter interpreter;

                lock (syncRoot)
                {
                    interpreter = this.interpreter;
                }

                DebugOps.Complain(interpreter, code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventWaitHandle GetEmptyEvent(
            bool idle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return idle ? idleEmptyEvent : emptyEvent;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool CheckForEmptyQueue(
            bool idle
            )
        {
            return (GetEventCount(idle) == 0);
        }

        ///////////////////////////////////////////////////////////////////////

        private void SignalEmptyQueue(
            bool idle
            )
        {
            ReturnCode code;
            Result error = null;

            code = SignalEmptyQueue(idle, ref error);

            MaybeComplain(code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode SignalEmptyQueue(
            bool idle,
            ref Result error
            )
        {
            EventWaitHandle emptyEvent = GetEmptyEvent(idle);

            if (emptyEvent != null)
            {
                if (ThreadOps.SetEvent(emptyEvent))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "failed to signal empty queue";
                }
            }
            else
            {
                error = "cannot signal empty queue";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private EventWaitHandle GetEnqueueEvent(
            bool idle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return idle ? idleEnqueueEvent : enqueueEvent;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool CheckForEventEnqueued(
            bool idle
            )
        {
            return GetEventCount(idle) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SignalEventEnqueued(
            bool idle
            )
        {
            ReturnCode code;
            Result error = null;

            code = SignalEventEnqueued(idle, ref error);

            MaybeComplain(code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode SignalEventEnqueued(
            bool idle,
            ref Result error
            )
        {
            EventWaitHandle enqueueEvent = GetEnqueueEvent(idle);

            if (enqueueEvent != null)
            {
                if (ThreadOps.SetEvent(enqueueEvent))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "failed to signal event enqueued";
                }
            }
            else
            {
                error = "cannot signal event enqueued";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return interpreter;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    interpreter = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        //
        // NOTE: Some people may wonder why the IEventManager interface exposes
        //       this property at all, since doing so is contrary to the best
        //       practices normally used when implementing the IDisposable
        //       interface [and "pattern"].  The reasoning is that the event
        //       manager properties and methods are called from various places
        //       where asynchronous event processing may have occurred and the
        //       event manager itself may end up being disposed out from under
        //       the running code.  Therefore, this property can be used to
        //       check for this problem from the few critical places in the
        //       code where this kind of safety check is required.
        //
        public bool Disposed
        {
            get
            {
                //
                // NOTE: Obviously, this would be pointless.
                //
                // CheckDisposed();

                lock (syncRoot)
                {
                    return disposed;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISimpleSynchronize Members
        /* DANGEROUS: EXTERNAL USE ONLY. */
        public bool TryLock() /* NOT USED BY CORE */
        {
            CheckDisposed();

            if (syncRoot == null)
                return false;

            return Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /* DANGEROUS: EXTERNAL USE ONLY. */
        public void Lock() /* NOT USED BY CORE */
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            Monitor.Enter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /* DANGEROUS: EXTERNAL USE ONLY. */
        public void Unlock() /* NOT USED BY CORE */
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            Monitor.Exit(syncRoot);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /* DANGEROUS: EXTERNAL USE ONLY. */
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        /* EXTERNAL USE ONLY. */
        public void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /* EXTERNAL USE ONLY. */
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////

        /* EXTERNAL USE ONLY. */
        public void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        /* EXTERNAL USE ONLY. */
        public void ExitLock(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEventManager Members
        public int QueueEventCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return queueCount;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int QueueIdleEventCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return queueIdleCount;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int EventCount
        {
            get
            {
                CheckDisposed();

                return GetEventCount(false);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int IdleEventCount
        {
            get
            {
                CheckDisposed();

                return GetEventCount(true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int TotalEventCount
        {
            get
            {
                CheckDisposed();

                return GetTotalEventCount();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaximumEventCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return maximumCount;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaximumIdleEventCount
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return maximumIdleCount;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int MaybeDisposeEventCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref maybeDisposeCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int ReallyDisposeEventCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref reallyDisposeCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int WaitForEmptyQueueTotalCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref waitForEmptyQueueTotalCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int WaitForEmptyQueueErrorCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref waitForEmptyQueueErrorCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int WaitForEventEnqueuedTotalCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref waitForEventEnqueuedTotalCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int WaitForEventEnqueuedErrorCount
        {
            get
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref waitForEventEnqueuedErrorCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public EventWaitHandle EmptyEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return emptyEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EventWaitHandle EnqueueEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return enqueueEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EventWaitHandle IdleEmptyEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return idleEmptyEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EventWaitHandle IdleEnqueueEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return idleEnqueueEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EventWaitHandle[] UserEvents
        {
            get
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (userEvents == null)
                        return null;

                    int length = userEvents.Length;
                    EventWaitHandle[] result = new EventWaitHandle[length];

                    Array.Copy(userEvents, result, length); /* throw */

                    return result;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (value != null)
                    {
                        int length = value.Length;
                        EventWaitHandle[] result = new EventWaitHandle[length];

                        Array.Copy(value, result, length); /* throw */

                        //
                        // NOTE: Replace the user events.  Do not dispose of
                        //       them because we do not own them.
                        //
                        userEvents = result;
                    }
                    else
                    {
                        //
                        // NOTE: Clear out the user events.  Do not dispose
                        //       of them because we do not own them.
                        //
                        userEvents = null;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Enabled
        {
            get
            {
                CheckDisposed();

                return IsEnabled();
            }
            set
            {
                CheckDisposed();

                if (value)
                    Interlocked.Increment(ref enabled);
                else
                    Interlocked.Decrement(ref enabled);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Active
        {
            get
            {
                CheckDisposed();

                return IsActive();
            }
            set
            {
                CheckDisposed();

                if (value)
                    EnterLevel();
                else
                    ExitLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool NoNotify
        {
            get
            {
                CheckDisposed();

                return IsNoNotify();
            }
            set
            {
                CheckDisposed();

                if (value)
                    Interlocked.Increment(ref noNotify);
                else
                    Interlocked.Decrement(ref noNotify);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public DateTimeNowCallback NowCallback
        {
            get { CheckDisposed(); lock (syncRoot) { return nowCallback; } }
            set { CheckDisposed(); lock (syncRoot) { nowCallback = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public void SaveEnabledAndForceDisabled(
            ref int savedEnabled
            )
        {
            CheckDisposed();

            savedEnabled = Interlocked.Exchange(ref enabled, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RestoreEnabled(
            int savedEnabled
            )
        {
            CheckDisposed();

            return Interlocked.Exchange(ref enabled, savedEnabled) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Dump(
            ref Result result
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    StringList list = new StringList();

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Process all the events form this queue, in
                        //       order.
                        //
                        for (int index = 0; index < eventQueue.Count; index++)
                        {
                            IEvent localEvent = eventQueue[index];

                            //
                            // TODO: Is there a point to including null events?
                            //
                            if (localEvent == null)
                                continue;

                            //
                            // NOTE: Return the event information as a string
                            //       element in the resulting list.
                            //
                            list.Add(EventToList(index, localEvent));
                        }
                    }

                    result = list;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ClearEvents(
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Are we dealing with idle events now?
                        //
                        bool idle = IsIdleEventQueue(eventQueue);

                        //
                        // NOTE: Clear this event queue now.
                        //
                        eventQueue.Clear(true, false);

#if NOTIFY
                        if (!IsNoNotify() && (interpreter != null))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                (idle ? NotifyType.Idle : NotifyType.None) |
                                    NotifyType.Event, NotifyFlags.Cleared,
                                null, interpreter,
                                null, null, null, ref error);
                        }
#endif
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode PeekEvent( /* NOT USED BY CORE */
            bool strict,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: If there is an event, we are done.
                        //
                        if (eventQueue.Count > 0)
                        {
                            @event = eventQueue.Peek();
                            return ReturnCode.Ok;
                        }
                    }

                    if (strict)
                    {
                        error = "no events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    if (strict)
                    {
                        error = "not accepting events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode GetEvent(
            string name,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Process all the events form this queue, in
                        //       order, searching by event "id"...
                        //
                        for (int index = 0; index < eventQueue.Count; index++)
                        {
                            IEvent localEvent = eventQueue[index];

                            //
                            // TODO: Prevent returning a null event?
                            //
                            if (localEvent == null)
                                continue;

                            //
                            // NOTE: Does this event match based on its name?
                            //       If the name is null, any event will match.
                            //
                            if (MatchEventName(localEvent, name))
                            {
                                @event = localEvent;
                                return ReturnCode.Ok;
                            }
                        }
                    }

                    error = String.Format(
                        "event \"{0}\" doesn't exist",
                        name);
                }
                else
                {
                    error = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DiscardEvent( /* NOT USED BY CORE */
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Are we dealing with idle events now?
                        //
                        bool idle = IsIdleEventQueue(eventQueue);

                        //
                        // NOTE: Dequeue one event from the queue and
                        //       mark it as discarded.
                        //
                        IEvent localEvent = eventQueue.Dequeue();
                        Event.MarkDequeuedAndDiscarded(localEvent);

                        //
                        // HACK: The event can (only) be disposed at
                        //       this point if we know it was flagged
                        //       as "fire-and-forget"; otherwise, it
                        //       could [still] be used to fetch its
                        //       result, even though it is now being
                        //       discarded.
                        //
                        Event.MaybeDispose(localEvent);

                        //
                        // NOTE: Check if the event queue is empty at
                        //       this point and raise a signal if so.
                        //
                        if (CheckForEmptyQueue(idle))
                            SignalEmptyQueue(idle);

#if NOTIFY
                        if (!IsNoNotify() && (interpreter != null))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                (idle ? NotifyType.Idle : NotifyType.None) |
                                    NotifyType.Event, NotifyFlags.Discarded,
                                localEvent, interpreter,
                                null, null, null);
                        }
#endif

                        return ReturnCode.Ok;
                    }

                    if (strict)
                        error = "no events";
                    else
                        return ReturnCode.Ok;
                }
                else
                {
                    if (strict)
                        error = "not accepting events";
                    else
                        return ReturnCode.Ok;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DequeueEvent( /* NOT USED BY CORE */
            bool strict,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Are we dealing with idle events now?
                        //
                        bool idle = IsIdleEventQueue(eventQueue);

                        //
                        // NOTE: Dequeue one event from the queue and
                        //       mark it as dequeued.
                        //
                        IEvent localEvent = eventQueue.Dequeue();
                        Event.MarkDequeued(localEvent);

                        @event = localEvent;

                        //
                        // NOTE: Check if the event queue is empty at
                        //       this point and raise a signal if so.
                        //
                        if (CheckForEmptyQueue(idle))
                            SignalEmptyQueue(idle);

#if NOTIFY
                        if (!IsNoNotify() && (interpreter != null))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                (idle ? NotifyType.Idle : NotifyType.None) |
                                    NotifyType.Event, NotifyFlags.Dequeued,
                                new ObjectPair(strict, @event), interpreter,
                                null, null, null, ref error);
                        }
#endif

                        return ReturnCode.Ok;
                    }

                    if (strict)
                    {
                        error = "no events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    if (strict)
                    {
                        error = "not accepting events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueEvent(
            string name,
            DateTime dateTime,
            EventCallback callback,
            IClientData clientData,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref Result error
            )
        {
            CheckDisposed();

            IEvent @event = null;

            return QueueEvent(name, dateTime, callback, clientData,
                eventFlags | EventFlags.FireAndForget, priority,
                threadId, limit, ref @event, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueEvent(
            string name,
            DateTime dateTime,
            EventCallback callback,
            IClientData clientData,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool idle = IsIdleEvent(eventFlags);
                EventQueue eventQueue = GetEventQueue(idle);

                if (eventQueue != null)
                {
                    int oldCount = eventQueue.Count;

                    if ((limit <= 0) || (oldCount < limit))
                    {
                        EventPriority oldPriority = priority;

                        priority = GetAutomaticEventPriority(
                            eventFlags, priority);

                        IEvent localEvent = Event.Create(
                            new object(), null, EventType.Callback,
                            eventFlags | EventFlags.Queued |
                                EventFlags.UnknownThread |
                                EventFlags.Internal,
                            priority, interpreter, name, dateTime,
                            callback, threadId, clientData, ref error);

                        if (localEvent == null)
                            return ReturnCode.Error;

                        EventQueueKey key = CreateEventQueueKey(
                            priority, dateTime);

                        eventQueue.Enqueue(key, localEvent);

                        int newCount = eventQueue.Count;
                        int newMaximumCount = Count.Invalid;

                        if (!idle)
                        {
                            queueCount++;

                            if (newCount > maximumCount)
                                newMaximumCount = maximumCount = newCount;
                        }
                        else
                        {
                            queueIdleCount++;

                            if (newCount > maximumIdleCount)
                                newMaximumCount = maximumIdleCount = newCount;
                        }

                        if (newMaximumCount != Count.Invalid)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "QueueEvent: maximum {0}event count " +
                                "exceeded, interpreter = {1}, dateTime = {2}, " +
                                "callback = {3}, clientData = {4}, " +
                                "eventFlags = {5}, oldPriority = {6}, " +
                                "priority = {7}, threadId = {8}, limit = {9}, " +
                                "{10} = {11}, {12} = {13}",
                                idle ? "idle " : String.Empty,
                                FormatOps.InterpreterNoThrow(interpreter),
                                FormatOps.WrapOrNull(
                                    FormatOps.Iso8601FullDateTime(dateTime)),
                                FormatOps.WrapOrNull(
                                    FormatOps.DelegateName(callback)),
                                FormatOps.WrapOrNull(clientData),
                                FormatOps.WrapOrNull(eventFlags),
                                FormatOps.WrapOrNull(oldPriority),
                                FormatOps.WrapOrNull(priority),
                                FormatOps.WrapOrNull(threadId), limit,
                                idle ? "idleCount" : "eventCount", newCount,
                                idle ? "maximumIdleCount" : "maximumCount",
                                newMaximumCount), typeof(EventManager).Name,
                                TracePriority.EventDebug);
                        }

                        SignalEventEnqueued(idle);

#if NOTIFY
                        if (!IsNoNotify() && (interpreter != null))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                (idle ? NotifyType.Idle : NotifyType.None) |
                                    NotifyType.Event, NotifyFlags.Queued,
                                new ObjectList(dateTime, eventFlags,
                                    priority, threadId, localEvent),
                                interpreter, clientData, null, null,
                                ref error);
                        }
#endif

                        @event = localEvent;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "{0}event limit would be exceeded",
                            idle ? "idle " : String.Empty);
                    }
                }
                else
                {
                    error = String.Format(
                        "not accepting {0}events",
                        idle ? "idle " : String.Empty);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref Result error
            )
        {
            CheckDisposed();

            IEvent @event = null;

            return QueueScript(
                dateTime, text, engineFlags, substitutionFlags,
                eventFlags | EventFlags.FireAndForget, expressionFlags,
                priority, threadId, limit, ref @event, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            string name = FormatOps.Id(ScriptTypes.Queue, null,
                GlobalState.NextId(interpreter));

            IScript script = Script.Create(
                name, null, null, ScriptTypes.Queue, text, dateTime,
                EngineMode.EvaluateScript, ScriptFlags.None, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ClientData.Empty);

            return QueueEvent(name, dateTime, ScriptEventCallback,
                new ClientData(script), eventFlags, priority, threadId,
                limit, ref @event, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueScript(
            string name,
            DateTime dateTime,
            IScript script,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref Result error
            )
        {
            CheckDisposed();

            IEvent @event = null;

            return QueueScript(name, dateTime, script,
                eventFlags | EventFlags.FireAndForget,
                priority, threadId, limit, ref @event,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode QueueScript(
            string name,
            DateTime dateTime,
            IScript script,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            return QueueEvent(name, dateTime, ScriptEventCallback,
                new ClientData(script), eventFlags, priority,
                threadId, limit, ref @event, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DequeueAnyReadyEvent(
            DateTime dateTime,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            bool strict,
            ref IEvent @event,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    int count = GetTotalEventCount();

                    if (count > 0)
                    {
                        if (DequeueAnyReadyEvent(
                                events, dateTime, eventFlags,
                                priority, threadId, true,
                                ref @event, ref error) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }

                        int nonIdlecount = GetEventCount(false);

                        if (ShouldDequeueIdleEvent(eventFlags, nonIdlecount))
                        {
                            if (DequeueAnyReadyEvent(
                                    idleEvents, dateTime, eventFlags,
                                    priority, threadId, true,
                                    ref @event, ref error) == ReturnCode.Ok)
                            {
                                return ReturnCode.Ok;
                            }
                        }
                    }
                    else
                    {
                        if (strict)
                        {
                            error = "no events";
                        }
                        else
                        {
                            @event = null;
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    if (strict)
                    {
                        error = "not accepting events";
                    }
                    else
                    {
                        @event = null;
                        return ReturnCode.Ok;
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ListEvents(
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    StringList localList = new StringList();

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        if (GenericOps<string>.FilterList(
                                new StringList(eventQueue.Values),
                                localList, Index.Invalid, Index.Invalid,
                                ToStringFlags.None, pattern, noCase,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }

                    list = localList;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ListEvents( /* NOT USED BY CORE */
            EventMatchCallback callback,
            IClientData clientData,
            ref IEnumerable<IEvent> events,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        this.events, idleEvents
                    };

                    IList<IEvent> result = new List<IEvent>();

                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Process all the events form this queue, in
                        //       order.
                        //
                        for (int index = 0; index < eventQueue.Count; index++)
                        {
                            IEvent localEvent = eventQueue[index];

                            //
                            // TODO: Is there a point to including null events?
                            //
                            if (localEvent == null)
                                continue;

                            //
                            // NOTE: Does this event match based on the provided
                            //       callback, if any?  If there is no callback,
                            //       all (valid) events match.
                            //
                            if (MatchEvent(localEvent, callback, clientData))
                                result.Add(localEvent);
                        }
                    }

                    events = result;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode CancelEvents(
            string nameOrScript,
            bool strict,
            bool all,
            ref Result error
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveAnyEventQueue())
                {
                    EventQueue[] eventQueues = {
                        events, idleEvents
                    };

                    //
                    // NOTE: Have we actually removed any events yet?  If so,
                    //       how many?
                    //
                    int count = 0;

                    //
                    // NOTE: First, search all events by their "id"...
                    //
                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Are we dealing with idle events now?
                        //
                        bool idle = IsIdleEventQueue(eventQueue);

                        //
                        // NOTE: Process all the events form this queue, in
                        //       reverse order, possibly stopping after the
                        //       first match.
                        //
                        for (int index = eventQueue.Count - 1; index >= 0; index--)
                        {
                            IEvent localEvent = eventQueue[index];

                            if (localEvent == null)
                                continue;

                            //
                            // NOTE: Does this event match based on its name?
                            //       If the name is null, any event will match.
                            //
                            if (MatchEventName(localEvent, nameOrScript))
                            {
                                Event.MarkDequeuedAndCanceled(localEvent);
                                eventQueue.RemoveAt(index);

                                //
                                // HACK: The event can (only) be disposed at
                                //       this point if we know it was flagged
                                //       as "fire-and-forget"; otherwise, it
                                //       could [still] be used to fetch its
                                //       result, even though it is now being
                                //       canceled.
                                //
                                Event.MaybeDispose(localEvent);

#if NOTIFY
                                if (!IsNoNotify() && (interpreter != null))
                                {
                                    /* IGNORED */
                                    interpreter.CheckNotification(
                                        (idle ? NotifyType.Idle : NotifyType.None) |
                                            NotifyType.Event, NotifyFlags.Canceled,
                                        new ObjectPair(nameOrScript, localEvent),
                                        interpreter, null, null, null, ref error);
                                }
#endif

                                if (all)
                                {
                                    count++;
                                }
                                else
                                {
                                    //
                                    // NOTE: Check if the event queue is empty at
                                    //       this point and raise a signal if so.
                                    //
                                    if (CheckForEmptyQueue(idle))
                                        SignalEmptyQueue(idle);

                                    return ReturnCode.Ok;
                                }
                            }
                        }
                    }

                    //
                    // NOTE: Finally, search all events by their script text...
                    //
                    foreach (EventQueue eventQueue in eventQueues)
                    {
                        //
                        // NOTE: Just skip over null event queues.
                        //
                        if (eventQueue == null)
                            continue;

                        //
                        // NOTE: Are we dealing with idle events now?
                        //
                        bool idle = IsIdleEventQueue(eventQueue);

                        //
                        // NOTE: Process all the events form this queue, in
                        //       reverse order, possibly stopping after the
                        //       first match.
                        //
                        for (int index = eventQueue.Count - 1; index >= 0; index--)
                        {
                            IEvent localEvent = eventQueue[index];

                            if (localEvent == null)
                                continue;

                            if (!IsScriptEvent(localEvent))
                                continue;

                            IClientData clientData = localEvent.ClientData;

                            if (clientData == null)
                                continue;

                            IScript script = clientData.Data as IScript;

                            if (script == null)
                                continue;

                            lock (script.SyncRoot) /* TRANSACTIONAL */
                            {
                                //
                                // NOTE: Does this event match based on its script?
                                //       If the script is null, any event will match.
                                //
                                if (MatchScriptText(script, nameOrScript))
                                {
                                    Event.MarkDequeuedAndCanceled(localEvent);
                                    eventQueue.RemoveAt(index);

                                    //
                                    // HACK: The event can (only) be disposed at
                                    //       this point if we know it was flagged
                                    //       as "fire-and-forget"; otherwise, it
                                    //       could [still] be used to fetch its
                                    //       result, even though it is now being
                                    //       canceled.
                                    //
                                    Event.MaybeDispose(localEvent);

#if NOTIFY
                                    if (!IsNoNotify() && (interpreter != null))
                                    {
                                        /* IGNORED */
                                        interpreter.CheckNotification(
                                            (idle ? NotifyType.Idle : NotifyType.None) |
                                                NotifyType.Event, NotifyFlags.Canceled,
                                            new ObjectPair(nameOrScript, localEvent),
                                            interpreter, null, null, null, ref error);
                                    }
#endif

                                    if (all)
                                    {
                                        count++;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: Check if the event queue is empty at
                                        //       this point and raise a signal if so.
                                        //
                                        if (CheckForEmptyQueue(idle))
                                            SignalEmptyQueue(idle);

                                        return ReturnCode.Ok;
                                    }
                                }
                            }
                        }
                    }

                    //
                    // NOTE: In strict mode, if we matched zero events, this is
                    //       a failure; otherwise, success.
                    //
                    if (!strict || (count > 0))
                    {
                        //
                        // NOTE: Only signal an empty queue if we actually
                        //       removed something.  First, check the non-idle
                        //       queue and then the idle queue.
                        //
                        if (count > 0)
                        {
                            foreach (bool idle in new bool[] { false, true })
                            {
                                //
                                // NOTE: We do not rely on the count calculated
                                //       by the above code (in this method) for
                                //       anything EXCEPT as a general indicator
                                //       that one or more event queues MAY now
                                //       be empty.  Check each event queue and
                                //       see if it is now empty.  If so, signal
                                //       it as empty.
                                //
                                if (CheckForEmptyQueue(idle))
                                    SignalEmptyQueue(idle);
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "event \"{0}\" doesn't exist",
                            nameOrScript);
                    }
                }
                else
                {
                    error = "not accepting events";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetSleepTime(
            SleepType sleepType
            )
        {
            CheckDisposed();

            int sleepTime;

            if (TryGetSleepTime(sleepType, false, out sleepTime))
                return sleepTime;

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            int result = DefaultSleepTime;

            if (interpreter != null)
                result = interpreter.SleepTime;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetSleepTime(
            SleepType sleepType,
            int? sleepTime
            )
        {
            CheckDisposed();

            return TrySetSleepTime(sleepType, false, sleepTime);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetMinimumSleepTime(
            SleepType sleepType
            )
        {
            CheckDisposed();

            int result = GetSleepTime(sleepType);

            if (result < MinimumSleepTime)
            {
                int sleepTime;

                if (TryGetSleepTime(sleepType, true, out sleepTime))
                    result = sleepTime;
                else
                    result = MinimumSleepTime;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetMinimumSleepTime(
            SleepType sleepType,
            int? sleepTime
            )
        {
            CheckDisposed();

            return TrySetSleepTime(sleepType, true, sleepTime);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Sleep(
            SleepType sleepType,
            bool minimum,
            ref Result error
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            int milliseconds = minimum ?
                GetMinimumSleepTime(sleepType) :
                GetSleepTime(sleepType);

            if (HostOps.Sleep(
                    interpreter, milliseconds,
                    ref error) == ReturnCode.Ok)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Yield(
            ref Result error
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (HostOps.Yield(
                    interpreter, false,
                    ref error) == ReturnCode.Ok)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ProcessEvents( /* NOT USED BY CORE */
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool stopOnError,
            bool errorOnEmpty,
            ref Result result
            )
        {
            CheckDisposed();

            int eventCount = 0;

            return ProcessEvents(
                eventFlags, priority, threadId, limit, stopOnError,
                errorOnEmpty, ref eventCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ProcessEvents(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool stopOnError,
            bool errorOnEmpty,
            ref int eventCount,
            ref Result result
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            if (IsEnabled())
            {
                EnterLevel();

                try
                {
                    //
                    // NOTE: Probe for an available event in the queue.
                    //
                    bool bgError = !FlagOps.HasFlags(
                        eventFlags, EventFlags.NoBgError, true);

                    IEvent localEvent = null;
                    int count = 0;
                    ResultList errors = null;

                    while (IsEnabled() &&
                        ((limit <= 0) || (count++ < limit)) &&
                        (DequeueAnyReadyEvent(
                            GetNow(), eventFlags, priority, threadId,
                            false, ref localEvent) == ReturnCode.Ok) &&
                        (localEvent != null))
                    {
                        //
                        // NOTE: Just skip over events that have no callback
                        //       delegate.
                        //
                        EventCallback eventCallback = localEvent.Callback;

                        if (eventCallback == null)
                        {
                            MaybeDispose(eventFlags, ref localEvent);
                            continue;
                        }

                        Interpreter eventInterpreter = localEvent.Interpreter;
                        IClientData eventClientData = localEvent.ClientData;
                        Result localResult = null;

                        try
                        {
                            //
                            // NOTE: Invoke the callback delegate for this
                            //       event.
                            //
                            code = eventCallback(
                                eventInterpreter, eventClientData,
                                ref localResult);

                            //
                            // NOTE: Mark this event as "completed", since it
                            //       was dequeued and executed.  This doesn't
                            //       imply that it was successful.
                            //
                            Event.MarkCompleted(localEvent);

                            //
                            // NOTE: Increment the number of events that were
                            //       processed by this call.
                            //
                            eventCount++;

                            //
                            // NOTE: Set the code and result for this event
                            //       object and signal it as ready.
                            //
                            Result setError = null;

                            //
                            // HACK: The error line is unknown here because
                            //       we do not even know if a script was
                            //       evaluated.
                            //
                            if (!localEvent.SetResult(
                                    true, code, localResult, 0, ref setError))
                            {
                                DebugOps.Complain(
                                    eventInterpreter, ReturnCode.Error,
                                    setError);
                            }

                            //
                            // NOTE: Now, handle the callback return code.
                            //
                            if (code == ReturnCode.Return)
                            {
                                //
                                // NOTE: Skip process all further events
                                //       and return an overall success.
                                //
                                code = ReturnCode.Ok;

                                MaybeDispose(eventFlags, ref localEvent);
                                break;
                            }

                            if (code == ReturnCode.Continue)
                            {
                                //
                                // NOTE: Skip background error processing
                                //       and re-checking of the interpreter
                                //       event processing flag.
                                //
                                // BUGFIX: Make sure to [re-]set the code
                                //         here to Ok; otherwise, callers
                                //         may think this method failed.
                                //         This is an issue if this is the
                                //         last event being processed prior
                                //         to the while loop exiting.
                                //
                                code = ReturnCode.Ok;

                                MaybeDispose(eventFlags, ref localEvent);
                                continue;
                            }

                            if (code != ReturnCode.Ok)
                            {
                                //
                                // NOTE: This is considered to be an error,
                                //       save it.
                                //
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localResult);

                                //
                                // NOTE: Ok, now check if we need to stop
                                //       processing further events.
                                //
                                if (code == ReturnCode.Break)
                                {
                                    //
                                    // NOTE: Skip background error
                                    //       processing and all further
                                    //       events and then return an
                                    //       overall failure.
                                    //
                                    code = ReturnCode.Error;

                                    MaybeDispose(eventFlags, ref localEvent);
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            localResult = e;

                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localResult);
                            code = ReturnCode.Error;
                        }

                        //
                        // NOTE: Was there an error and should be handle it
                        //       via the background error handler?
                        //
                        if ((code != ReturnCode.Ok) && bgError)
                        {
                            //
                            // NOTE: If there is no interpreter context, do
                            //       nothing.
                            //
                            if (eventInterpreter != null)
                            {
                                /* IGNORED */
                                EventOps.HandleBackgroundError(
                                    eventInterpreter, code, localResult,
                                    ref bgError);
                            }
                        }

                        //
                        // NOTE: Did we hit an error?
                        //
                        if (code != ReturnCode.Ok)
                        {
                            //
                            // NOTE: Are we stopping upon hitting an error?
                            //       If so, stop now.  Also, always stop if
                            //       the interpreter has been disposed.
                            //
                            if (stopOnError || Interpreter.IsDeletedOrDisposed(
                                    eventInterpreter, false))
                            {
                                //
                                // NOTE: Stop on the first error.
                                //
                                MaybeDispose(eventFlags, ref localEvent);
                                break;
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "ProcessEvents: error ignored, " +
                                    "eventInterpreter = {0}, eventFlags = {1}, " +
                                    "priority = {2}, threadId = {3}, " +
                                    "limit = {4}, stopOnError = {5}, " +
                                    "errorOnEmpty = {6}, eventCount = {7}, " +
                                    "code = {8}, localResult = {9}, " +
                                    "result = {10}",
                                    FormatOps.InterpreterNoThrow(eventInterpreter),
                                    FormatOps.WrapOrNull(eventFlags), priority,
                                    FormatOps.WrapOrNull(threadId), limit,
                                    stopOnError, errorOnEmpty, eventCount, code,
                                    FormatOps.WrapOrNull(true, true, localResult),
                                    FormatOps.WrapOrNull(true, true, result)),
                                    typeof(EventManager).Name,
                                    TracePriority.EventDebug);

                                //
                                // NOTE: Ignore the error and keep going.
                                //
                                code = ReturnCode.Ok;
                            }
                        }

                        MaybeDispose(eventFlags, ref localEvent);
                    }

                    if (errorOnEmpty)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "no more events are ready, processed: {0}",
                            count));

                        code = ReturnCode.Error;
                    }

                    if (errors != null)
                        result = errors;
                }
                finally
                {
                    ExitLevel();
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DoOneEvent( /* NOT USED BY CORE */
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit, /* NOTE: Pass zero for ALL. */
            bool stopOnError,
            bool errorOnEmpty,
            bool userInterface,
            ref Result result
            )
        {
            CheckDisposed();

            int eventCount = 0;

            return DoOneEvent(
                eventFlags, priority, threadId, limit, stopOnError,
                errorOnEmpty, userInterface, ref eventCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DoOneEvent(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit, /* NOTE: Pass zero for ALL. */
            bool stopOnError,
            bool errorOnEmpty,
            bool userInterface,
            ref int eventCount,
            ref Result result
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS && WINFORMS
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }
#endif

            //
            // NOTE: Attempt to process some [or all] pending events [possibly]
            //       stopping if an error is encountered.
            //
            ReturnCode code = ProcessEvents(
                eventFlags, priority, threadId, limit, stopOnError,
                errorOnEmpty, ref eventCount, ref result);

#if WINFORMS
            //
            // NOTE: If necessary, process all Windows messages from the queue.
            //
            if ((code == ReturnCode.Ok) && userInterface)
                code = WindowOps.ProcessEvents(interpreter, ref result);
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ServiceEvents(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool noCancel,
            bool noGlobalCancel,
            bool stopOnError,
            bool errorOnEmpty,
            bool userInterface,
            ref Result result
            )
        {
            CheckDisposed();

            int eventCount = 0;

            return ServiceEvents(
                eventFlags, priority, threadId, limit, noCancel,
                noGlobalCancel, stopOnError, errorOnEmpty,
                userInterface, ref eventCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ServiceEvents(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool noCancel,
            bool noGlobalCancel,
            bool stopOnError,
            bool errorOnEmpty,
            bool userInterface,
            ref int eventCount,
            ref Result result
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            ReturnCode code;

            //
            // NOTE: Keep processing asynchronous events until the interpreter
            //       is no longer valid.
            //
            while ((code = Interpreter.EventReady(
                    interpreter, noCancel, noGlobalCancel,
                    ref result)) == ReturnCode.Ok)
            {
                //
                // NOTE: Attempt to process some [or all] pending events [possibly]
                //       stopping if an error is encountered.
                //
                code = DoOneEvent(
                    eventFlags, priority, threadId, limit, stopOnError,
                    errorOnEmpty, userInterface, ref eventCount, ref result);

                //
                // NOTE: If we encountered an error processing events, break out
                //       of the loop and return the error code and result to the
                //       caller.
                //
                if (code != ReturnCode.Ok)
                    break;

                //
                // NOTE: We always yield to other running threads.  This also gives
                //       them an opportunity to cancel the script in progress on
                //       this thread and/or update the variable we are waiting for.
                //
                // TODO: This default is way too fast.  We may want to introduce a
                //       slight delay (half a second?) here.
                //
                code = HostOps.Sleep(
                    interpreter, GetMinimumSleepTime(SleepType.Service),
                    ref result);

                //
                // NOTE: If we encountered an error sleeping, break out of the
                //       loop and return the error code and result to the caller.
                //
                if (code != ReturnCode.Ok)
                    break;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode WaitForEmptyQueue(
            int timeout,
            bool idle,
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            Interlocked.Increment(ref waitForEmptyQueueTotalCount);
            EventWaitHandle emptyEvent = GetEmptyEvent(idle);

            if (emptyEvent != null)
            {
                //
                // BUGFIX: Do not try waiting for an empty queue if it is
                //         already empty.  However, we must at least try to
                //         wait [without blocking this thread]; otherwise, if
                //         the event is currently signaled will still be
                //         signaled when the next caller attempts to wait.
                //
                if (ThreadOps.WaitEvent(emptyEvent, 0))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEmptyQueue: {0}queue was already emptied",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (!strict && CheckForEmptyQueue(idle))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEmptyQueue: {0}queue is already empty",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (ThreadOps.WaitEvent(emptyEvent, timeout))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEmptyQueue: {0}queue emptied after waiting",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (!strict && CheckForEmptyQueue(idle))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEmptyQueue: {0}queue emptied after timeout",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEmptyQueue: {0}timeout, {1} milliseconds",
                        idle ? "idle " : String.Empty, timeout),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    error = "failed to wait for empty queue";
                }
            }
            else
            {
                error = "cannot wait for empty queue";
            }

            Interlocked.Increment(ref waitForEmptyQueueErrorCount);
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode WaitForEventEnqueued(
            int timeout,
            bool idle,
            bool strict,
            ref Result error
            )
        {
            CheckDisposed();

            Interlocked.Increment(ref waitForEventEnqueuedTotalCount);
            EventWaitHandle enqueueEvent = GetEnqueueEvent(idle);

            if (enqueueEvent != null)
            {
                if (ThreadOps.WaitEvent(enqueueEvent, 0))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEventEnqueued: {0}event was already enqueued",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (!strict && CheckForEventEnqueued(idle))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEventEnqueued: {0}event is already enqueued",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (ThreadOps.WaitEvent(enqueueEvent, timeout))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEventEnqueued: {0}event enqueued after waiting",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else if (!strict && CheckForEventEnqueued(idle))
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEventEnqueued: {0}event enqueued after timeout",
                        idle ? "idle " : String.Empty),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    return ReturnCode.Ok;
                }
                else
                {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "WaitForEventEnqueued: {0}timeout, {1} milliseconds",
                        idle ? "idle " : String.Empty, timeout),
                        typeof(EventManager).Name,
                        TracePriority.EventDebug);
#endif

                    error = "failed to wait for event enqueued";
                }
            }
            else
            {
                error = "cannot wait for event enqueued";
            }

            Interlocked.Increment(ref waitForEventEnqueuedErrorCount);
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(EventManager));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(EventManager).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        lastNow = DateTime.MinValue;

                        maximumCount = 0;
                        maximumIdleCount = 0;

                        queueCount = 0;
                        queueIdleCount = 0;

                        Interlocked.Exchange(ref maybeDisposeCount, 0);
                        Interlocked.Exchange(ref reallyDisposeCount, 0);

                        Interlocked.Exchange(ref waitForEmptyQueueTotalCount, 0);
                        Interlocked.Exchange(ref waitForEmptyQueueErrorCount, 0);

                        Interlocked.Exchange(ref waitForEventEnqueuedTotalCount, 0);
                        Interlocked.Exchange(ref waitForEventEnqueuedErrorCount, 0);

                        Interlocked.Exchange(ref enabled, 0);
                        Interlocked.Exchange(ref levels, 0);
                        Interlocked.Exchange(ref noNotify, 0);

                        if (events != null)
                        {
                            events.Dispose();
                            events = null;
                        }

                        if (idleEvents != null)
                        {
                            idleEvents.Dispose();
                            idleEvents = null;
                        }

                        //
                        // NOTE: Close the queue events (that we own).
                        //
                        ThreadOps.CloseEvent(ref emptyEvent);
                        ThreadOps.CloseEvent(ref enqueueEvent);
                        ThreadOps.CloseEvent(ref idleEmptyEvent);
                        ThreadOps.CloseEvent(ref idleEnqueueEvent);

                        //
                        // NOTE: Clear out the user events array (i.e. do not
                        //       dispose it as we do not own it).
                        //
                        if (userEvents != null)
                            userEvents = null; /* NOT OWNED, DO NOT DISPOSE. */

                        //
                        // NOTE: Clear out the parent interpreter (i.e. do not
                        //       dispose it as we do not own it).
                        //
                        if (interpreter != null)
                            interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~EventManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
