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
    [ObjectId("0fb6f163-3c5e-4bb8-a948-93bab9968b0f")]
    public interface IEventManager : IMaybeDisposed, ISimpleSynchronize, ISynchronize
    {
        int QueueEventCount { get; }
        int QueueIdleEventCount { get; }

        int EventCount { get; }
        int IdleEventCount { get; }
        int TotalEventCount { get; }

        int MaximumEventCount { get; }
        int MaximumIdleEventCount { get; }

        int MaybeDisposeEventCount { get; }
        int ReallyDisposeEventCount { get; }

        int WaitForEmptyQueueTotalCount { get; }
        int WaitForEmptyQueueErrorCount { get; }

        int WaitForEventEnqueuedTotalCount { get; }
        int WaitForEventEnqueuedErrorCount { get; }

        EventWaitHandle EmptyEvent { get; }
        EventWaitHandle EnqueueEvent { get; }
        EventWaitHandle IdleEmptyEvent { get; }
        EventWaitHandle IdleEnqueueEvent { get; }

        EventWaitHandle[] UserEvents { get; set; }
        bool Enabled { get; set; }
        bool Active { get; set; }
        bool NoNotify { get; set; }

        DateTimeNowCallback NowCallback { get; set; }

        void SaveEnabledAndForceDisabled(ref int savedEnabled);
        bool RestoreEnabled(int savedEnabled);

        ReturnCode Dump(ref Result result);

        ReturnCode ClearEvents(ref Result error);
        ReturnCode PeekEvent(bool strict, ref IEvent @event, ref Result error);
        ReturnCode GetEvent(string name, ref IEvent @event, ref Result error);

        ReturnCode DiscardEvent(bool strict, ref Result error);
        ReturnCode DequeueEvent(bool strict, ref IEvent @event,
            ref Result error);

        ReturnCode QueueEvent(string name, DateTime dateTime,
            EventCallback callback, IClientData clientData,
            EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref Result error);

        ReturnCode QueueEvent(string name, DateTime dateTime,
            EventCallback callback, IClientData clientData,
            EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref IEvent @event,
            ref Result error);

        ReturnCode QueueScript(string name, DateTime dateTime,
            IScript script, EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref Result error);

        ReturnCode QueueScript(string name, DateTime dateTime,
            IScript script, EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, ref IEvent @event, ref Result error);

        ReturnCode QueueScript(DateTime dateTime, string text,
            EngineFlags engineFlags, SubstitutionFlags substitutionFlags,
            EventFlags eventFlags, ExpressionFlags expressionFlags,
            EventPriority priority, long? threadId, int limit,
            ref Result error);

        ReturnCode QueueScript(DateTime dateTime, string text,
            EngineFlags engineFlags, SubstitutionFlags substitutionFlags,
            EventFlags eventFlags, ExpressionFlags expressionFlags,
            EventPriority priority, long? threadId, int limit,
            ref IEvent @event, ref Result error);

        ReturnCode DequeueAnyReadyEvent(DateTime dateTime,
            EventFlags eventFlags, EventPriority priority, long? threadId,
            bool strict, ref IEvent @event, ref Result error);

        ReturnCode ListEvents(string pattern, bool noCase, ref StringList list,
            ref Result error);
        ReturnCode ListEvents(EventMatchCallback callback,
            IClientData clientData, ref IEnumerable<IEvent> events,
            ref Result error);

        ReturnCode CancelEvents(string nameOrScript, bool strict, bool all,
            ref Result error);

        int GetSleepTime(SleepType sleepType);
        bool SetSleepTime(SleepType sleepType, int? sleepTime);

        int GetMinimumSleepTime(SleepType sleepType);
        bool SetMinimumSleepTime(SleepType sleepType, int? sleepTime);

        bool Sleep(SleepType sleepType, bool minimum, ref Result error);
        bool Yield();

        ReturnCode ProcessEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            ref Result result);

        ReturnCode ProcessEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            ref int eventCount, ref Result result);

        ReturnCode DoOneEvent(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            bool userInterface, ref Result result);

        ReturnCode DoOneEvent(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool stopOnError, bool errorOnEmpty,
            bool userInterface, ref int eventCount, ref Result result);

        ReturnCode ServiceEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool noCancel, bool noGlobalCancel,
            bool stopOnError, bool errorOnEmpty, bool userInterface,
            ref Result result);

        ReturnCode ServiceEvents(EventFlags eventFlags, EventPriority priority,
            long? threadId, int limit, bool noCancel, bool noGlobalCancel,
            bool stopOnError, bool errorOnEmpty, bool userInterface,
            ref int eventCount, ref Result result);

        ReturnCode WaitForEmptyQueue(int timeout, bool idle, bool strict,
            ref Result error);

        ReturnCode WaitForEventEnqueued(int timeout, bool idle, bool strict,
            ref Result error);
    }
}
