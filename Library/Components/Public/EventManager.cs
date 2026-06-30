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
    /// <summary>
    /// This class manages the event queues associated with an Eagle
    /// interpreter.  It implements the machinery behind asynchronous and
    /// deferred script execution (e.g. the <c>after</c> and <c>vwait</c>
    /// commands), maintaining both a normal event queue and a separate idle
    /// event queue, each ordered by event priority and scheduled time.  Events
    /// may be queued, peeked, dequeued, listed, canceled, and serviced; the
    /// class also exposes the wait handles and sleep/yield helpers used to
    /// coordinate event processing across threads.  It implements
    /// <see cref="IEventManager" /> and is disposable.
    /// </summary>
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
        /// <summary>
        /// The default amount of time, in milliseconds, to sleep when no more
        /// specific sleep time has been configured.
        /// </summary>
        internal static readonly int DefaultSleepTime = 0;

        /// <summary>
        /// The minimum amount of time, in milliseconds, that may be used as a
        /// sleep time.
        /// </summary>
        internal static readonly int MinimumSleepTime = 50;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The minimum amount of time, in milliseconds, by which the value
        /// returned from the "now" callback must advance between successive
        /// calls.
        /// </summary>
        internal static readonly int MinimumEventTime = 1;

        /// <summary>
        /// The minimum amount of time, in milliseconds, to wait while idle.
        /// </summary>
        internal static readonly int MinimumIdleWaitTime = 1000;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, failures encountered during event processing are not
        /// reported via the complaint mechanism.
        /// </summary>
        internal static bool DefaultNoComplain = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the data in this event
        /// manager.
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// The high-water mark for the number of events in the normal event
        /// queue.
        /// </summary>
        private int maximumCount;

        /// <summary>
        /// The high-water mark for the number of events in the idle event
        /// queue.
        /// </summary>
        private int maximumIdleCount;

        /// <summary>
        /// The running total of events queued to the normal event queue.
        /// </summary>
        private int queueCount;

        /// <summary>
        /// The running total of events queued to the idle event queue.
        /// </summary>
        private int queueIdleCount;

        /// <summary>
        /// The number of times an event was considered for disposal.
        /// </summary>
        private int maybeDisposeCount;

        /// <summary>
        /// The number of times an event was actually disposed.
        /// </summary>
        private int reallyDisposeCount;

        /// <summary>
        /// The total number of times a caller waited for an event queue to
        /// become empty.
        /// </summary>
        private int waitForEmptyQueueTotalCount;

        /// <summary>
        /// The number of times waiting for an event queue to become empty
        /// resulted in an error.
        /// </summary>
        private int waitForEmptyQueueErrorCount;

        /// <summary>
        /// The total number of times a caller waited for an event to be
        /// enqueued.
        /// </summary>
        private int waitForEventEnqueuedTotalCount;

        /// <summary>
        /// The number of times waiting for an event to be enqueued resulted in
        /// an error.
        /// </summary>
        private int waitForEventEnqueuedErrorCount;

        /// <summary>
        /// The most recent value returned by the "now" callback, used to
        /// guarantee that time advances monotonically.
        /// </summary>
        private DateTime lastNow;

        /// <summary>
        /// The normal (non-idle) event queue.
        /// </summary>
        private EventQueue events;

        /// <summary>
        /// The idle event queue.
        /// </summary>
        private EventQueue idleEvents;

        /// <summary>
        /// The wait handle signaled when the normal event queue becomes empty.
        /// </summary>
        private EventWaitHandle emptyEvent;

        /// <summary>
        /// The wait handle signaled when an event is enqueued to the normal
        /// event queue.
        /// </summary>
        private EventWaitHandle enqueueEvent;

        /// <summary>
        /// The wait handle signaled when the idle event queue becomes empty.
        /// </summary>
        private EventWaitHandle idleEmptyEvent;

        /// <summary>
        /// The wait handle signaled when an event is enqueued to the idle
        /// event queue.
        /// </summary>
        private EventWaitHandle idleEnqueueEvent;

        /// <summary>
        /// An optional array of caller-supplied wait handles that are not
        /// owned by this event manager.
        /// </summary>
        private EventWaitHandle[] userEvents;

        /// <summary>
        /// The per-sleep-type configured sleep times, in milliseconds.
        /// </summary>
        private SleepTypeIntDictionary sleepTimes;

        /// <summary>
        /// The per-sleep-type configured minimum sleep times, in milliseconds.
        /// </summary>
        private SleepTypeIntDictionary minimumSleepTimes;

        /// <summary>
        /// When greater than zero, event processing is enabled.
        /// </summary>
        private int enabled;

        /// <summary>
        /// The current event processing nesting level.
        /// </summary>
        private int levels;

        /// <summary>
        /// When greater than zero, notifications are not sent for event
        /// activity.
        /// </summary>
        private int noNotify;

        /// <summary>
        /// The optional callback used to obtain the current date and time.
        /// </summary>
        private DateTimeNowCallback nowCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an event manager, initializing the event queues, wait
        /// handles, counters, and other internal state to their default
        /// values.
        /// </summary>
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
        /// <summary>
        /// Constructs an event manager associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this event manager.
        /// </param>
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
        /// <summary>
        /// This method formats the specified event as a string suitable for
        /// inclusion in a list of events.
        /// </summary>
        /// <param name="index">
        /// The position of the event within its event queue.
        /// </param>
        /// <param name="event">
        /// The event to format.
        /// </param>
        /// <returns>
        /// The string representation of the event, or null if the event is
        /// null.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the priority of the specified event
        /// is high enough relative to the requested priority.
        /// </summary>
        /// <param name="event">
        /// The event whose priority is to be checked.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is considered ready.
        /// </param>
        /// <returns>
        /// True if the event is ready based on its priority; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the scheduled time of the specified
        /// event has arrived.
        /// </summary>
        /// <param name="event">
        /// The event whose scheduled time is to be checked.
        /// </param>
        /// <param name="dateTime">
        /// The current date and time.
        /// </param>
        /// <returns>
        /// True if the event is ready based on its scheduled time; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified event is targeted at a
        /// thread that the caller is willing to service.
        /// </summary>
        /// <param name="event">
        /// The event whose target thread is to be checked.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern thread matching, including the greedy
        /// thread flag.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the caller is servicing events for, or
        /// null to service events not targeted at a specific thread.
        /// </param>
        /// <returns>
        /// True if the event is ready based on its target thread; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the flags of the specified event
        /// match the requested event flags.
        /// </summary>
        /// <param name="event">
        /// The event whose flags are to be checked.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to match against the event flags.
        /// </param>
        /// <param name="notHas">
        /// Non-zero to invert the sense of the match (i.e. the event is ready
        /// when it does not have the requested flags).
        /// </param>
        /// <param name="all">
        /// Non-zero to require all of the requested flags to be present; zero
        /// to require any of them.
        /// </param>
        /// <returns>
        /// True if the event flags are ready; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified event flags indicate
        /// an idle event.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags to check.
        /// </param>
        /// <returns>
        /// True if the flags indicate an idle event; otherwise, false.
        /// </returns>
        private static bool IsIdleEvent(
            EventFlags eventFlags
            )
        {
            return FlagOps.HasFlags(eventFlags, EventFlags.Idle, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified event is an idle
        /// event.
        /// </summary>
        /// <param name="event">
        /// The event to check.
        /// </param>
        /// <returns>
        /// True if the event is an idle event; otherwise, false.
        /// </returns>
        private static bool IsIdleEvent(
            IEvent @event
            )
        {
            return (@event != null) ? IsIdleEvent(@event.Flags) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified event is a script
        /// event (i.e. one whose callback evaluates a script).
        /// </summary>
        /// <param name="event">
        /// The event to check.
        /// </param>
        /// <returns>
        /// True if the event is a script event; otherwise, false.
        /// </returns>
        internal static bool IsScriptEvent(
            IEvent @event
            )
        {
            return (@event != null) ?
                (@event.Callback == ScriptEventCallback) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the name of the specified event
        /// equals the specified pattern.
        /// </summary>
        /// <param name="event">
        /// The event whose name is to be compared.
        /// </param>
        /// <param name="pattern">
        /// The name to compare against the event name.
        /// </param>
        /// <returns>
        /// True if the event name matches; otherwise, false.
        /// </returns>
        private static bool MatchEventName(
            IEvent @event,
            string pattern
            )
        {
            return (@event != null) ?
                SharedStringOps.SystemEquals(@event.Name, pattern) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the text of the specified script
        /// equals the specified pattern.
        /// </summary>
        /// <param name="script">
        /// The script whose text is to be compared.
        /// </param>
        /// <param name="pattern">
        /// The text to compare against the script text.
        /// </param>
        /// <returns>
        /// True if the script text matches; otherwise, false.
        /// </returns>
        private static bool MatchScriptText(
            IScript script,
            string pattern
            )
        {
            return (script != null) ?
                StringOps.UserEquals(script.Text, pattern) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified event matches using
        /// the supplied match callback.  When there is no callback, the event
        /// is considered to match.
        /// </summary>
        /// <param name="event">
        /// The event to test for a match.
        /// </param>
        /// <param name="callback">
        /// The callback used to test the event, or null to match any event.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the match callback.
        /// </param>
        /// <returns>
        /// True if the event matches; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether an idle event should be dequeued,
        /// based on the requested event flags and the number of pending
        /// non-idle events.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags that govern idle event handling.
        /// </param>
        /// <param name="nonIdleCount">
        /// The number of pending non-idle events.
        /// </param>
        /// <returns>
        /// True if an idle event should be dequeued; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method resolves an automatic event priority into a concrete
        /// priority, based on the event flags.  Any explicitly specified
        /// priority is returned unchanged.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to determine the priority when the requested
        /// priority is automatic.
        /// </param>
        /// <param name="priority">
        /// The requested priority, which may be automatic.
        /// </param>
        /// <returns>
        /// The resolved event priority.
        /// </returns>
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

        /// <summary>
        /// This method computes the effective priority at which the specified
        /// event should be considered ready, based on its event flags and the
        /// lowest priority the caller is willing to accept.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags of the event being considered.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority the caller is accepting.
        /// </param>
        /// <returns>
        /// The effective event priority.
        /// </returns>
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

        /// <summary>
        /// This method resolves an automatic event thread identifier into a
        /// concrete thread identifier.  A null identifier (consume all) is
        /// returned unchanged, a non-zero identifier (consume specific) is
        /// returned unchanged, and a zero identifier is resolved to the
        /// current system thread.
        /// </summary>
        /// <param name="threadId">
        /// The requested thread identifier, which may be null or zero.
        /// </param>
        /// <returns>
        /// The resolved thread identifier.
        /// </returns>
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
        /// <summary>
        /// This method is a thread start routine that queues a script event on
        /// the interpreter contained in the supplied state object.  Any
        /// resulting status is reported via the interpreter's interactive host.
        /// </summary>
        /// <param name="obj">
        /// The thread state object, expected to be an
        /// <see cref="IAnyPair{Interpreter, IScript}" /> containing the
        /// interpreter and the script to queue.
        /// </param>
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

        /// <summary>
        /// This method is a thread start routine that services events for the
        /// interpreter contained in the supplied state object until an error
        /// occurs.  Any resulting status is reported via the interpreter's
        /// interactive host.
        /// </summary>
        /// <param name="obj">
        /// The thread state object, expected to be a
        /// <see cref="ServiceEventClientData" /> describing the interpreter and
        /// the event servicing parameters.
        /// </param>
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
                        clientData.ThreadId, clientData.Timeout,
                        clientData.Limit, clientData.NoCancel,
                        clientData.NoGlobalCancel, clientData.StopOnError,
                        clientData.ErrorOnEmpty, clientData.UserInterface,
                        ref eventCount, ref result);

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
        /// <summary>
        /// This method performs the call frame management required before
        /// evaluating a script event.  It creates and pushes the appropriate
        /// call frame, taking interpreter namespace support into account.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the script event will be evaluated.
        /// </param>
        /// <param name="name">
        /// The name to associate with the created call frame.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that will be used to evaluate the script event.
        /// </param>
        /// <param name="useNamespaces">
        /// Upon return, non-zero if namespaces are enabled for the interpreter
        /// and a namespace call frame was pushed; otherwise, zero.
        /// </param>
        /// <param name="frame">
        /// Upon return, the call frame that was created and pushed, if any.
        /// </param>
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
                    useNamespaces = interpreter.InternalAreNamespacesEnabled();

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

        /// <summary>
        /// This method performs the call frame cleanup required after
        /// evaluating a script event.  It pops the call frame that was pushed
        /// by <c>ScriptEventPrologue</c>, taking interpreter namespace support
        /// into account.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the script event was evaluated.
        /// </param>
        /// <param name="useNamespaces">
        /// Non-zero if a namespace call frame was pushed and must now be
        /// popped.
        /// </param>
        /// <param name="frame">
        /// The call frame that was pushed and is now to be popped.
        /// </param>
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

        /// <summary>
        /// This method emits a trace message describing a script event that is
        /// being evaluated.
        /// </summary>
        /// <param name="prefix">
        /// A short prefix describing the trace point.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter in which the script event is being evaluated.
        /// </param>
        /// <param name="scriptName">
        /// The name of the script being evaluated.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode used to evaluate the script.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags used to evaluate the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags used to evaluate the script.
        /// </param>
        /// <param name="combinedEventFlags">
        /// The combined event flags used to evaluate the script.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags used to evaluate the script.
        /// </param>
        /// <param name="code">
        /// The return code produced by evaluating the script, if any.
        /// </param>
        /// <param name="text">
        /// The script text being evaluated.
        /// </param>
        /// <param name="result">
        /// The result produced by evaluating the script, if any.
        /// </param>
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

        /// <summary>
        /// This method evaluates the text of a script event using the engine
        /// operation selected by the specified engine mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to evaluate the script.
        /// </param>
        /// <param name="engineMode">
        /// The engine mode that selects which engine operation to perform.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use.
        /// </param>
        /// <param name="text">
        /// The script text to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of the evaluation; upon failure, an error
        /// message.
        /// </param>
        /// <returns>
        /// The return code indicating whether the evaluation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method is the event callback used for script events.  It
        /// extracts the script from the event client data and evaluates each
        /// of its sections within a managed call frame, stopping if any
        /// section raises an error.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to evaluate the script.
        /// </param>
        /// <param name="clientData">
        /// The event client data, expected to contain the
        /// <see cref="IScript" /> to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, the result of the evaluation; upon failure, an error
        /// message.
        /// </param>
        /// <returns>
        /// The return code indicating whether the evaluation succeeded.
        /// </returns>
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
                            /* IGNORED */
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
        /// <summary>
        /// This method determines whether event processing is currently
        /// enabled.
        /// </summary>
        /// <returns>
        /// True if event processing is enabled; otherwise, false.
        /// </returns>
        private bool IsEnabled()
        {
            return Interlocked.CompareExchange(ref enabled, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether event processing is currently active
        /// (i.e. in progress on some thread).
        /// </summary>
        /// <returns>
        /// True if event processing is active; otherwise, false.
        /// </returns>
        private bool IsActive()
        {
            return Interlocked.CompareExchange(ref levels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether notifications are currently
        /// suppressed for event activity.
        /// </summary>
        /// <returns>
        /// True if notifications are suppressed; otherwise, false.
        /// </returns>
        private bool IsNoNotify()
        {
            return Interlocked.CompareExchange(ref noNotify, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the event processing nesting level.
        /// </summary>
        /// <returns>
        /// The new event processing nesting level.
        /// </returns>
        private int EnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the event processing nesting level.
        /// </summary>
        /// <returns>
        /// The new event processing nesting level.
        /// </returns>
        private int ExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the current date and time, using the configured
        /// "now" callback when present.  It guarantees that the returned value
        /// advances by at least a minimal amount between successive calls.
        /// </summary>
        /// <returns>
        /// The current date and time.
        /// </returns>
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

        /// <summary>
        /// This method returns the normal or idle event queue.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to return the idle event queue; zero to return the normal
        /// event queue.
        /// </param>
        /// <returns>
        /// The requested event queue.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of events in the normal or idle
        /// event queue.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to count the idle event queue; zero to count the normal
        /// event queue.
        /// </param>
        /// <returns>
        /// The number of events in the requested event queue.
        /// </returns>
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

        /// <summary>
        /// This method determines whether either the normal or idle event
        /// queue currently exists.
        /// </summary>
        /// <returns>
        /// True if at least one event queue exists; otherwise, false.
        /// </returns>
        private bool HaveAnyEventQueue()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((events != null) || (idleEvents != null));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the total number of events in both the normal
        /// and idle event queues.
        /// </summary>
        /// <returns>
        /// The total number of pending events.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified event queue is the
        /// idle event queue.
        /// </summary>
        /// <param name="eventQueue">
        /// The event queue to check.
        /// </param>
        /// <returns>
        /// True if the specified event queue is the idle event queue;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method creates a key used to order an event within an event
        /// queue, combining the event priority, scheduled time, and a unique
        /// sequence number.
        /// </summary>
        /// <param name="priority">
        /// The priority of the event.
        /// </param>
        /// <param name="dateTime">
        /// The scheduled time of the event.
        /// </param>
        /// <returns>
        /// The created event queue key.
        /// </returns>
        private EventQueueKey CreateEventQueueKey(
            EventPriority priority,
            DateTime dateTime
            )
        {
            return new AnyTriplet<EventPriority, DateTime, long>(
                priority, dateTime, GlobalState.NextId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified event if it is flagged as
        /// fire-and-forget, and then clears the reference to it.  The
        /// maybe-dispose counter is always incremented.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags governing this dispose operation.
        /// </param>
        /// <param name="event">
        /// The event to maybe dispose.  Upon return, the reference is cleared.
        /// </param>
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

        /// <summary>
        /// This method attempts to retrieve the configured sleep time for the
        /// specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose configured time is to be retrieved.
        /// </param>
        /// <param name="minimum">
        /// Non-zero to retrieve from the minimum sleep times; zero to retrieve
        /// from the normal sleep times.
        /// </param>
        /// <param name="sleepTime">
        /// Upon success, the configured sleep time, in milliseconds.
        /// </param>
        /// <returns>
        /// True if a sleep time was configured for the specified sleep type;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method sets or removes the configured sleep time for the
        /// specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose configured time is to be set or removed.
        /// </param>
        /// <param name="minimum">
        /// Non-zero to operate on the minimum sleep times; zero to operate on
        /// the normal sleep times.
        /// </param>
        /// <param name="sleepTime">
        /// The sleep time to set, in milliseconds, or null to remove any
        /// existing configured sleep time.
        /// </param>
        /// <returns>
        /// True if the configured sleep times were changed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method dequeues the next ready event, discarding any error
        /// information.  It is a convenience wrapper over the overload that
        /// reports an error.
        /// </summary>
        /// <param name="dateTime">
        /// The current date and time, used to determine which events are
        /// scheduled to run.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success with no event.
        /// </param>
        /// <param name="event">
        /// Upon success, the dequeued event, if any.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method dequeues the next ready event from the specified event
        /// queue.  Events are considered in priority and scheduled-time order;
        /// the first event matching the requested priority, time, thread, and
        /// flags is removed and returned.
        /// </summary>
        /// <param name="eventQueue">
        /// The event queue to dequeue from.
        /// </param>
        /// <param name="dateTime">
        /// The current date and time, used to determine which events are
        /// scheduled to run.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success with no event.
        /// </param>
        /// <param name="event">
        /// Upon success, the dequeued event, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no event could be
        /// dequeued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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
                            eventFlags, priority); /* TRANSLATE */

                        threadId = GetAutomaticEventThread(
                            threadId); /* TRANSLATE */

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
                                continue;

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

        /// <summary>
        /// This method reports a failure via the complaint mechanism, unless
        /// the code indicates success or complaints have been suppressed.
        /// </summary>
        /// <param name="code">
        /// The return code to check; a non-success code may trigger a
        /// complaint.
        /// </param>
        /// <param name="error">
        /// The error information to report.
        /// </param>
        private void MaybeComplain(
            ReturnCode code,
            Result error
            )
        {
            if ((code != ReturnCode.Ok) && !DefaultNoComplain)
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

        /// <summary>
        /// This method returns the wait handle that is signaled when the
        /// normal or idle event queue becomes empty.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to return the idle empty-queue wait handle; zero to return
        /// the normal empty-queue wait handle.
        /// </param>
        /// <returns>
        /// The requested empty-queue wait handle.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the normal or idle event queue is
        /// currently empty.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to check the idle event queue; zero to check the normal
        /// event queue.
        /// </param>
        /// <returns>
        /// True if the requested event queue is empty; otherwise, false.
        /// </returns>
        private bool CheckForEmptyQueue(
            bool idle
            )
        {
            return (GetEventCount(idle) == 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method signals that the normal or idle event queue has become
        /// empty, complaining if the signal could not be raised.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to signal the idle event queue; zero to signal the normal
        /// event queue.
        /// </param>
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

        /// <summary>
        /// This method signals that the normal or idle event queue has become
        /// empty.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to signal the idle event queue; zero to signal the normal
        /// event queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the signal could not
        /// be raised.
        /// </param>
        /// <returns>
        /// The return code indicating whether the signal was raised.
        /// </returns>
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

        /// <summary>
        /// This method returns the wait handle that is signaled when an event
        /// is enqueued to the normal or idle event queue.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to return the idle enqueue wait handle; zero to return the
        /// normal enqueue wait handle.
        /// </param>
        /// <returns>
        /// The requested enqueue wait handle.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the normal or idle event queue
        /// currently has any enqueued events.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to check the idle event queue; zero to check the normal
        /// event queue.
        /// </param>
        /// <returns>
        /// True if the requested event queue has at least one event;
        /// otherwise, false.
        /// </returns>
        private bool CheckForEventEnqueued(
            bool idle
            )
        {
            return GetEventCount(idle) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method signals that an event has been enqueued to the normal
        /// or idle event queue, complaining if the signal could not be raised.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to signal the idle event queue; zero to signal the normal
        /// event queue.
        /// </param>
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

        /// <summary>
        /// This method signals that an event has been enqueued to the normal
        /// or idle event queue.
        /// </summary>
        /// <param name="idle">
        /// Non-zero to signal the idle event queue; zero to signal the normal
        /// event queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the signal could not
        /// be raised.
        /// </param>
        /// <returns>
        /// The return code indicating whether the signal was raised.
        /// </returns>
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
        /// <summary>
        /// The interpreter that owns this event manager.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter that owns this event manager.
        /// </summary>
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
        /// <summary>
        /// Gets a value indicating whether this event manager has been
        /// disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                //
                // NOTE: Obviously, this would be pointless.
                //
                // CheckDisposed(); /* EXEMPT */

                lock (syncRoot)
                {
                    return disposed;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this event manager is in the
        /// process of being disposed.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronizeSimple Members
        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// event manager without blocking.
        /// </summary>
        /// <returns>
        /// True if the lock was acquired; otherwise, false.
        /// </returns>
        /* DANGEROUS: EXTERNAL USE ONLY. */
        public bool TryLock() /* NOT USED BY CORE */
        {
            CheckDisposed();

            if (syncRoot == null)
                return false;

            return Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method acquires the synchronization lock for this event
        /// manager, blocking until it is available.
        /// </summary>
        /* DANGEROUS: EXTERNAL USE ONLY. */
        public void Lock() /* NOT USED BY CORE */
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            Monitor.Enter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the synchronization lock for this event
        /// manager.
        /// </summary>
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

        #region ISynchronizeBase Members
        /// <summary>
        /// Gets the object used to synchronize access to this event manager.
        /// </summary>
        /* DANGEROUS: EXTERNAL USE ONLY. */
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// event manager without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, non-zero if the lock was acquired; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// event manager, waiting up to the default wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, non-zero if the lock was acquired; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// event manager without blocking and without throwing if this event
        /// manager has been disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon return, non-zero if the lock was acquired; otherwise, zero.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// event manager, waiting up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock.
        /// </param>
        /// <param name="locked">
        /// Upon return, non-zero if the lock was acquired; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// This method releases the synchronization lock for this event
        /// manager, if it is currently held.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the lock is currently held.  Upon return,
        /// zero if the lock was released.
        /// </param>
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
        /// <summary>
        /// Gets the running total of events queued to the normal event queue.
        /// </summary>
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

        /// <summary>
        /// Gets the running total of events queued to the idle event queue.
        /// </summary>
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

        /// <summary>
        /// Gets the number of events currently in the normal event queue.
        /// </summary>
        public int EventCount
        {
            get
            {
                CheckDisposed();

                return GetEventCount(false);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of events currently in the idle event queue.
        /// </summary>
        public int IdleEventCount
        {
            get
            {
                CheckDisposed();

                return GetEventCount(true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the total number of events currently in both the normal and
        /// idle event queues.
        /// </summary>
        public int TotalEventCount
        {
            get
            {
                CheckDisposed();

                return GetTotalEventCount();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the high-water mark for the number of events in the normal
        /// event queue.
        /// </summary>
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

        /// <summary>
        /// Gets the high-water mark for the number of events in the idle event
        /// queue.
        /// </summary>
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

        /// <summary>
        /// Gets the number of times an event was considered for disposal.
        /// </summary>
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

        /// <summary>
        /// Gets the number of times an event was actually disposed.
        /// </summary>
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

        /// <summary>
        /// Gets the total number of times a caller waited for an event queue to
        /// become empty.
        /// </summary>
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

        /// <summary>
        /// Gets the number of times waiting for an event queue to become empty
        /// resulted in an error.
        /// </summary>
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

        /// <summary>
        /// Gets the total number of times a caller waited for an event to be
        /// enqueued.
        /// </summary>
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

        /// <summary>
        /// Gets the number of times waiting for an event to be enqueued
        /// resulted in an error.
        /// </summary>
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

        /// <summary>
        /// Gets the wait handle signaled when the normal event queue becomes
        /// empty.
        /// </summary>
        public EventWaitHandle EmptyEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return emptyEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wait handle signaled when an event is enqueued to the
        /// normal event queue.
        /// </summary>
        public EventWaitHandle EnqueueEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return enqueueEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wait handle signaled when the idle event queue becomes
        /// empty.
        /// </summary>
        public EventWaitHandle IdleEmptyEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return idleEmptyEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the wait handle signaled when an event is enqueued to the idle
        /// event queue.
        /// </summary>
        public EventWaitHandle IdleEnqueueEvent
        {
            get { CheckDisposed(); lock (syncRoot) { return idleEnqueueEvent; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the array of caller-supplied wait handles that are not
        /// owned by this event manager.  The accessors return and accept a copy
        /// of the array.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether event processing is
        /// enabled.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether event processing is
        /// currently active.  Setting this property enters or exits an event
        /// processing level.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether notifications are suppressed
        /// for event activity.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the optional callback used to obtain the current date
        /// and time.
        /// </summary>
        public DateTimeNowCallback NowCallback
        {
            get { CheckDisposed(); lock (syncRoot) { return nowCallback; } }
            set { CheckDisposed(); lock (syncRoot) { nowCallback = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the current enabled state of event processing and
        /// then forces event processing to be disabled.
        /// </summary>
        /// <param name="savedEnabled">
        /// Upon return, the previous enabled state, suitable for passing to
        /// <see cref="RestoreEnabled" />.
        /// </param>
        public void SaveEnabledAndForceDisabled(
            ref int savedEnabled
            )
        {
            CheckDisposed();

            savedEnabled = Interlocked.Exchange(ref enabled, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores a previously saved enabled state of event
        /// processing.
        /// </summary>
        /// <param name="savedEnabled">
        /// The previously saved enabled state, as returned by
        /// <see cref="SaveEnabledAndForceDisabled" />.
        /// </param>
        /// <returns>
        /// True if event processing is enabled after the restore; otherwise,
        /// false.
        /// </returns>
        public bool RestoreEnabled(
            int savedEnabled
            )
        {
            CheckDisposed();

            return Interlocked.Exchange(ref enabled, savedEnabled) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list describing every event currently in the
        /// normal and idle event queues.
        /// </summary>
        /// <param name="result">
        /// Upon success, a list describing the queued events; upon failure, an
        /// error message.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method removes every event from both the normal and idle event
        /// queues.
        /// </summary>
        /// <param name="error">
        /// Upon failure, an error message describing why the events could not
        /// be cleared.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method returns the next event from the normal event queue, or
        /// the idle event queue when the normal queue is empty, without
        /// removing it.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success with no event.
        /// </param>
        /// <param name="event">
        /// Upon success, the peeked event, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no event could be
        /// peeked.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method searches the normal and idle event queues for an event
        /// whose name matches the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the event to find, or null to match any event.
        /// </param>
        /// <param name="event">
        /// Upon success, the matching event.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no matching event was
        /// found.
        /// </param>
        /// <returns>
        /// The return code indicating whether a matching event was found.
        /// </returns>
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

        /// <summary>
        /// This method removes and discards a single event from the normal
        /// event queue, or the idle event queue when the normal queue is empty.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no event could be
        /// discarded.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method removes and returns a single event from the normal
        /// event queue, or the idle event queue when the normal queue is empty.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success with no event.
        /// </param>
        /// <param name="event">
        /// Upon success, the dequeued event, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no event could be
        /// dequeued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method queues a fire-and-forget event that invokes the
        /// specified callback when it is serviced.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the event.
        /// </param>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the event is serviced.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method queues an event that invokes the specified callback
        /// when it is serviced, and returns the created event.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the event.
        /// </param>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the event is serviced.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, the event that was created and queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method queues a fire-and-forget event that evaluates the
        /// specified script text when it is serviced.
        /// </summary>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="text">
        /// The script text to evaluate.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the script.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method queues an event that evaluates the specified script text
        /// when it is serviced, and returns the created event.
        /// </summary>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="text">
        /// The script text to evaluate.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the script.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the script.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, the event that was created and queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method queues a fire-and-forget event that evaluates the
        /// specified script object when it is serviced.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the event.
        /// </param>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="script">
        /// The script object to evaluate.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method queues an event that evaluates the specified script
        /// object when it is serviced, and returns the created event.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the event.
        /// </param>
        /// <param name="dateTime">
        /// The earliest time at which the event should be serviced.
        /// </param>
        /// <param name="script">
        /// The script object to evaluate.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags that govern how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority of the event, which may be automatic.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is targeted at, or null for
        /// no specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events allowed in the target queue, or zero
        /// or less for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, the event that was created and queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the event could not be
        /// queued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the event was queued.
        /// </returns>
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

        /// <summary>
        /// This method dequeues the next ready event, considering the normal
        /// event queue first and then, if appropriate, the idle event queue.
        /// </summary>
        /// <param name="dateTime">
        /// The current date and time, used to determine which events are
        /// scheduled to run.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of events as an error; zero to treat
        /// it as success with no event.
        /// </param>
        /// <param name="event">
        /// Upon success, the dequeued event, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why no event could be
        /// dequeued.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method produces a list of the events in the normal and idle
        /// event queues whose string representations match the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the events, or null to match all events.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match.
        /// </param>
        /// <param name="list">
        /// Upon success, the list of matching events.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the events could not
        /// be listed.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method produces a collection of the events in the normal and
        /// idle event queues that match using the supplied match callback.
        /// </summary>
        /// <param name="callback">
        /// The callback used to test each event, or null to match all events.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the match callback.
        /// </param>
        /// <param name="events">
        /// Upon success, the collection of matching events.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the events could not
        /// be listed.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method cancels events in the normal and idle event queues that
        /// match the specified name or script text.  Events are first matched
        /// by name and then by script text.
        /// </summary>
        /// <param name="nameOrScript">
        /// The event name or script text to match, or null to match any event.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat matching zero events as an error; zero to treat it
        /// as success.
        /// </param>
        /// <param name="all">
        /// Non-zero to cancel all matching events; zero to cancel only the
        /// first matching event.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the events could not
        /// be canceled.
        /// </param>
        /// <returns>
        /// The return code indicating whether the operation succeeded.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured sleep time for the specified
        /// sleep type, falling back to the interpreter or default sleep time
        /// when none has been configured.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose sleep time is to be returned.
        /// </param>
        /// <returns>
        /// The sleep time, in milliseconds.
        /// </returns>
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

        /// <summary>
        /// This method sets or removes the configured sleep time for the
        /// specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose sleep time is to be set or removed.
        /// </param>
        /// <param name="sleepTime">
        /// The sleep time to set, in milliseconds, or null to remove any
        /// existing configured sleep time.
        /// </param>
        /// <returns>
        /// True if the configured sleep times were changed; otherwise, false.
        /// </returns>
        public bool SetSleepTime(
            SleepType sleepType,
            int? sleepTime
            )
        {
            CheckDisposed();

            return TrySetSleepTime(sleepType, false, sleepTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the effective minimum sleep time for the
        /// specified sleep type, ensuring the result is not below the minimum
        /// allowed sleep time.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose minimum sleep time is to be returned.
        /// </param>
        /// <returns>
        /// The minimum sleep time, in milliseconds.
        /// </returns>
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

        /// <summary>
        /// This method sets or removes the configured minimum sleep time for
        /// the specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose minimum sleep time is to be set or removed.
        /// </param>
        /// <param name="sleepTime">
        /// The minimum sleep time to set, in milliseconds, or null to remove
        /// any existing configured minimum sleep time.
        /// </param>
        /// <returns>
        /// True if the configured minimum sleep times were changed; otherwise,
        /// false.
        /// </returns>
        public bool SetMinimumSleepTime(
            SleepType sleepType,
            int? sleepTime
            )
        {
            CheckDisposed();

            return TrySetSleepTime(sleepType, true, sleepTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sleeps for the configured amount of time for the
        /// specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The sleep type whose sleep time governs how long to sleep.
        /// </param>
        /// <param name="minimum">
        /// Non-zero to use the minimum sleep time; zero to use the normal
        /// sleep time.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the sleep could not be
        /// performed.
        /// </param>
        /// <returns>
        /// True if the sleep completed successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method yields the current thread, giving other threads an
        /// opportunity to run.
        /// </summary>
        /// <param name="error">
        /// Upon failure, an error message describing why the yield could not be
        /// performed.
        /// </param>
        /// <returns>
        /// True if the yield completed successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method processes pending events, dispatching each ready event
        /// to its callback until the queue is exhausted or a stopping
        /// condition is reached.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero or less for no
        /// limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of processing.
        /// </returns>
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

        /// <summary>
        /// This method processes pending events, dispatching each ready event
        /// to its callback until the queue is exhausted or a stopping
        /// condition is reached, and reports the number of events processed.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero or less for no
        /// limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count.  Upon return, increased by the
        /// number of events that were processed.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of processing.
        /// </returns>
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
                    bool noComplain = FlagOps.HasFlags(
                        eventFlags, EventFlags.NoComplain, true);

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
                                    true, code, localResult, 0, ref setError) &&
                                !noComplain)
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

        /// <summary>
        /// This method processes pending events and, optionally, any pending
        /// user-interface messages.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero for all.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also process pending user-interface messages.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of processing.
        /// </returns>
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

        /// <summary>
        /// This method processes pending events and, optionally, any pending
        /// user-interface messages, and reports the number of events
        /// processed.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero for all.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also process pending user-interface messages.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count.  Upon return, increased by the
        /// number of events that were processed.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of processing.
        /// </returns>
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

        /// <summary>
        /// This method repeatedly processes pending events while the
        /// interpreter remains valid, yielding between iterations, until an
        /// error occurs.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for an event to
        /// become ready, or null for no timeout.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process per iteration, or zero for
        /// all.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore script cancellation while waiting.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore global script cancellation while waiting.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also process pending user-interface messages.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of servicing.
        /// </returns>
        public ReturnCode ServiceEvents(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int? timeout,
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
                eventFlags, priority, threadId, timeout, limit,
                noCancel, noGlobalCancel, stopOnError, errorOnEmpty,
                userInterface, ref eventCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method repeatedly processes pending events while the
        /// interpreter remains valid, yielding between iterations, until an
        /// error occurs, and reports the number of events processed.
        /// </summary>
        /// <param name="eventFlags">
        /// The event flags used to select which events are eligible and to
        /// govern processing behavior.
        /// </param>
        /// <param name="priority">
        /// The lowest relative priority that is eligible.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread events must be targeted at, or null to
        /// consider events not targeted at a specific thread.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for an event to
        /// become ready, or null for no timeout.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process per iteration, or zero for
        /// all.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore script cancellation while waiting.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore global script cancellation while waiting.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing upon the first event that fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to treat the absence of further ready events as an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also process pending user-interface messages.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count.  Upon return, increased by the
        /// number of events that were processed.
        /// </param>
        /// <param name="result">
        /// Upon failure, the accumulated error information.
        /// </param>
        /// <returns>
        /// The return code indicating the overall outcome of servicing.
        /// </returns>
        public ReturnCode ServiceEvents(
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int? timeout,
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
                    interpreter, timeout, noCancel, noGlobalCancel,
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

        /// <summary>
        /// This method waits for the normal or idle event queue to become
        /// empty, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <param name="idle">
        /// Non-zero to wait for the idle event queue; zero to wait for the
        /// normal event queue.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require an explicit empty-queue signal; zero to also
        /// succeed if the queue is observed to be empty.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the wait did not
        /// succeed.
        /// </param>
        /// <returns>
        /// The return code indicating whether the queue became empty.
        /// </returns>
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

        /// <summary>
        /// This method waits for an event to be enqueued to the normal or idle
        /// event queue, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <param name="idle">
        /// Non-zero to wait on the idle event queue; zero to wait on the
        /// normal event queue.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require an explicit enqueue signal; zero to also succeed
        /// if an event is observed to be enqueued.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the wait did not
        /// succeed.
        /// </param>
        /// <returns>
        /// The return code indicating whether an event was enqueued.
        /// </returns>
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
        /// <summary>
        /// Stores a value indicating whether this event manager has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this event manager has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this event manager has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(EventManager));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this event manager.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this event manager and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this event manager, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~EventManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
