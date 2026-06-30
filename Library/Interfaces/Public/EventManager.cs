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
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the per-interpreter event manager,
    /// which maintains the queues of pending events and idle events and
    /// services them (this is the Eagle equivalent of the Tcl event loop).  It
    /// supports queuing both callback-based events and script-based events,
    /// inspecting and cancelling pending events, sleeping and yielding, and
    /// waiting for the queues to drain or to receive new events.  It composes
    /// the disposal-awareness (<see cref="IMaybeDisposed" />) and
    /// synchronization (<see cref="ISynchronizeSimple" />,
    /// <see cref="ISynchronize" />) contracts.
    /// </summary>
    [ObjectId("0fb6f163-3c5e-4bb8-a948-93bab9968b0f")]
    public interface IEventManager : IMaybeDisposed, ISynchronizeSimple, ISynchronize
    {
        /// <summary>
        /// Gets the number of events currently in the main event queue.
        /// </summary>
        int QueueEventCount { get; }
        /// <summary>
        /// Gets the number of events currently in the idle event queue.
        /// </summary>
        int QueueIdleEventCount { get; }

        /// <summary>
        /// Gets the total number of events that have been processed from the
        /// main event queue.
        /// </summary>
        int EventCount { get; }
        /// <summary>
        /// Gets the total number of events that have been processed from the
        /// idle event queue.
        /// </summary>
        int IdleEventCount { get; }
        /// <summary>
        /// Gets the combined total number of events that have been processed
        /// from both the main and idle event queues.
        /// </summary>
        int TotalEventCount { get; }

        /// <summary>
        /// Gets the high-water mark for the number of events held in the main
        /// event queue.
        /// </summary>
        int MaximumEventCount { get; }
        /// <summary>
        /// Gets the high-water mark for the number of events held in the idle
        /// event queue.
        /// </summary>
        int MaximumIdleEventCount { get; }

        /// <summary>
        /// Gets the number of events for which conditional (maybe) disposal has
        /// been performed.
        /// </summary>
        int MaybeDisposeEventCount { get; }
        /// <summary>
        /// Gets the number of events that have actually been disposed.
        /// </summary>
        int ReallyDisposeEventCount { get; }

        /// <summary>
        /// Gets the total number of times that waiting for the event queue to
        /// become empty has been attempted.
        /// </summary>
        int WaitForEmptyQueueTotalCount { get; }
        /// <summary>
        /// Gets the number of times that waiting for the event queue to become
        /// empty has failed.
        /// </summary>
        int WaitForEmptyQueueErrorCount { get; }

        /// <summary>
        /// Gets the total number of times that waiting for an event to be
        /// enqueued has been attempted.
        /// </summary>
        int WaitForEventEnqueuedTotalCount { get; }
        /// <summary>
        /// Gets the number of times that waiting for an event to be enqueued
        /// has failed.
        /// </summary>
        int WaitForEventEnqueuedErrorCount { get; }

        /// <summary>
        /// Gets the wait handle that is signaled when the main event queue
        /// becomes empty.
        /// </summary>
        EventWaitHandle EmptyEvent { get; }
        /// <summary>
        /// Gets the wait handle that is signaled when an event is enqueued into
        /// the main event queue.
        /// </summary>
        EventWaitHandle EnqueueEvent { get; }
        /// <summary>
        /// Gets the wait handle that is signaled when the idle event queue
        /// becomes empty.
        /// </summary>
        EventWaitHandle IdleEmptyEvent { get; }
        /// <summary>
        /// Gets the wait handle that is signaled when an event is enqueued into
        /// the idle event queue.
        /// </summary>
        EventWaitHandle IdleEnqueueEvent { get; }

        /// <summary>
        /// Gets or sets the array of caller-supplied wait handles that are
        /// also monitored while servicing events.
        /// </summary>
        EventWaitHandle[] UserEvents { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this event manager is
        /// enabled.  When not enabled, events are not serviced.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this event manager is
        /// currently active (that is, servicing events).
        /// </summary>
        bool Active { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether queue change notifications
        /// (signaling of the associated wait handles) should be suppressed.
        /// </summary>
        bool NoNotify { get; set; }

        /// <summary>
        /// Gets or sets the callback used to obtain the current date and time.
        /// </summary>
        DateTimeNowCallback NowCallback { get; set; }

        /// <summary>
        /// This method records the current enabled state of this event manager
        /// and then forces it into the disabled state.
        /// </summary>
        /// <param name="savedEnabled">
        /// Upon return, this contains an opaque value capturing the previous
        /// enabled state, suitable for passing to <see cref="RestoreEnabled" />.
        /// </param>
        void SaveEnabledAndForceDisabled(ref int savedEnabled);
        /// <summary>
        /// This method restores the enabled state of this event manager from a
        /// value previously produced by
        /// <see cref="SaveEnabledAndForceDisabled" />.
        /// </summary>
        /// <param name="savedEnabled">
        /// The previously saved enabled state to restore.
        /// </param>
        /// <returns>
        /// True if the enabled state was restored; otherwise, false.
        /// </returns>
        bool RestoreEnabled(int savedEnabled);

        /// <summary>
        /// This method produces a diagnostic dump of the current state of this
        /// event manager and its queues.
        /// </summary>
        /// <param name="result">
        /// Upon success, this contains the diagnostic dump.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode Dump(ref Result result);

        /// <summary>
        /// This method removes all pending events from the queues managed by
        /// this event manager.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode ClearEvents(ref Result error);
        /// <summary>
        /// This method returns the next pending event without removing it from
        /// the queue.
        /// </summary>
        /// <param name="strict">
        /// Non-zero if an empty queue should be treated as an error.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the next pending event, or null if the
        /// queue is empty and <paramref name="strict" /> is false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode PeekEvent(bool strict, ref IEvent @event, ref Result error);
        /// <summary>
        /// This method returns the pending event with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the event to locate.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the matching event.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode GetEvent(string name, ref IEvent @event, ref Result error);

        /// <summary>
        /// This method removes and discards the next pending event without
        /// returning it.
        /// </summary>
        /// <param name="strict">
        /// Non-zero if an empty queue should be treated as an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode DiscardEvent(bool strict, ref Result error);
        /// <summary>
        /// This method removes and returns the next pending event.
        /// </summary>
        /// <param name="strict">
        /// Non-zero if an empty queue should be treated as an error.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the dequeued event, or null if the
        /// queue is empty and <paramref name="strict" /> is false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode DequeueEvent(bool strict, ref IEvent @event,
            ref Result error);

        /// <summary>
        /// This method queues a callback-based event for later execution.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the queued event.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the event is serviced.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueEvent(string name, DateTime dateTime,
            EventCallback callback, IClientData clientData,
            EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref Result error);

        /// <summary>
        /// This method queues a callback-based event for later execution and
        /// returns the event that was created.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the queued event.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the event is serviced.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the event that was queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueEvent(string name, DateTime dateTime,
            EventCallback callback, IClientData clientData,
            EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref IEvent @event,
            ref Result error);

        /// <summary>
        /// This method queues a script object for later execution.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the queued event.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="script">
        /// The script to evaluate when the event is serviced.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueScript(string name, DateTime dateTime,
            IScript script, EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref Result error);

        /// <summary>
        /// This method queues a script object for later execution and returns
        /// the event that was created.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the queued event.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="script">
        /// The script to evaluate when the event is serviced.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the event that was queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueScript(string name, DateTime dateTime,
            IScript script, EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref IEvent @event, ref Result error);

        /// <summary>
        /// This method queues a script, given as text, for later execution.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="text">
        /// The text of the script to evaluate when the event is serviced.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the script.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the script.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueScript(DateTime dateTime, string text,
            EngineFlags engineFlags, SubstitutionFlags substitutionFlags,
            EventFlags eventFlags, ExpressionFlags expressionFlags,
            EventPriority priority, long? threadId, int limit,
            ref Result error);

        /// <summary>
        /// This method queues a script, given as text, for later execution and
        /// returns the event that was created.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at or after which the event becomes ready to be
        /// serviced.
        /// </param>
        /// <param name="text">
        /// The text of the script to evaluate when the event is serviced.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the script.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the script.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control how the event is queued and serviced.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the script.
        /// </param>
        /// <param name="priority">
        /// The priority assigned to the event.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread the event is associated with, or null
        /// for none.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching events permitted in the queue, or
        /// zero for no limit.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the event that was queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode QueueScript(DateTime dateTime, string text,
            EngineFlags engineFlags, SubstitutionFlags substitutionFlags,
            EventFlags eventFlags, ExpressionFlags expressionFlags,
            EventPriority priority, long? threadId, int limit,
            ref IEvent @event, ref Result error);

        /// <summary>
        /// This method removes and returns any one event that is ready to be
        /// serviced, subject to the specified criteria.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time used to determine whether a queued event is ready
        /// to be serviced.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that an event must match to be considered.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be considered.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be considered, or
        /// null for any.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the absence of a ready event should be treated as an
        /// error.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the ready event that was dequeued, or
        /// null if none was ready and <paramref name="strict" /> is false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode DequeueAnyReadyEvent(DateTime dateTime,
            EventFlags eventFlags, EventPriority priority, long? threadId,
            bool strict, ref IEvent @event, ref Result error);

        /// <summary>
        /// This method lists the names of the pending events that match the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that an event name must match to be included, or null
        /// to include all events.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, this contains the list of matching event names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode ListEvents(string pattern, bool noCase, ref StringList list,
            ref Result error);
        /// <summary>
        /// This method lists the pending events that are selected by the
        /// specified match callback.
        /// </summary>
        /// <param name="callback">
        /// The callback invoked to determine whether each event should be
        /// included.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="events">
        /// Upon success, this contains the matching events.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode ListEvents(EventMatchCallback callback,
            IClientData clientData, ref IEnumerable<IEvent> events,
            ref Result error);

        /// <summary>
        /// This method cancels pending events matching the specified name or
        /// script.
        /// </summary>
        /// <param name="nameOrScript">
        /// The event name or script text used to select the events to cancel.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the absence of a matching event should be treated as an
        /// error.
        /// </param>
        /// <param name="all">
        /// Non-zero if all matching events should be cancelled rather than just
        /// the first one.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode CancelEvents(string nameOrScript, bool strict, bool all,
            ref Result error);

        /// <summary>
        /// This method returns the configured sleep time for the specified
        /// sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The category of sleep whose configured time is requested.
        /// </param>
        /// <returns>
        /// The sleep time, in milliseconds, for the specified sleep type.
        /// </returns>
        int GetSleepTime(SleepType sleepType);
        /// <summary>
        /// This method sets the sleep time for the specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The category of sleep whose time is being set.
        /// </param>
        /// <param name="sleepTime">
        /// The sleep time, in milliseconds, to set, or null to reset it to the
        /// default.
        /// </param>
        /// <returns>
        /// True if the sleep time was set; otherwise, false.
        /// </returns>
        bool SetSleepTime(SleepType sleepType, int? sleepTime);

        /// <summary>
        /// This method returns the configured minimum sleep time for the
        /// specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The category of sleep whose configured minimum time is requested.
        /// </param>
        /// <returns>
        /// The minimum sleep time, in milliseconds, for the specified sleep
        /// type.
        /// </returns>
        int GetMinimumSleepTime(SleepType sleepType);
        /// <summary>
        /// This method sets the minimum sleep time for the specified sleep
        /// type.
        /// </summary>
        /// <param name="sleepType">
        /// The category of sleep whose minimum time is being set.
        /// </param>
        /// <param name="sleepTime">
        /// The minimum sleep time, in milliseconds, to set, or null to reset
        /// it to the default.
        /// </param>
        /// <returns>
        /// True if the minimum sleep time was set; otherwise, false.
        /// </returns>
        bool SetMinimumSleepTime(SleepType sleepType, int? sleepTime);

        /// <summary>
        /// This method causes the current thread to sleep for the time
        /// configured for the specified sleep type.
        /// </summary>
        /// <param name="sleepType">
        /// The category of sleep that determines how long to sleep.
        /// </param>
        /// <param name="minimum">
        /// Non-zero to use the configured minimum sleep time rather than the
        /// regular sleep time.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the sleep completed successfully; otherwise, false.
        /// </returns>
        bool Sleep(SleepType sleepType, bool minimum, ref Result error);
        /// <summary>
        /// This method yields the remainder of the current thread's time slice
        /// to other threads.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the yield completed successfully; otherwise, false.
        /// </returns>
        bool Yield(ref Result error);

        /// <summary>
        /// This method services pending events matching the specified
        /// criteria.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the last event
        /// serviced.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode ProcessEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            ref Result result);

        /// <summary>
        /// This method services pending events matching the specified criteria
        /// and reports how many events were serviced.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count; on output, that count increased
        /// by the number of events serviced.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the last event
        /// serviced.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode ProcessEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            ref int eventCount, ref Result result);

        /// <summary>
        /// This method services at most one pending event matching the
        /// specified criteria.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user-interface events should also be serviced.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the event serviced.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode DoOneEvent(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            bool userInterface, ref Result result);

        /// <summary>
        /// This method services at most one pending event matching the
        /// specified criteria and reports how many events were serviced.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user-interface events should also be serviced.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count; on output, that count increased
        /// by the number of events serviced.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the event serviced.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode DoOneEvent(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            bool userInterface, ref int eventCount, ref Result result);

        /// <summary>
        /// This method services pending events for up to the specified timeout,
        /// optionally honoring or ignoring cancellation.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="timeout">
        /// The maximum time, in milliseconds, to spend servicing events, or
        /// null for no timeout.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore any pending local script cancellation while
        /// servicing events.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore any pending global script cancellation while
        /// servicing events.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user-interface events should also be serviced.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the last event
        /// serviced.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode ServiceEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int? timeout, int limit, bool noCancel, bool noGlobalCancel,
            bool stopOnError, bool errorOnEmpty, bool userInterface,
            ref Result result);

        /// <summary>
        /// This method services pending events for up to the specified timeout,
        /// optionally honoring or ignoring cancellation, and reports how many
        /// events were serviced.
        /// </summary>
        /// <param name="eventFlags">
        /// The flags that an event must match to be serviced.
        /// </param>
        /// <param name="priority">
        /// The priority that an event must match to be serviced.
        /// </param>
        /// <param name="threadId">
        /// The thread identifier that an event must match to be serviced, or
        /// null for any.
        /// </param>
        /// <param name="timeout">
        /// The maximum time, in milliseconds, to spend servicing events, or
        /// null for no timeout.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to service, or zero for no limit.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore any pending local script cancellation while
        /// servicing events.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore any pending global script cancellation while
        /// servicing events.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop servicing events as soon as one fails.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero if the absence of any matching event should be treated as
        /// an error.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user-interface events should also be serviced.
        /// </param>
        /// <param name="eventCount">
        /// On input, the initial event count; on output, that count increased
        /// by the number of events serviced.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of the last event
        /// serviced.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode ServiceEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int? timeout, int limit, bool noCancel, bool noGlobalCancel,
            bool stopOnError, bool errorOnEmpty, bool userInterface,
            ref int eventCount, ref Result result);

        /// <summary>
        /// This method waits, up to the specified timeout, for the event queue
        /// to become empty.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time, in milliseconds, to wait.
        /// </param>
        /// <param name="idle">
        /// Non-zero to wait on the idle event queue rather than the main event
        /// queue.
        /// </param>
        /// <param name="strict">
        /// Non-zero if a timeout (the queue not becoming empty) should be
        /// treated as an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode WaitForEmptyQueue(int timeout, bool idle, bool strict,
            ref Result error);

        /// <summary>
        /// This method waits, up to the specified timeout, for an event to be
        /// enqueued.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time, in milliseconds, to wait.
        /// </param>
        /// <param name="idle">
        /// Non-zero to wait on the idle event queue rather than the main event
        /// queue.
        /// </param>
        /// <param name="strict">
        /// Non-zero if a timeout (no event being enqueued) should be treated as
        /// an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode WaitForEventEnqueued(int timeout, bool idle, bool strict,
            ref Result error);
    }
}
