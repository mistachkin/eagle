/*
 * EventOps.cs --
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
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods used to support the Eagle
    /// event manager subsystem, including computing wait and sleep timings,
    /// processing the event queue, waiting for events, and dispatching
    /// background errors.
    /// </summary>
    [ObjectId("214d2a13-4973-41cd-a765-3f94b3c514ca")]
    internal static class EventOps
    {
        #region Private Constants
        #region Wait Handling
        /// <summary>
        /// The minimum amount of time, in milliseconds, to sleep while waiting
        /// for an object to be disposed.
        /// </summary>
        private static readonly int DisposeSleepMinimumTime = 1; /* milliseconds */

        /// <summary>
        /// The divisor used to scale a requested dispose time down into a
        /// per-iteration sleep time.
        /// </summary>
        private static readonly int DisposeSleepDivisor = 10;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The divisor used when converting a microsecond wait time into a
        /// millisecond value, to account for processing overhead.
        /// </summary>
        private static readonly int WaitGeneralDivisor = 2;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The maximum amount of time, in milliseconds, to sleep during a
        /// single iteration of a wait loop.
        /// </summary>
        private static readonly int WaitSleepMaximumTime = 50; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The divisor used to compute the slop time allowed when checking
        /// whether a wait has elapsed.
        /// </summary>
        private static readonly int WaitSlopDivisor = 40;

        /// <summary>
        /// The maximum slop time, in microseconds, permitted when checking
        /// whether a wait has elapsed.
        /// </summary>
        private static readonly int WaitSlopMinimumTime = 25000; /* microseconds */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The minimum elapsed time, in microseconds, beyond which a completed
        /// wait is traced at a higher priority.
        /// </summary>
        private static readonly int WaitTraceMinimumTime = 2000000; /* microseconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Handling
        //
        // NOTE: This string is used to indent the details about a background
        //       error.
        //
        /// <summary>
        /// The string used to indent the detail lines included with a reported
        /// background error.
        /// </summary>
        private const string BackgroundErrorDetailIndent = "    ";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Manager Support Methods
        /// <summary>
        /// This method computes the amount of time to sleep, in milliseconds,
        /// between checks while waiting for an object to be disposed.
        /// </summary>
        /// <param name="milliseconds">
        /// The total amount of time, in milliseconds, available for the wait.
        /// </param>
        /// <returns>
        /// The number of milliseconds to sleep between successive checks.
        /// </returns>
        public static int GetDisposeSleepMilliseconds(
            int milliseconds
            )
        {
            return Math.Max(DisposeSleepMinimumTime,
                milliseconds / DisposeSleepDivisor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes an optional microsecond value into a concrete
        /// microsecond value, substituting a default when none is supplied.
        /// </summary>
        /// <param name="microseconds">
        /// The requested time, in microseconds, or null to use the default
        /// based on the maximum per-iteration sleep time.
        /// </param>
        /// <returns>
        /// The resolved time, in microseconds.
        /// </returns>
        private static long GetMicroseconds(
            long? microseconds
            )
        {
            if (microseconds != null)
                return (long)microseconds;

            return PerformanceOps.GetMicrosecondsFromMilliseconds(
                WaitSleepMaximumTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a time expressed in microseconds into a
        /// millisecond value suitable for use as a per-iteration sleep time.
        /// </summary>
        /// <param name="microseconds">
        /// The time, in microseconds, to convert.
        /// </param>
        /// <returns>
        /// The equivalent time, in milliseconds, scaled to account for
        /// processing overhead.
        /// </returns>
        public static int GetMilliseconds(
            long microseconds
            )
        {
            return ConversionOps.ToInt(
                PerformanceOps.GetMillisecondsFromMicroseconds(
                    microseconds) / WaitGeneralDivisor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the slop time, in microseconds, allowed when
        /// determining whether a wait has elapsed, ensuring it does not exceed
        /// the actual wait time.
        /// </summary>
        /// <param name="microseconds">
        /// The total wait time, in microseconds.
        /// </param>
        /// <returns>
        /// The slop time, in microseconds.
        /// </returns>
        public static long GetSlopMicroseconds(
            long microseconds
            )
        {
            //
            // BUGFIX: Make sure the slop time does not exceed the actual
            //         wait.
            //
            return Math.Min(
                microseconds / WaitSlopDivisor, WaitSlopMinimumTime);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the event flags used when queueing an event,
        /// based on the supplied flags.
        /// </summary>
        /// <param name="eventFlags">
        /// The base event flags to start from.
        /// </param>
        /// <param name="debug">
        /// Non-zero to include the debug event flag in the result.
        /// </param>
        /// <returns>
        /// The event flags suitable for queueing an event.
        /// </returns>
        public static EventFlags GetQueueEventFlags(
            EventFlags eventFlags,
            bool debug
            )
        {
            EventFlags newEventFlags = eventFlags;

            newEventFlags &= ~EventFlags.DequeueMask;
            newEventFlags |= EventFlags.After | EventFlags.Interpreter;

            if (debug)
                newEventFlags |= EventFlags.Debug;

            return newEventFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the supplied interpreter readiness flags so that
        /// full readiness checks are performed when script cancellation must be
        /// honored.
        /// </summary>
        /// <param name="noCancel">
        /// Non-zero if script cancellation is not being checked for; otherwise,
        /// zero.
        /// </param>
        /// <param name="readyFlags">
        /// The interpreter readiness flags to adjust, in place.
        /// </param>
        public static void AdjustReadyFlags(
            bool noCancel,            /* in */
            ref ReadyFlags readyFlags /* in, out */
            )
        {
            //
            // HACK: Always perform full interpreter readiness checks
            //       *IF* we actually care about script cancellation.
            //
            if (!noCancel)
                readyFlags &= ~ReadyFlags.Limited;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the pre-wait and post-wait callbacks currently
        /// configured for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query, or null.
        /// </param>
        /// <param name="preCallback">
        /// Upon success, receives the configured pre-wait callback, if any;
        /// otherwise, receives null.
        /// </param>
        /// <param name="postCallback">
        /// Upon success, receives the configured post-wait callback, if any;
        /// otherwise, receives null.
        /// </param>
        public static void QueryWaitCallbacks(
            Interpreter interpreter,
            out EventCallback preCallback,
            out EventCallback postCallback
            )
        {
            preCallback = null;
            postCallback = null;

            if (interpreter == null)
                return;

            bool locked = false;

            try
            {
                interpreter.InternalSoftTryLock(
                    ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    preCallback = interpreter.InternalPreWaitCallback;
                    postCallback = interpreter.InternalPostWaitCallback;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified event manager is valid
        /// and usable.
        /// </summary>
        /// <param name="eventManager">
        /// The event manager to check, or null.
        /// </param>
        /// <returns>
        /// True if the event manager is non-null and has not been disposed;
        /// otherwise, false.
        /// </returns>
        public static bool ManagerIsOk(
            IEventManager eventManager
            ) /* THREAD-SAFE */
        {
            if (eventManager == null)
                return false;

            if (eventManager.Disposed)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the current enabled state of the event manager for
        /// the specified interpreter and then forcibly disables it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose event manager is to be modified, or null.
        /// </param>
        /// <param name="nullOk">
        /// Non-zero to treat a missing event manager as success.
        /// </param>
        /// <param name="savedEnabled">
        /// Upon success, receives the previously saved enabled state of the
        /// event manager.
        /// </param>
        /// <returns>
        /// True if the operation was performed (or tolerated); otherwise,
        /// false.
        /// </returns>
        public static bool SaveEnabledAndForceDisabled(
            Interpreter interpreter,
            bool nullOk,
            ref int savedEnabled
            )
        {
            if (interpreter == null)
                return false;

            //
            // TODO: Consider allowing this method to succeed when there is
            //       no valid event manager for the interpreter, i.e. if no
            //       event manager, no events could be processed anyway?
            //
            IEventManager eventManager = interpreter.EventManager;

            if (!ManagerIsOk(eventManager))
                return nullOk && (eventManager == null);

            /* NO RESULT */
            eventManager.SaveEnabledAndForceDisabled(ref savedEnabled);

            /* SUCCESS */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved enabled state of the event
        /// manager for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose event manager is to be modified, or null.
        /// </param>
        /// <param name="savedEnabled">
        /// The previously saved enabled state to restore.
        /// </param>
        /// <returns>
        /// The result of restoring the enabled state, or null if the
        /// interpreter or its event manager was not available.
        /// </returns>
        public static bool? RestoreEnabled(
            Interpreter interpreter,
            int savedEnabled
            )
        {
            if (interpreter == null)
                return null;

            IEventManager eventManager = interpreter.EventManager;

            if (!ManagerIsOk(eventManager))
                return null;

            return eventManager.RestoreEnabled(savedEnabled);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sleeps using the event manager for the specified
        /// interpreter, allowing events to be processed as appropriate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose event manager should perform the sleep, or
        /// null.
        /// </param>
        /// <param name="sleepType">
        /// The type of sleep being requested.
        /// </param>
        /// <param name="minimum">
        /// Non-zero to sleep for the minimum amount of time.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the sleep was performed successfully; otherwise, false.
        /// </returns>
        public static bool Sleep(
            Interpreter interpreter,
            SleepType sleepType,
            bool minimum,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (!ManagerIsOk(eventManager))
                return false;

            return eventManager.Sleep(sleepType, minimum, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes pending events for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose events should be processed.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags controlling which events are processed.
        /// </param>
        /// <param name="priority">
        /// The minimum priority of events to be processed.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose events should be processed, or
        /// null for any thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero for all of them.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing events upon the first error.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to return an error when there are no events to process.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of processing events; upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ProcessEvents(
            Interpreter interpreter,
            EventFlags eventFlags,
            EventPriority priority,
            long? threadId,
            int limit,
            bool stopOnError,
            bool errorOnEmpty,
            ref Result result
            )
        {
            int eventCount = 0;

            return ProcessEvents(
                interpreter, eventFlags, priority, threadId, limit,
                stopOnError, errorOnEmpty, ref eventCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes pending events for the specified interpreter,
        /// updating a running count of the events that have been processed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose events should be processed.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags controlling which events are processed.
        /// </param>
        /// <param name="priority">
        /// The minimum priority of events to be processed.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose events should be processed, or
        /// null for any thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero for all of them.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing events upon the first error.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to return an error when there are no events to process.
        /// </param>
        /// <param name="eventCount">
        /// The running count of processed events, updated in place to include
        /// the events processed by this call.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of processing events; upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ProcessEvents(
            Interpreter interpreter,
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
            if (interpreter != null)
            {
                IEventManager eventManager = interpreter.EventManager;

                if (ManagerIsOk(eventManager))
                {
                    return eventManager.ProcessEvents(
                        eventFlags, priority, threadId, limit, stopOnError,
                        errorOnEmpty, ref eventCount, ref result);
                }
                else
                {
                    result = "event manager not available";
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes a single pending event for the specified
        /// interpreter, updating a running count of the events that have been
        /// processed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose event should be processed.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags controlling which events are processed.
        /// </param>
        /// <param name="priority">
        /// The minimum priority of events to be processed.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread whose events should be processed, or
        /// null for any thread.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process, or zero for all of them.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing events upon the first error.
        /// </param>
        /// <param name="errorOnEmpty">
        /// Non-zero to return an error when there are no events to process.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also process pending user-interface events.
        /// </param>
        /// <param name="eventCount">
        /// The running count of processed events, updated in place to include
        /// the events processed by this call.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of processing the event; upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode DoOneEvent(
            Interpreter interpreter,
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
            if (interpreter != null)
            {
                //
                // BUGFIX: Wrap in try/catch in case the interpreter is
                //         disposed (from WaitVariable).
                //
                try
                {
                    IEventManager eventManager = interpreter.EventManager;

                    if (ManagerIsOk(eventManager))
                    {
                        return eventManager.DoOneEvent(
                            eventFlags, priority, threadId, limit,
                            stopOnError, errorOnEmpty, userInterface,
                            ref eventCount, ref result);
                    }
                    else
                    {
                        result = "event manager not available";
                    }
                }
                catch (Exception e)
                {
                    result = e;
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the trace priority to use when tracing the
        /// completion of a wait, based on how long the wait actually took.
        /// </summary>
        /// <param name="waitMicroseconds">
        /// The requested wait time, in microseconds, or null.
        /// </param>
        /// <param name="outerElapsedMicroseconds">
        /// The actual elapsed time, in microseconds, of the wait.
        /// </param>
        /// <returns>
        /// The trace priority to use.
        /// </returns>
        private static TracePriority GetTracePriority(
            long? waitMicroseconds,
            double outerElapsedMicroseconds
            )
        {
            if ((waitMicroseconds == null) ||
                (outerElapsedMicroseconds <= (double)waitMicroseconds) ||
                (outerElapsedMicroseconds < WaitTraceMinimumTime))
            {
                return TracePriority.EventDebug3;
            }

            return TracePriority.EventDebug2;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified event to be signaled, for up to
        /// the requested amount of time, optionally processing events and
        /// honoring script cancellation while it waits.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the wait.
        /// </param>
        /// <param name="event">
        /// The event to wait on, or null to simply wait for the requested
        /// amount of time.
        /// </param>
        /// <param name="waitMicroseconds">
        /// The total amount of time, in microseconds, to wait, or null to use
        /// the default.
        /// </param>
        /// <param name="readyMicroseconds">
        /// The amount of time, in microseconds, to wait for interpreter
        /// readiness, or null to use the default.
        /// </param>
        /// <param name="timeout">
        /// Non-zero to time out the interpreter readiness checks.
        /// </param>
        /// <param name="noWindows">
        /// Non-zero to skip processing of Windows messages.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore script cancellation while waiting.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore global script cancellation while waiting.
        /// </param>
        /// <param name="trace">
        /// Non-zero to emit diagnostic trace output about the wait.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            bool timeout,
            bool noWindows,
            bool noCancel,
            bool noGlobalCancel,
            bool trace,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool timedOut = false;

            return Wait(
                interpreter, @event, waitMicroseconds, readyMicroseconds,
                timeout, noWindows, noCancel, noGlobalCancel, trace,
                ref timedOut, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified event to be signaled, for up to
        /// the requested amount of time, optionally processing events and
        /// honoring script cancellation while it waits, and reporting whether
        /// the wait timed out.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the wait.
        /// </param>
        /// <param name="event">
        /// The event to wait on, or null to simply wait for the requested
        /// amount of time.
        /// </param>
        /// <param name="waitMicroseconds">
        /// The total amount of time, in microseconds, to wait, or null to use
        /// the default.
        /// </param>
        /// <param name="readyMicroseconds">
        /// The amount of time, in microseconds, to wait for interpreter
        /// readiness, or null to use the default.
        /// </param>
        /// <param name="timeout">
        /// Non-zero to time out the interpreter readiness checks.
        /// </param>
        /// <param name="noWindows">
        /// Non-zero to skip processing of Windows messages.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to ignore script cancellation while waiting.
        /// </param>
        /// <param name="noGlobalCancel">
        /// Non-zero to ignore global script cancellation while waiting.
        /// </param>
        /// <param name="trace">
        /// Non-zero to emit diagnostic trace output about the wait.
        /// </param>
        /// <param name="timedOut">
        /// Upon return, indicates whether the interpreter readiness check timed
        /// out.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            bool timeout,
            bool noWindows,
            bool noCancel,
            bool noGlobalCancel,
            bool trace,
            ref bool timedOut,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            long outerStartCount = PerformanceOps.GetCount();
            long outerStopCount = 0;

            try
            {
                IAnyClientData clientData = null;

                try
                {
                    ReturnCode code = ReturnCode.Ok;

                    ///////////////////////////////////////////////////////////

                    EventCallback preCallback;
                    EventCallback postCallback;

                    QueryWaitCallbacks(
                        interpreter, out preCallback, out postCallback);

                    if ((preCallback != null) || (postCallback != null))
                    {
                        clientData = new AnyClientData();

                        clientData.TrySetAny("event", @event);

                        clientData.TrySetAny(
                            "waitMicroseconds", waitMicroseconds);

                        clientData.TrySetAny(
                            "readyMicroseconds", readyMicroseconds);

                        clientData.TrySetAny("timeout", timeout);
                        clientData.TrySetAny("noWindows", noWindows);
                        clientData.TrySetAny("noCancel", noCancel);
                        clientData.TrySetAny("noGlobalCancel", noGlobalCancel);
                    }

                    ///////////////////////////////////////////////////////////

                    if (preCallback != null)
                    {
                        clientData.TrySetAny("callback", "preWaitBusy");
                        clientData.TrySetAny("code", code);
                        clientData.TrySetAny("error", error);

                        try
                        {
                            ReturnCode callbackCode;
                            Result callbackError = null;

                            callbackCode = preCallback(
                                interpreter, clientData,
                                ref callbackError); /* throw */

                            if (callbackCode != ReturnCode.Ok)
                            {
                                error = callbackError;
                                return callbackCode;
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return ReturnCode.Error;
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    int waitCount;

                    if ((waitCount = interpreter.EnterWait()) > 0)
                    {
                        if (waitMicroseconds == 0)
                        {
                            if ((@event == null) ||
                                !ThreadOps.WaitEvent(@event, 0))
                            {
#if WINFORMS
                                //
                                // NOTE: If necessary, process all Windows
                                //       messages from the queue.
                                //
                                if (!noWindows)
                                {
                                    code = WindowOps.ProcessEvents(
                                        interpreter, ref error);
                                }

                                if (code == ReturnCode.Ok)
#endif
                                {
                                    //
                                    // NOTE: Yield to other running threads.
                                    //       This gives them an opportunity
                                    //       to cancel the script in progress
                                    //       on this thread.
                                    //
                                    code = HostOps.ThreadYield(ref error);
                                }
                            }
                            else
                            {
                                error = "event was signaled";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            //
                            // HACK: Attempt to transform (nullable) long
                            //       integer microsecond parameter values
                            //       into long integer values usable by
                            //       this method.
                            //
                            if ((waitMicroseconds != null) &&
                                (readyMicroseconds == null))
                            {
                                readyMicroseconds = waitMicroseconds;
                            }

                            long localWaitMicroseconds = GetMicroseconds(
                                waitMicroseconds);

                            long localReadyMicroseconds = GetMicroseconds(
                                readyMicroseconds);

                            //
                            // NOTE: Keep track of how many iterations
                            //       through the loop we take.
                            //
                            int iterations = 0;

                            //
                            // HACK: Account for our processing overhead;
                            //       use half of the requested delay.
                            //
                            int waitMilliseconds = GetMilliseconds(
                                localWaitMicroseconds);

                            if (waitMilliseconds < 0)
                                waitMilliseconds = 0;

                            if (waitMilliseconds > WaitSleepMaximumTime)
                                waitMilliseconds = WaitSleepMaximumTime;

                            int readyMilliseconds = GetMilliseconds(
                                localReadyMicroseconds);

                            if (readyMilliseconds < 0)
                                readyMilliseconds = 0;

                            if (readyMilliseconds > waitMilliseconds)
                                readyMilliseconds = waitMilliseconds;

                            //
                            // NOTE: For precise timing, use high-resolution
                            //       CPU performance counter.
                            //
                            long innerStartCount = PerformanceOps.GetCount();
                            long innerStopCount = 0;

                            //
                            // BUGFIX: Make sure slop time does not exceed
                            //         the actual wait.
                            //
                            long slopMicroseconds = GetSlopMicroseconds(
                                localWaitMicroseconds);

                            //
                            // NOTE: Delay for approximately the specified
                            //       number of microseconds, optionally
                            //       timing out if we cannot obtain the
                            //       interpreter lock in time.
                            //
                            double totalWaitMilliseconds = 0.0;
                            bool notReady; /* NOT USED */

                            while (((code = Interpreter.EventReady(
                                    interpreter, timeout ?
                                        (int?)readyMilliseconds : null,
                                    noCancel, noGlobalCancel, out notReady,
                                    out timedOut, ref error)) == ReturnCode.Ok) &&
                                !PerformanceOps.HasElapsed(
                                    innerStartCount, ref innerStopCount,
                                    localWaitMicroseconds, slopMicroseconds))
                            {
                                //
                                // NOTE: If user-specified event is signaled,
                                //       exit loop prior to other processing.
                                //
                                if ((@event != null) &&
                                    ThreadOps.WaitEvent(@event, 0))
                                {
                                    error = "event was signaled";
                                    code = ReturnCode.Error;

                                    break;
                                }

#if WINFORMS
                                if (!noWindows)
                                {
                                    code = WindowOps.ProcessEvents(
                                        interpreter, ref error);

                                    if (code != ReturnCode.Ok)
                                        break;
                                }
#endif

                                if (@event != null)
                                {
                                    if (ThreadOps.WaitEvent(
                                            @event, waitMilliseconds))
                                    {
                                        error = "event was signaled";
                                        code = ReturnCode.Error;

                                        break;
                                    }
                                }
                                else
                                {
                                    code = HostOps.Sleep(
                                        interpreter, waitMilliseconds,
                                        ref error);

                                    if (code != ReturnCode.Ok)
                                        break;
                                }

                                totalWaitMilliseconds += waitMilliseconds;
                                iterations++;
                            }

                            if (trace)
                            {
                                double innerElapsedMicroseconds =
                                    PerformanceOps.GetMicrosecondsFromCount(
                                        innerStartCount, innerStopCount, 1,
                                        false); /* EXEMPT */

                                TraceOps.DebugTrace(String.Format(
                                    "Wait: interpreter = {0}, event = {1}, " +
                                    "code = {2}, iterations = {3}, " +
                                    "waitMicroseconds = {4}, " +
                                    "readyMicroseconds = {5}, timeout = {6}, " +
                                    "noWindows = {7}, noCancel = {8}, " +
                                    "elapsedMicroseconds = {9}, " +
                                    "waitMilliseconds = {10}, " +
                                    "readyMilliseconds = {11}, " +
                                    "slopMicroseconds = {12}, " +
                                    "differenceMicroseconds = {13}, " +
                                    "totalWaitMilliseconds = {14}, " +
                                    "waitCount = {15}, error = {16}",
                                    FormatOps.InterpreterNoThrow(interpreter),
                                    FormatOps.WrapOrNull(@event), code,
                                    iterations, localWaitMicroseconds,
                                    localReadyMicroseconds, timeout, noWindows,
                                    noCancel, innerElapsedMicroseconds,
                                    waitMilliseconds, readyMilliseconds,
                                    slopMicroseconds, innerElapsedMicroseconds -
                                    (double)localWaitMicroseconds,
                                    FormatOps.PerformanceMilliseconds(
                                        totalWaitMilliseconds),
                                    waitCount, FormatOps.WrapOrNull(
                                        true, true, error)),
                                    typeof(EventOps).Name,
                                    TracePriority.EventDebug);
                            }
                        }

                        /* IGNORED */
                        interpreter.ExitWait();
                    }
                    else
                    {
                        error = "wait subsystem locked";
                        code = ReturnCode.Error;
                    }

                    ///////////////////////////////////////////////////////////

                    if (postCallback != null)
                    {
                        clientData.TrySetAny("callback", "postWaitBusy");
                        clientData.TrySetAny("code", code);
                        clientData.TrySetAny("error", error);

                        try
                        {
                            ReturnCode callbackCode;
                            Result callbackError = null;

                            callbackCode = postCallback(
                                interpreter, clientData,
                                ref callbackError); /* throw */

                            if (callbackCode != ReturnCode.Ok)
                            {
                                error = callbackError;
                                return callbackCode;
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return ReturnCode.Error;
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    return code;
                }
                finally
                {
                    ObjectOps.DisposeOrTrace<IAnyClientData>(
                        interpreter, ref clientData);

                    clientData = null;
                }
            }
            finally
            {
                outerStopCount = PerformanceOps.GetCount();

                if (trace)
                {
                    double outerElapsedMicroseconds =
                        PerformanceOps.GetMicrosecondsFromCount(
                            outerStartCount, outerStopCount, 1,
                            false); /* EXEMPT */

                    TracePriority priority = GetTracePriority(
                        waitMicroseconds, outerElapsedMicroseconds);

                    TraceOps.DebugTrace(String.Format(
                        "Wait: {0} for interpreter {1}",
                        FormatOps.PerformanceMicroseconds(
                            outerElapsedMicroseconds),
                        FormatOps.InterpreterNoThrow(interpreter)),
                        typeof(EventOps).Name, priority);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Reporter
        /// <summary>
        /// This method formats and reports the details of a background error to
        /// the appropriate diagnostic channel.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the background error, or null.
        /// </param>
        /// <param name="handlerName">
        /// The name of the background error handler, if any.
        /// </param>
        /// <param name="description0">
        /// The format string describing the overall background error, or null.
        /// </param>
        /// <param name="description1">
        /// The label describing the first (original) error detail, or null.
        /// </param>
        /// <param name="code1">
        /// The return code associated with the first error detail.
        /// </param>
        /// <param name="result1">
        /// The result associated with the first error detail.
        /// </param>
        /// <param name="errorLine1">
        /// The error line number associated with the first error detail.
        /// </param>
        /// <param name="description2">
        /// The label describing the second error detail, or null.
        /// </param>
        /// <param name="code2">
        /// The return code associated with the second error detail.
        /// </param>
        /// <param name="result2">
        /// The result associated with the second error detail.
        /// </param>
        /// <param name="errorLine2">
        /// The error line number associated with the second error detail.
        /// </param>
        private static void ReportBackgroundError(
            Interpreter interpreter,
            string handlerName,
            string description0,
            string description1,
            ReturnCode code1,
            Result result1,
            int errorLine1,
            string description2,
            ReturnCode code2,
            Result result2,
            int errorLine2
            )
        {
            bool[] haveDescription = {
                !String.IsNullOrEmpty(description0),
                !String.IsNullOrEmpty(description1),
                !String.IsNullOrEmpty(description2)
            };

            Result bgReport = String.Concat(
                haveDescription[0] ?
                    String.Format(description0,
                        FormatOps.WrapOrNull(handlerName)) :
                        String.Empty, haveDescription[0] ?
                            Environment.NewLine : String.Empty,
                haveDescription[1] ?
                    String.Format("{0}{1}: {2}", haveDescription[0] ?
                        BackgroundErrorDetailIndent : String.Empty,
                        description1,
                            ResultOps.Format(code1,
                                result1, errorLine1, false, true)) :
                        String.Empty, haveDescription[1] ?
                            Environment.NewLine : String.Empty,
                haveDescription[2] ?
                    String.Format("{0}{1}: {2}", haveDescription[0] ?
                        BackgroundErrorDetailIndent : String.Empty,
                        description2,
                            ResultOps.Format(code2,
                                result2, errorLine2, false, true)) :
                        String.Empty, haveDescription[2] ?
                            Environment.NewLine : String.Empty);

            //
            // TODO: Something else here as well?
            //
            if (!String.IsNullOrEmpty(bgReport))
                DebugOps.Complain(interpreter, code2, bgReport);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Executor
        /// <summary>
        /// This method executes the background error handler for the specified
        /// interpreter within a dedicated call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to execute the background error handler.
        /// </param>
        /// <param name="handlerName">
        /// The name of the background error handler being executed.
        /// </param>
        /// <param name="execute">
        /// The resolved entity used to execute the background error handler.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the background error handler, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments to pass to the background error handler.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of executing the background error
        /// handler; upon failure, receives an error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, receives the error line number reported by the
        /// background error handler.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode ExecuteBackgroundError(
            Interpreter interpreter,
            string handlerName,
            IExecute execute,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result,
            ref int errorLine
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Create a new call frame for the background error handler
            //       and push it.
            //
            ICallFrame frame = interpreter.NewTrackingCallFrame(
                StringList.MakeList("bgerror", handlerName),
                CallFrameFlags.BackgroundError);

            interpreter.PushAutomaticCallFrame(frame);

            try
            {
                //
                // NOTE: Save current engine flags and then enable external
                //       execution.
                //
                EngineFlags savedEngineFlags = interpreter.BeginExternalExecution();

                try
                {
                    //
                    // NOTE: If the interpreter is configured to reset the
                    //       script cancellation flags prior to executing
                    //       the background error handler, do that now.
                    //
                    if (ScriptOps.HasFlags(
                            interpreter, InterpreterFlags.BgErrorResetCancel,
                            true))
                    {
                        /* IGNORED */
                        Engine.ResetCancel(interpreter, CancelFlags.BgError);
                    }

                    //
                    // NOTE: Evaluate the script and then check the result to
                    //       see if the background error handler failed or
                    //       canceled further background error handling for
                    //       this invocation of ProcessEvents.
                    //
                    ReturnCode code;

                    code = interpreter.Execute(
                        handlerName, execute, clientData, arguments,
                        ref result);

                    //
                    // NOTE: Maybe grab the new error line number, if any.
                    //
                    if (code != ReturnCode.Ok)
                        errorLine = Interpreter.GetErrorLine(interpreter);

                    //
                    // NOTE: We are done now, return.
                    //
                    return code;
                }
                finally
                {
                    //
                    // NOTE: Restore saved engine flags, disabling external
                    //       execution as necessary.
                    //
                    /* IGNORED */
                    interpreter.EndAndCleanupExternalExecution(
                        savedEngineFlags);
                }
            }
            finally
            {
                //
                // NOTE: Pop the original call frame that we pushed above
                //       and any intervening scope call frames that may be
                //       leftover (i.e. they were not explicitly closed).
                //
                /* IGNORED */
                interpreter.PopScopeCallFramesAndOneMore();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Dispatcher
        /// <summary>
        /// This method handles a background error by dispatching it to the
        /// background error handler configured for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the background error.
        /// </param>
        /// <param name="code">
        /// The return code associated with the original error.
        /// </param>
        /// <param name="result">
        /// The result associated with the original error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the background error was handled;
        /// otherwise, an error code.
        /// </returns>
        public static ReturnCode HandleBackgroundError(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            )
        {
            bool bgError = false;

            return HandleBackgroundError(
                interpreter, code, result, ref bgError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles a background error by dispatching it to the
        /// background error handler configured for the specified interpreter,
        /// reporting whether the handler should be invoked again later.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the background error.
        /// </param>
        /// <param name="code">
        /// The return code associated with the original error.
        /// </param>
        /// <param name="result">
        /// The result associated with the original error.
        /// </param>
        /// <param name="bgError">
        /// Upon return, indicates whether the background error handler should be
        /// invoked again on the next pass through the event loop.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the background error was handled;
        /// otherwise, an error code.
        /// </returns>
        public static ReturnCode HandleBackgroundError(
            Interpreter interpreter,
            ReturnCode code,
            Result result,
            ref bool bgError
            )
        {
            if (interpreter == null)
            {
                ReportBackgroundError(interpreter /* null */, null,
                    "cannot handle background error, interpreter is " +
                    "invalid.", "Original error", code, result, 0, null,
                    ReturnCode.Ok, null, 0);

                return ReturnCode.Error;
            }

            //
            // BUGFIX: Acquire the interpreter lock here; however, do not use
            //         the public property just in case the interpreter may be
            //         disposed at this point.
            //
            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: Do not try to handle any background errors with a
                //         deleted or disposed interpreter.
                //
                if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                {
                    ReportBackgroundError(interpreter /* disposed? */, null,
                        "cannot handle background error, interpreter is " +
                        "deleted or disposed.", "Original error", code,
                        result, 0, null, ReturnCode.Ok, null, 0);

                    return ReturnCode.Error;
                }

                int errorLine = Interpreter.GetErrorLine(interpreter);
                string handlerName = interpreter.BackgroundError;

                //
                // NOTE: If there is an invalid background error handler set,
                //       ignore the error.
                //
                if (!String.IsNullOrEmpty(handlerName))
                {
                    //
                    // NOTE: Should a failure to handle the background
                    //       error simply be ignored?
                    //
                    bool ignoreFailure = ScriptOps.HasFlags(interpreter,
                        InterpreterFlags.IgnoreBgErrorFailure, true);

                    //
                    // NOTE: We do not yet know if the background error
                    //       handler can actually be resolved.
                    //
                    bool haveBgError = false;

                    //
                    // NOTE: Construct a new argument list to pass along
                    //       to the actual error handler command, e.g.
                    //       "[list bgerror <message>]".
                    //
                    ArgumentList bgArguments = new ArgumentList(
                        handlerName, result);

                    //
                    // NOTE: Attempt to lookup the background error
                    //       handler via the current command resolvers
                    //       for the interpreter.  If this lookup fails
                    //       the default background error processing
                    //       will be used.
                    //
                    ReturnCode resolveCode;
                    Result resolveError = null;
                    IExecute bgExecute = null;

                    resolveCode = interpreter.InternalGetIExecuteViaResolvers(
                        interpreter.GetResolveEngineFlagsNoLock(true),
                        handlerName, bgArguments, LookupFlags.Default,
                        ref bgExecute, ref resolveError);

                    if (resolveCode == ReturnCode.Ok)
                    {
                        //
                        // NOTE: We found a background error handler.
                        //
                        haveBgError = true;

                        //
                        // NOTE: Execute the background error handler now
                        //       and save the results.
                        //
                        ReturnCode bgCode;
                        Result bgResult = null;
                        int bgErrorLine = 0;

                        bgCode = ExecuteBackgroundError(
                            interpreter, handlerName, bgExecute, null,
                            bgArguments, ref bgResult, ref bgErrorLine);

                        //
                        // NOTE: Now we handle the return code for the
                        //       background error handler.
                        //
                        if (bgCode == ReturnCode.Break)
                        {
                            //
                            // NOTE: A return code of "Break" indicates
                            //       that we should not call the background
                            //       error handler until the next time
                            //       ProcessEvents is invoked.
                            //
                            bgError = false;
                        }
                        else if (!ignoreFailure && (bgCode != ReturnCode.Ok))
                        {
                            //
                            // NOTE: Any other non-"Ok" return code is an
                            //       error an gets reported to the standard
                            //       error channel of the host, if any.
                            //
                            ReportBackgroundError(interpreter, handlerName,
                                "handler {0} failed for background error.",
                                "Original error", code, result, errorLine,
                                "Handler error", bgCode, bgResult,
                                bgErrorLine);
                        }
                    }

                    //
                    // NOTE: If there is no background error handler setup
                    //       just write the errorInfo to the error channel
                    //       (if possible).  If failures should be ignored,
                    //       skip reporting the problem.
                    //
                    if (!ignoreFailure && !haveBgError)
                    {
                        ReportBackgroundError(interpreter, handlerName,
                            "handler {0} missing for background error.",
                            "Original error", code, result, errorLine,
                            "Resolver error", resolveCode, resolveError,
                            0);
                    }
                }

                return ReturnCode.Ok;
            }
        }
        #endregion
        #endregion
    }
}
