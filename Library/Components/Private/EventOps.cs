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
    [ObjectId("214d2a13-4973-41cd-a765-3f94b3c514ca")]
    internal static class EventOps
    {
        #region Private Constants
        #region Wait Handling
        private static readonly int DisposeSleepMinimumTime = 1; /* milliseconds */
        private static readonly int DisposeSleepDivisor = 10;

        ///////////////////////////////////////////////////////////////////////

        private static readonly int WaitGeneralDivisor = 2;

        ///////////////////////////////////////////////////////////////////////

        private static readonly int WaitSleepMaximumTime = 50; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        private static readonly int WaitSlopDivisor = 40;
        private static readonly int WaitSlopMinimumTime = 25000; /* microseconds */

        ///////////////////////////////////////////////////////////////////////

        private static readonly int WaitTraceMinimumTime = 2000000; /* microseconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Handling
        //
        // NOTE: This string is used to indent the details about a background
        //       error.
        //
        private const string BackgroundErrorDetailIndent = "    ";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Manager Support Methods
        public static int GetDisposeSleepMilliseconds(
            int milliseconds
            )
        {
            return Math.Max(DisposeSleepMinimumTime,
                milliseconds / DisposeSleepDivisor);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static int GetMilliseconds(
            long microseconds
            )
        {
            return ConversionOps.ToInt(
                PerformanceOps.GetMillisecondsFromMicroseconds(
                    microseconds) / WaitGeneralDivisor);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            bool timeout,
            bool noWindows,
            bool noCancel,
            bool noGlobalCancel,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool timedOut = false;

            return Wait(
                interpreter, @event, waitMicroseconds, readyMicroseconds,
                timeout, noWindows, noCancel, noGlobalCancel, ref timedOut,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            bool timeout,
            bool noWindows,
            bool noCancel,
            bool noGlobalCancel,
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

                            while (((code = Interpreter.EventReady(
                                    interpreter, timeout ?
                                        readyMilliseconds : _Timeout.Infinite,
                                    noCancel, noGlobalCancel, out timedOut,
                                    ref error)) == ReturnCode.Ok) &&
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
                }
            }
            finally
            {
                outerStopCount = PerformanceOps.GetCount();

                double outerElapsedMicroseconds =
                    PerformanceOps.GetMicrosecondsFromCount(
                        outerStartCount, outerStopCount, 1,
                        false); /* EXEMPT */

                TracePriority priority = (waitMicroseconds != null) &&
                    (outerElapsedMicroseconds > (double)waitMicroseconds) &&
                    (outerElapsedMicroseconds > WaitTraceMinimumTime) ?
                        TracePriority.EventDebug2 : TracePriority.EventDebug3;

                TraceOps.DebugTrace(String.Format(
                    "Wait: {0} for interpreter {1}",
                    FormatOps.PerformanceMicroseconds(
                        outerElapsedMicroseconds),
                    FormatOps.InterpreterNoThrow(interpreter)),
                    typeof(EventOps).Name, priority);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Background Error Reporter
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
