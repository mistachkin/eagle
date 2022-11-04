/*
 * Engine.cs --
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
using System.Globalization;
using System.IO;

#if NETWORK
using System.Net;
#endif

using System.Reflection;
using System.Text;
using System.Threading;

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not add any non-static members to this class.  It is not allowed to maintain any
// kind of state information because all script state information is stored in the Interpreter
// object(s).
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

namespace Eagle._Components.Public
{
    [ObjectId("204a6f65-204d-6973-7461-63686b696e20")]
    public static class Engine /* unique */
    {
        #region Private Constants
        //
        // NOTE: The maximum length used when adding the original command text that
        //       caused the current script error to the interpreter error info.
        //
        private const int ErrorInfoCommandLength = 150;

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The maximum number of times to append to the errorInfo variable
        //       while unwinding the evaluation stack after a stack overflow.
        //
        private const int ErrorInfoStackOverflowFrames = 5;

        //
        // NOTE: The maximum level beyond which the errorInfo variable should not
        //       be appended to while unwinding the evaluation stack after a stack
        //       overflow.
        //
        private const int ErrorInfoStackOverflowLevels = 5;

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The flags used when the engine needs to set a script variable
        //       (typically to report extended error information).
        //
        private static readonly VariableFlags ErrorVariableFlags =
            VariableFlags.LibraryMask | VariableFlags.ViaEngine;

        //
        // NOTE: The flags used when the engine needs to set the "::errorCode"
        //       script variable.
        //
        internal static readonly VariableFlags ErrorCodeVariableFlags =
            ErrorVariableFlags |
#if FAST_ERRORCODE
            VariableFlags.FastTraceMask;
#else
            VariableFlags.None;
#endif

        //
        // NOTE: The flags used when the engine needs to set the "::errorInfo"
        //       script variable.
        //
        internal static readonly VariableFlags ErrorInfoVariableFlags =
            ErrorVariableFlags |
#if FAST_ERRORINFO
            VariableFlags.FastTraceMask;
#else
            VariableFlags.None;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default stack size for new threads when compiled or run
        //       on a Mono or Unix platform (or without native code support).
        //
        private const int DefaultStackSize = 0x100000; // 1MB

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the error result used as a last resort when there is
        //       no more memory available.
        //
        private static readonly Result OutOfMemoryException = typeof(Engine) +
            ".Critical.OutOfMemoryException";

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the error result used as a last resort when there is
        //       no more stack space available.
        //
        private static readonly Result StackOverflowException = typeof(Engine) +
            ".Critical.StackOverflowException";

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the error result used when its thread is interrupted
        //       via the Thread.Interrupt() method.
        //
        private static readonly Result ThreadInterruptedException = typeof(Engine) +
            ".Critical.ThreadInterruptedException";

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the error result used when its thread is aborted via
        //       the Thread.Abort() method.
        //
        private static readonly Result ThreadAbortException = typeof(Engine) +
            ".Critical.ThreadAbortException";

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the error message returned if the interpreter is
        //       somehow unusable (i.e. it may have been disposed, deleted,
        //       etc).
        //
        internal static readonly Result InterpreterUnusableError =
            "interpreter is unusable (it may have been disposed)";

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static readonly Result EvalCanceledError = "eval canceled";
        internal static readonly Result EvalUnwoundError = "eval unwound";

        internal static readonly Result EvalCanceledTimeoutError = "eval canceled due to timeout";
        internal static readonly Result EvalUnwoundTimeoutError = "eval unwound due to timeout";

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static readonly Result HaltedError = "halted";
        internal static readonly Result InterpreterHaltedError = "INTERPRETER HALTED";

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of bytes to attempt to read at once from
        //       after the "soft" end-of-file.
        //
        // HACK: This is purposely not read-only.
        //
        private static int ReadPostScriptBufferSize = 262144; /* 256K */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        #region Global Engine Lock
        //
        // NOTE: This is only used to protect global engine data that is not
        //       read-only.  The only data currently in this category is the
        //       global throw-on-disposed flag.
        //
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Global Throw-On-Disposed Flag
        //
        // NOTE: The default value here should always be "true", use the
        //       "NoThrowOnDisposed" environment variable to override.
        //
        private static bool ThrowOnDisposed = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Global Extra (Reserved) Stack Space
#if NATIVE
        private static ulong ExtraStackSpace = 0;
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Engine Flags Methods
        internal static EngineFlags CombineFlagsWithMasks(
            EngineFlags oldFlags,
            EngineFlags newFlags,
            EngineFlags addMask,
            EngineFlags removeMask
            )
        {
            //
            // NOTE: What flags were added (between the old and new flags)?
            //
            EngineFlags addedFlags = ~oldFlags & newFlags;

            //
            // NOTE: What flags were removed (between the old and new flags)?
            //
            EngineFlags removedFlags = oldFlags & ~newFlags;

            //
            // NOTE: For the flags that were added, just mask off the ones
            //       that are not permitted.
            //
            addedFlags &= addMask;

            //
            // NOTE: For the flags that were removed, mask off the ones that
            //       are not permitted.
            //
            removedFlags &= removeMask;

            //
            // NOTE: Start with the old flags.
            //
            EngineFlags result = oldFlags;

            //
            // NOTE: Add flags that were added -AND- that were permitted to
            //       be added.
            //
            result |= addedFlags;

            //
            // NOTE: Remove flags that were removed -AND- that were permitted
            //       to be removed.
            //
            result &= ~removedFlags;

            //
            // NOTE: Return the final resulting flags.
            //
            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static CancelFlags GetCancelFlags(
            EngineFlags engineFlags
            )
        {
            CancelFlags cancelFlags = CancelFlags.Default;

            if (EngineFlagOps.HasResetCancel(engineFlags))
                cancelFlags |= CancelFlags.IgnorePending;

            return cancelFlags | CancelFlags.Engine;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static EngineFlags AddStackCheckFlags(
            ref EngineFlags engineFlags
            )
        {
            //
            // NOTE: The stack checking flags must be temporarily set in order
            //       to avoid native stack overflows in the case of deeply
            //       nested execution that does not go through the evaluator.
            //
            EngineFlags savedEngineFlags = engineFlags;
            engineFlags |= EngineFlags.FullStackMask;

            return savedEngineFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void RemoveStackCheckFlags(
            EngineFlags savedEngineFlags,
            ref EngineFlags engineFlags
            )
        {
            //
            // NOTE: Check the individual engine flags that we are responsible
            //       for.  Remove them from the current engine flags if they
            //       were not previously set (i.e. restore the engine flags to
            //       their previous state).
            //
            if (!EngineFlagOps.HasCheckStack(savedEngineFlags))
                engineFlags &= ~EngineFlags.CheckStack;

            if (!EngineFlagOps.HasForceStack(savedEngineFlags))
                engineFlags &= ~EngineFlags.ForceStack;

            if (!EngineFlagOps.HasForcePoolStack(savedEngineFlags))
                engineFlags &= ~EngineFlags.ForcePoolStack;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static EngineFlags GetResolveFlags(
            EngineFlags engineFlags,
            bool exact
            )
        {
            EngineFlags result = engineFlags;

            if (!EngineFlagOps.HasUseIExecutes(result) &&
                !EngineFlagOps.HasUseCommands(result) &&
                !EngineFlagOps.HasUseProcedures(result))
            {
                //
                // NOTE: If none of these flags are set, use the default
                //       (i.e. set them all).
                //
                result |= EngineFlags.UseAllMask;
            }

            if (exact)
                result |= EngineFlags.ExactMatch;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReadyFlags GetReadyFlags(
            EngineFlags engineFlags
            )
        {
            ReadyFlags readyFlags = ReadyFlags.None;

            if (EngineFlagOps.HasCheckStack(engineFlags))
                readyFlags |= ReadyFlags.CheckStack;

            if (EngineFlagOps.HasForceStack(engineFlags))
                readyFlags |= ReadyFlags.ForceStack;

            if (EngineFlagOps.HasForcePoolStack(engineFlags))
                readyFlags |= ReadyFlags.ForcePoolStack;

#if false
            if (EngineFlagOps.HasNoReady(engineFlags))
                readyFlags |= ReadyFlags.Disabled;
#endif

            if (EngineFlagOps.HasNoCancel(engineFlags))
                readyFlags |= ReadyFlags.NoCancel;

            if (EngineFlagOps.HasNoGlobalCancel(engineFlags))
                readyFlags |= ReadyFlags.NoGlobalCancel;

            return readyFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static EngineFlags CombineFlags(
            Interpreter interpreter,
            EngineFlags engineFlags,
            bool checkStack,
            bool errorMask
            )
        {
            EngineFlags result = engineFlags;

            //
            // NOTE: Make sure that we honor any flags set in the interpreter,
            //       if available, in addition to the ones passed by the caller.
            //
            if (interpreter != null)
                result |= interpreter.EngineFlags;

            //
            // BUGFIX: If requested, make sure the native stack space checking
            //         flag is set from within the engine itself.
            //
            if (checkStack)
                result |= EngineFlags.BaseStackMask;

            //
            // BUGFIX: Make sure the error handling flags are not copied.  The
            //         error handling flags will be removed from the interpreter
            //         itself by the ResetResult method; however, that method
            //         will not affect the flags in this local variable.
            //
            if (errorMask)
                result &= ~EngineFlags.ErrorMask;

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Feature Support Methods
        #region Throw-On-Disposed Support Methods
        public static bool IsThrowOnDisposed(
            Interpreter interpreter,
            bool? all
            )
        {
            CreateFlags createFlags = CreateFlags.None;

            //
            // BUGFIX: Avoid ever taking the interpreter lock while in this
            //         method, as we have no idea under what conditions it
            //         could be [legitimately] called.
            //
            if (interpreter != null)
                createFlags = interpreter.CreateFlagsNoLock;

            lock (syncRoot) /* ENGINE-LOCK */
            {
                bool newAll;

                if (all != null)
                    newAll = (bool)all;
                else if (interpreter != null)
                    newAll = true;
                else
                    newAll = false;

                ///////////////////////////////////////////////////////////////

                if (newAll)
                {
                    return ThrowOnDisposed && FlagOps.HasFlags(
                        createFlags, CreateFlags.ThrowOnDisposed, true);
                }
                else
                {
                    return ThrowOnDisposed || FlagOps.HasFlags(
                        createFlags, CreateFlags.ThrowOnDisposed, true);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static void SetThrowOnDisposed(
            Interpreter interpreter,
            bool throwOnDisposed,
            bool all
            )
        {
            if (interpreter != null)
            {
                if (throwOnDisposed)
                    interpreter.CreateFlags |= CreateFlags.ThrowOnDisposed;
                else
                    interpreter.CreateFlags &= ~CreateFlags.ThrowOnDisposed;
            }

            if (all || (interpreter == null))
            {
                lock (syncRoot) /* ENGINE-LOCK */
                {
                    ThrowOnDisposed = throwOnDisposed;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Stack Space Methods
        private static int GetNewThreadStackSize(
            Interpreter interpreter,
            int maxStackSize
            )
        {
            if (maxStackSize != 0)
                return maxStackSize;

            if (interpreter != null)
            {
                maxStackSize = interpreter.InternalThreadStackSize;

                if (maxStackSize != 0)
                    return maxStackSize;
            }

            ///////////////////////////////////////////////////////////////////

            bool isMono = CommonOps.Runtime.IsMono();

#if NATIVE
            if (!isMono)
            {
                //
                // NOTE: When running under the .NET Framework on Windows, use
                //       the native stack checking code to determine the proper
                //       stack size for new threads; otherwise, just use one of
                //       our "fail-safe" defaults.
                //
                return ConversionOps.ToInt(
                    NativeStack.GetNewThreadNativeStackSize());
            }
            else
#endif
            {
                //
                // HACK: *MONO* Use the process-wide default.  Apparently, if
                //       we are running on Mono we must use a non-zero stack
                //       size or thread creation will fail.
                //
                return isMono ? DefaultStackSize : 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        internal static ulong GetExtraStackSpace()
        {
            lock (syncRoot)
            {
                return ExtraStackSpace;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void SetExtraStackSpace(
            ulong extraSpace
            )
        {
            lock (syncRoot)
            {
                ExtraStackSpace = extraSpace;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Threading Support Methods
        #region Thread Creation Methods
        public static Thread CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return CreateThread(
                null, start, maxStackSize, userInterface, isBackground,
                useActiveStack);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static Thread CreateThread(
            Interpreter interpreter,
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Thread thread = null;

            EngineThread engineThread = EngineThread.Create(
                interpreter, start, useActiveStack);

            if (engineThread == null)
                return thread;

#if NOTIFY
            EngineFlags engineFlags = EngineFlags.None;
#endif
            IThreadHost threadHost = null;

            if (interpreter != null)
            {
#if NOTIFY
                engineFlags = interpreter.EngineFlagsNoLock;
#endif

                threadHost = GetThreadHost(interpreter);
            }

            try
            {
                maxStackSize = GetNewThreadStackSize(interpreter, maxStackSize);

                if ((threadHost != null) &&
                    FlagOps.HasFlags(threadHost.GetHostFlags(), HostFlags.Thread, true))
                {
                    ReturnCode code;
                    Result error = null;

                    code = threadHost.CreateThread(
                        engineThread.ThreadStart, maxStackSize, userInterface,
                        isBackground, useActiveStack, ref thread, ref error);

                    engineThread.SetThread(thread);

                    if (code != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, code, error);
                }
                else
                {
                    //
                    // NOTE: It is highly recommended that external users of the script engine
                    //       should use at LEAST this value when creating threads that will be
                    //       using this class (the script engine).
                    //
                    thread = new Thread(
                        engineThread.ThreadStart, maxStackSize);

                    engineThread.SetThread(thread);

                    if (userInterface)
                        thread.SetApartmentState(ApartmentState.STA);

                    if (thread.IsBackground != isBackground)
                        thread.IsBackground = isBackground;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectList(start, engineThread, maxStackSize,
                        userInterface, isBackground, thread), interpreter,
                        null, null, e);
                }
#endif
            }

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static Thread CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return CreateThread(
                null, start, maxStackSize, userInterface, isBackground,
                useActiveStack);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static Thread CreateThread(
            Interpreter interpreter,
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Thread thread = null;

            EngineThread engineThread = EngineThread.Create(
                interpreter, start, useActiveStack);

            if (engineThread == null)
                return thread;

#if NOTIFY
            EngineFlags engineFlags = EngineFlags.None;
#endif
            IThreadHost threadHost = null;

            if (interpreter != null)
            {
#if NOTIFY
                engineFlags = interpreter.EngineFlagsNoLock;
#endif

                threadHost = GetThreadHost(interpreter);
            }

            try
            {
                maxStackSize = GetNewThreadStackSize(interpreter, maxStackSize);

                if ((threadHost != null) &&
                    FlagOps.HasFlags(threadHost.GetHostFlags(), HostFlags.Thread, true))
                {
                    ReturnCode code;
                    Result error = null;

                    code = threadHost.CreateThread(
                        engineThread.ParameterizedThreadStart, maxStackSize,
                        userInterface, isBackground, useActiveStack, ref thread,
                        ref error);

                    engineThread.SetThread(thread);

                    if (code != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, code, error);
                }
                else
                {
                    //
                    // NOTE: It is highly recommended that external users of the script engine
                    //       should use at LEAST this value when creating threads that will be
                    //       using this class (the script engine).
                    //
                    thread = new Thread(
                        engineThread.ParameterizedThreadStart, maxStackSize);

                    engineThread.SetThread(thread);

                    if (userInterface)
                        thread.SetApartmentState(ApartmentState.STA);

                    if (thread.IsBackground != isBackground)
                        thread.IsBackground = isBackground;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectList(start, engineThread, maxStackSize,
                        userInterface, isBackground, thread), interpreter,
                        null, null, e);
                }
#endif
            }

            return thread;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Thread Queue Methods
        internal static bool QueueWorkItem(
            WaitCallback callBack,
            object state
            ) /* throw */
        {
            return ThreadOps.QueueUserWorkItem(callBack, state); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool QueueWorkItem(
            Interpreter interpreter,
            ParameterizedThreadStart start,
            object obj
            ) /* throw */
        {
#if NOTIFY
            EngineFlags engineFlags = EngineFlags.None;
#endif
            IThreadHost threadHost = null;

            if (interpreter != null)
            {
#if NOTIFY
                engineFlags = interpreter.EngineFlagsNoLock;
#endif

                threadHost = GetThreadHost(interpreter);
            }

            try
            {
                if ((threadHost != null) &&
                    FlagOps.HasFlags(threadHost.GetHostFlags(), HostFlags.WorkItem, true))
                {
                    ReturnCode code;
                    Result error = null;

                    code = threadHost.QueueWorkItem(new WaitCallback(start), obj, ref error);

                    if (code == ReturnCode.Ok)
                        return true;
                    else
                        DebugOps.Complain(interpreter, code, error);
                }
                else
                {
                    if (QueueWorkItem(new WaitCallback(start), obj)) /* throw */
                        return true;
                    else
                        DebugOps.Complain(interpreter,
                            ReturnCode.Error, "could not queue work item");
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectPair(start, obj), interpreter,
                        null, null, e);
                }
#endif
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Private Thread Support Methods
        private static IThreadHost GetThreadHost(
            Interpreter interpreter
            )
        {
            if ((interpreter != null) && AppDomainOps.IsSame(interpreter))
            {
                IThreadHost threadHost = interpreter.InternalHost;

                if (!AppDomainOps.IsTransparentProxy(threadHost))
                    return threadHost;
            }

            return null;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Asynchronous Support Methods
        private static void EngineThreadStart(
            object obj
            ) /* System.Threading.ParameterizedThreadStart */
        {
#if NATIVE
            RuntimeOps.RefreshNativeStackPointers(true);
#endif

            IAsynchronousContext context = obj as IAsynchronousContext;

            if (context != null)
            {
                Interpreter interpreter = context.Interpreter;
                EngineMode engineMode = context.EngineMode;
                string text = context.Text;
                EngineFlags engineFlags = context.EngineFlags;
                SubstitutionFlags substitutionFlags = context.SubstitutionFlags;
                EventFlags eventFlags = context.EventFlags;
                ExpressionFlags expressionFlags = context.ExpressionFlags;
                AsynchronousCallback callback = context.Callback;

                bool bgError = !FlagOps.HasFlags(
                    eventFlags, EventFlags.NoBgError, true);

                bool disposeThread = FlagOps.HasFlags(
                    eventFlags, EventFlags.DisposeThread, true);

                try
                {
                    ReturnCode code;
                    Result result = null;
                    int errorLine = 0;

                    switch (engineMode)
                    {
                        case EngineMode.None:
                            {
                                //
                                // NOTE: Do nothing.
                                //
                                code = ReturnCode.Ok;

                                break;
                            }
                        case EngineMode.EvaluateExpression:
                            {
                                code = EvaluateExpression(
                                    interpreter, text, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref result);

                                break;
                            }
                        case EngineMode.EvaluateScript:
                            {
                                code = EvaluateScript(
                                    interpreter, text, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref result, ref errorLine);

                                break;
                            }
                        case EngineMode.EvaluateFile:
                            {
                                code = EvaluateFile(
                                    interpreter, text, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref result, ref errorLine);

                                break;
                            }
                        case EngineMode.SubstituteString:
                            {
                                code = SubstituteString(
                                    interpreter, text, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref result);

                                break;
                            }
                        case EngineMode.SubstituteFile:
                            {
                                code = SubstituteFile(
                                    interpreter, text, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref result);

                                break;
                            }
                        default:
                            {
                                result = String.Format(
                                    "invalid engine mode {0}",
                                    engineMode);

                                code = ReturnCode.Error;
                                break;
                            }
                    }

                    if (callback != null)
                    {
                        //
                        // NOTE: Modify the context to include the result of
                        //       the script evaluation.
                        //
                        context.SetResult(code, result, errorLine);

                        //
                        // NOTE: Notify the callback that the script has
                        //       completed.  We do not care at this point if
                        //       the script succeeded or generated an error.
                        //       The callback should take whatever action is
                        //       appropriate based on the result contained in
                        //       the context.
                        //
                        callback(context); /* throw */
                    }
                    else if (code == ReturnCode.Error)
                    {
                        //
                        // NOTE: The script generated an error and no callback
                        //       was specified; therefore, attempt to handle
                        //       this as a background error.
                        //
                        if (bgError && EventOps.HandleBackgroundError(
                                interpreter, code, result) != ReturnCode.Ok)
                        {
                            //
                            // NOTE: For some reason, that failed; therefore,
                            //       just complain about it to the interpreter
                            //       host.
                            //
                            DebugOps.Complain(interpreter, code, result);
                        }
                    }
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();

                    TraceOps.DebugTrace(
                        e, typeof(Engine).Name,
                        TracePriority.ThreadError2);
                }
                catch (ThreadInterruptedException e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Engine).Name,
                        TracePriority.ThreadError2);
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Nothing we can do here except log the failure.
                    //
                    TraceOps.DebugTrace(
                        e, typeof(Engine).Name,
                        TracePriority.ThreadError);

#if NOTIFY
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            new ObjectPair(obj, context), interpreter,
                            null, null, e);
                    }
#endif
                }
                finally
                {
                    if (disposeThread && (interpreter != null))
                    {
                        /* IGNORED */
                        interpreter.MaybeDisposeThread();
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Callback Queue Support Methods
#if CALLBACK_QUEUE
        private static string GetCommandName(
            StringList arguments
            )
        {
            return ((arguments != null) && (arguments.Count > 0)) ? arguments[0] : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ExecuteCallbackQueue(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref Result result
            )
        {
            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            return ExecuteCallbackQueue(
                interpreter, engineFlags, substitutionFlags, eventFlags,
                expressionFlags,
#if RESULT_LIMITS
                executeResultLimit,
#endif
                ref usable, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteCallbackQueue(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to dequeue all the previously queued callbacks
            //       and process them, in order, until we either encounter
            //       an error or we run out of callbacks.  This method now
            //       has snapshot semantics; any callbacks queued during
            //       process will not be executed until the next time this
            //       method is called.
            //
            ReturnCode code;
            CommandCallback[] callbacks = null;

            code = interpreter.DequeueAllCallbacks(ref callbacks, ref result);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: If the dequeue operation was successful and there are no
            //       callbacks, just skip everything else, returning success.
            //
            if (callbacks == null)
                return code;

            int nextIndex = Index.Invalid; /* First callback to re-enqueue. */
            int length = callbacks.Length; /* Total callbacks. */

            for (int index = 0; index < length; index++)
            {
                CommandCallback callback = callbacks[index];

                //
                // NOTE: Make sure the callback is valid; otherwise,
                //       just skip it.
                //
                if (callback == null)
                    continue;

                //
                // NOTE: Grab the arguments for the callback so that
                //       we can extract the command name.  The other
                //       arguments needed (if any) are already part
                //       of the command callback object itself.
                //
                string name = GetCommandName(callback.Arguments);

                //
                // NOTE: Execute the callback.  If we encounter an
                //       error, the loop will bail out.
                //
                code = Execute(
                    name, callback, interpreter,
                    GetClientData(
                        interpreter, null, false),
                    null, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit,
#endif
                    ref usable, ref result);

                //
                // NOTE: We need to bail out if there is an error
                //       -OR- the interpreter is no longer usable.
                //
                if ((code != ReturnCode.Ok) || !usable)
                {
                    //
                    // NOTE: Save the index of the next callback
                    //       that would have been executed, if any.
                    //       This will be used to re-enqueue the
                    //       callbacks to the callback queue that
                    //       we did not even attempt to execute.
                    //
                    if ((index + 1) < length)
                        nextIndex = index + 1;

                    break;
                }
            }

            //
            // NOTE: Are there any callbacks left that need to be
            //       [re-]enqueued to the interpreter?  This cannot
            //       be done if the interpreter is no longer usable
            //       (i.e. disposed).  However, that doesn't really
            //       matter because the remaining callbacks would
            //       never be executed anyway.
            //
            if (usable && (nextIndex != Index.Invalid))
            {
                ReturnCode enqueueCode;
                Result enqueueError = null;

                enqueueCode = interpreter.EnqueueSomeCallbacks(
                    nextIndex, ref callbacks, ref enqueueError);

                if (enqueueCode != ReturnCode.Ok)
                {
                    DebugOps.Complain(
                        interpreter, enqueueCode, enqueueError);
                }
            }

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Debugging Support Methods
#if DEBUGGER
#if DEBUGGER_ARGUMENTS
        #region Debugger Notification Methods
        internal static ArgumentList GetDebuggerExecuteArguments(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return null;

                IDebugger debugger = interpreter.Debugger;

                if (debugger == null)
                    return null;

                return debugger.ExecuteArguments;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used in the critical path within the script
        //          evaluation engine and must be as simple as possible.
        //
        private static void SetDebuggerExecuteArguments(
            Interpreter interpreter,
            ArgumentList arguments
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                IDebugger debugger = interpreter.Debugger;

                if (debugger == null)
                    return;

                debugger.ExecuteArguments = arguments;
            }
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Debugger Checking Methods
        private static void CheckIsDebuggerExiting(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                interpreter.MaybeResetIsDebuggerExiting();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebuggerInterpreter(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref Interpreter debugInterpreter,
            ref Result error
            )
        {
            IDebugger debugger = null;

            return CheckDebugger(interpreter, ignoreEnabled,
                ref debugger, ref debugInterpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled
            )
        {
            IDebugger debugger = null;
            bool enabled = false;
            HeaderFlags headerFlags = HeaderFlags.None;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref headerFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref Interpreter debugInterpreter,
            ref Result error
            )
        {
            bool enabled = false;
            HeaderFlags headerFlags = HeaderFlags.None;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref headerFlags, ref debugInterpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref bool enabled,
            ref HeaderFlags headerFlags,
            ref Interpreter debugInterpreter
            )
        {
            Result error = null;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref headerFlags, ref debugInterpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref bool enabled,
            ref HeaderFlags headerFlags,
            ref Interpreter debugInterpreter,
            ref Result error
            )
        {
            if (CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                    ref enabled, ref headerFlags, ref error))
            {
                debugInterpreter = debugger.Interpreter;

                if (debugInterpreter != null)
                    return true;
                else
                    error = "debugger interpreter not available";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref Result error
            )
        {
            bool enabled = false;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref bool enabled,
            ref Result error
            )
        {
            HeaderFlags headerFlags = HeaderFlags.None;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref headerFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref HeaderFlags headerFlags
            )
        {
            bool enabled = false;

            return CheckDebugger(interpreter, ignoreEnabled, ref debugger,
                ref enabled, ref headerFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref bool enabled,
            ref HeaderFlags headerFlags
            )
        {
            Result error = null;

            return CheckDebugger(interpreter, ignoreEnabled,
                ref debugger, ref enabled, ref headerFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool CheckDebugger(
            Interpreter interpreter,
            bool ignoreEnabled,
            ref IDebugger debugger,
            ref bool enabled,
            ref HeaderFlags headerFlags,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter, ref error))
                    return false;

                if (interpreter.GlobalHalt)
                {
                    error = "halted";
                    return false;
                }

                debugger = interpreter.Debugger;
                headerFlags = interpreter.HeaderFlags;

                if (debugger == null)
                {
                    error = "debugger not available";
                    return false;
                }

                enabled = debugger.Enabled;

                if (ignoreEnabled || enabled)
                {
                    return true;
                }
                else
                {
                    error = "debugger not enabled";
                    return false;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Debugger Setup Methods
        internal static bool SetupDebugger(
            Interpreter interpreter,
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList,
            bool ignoreModifiable,
            bool setup,
            bool isolated,
            ref Result result
            )
        {
            IDebugger debugger = null;
            bool modified = false;

            return SetupDebugger(
                interpreter, culture, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                pluginFlags, appDomain, host, libraryPath,
                autoPathList, ignoreModifiable, setup, isolated,
                ref debugger, ref modified, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetupDebugger(
            Interpreter interpreter,
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList,
            bool ignoreModifiable,
            bool setup,
            bool isolated,
            ref IDebugger debugger,
            ref bool modified,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return false;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter, ref result))
                    return false;

                //
                // NOTE: We intend to modify the interpreter state, make
                //       sure this is not forbidden.
                //
                if (!ignoreModifiable &&
                    !interpreter.IsModifiable(false, ref result))
                {
                    return false;
                }

                debugger = interpreter.Debugger;

                if (setup)
                {
                    if (debugger == null)
                    {
                        //
                        // NOTE: Create a new debugger using the
                        //       creation arguments provided by
                        //       the caller.
                        //
                        debugger = DebuggerOps.Create(
                            isolated, culture, createFlags, hostCreateFlags,
                            initializeFlags, scriptFlags, interpreterFlags,
                            pluginFlags, appDomain, host, libraryPath,
                            autoPathList);

                        //
                        // NOTE: Now, initialize the debugger field
                        //       for the interpreter.
                        //
                        interpreter.Debugger = debugger; modified = true;
                    }

                    if (isolated)
                    {
                        Interpreter debugInterpreter = debugger.Interpreter;

                        if (debugInterpreter == null)
                        {
                            debugInterpreter = DebuggerOps.CreateInterpreter(
                                culture, createFlags, hostCreateFlags,
                                initializeFlags, scriptFlags, interpreterFlags,
                                pluginFlags, appDomain, host, libraryPath,
                                autoPathList, ref result);

                            if (debugInterpreter == null)
                                return false;

                            debugger.Interpreter = debugInterpreter;
                        }
                    }
                }
                else if (debugger != null)
                {
                    Interpreter debugInterpreter = debugger.Interpreter;

                    if (debugInterpreter != null)
                    {
                        debugInterpreter.Dispose();
                        debugInterpreter = null;

                        debugger.Interpreter = null;
                    }

                    IDisposable disposable = debugger as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                        disposable = null;
                    }

                    debugger = null;

                    //
                    // NOTE: Finally, clear out the debugger field
                    //       for the interpreter.
                    //
                    interpreter.Debugger = null; modified = true;
                }

                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Breakpoint Support Methods
        #region Generic Execute Breakpoint Methods
        private static bool HasAnyBreakpoint(
            IExecute execute,
            IExecuteArgument executeArgument
            )
        {
            return HasExecuteBreakpoint(execute) ||
                HasExecuteArgumentBreakpoint(executeArgument);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool HasExecuteBreakpoint(
            IExecute execute
            )
        {
            if (execute != null)
            {
                IProcedure procedure = execute as IProcedure;

                if (procedure != null)
                    return EntityOps.HasBreakpoint(procedure);

                ICommand command = execute as ICommand;

                if (command != null)
                    return EntityOps.HasBreakpoint(command);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool HasExecuteArgumentBreakpoint(
            IExecuteArgument executeArgument
            )
        {
            if (executeArgument != null)
            {
                IFunction function = executeArgument as IFunction;

                if (function != null)
                    return EntityOps.HasBreakpoint(function);

                IOperator @operator = executeArgument as IOperator;

                if (@operator != null)
                    return EntityOps.HasBreakpoint(@operator);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetExecuteBreakpoint(
            IExecute execute,
            bool enable,
            ref Result error
            )
        {
            if (execute != null)
            {
                IProcedure procedure = execute as IProcedure;

                if (procedure != null)
                {
                    bool enabled = EntityOps.HasBreakpoint(procedure);

                    if (enable != enabled)
                    {
                        /* IGNORED */
                        EntityOps.SetBreakpoint(procedure, enable);

                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "procedure {0} breakpoint is already {1}",
                            FormatOps.DisplayName(EntityOps.GetNameNoThrow(
                            procedure)), enable ? "set" : "unset");
                    }

                    return false;
                }

                ICommand command = execute as ICommand;

                if (command != null)
                {
                    bool enabled = EntityOps.HasBreakpoint(command);

                    if (enable != enabled)
                    {
                        /* IGNORED */
                        EntityOps.SetBreakpoint(command, enable);

                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "command {0} breakpoint is already {1}",
                            FormatOps.DisplayName(EntityOps.GetNameNoThrow(
                            command)), enable ? "set" : "unset");
                    }

                    return false;
                }

                error = "not a command or procedure";
            }
            else
            {
                error = "invalid execute";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetExecuteArgumentBreakpoint(
            IExecuteArgument executeArgument,
            bool enable,
            ref Result error
            )
        {
            if (executeArgument != null)
            {
                IFunction function = executeArgument as IFunction;

                if (function != null)
                {
                    bool enabled = EntityOps.HasBreakpoint(function);

                    if (enable != enabled)
                    {
                        /* IGNORED */
                        EntityOps.SetBreakpoint(function, enable);

                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "function {0} breakpoint is already {1}",
                            FormatOps.DisplayName(EntityOps.GetNameNoThrow(
                            function)), enable ? "set" : "unset");
                    }

                    return false;
                }

                IOperator @operator = executeArgument as IOperator;

                if (@operator != null)
                {
                    bool enabled = EntityOps.HasBreakpoint(@operator);

                    if (enable != enabled)
                    {
                        /* IGNORED */
                        EntityOps.SetBreakpoint(@operator, enable);

                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "operator {0} breakpoint is already {1}",
                            FormatOps.DisplayName(EntityOps.GetNameNoThrow(
                            @operator)), enable ? "set" : "unset");
                    }

                    return false;
                }

                error = "not a function or operator";
            }
            else
            {
                error = "invalid execute argument";
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Token Breakpoint Methods
#if DEBUGGER_BREAKPOINTS
        private static bool HasTokenBreakpoint(
            Interpreter interpreter,
            IDebugger debugger,
            IToken token
            )
        {
            if (token != null)
            {
                if (FlagOps.HasFlags(token.Flags, TokenFlags.Breakpoint, true))
                {
                    return true;
                }
                else
                {
                    bool match = false;

                    if (debugger.MatchBreakpoint(
                            interpreter, token, ref match) == ReturnCode.Ok)
                    {
                        return match;
                    }
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Test Breakpoint Methods (for [test1] and [test2])
        private static bool HasTestBreakpoint(
            Interpreter interpreter,
            string name
            )
        {
            if (interpreter != null)
                return interpreter.HasTestBreakpoint(name);

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Breakpoint Methods
        internal static ReturnCode CheckBreakpoints(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            IToken token,
            ITraceInfo traceInfo,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IExecute execute,
            IExecuteArgument executeArgument,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IDebugger debugger = null;
            HeaderFlags headerFlags = HeaderFlags.None;
            bool breakpoint = false;

            if (CheckDebugger(interpreter, false, ref debugger, ref headerFlags))
            {
                if (FlagOps.HasFlags(debugger.Types, breakpointType, true))
                {
                    BreakpointType newBreakpointType = BreakpointType.None;

                    if (debugger.SingleStep)
                        newBreakpointType |= BreakpointType.SingleStep;

                    if (debugger.MaybeNextStep())
                        newBreakpointType |= BreakpointType.MultipleStep;

                    if (debugger.BreakOnExecute &&
                        HasAnyBreakpoint(execute, executeArgument))
                    {
                        newBreakpointType |= BreakpointType.Identifier;
                    }

                    if ((FlagOps.HasFlags(
                            breakpointType, BreakpointType.Cancel, true) ||
                        FlagOps.HasFlags(
                            breakpointType, BreakpointType.Unwind, true)) &&
                        debugger.BreakOnCancel)
                    {
                        //
                        // NOTE: This will be "Cancel" and possibly "Unwind".
                        //
                        newBreakpointType |= breakpointType;
                    }

                    if (FlagOps.HasFlags(
                            breakpointType, BreakpointType.Exit, true) &&
                        debugger.BreakOnExit)
                    {
                        //
                        // NOTE: This will be "Exit" and either "Evaluate" or
                        //       "Substitute".
                        //
                        newBreakpointType |= breakpointType;
                    }

                    if ((code == ReturnCode.Error) && debugger.BreakOnError)
                        newBreakpointType |= BreakpointType.Error;

                    if ((code == ReturnCode.Return) && debugger.BreakOnReturn)
                        newBreakpointType |= BreakpointType.Return;

                    if (debugger.BreakOnTest &&
                        HasTestBreakpoint(interpreter, breakpointName))
                    {
                        newBreakpointType |= BreakpointType.Test;
                    }

#if DEBUGGER_BREAKPOINTS
                    if (debugger.BreakOnToken &&
                        HasTokenBreakpoint(interpreter, debugger, token))
                    {
                        newBreakpointType |= BreakpointType.Token;
                    }
#endif

                    //
                    // NOTE: Did we meet at least one criteria for a
                    //       breakpoint at this point in the script?
                    //
                    if (newBreakpointType != BreakpointType.None)
                    {
                        breakpointType |= newBreakpointType;
                        breakpoint = true;
                    }
                }
            }

            if (breakpoint)
            {
#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Debugger, NotifyFlags.PreBreakpoint,
                        new ObjectList(code, breakpointType, breakpointName,
                        token, traceInfo, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, execute, executeArgument),
                        interpreter, clientData, arguments, null, ref result);
                }
#endif

                //
                // BUGFIX: Do not show full debugger info for a simple breakpoint (unless
                //         the default header display flags have been overridden by the
                //         user).
                //
                if (FlagOps.HasFlags(headerFlags, HeaderFlags.User, true))
                    headerFlags |= HeaderFlags.Breakpoint;
                else
                    headerFlags = HeaderFlags.Breakpoint;

                code = DebuggerOps.Breakpoint(
                    debugger, interpreter, new InteractiveLoopData(code, breakpointType,
                    breakpointName, token, traceInfo, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, headerFlags, clientData, arguments),
                    ref result);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Debugger, NotifyFlags.Breakpoint,
                        new ObjectList(code, breakpointType, breakpointName,
                        token, traceInfo, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, execute, executeArgument),
                        interpreter, clientData, arguments, null, ref result);
                }
#endif
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Watchpoint Methods
#if DEBUGGER_VARIABLE
        internal static ReturnCode CheckWatchpoints(
            ReturnCode code,
            BreakpointType breakpointType,
            string breakpointName,
            IToken token,
            ITraceInfo traceInfo,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            Interpreter interpreter,
            ref Result result
            )
        {
            IDebugger debugger = null;
            HeaderFlags headerFlags = HeaderFlags.None;
            bool watchpoint = false;

            if (CheckDebugger(interpreter, false, ref debugger, ref headerFlags))
            {
                if (FlagOps.HasFlags(debugger.Types, breakpointType, true))
                {
                    watchpoint = true;
                }
            }

            if (watchpoint)
            {
                //
                // BUGFIX: Do not show full debugger info for a variable watch (unless
                //         the default header display flags have been overridden by the
                //         user).
                //
                if (FlagOps.HasFlags(headerFlags, HeaderFlags.User, true))
                    headerFlags |= HeaderFlags.Watchpoint;
                else
                    headerFlags = HeaderFlags.Watchpoint;

                if (interpreter != null)
                    /* IGNORED */
                    interpreter.EnterWatchpointLevel();

                try
                {
#if NOTIFY
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Debugger, NotifyFlags.PreWatchpoint,
                            new ObjectList(code, breakpointType, breakpointName,
                            token, traceInfo, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags), interpreter, null,
                            null, null, ref result);
                    }
#endif

                    code = DebuggerOps.Watchpoint(
                        debugger, interpreter, new InteractiveLoopData(code, breakpointType,
                        breakpointName, token, traceInfo, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, headerFlags), ref result);

#if NOTIFY
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Debugger, NotifyFlags.Watchpoint,
                            new ObjectList(code, breakpointType, breakpointName,
                            token, traceInfo, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags), interpreter, null,
                            null, null, ref result);
                    }
#endif
                }
                finally
                {
                    if (interpreter != null)
                        /* IGNORED */
                        interpreter.ExitWatchpointLevel();
                }
            }

            return code;
        }
#endif
        #endregion
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Event Processing Support Methods
        internal static ReturnCode CheckEvents( /* NON-ENGINE USE ONLY */
            Interpreter interpreter,
            EventFlags eventFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            return CheckEvents(
                interpreter, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode CheckEvents(
            Interpreter interpreter,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, /* NOT USED */
            EventFlags eventFlags,
            ExpressionFlags expressionFlags, /* NOT USED */
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code = ReturnCode.Ok;

            if (!EngineFlagOps.HasNoReady(engineFlags))
            {
                //
                // NOTE: Check if the interpreter is still valid and ready for use.
                //
                code = Interpreter.EngineReady(
                    interpreter, GetReadyFlags(engineFlags), ref result);

                if (code != ReturnCode.Ok)
                    return code;
            }

            //
            // NOTE: Skip event processing if events have been disabled.
            //
            if (!EngineFlagOps.HasNoEvent(engineFlags))
            {
                //
                // NOTE: Process any pending asynchronous events.  This could cause
                //       almost anything to happen (including script evaluation).
                //
                code = EventOps.ProcessEvents(
                    interpreter, eventFlags, EventPriority.CheckEvents, null, 0,
                    true, false, ref result);

                if (code != ReturnCode.Ok)
                    return code;

                if (!EngineFlagOps.HasNoReady(engineFlags))
                {
                    //
                    // NOTE: Now, re-check if the interpreter is still valid and
                    //       ready for use because the asynchronous events, if any,
                    //       could have invalidated the interpreter in some way.
                    //
                    code = Interpreter.EngineReady(
                        interpreter, GetReadyFlags(engineFlags), ref result);

                    if (code != ReturnCode.Ok)
                        return code;
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Status Methods
        #region Interpreter "Usability" Methods
        internal static bool IsUsableNoLock(
            Interpreter interpreter
            )
        {
            Result error = null;

            return IsUsableNoLock(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUsableNoLock(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (interpreter.Disposed)
            {
                error = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if PROFILER
        private static bool IsUsableNoLock(
            IProfilerState profiler,
            bool usable
            )
        {
            if (!usable)
                return false;

            if (profiler == null)
                return false;

            if (profiler.Disposed)
                return false;

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Deletion Methods
        public static ReturnCode IsDeleted(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;

            bool fireCallback = true;
            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    code = interpreter.InternalDeleted ?
                        ReturnCode.Error : ReturnCode.Ok;

                    if (code == ReturnCode.Error)
                    {
                        bool needResult = FlagOps.HasFlags(
                            cancelFlags, CancelFlags.NeedResult, true);

                        if (needResult)
                            result = "attempt to call eval in deleted interpreter";
                    }
                }
                else
                {
                    fireCallback = false; /* TODO: Nothing was done? */

                    result = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

            if (fireCallback && (code == ReturnCode.Error))
            {
                bool waitForLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.WaitForLock, true);

                ReturnCode fireCode;
                Result fireError = null;

                fireCode = interpreter.FireInterruptCallback(
                    InterruptType.Deleted, ClientData.Empty,
                    waitForLock, ref fireError);

                if (fireCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, fireCode, fireError);
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Halting Methods
        internal static ReturnCode InternalIsHalted(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            CancelFlags cancelFlags,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Acquire the interpreter lock here;
            //         however, do not use the property
            //         because the interpreter may be
            //         disposed at this point.  We do not
            //         want to throw exceptions here
            //         primarily because we are called by
            //         Interpreter.Ready and that method
            //         checks for interpreter disposal
            //         already (although, not always at
            //         the right time to avoid a race
            //         condition).
            //
            // BUGBUG: This may not be the right fix for
            //         this issue.  It might be better to
            //         grab the interpreter lock inside of
            //         the Ready method while calling this
            //         method and only after checking if
            //         the interpreter has been disposed.
            //
            ReturnCode code;

            bool fireCallback = true;
            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref result))
                        return ReturnCode.Error;

                    code = interpreter.InternalIsHalted(
#if THREADING
                        engineContext,
#endif
                        cancelFlags, ref result);
                }
                else
                {
                    fireCallback = false; /* TODO: Nothing was done? */

                    result = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

            if (fireCallback && (code == ReturnCode.Error))
            {
                bool waitForLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.WaitForLock, true);

                ReturnCode fireCode;
                Result fireError = null;

                fireCode = interpreter.FireInterruptCallback(
                    InterruptType.Halted, ClientData.Empty,
                    waitForLock, ref fireError);

                if (fireCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, fireCode, fireError);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        internal static ReturnCode InteractiveIsHalted(
            Interpreter interpreter,
            ref Result result
            )
        {
#if THREADING
            IEngineContext engineContext = null;

            if (interpreter != null)
                engineContext = interpreter.GetEngineContextNoCreate();
#endif

            return InternalIsHalted(interpreter,
#if THREADING
                engineContext,
#endif
                CancelFlags.InteractiveIsHalted, ref result);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ResetHalt(
            Interpreter interpreter,
            CancelFlags cancelFlags
            )
        {
            Result error = null;

            return ResetHalt(interpreter, cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ResetHalt(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            bool reset = false;

            return ResetHalt(interpreter, cancelFlags, ref reset, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ResetHalt(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref bool reset,
            ref Result error
            )
        {
            Result haltedResults = null;
            bool halted = false;

            return InternalResetHalt(interpreter,
#if THREADING
                null,
#endif
                cancelFlags, ref haltedResults, ref halted, ref reset,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode InternalResetHalt(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            CancelFlags cancelFlags,
            ref Result haltedResults,
            ref bool halted,
            ref bool reset,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;

#if NOTIFY
            bool notify = FlagOps.HasFlags(
                cancelFlags, CancelFlags.Notify, true);
#endif

            bool force = FlagOps.HasFlags(
                cancelFlags, CancelFlags.IgnorePending, true);

            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref error))
                        return ReturnCode.Error;

                    code = interpreter.InternalResetHalt(
#if THREADING
                        engineContext,
#endif
                        cancelFlags, ref haltedResults,
                        ref halted, ref reset, ref error);
                }
                else
                {
                    error = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

#if NOTIFY
            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
#if DEBUGGER
                    NotifyType.Debugger,
#else
                    NotifyType.Script,
#endif
                    NotifyFlags.Reset | NotifyFlags.Halted,
                    new ObjectTriplet(code, cancelFlags, reset),
                    interpreter, null, null, null, ref error);
            }
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        internal static ReturnCode InteractiveResetHalt(
            Interpreter interpreter,
            ref bool reset,
            ref Result error
            )
        {
            return ResetHalt(
                interpreter, CancelFlags.InteractiveAutomaticResetHalt,
                ref reset, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode HaltEvaluate(
            Interpreter interpreter,
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            ) /* THREAD-SAFE */
        {
            return InternalHaltEvaluate(
                interpreter,
#if THREADING
                null,
#endif
                result, cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode InternalHaltEvaluate(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;

#if NOTIFY
            bool notify = FlagOps.HasFlags(
                cancelFlags, CancelFlags.Notify, true);

            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
#if DEBUGGER
                    NotifyType.Debugger,
#else
                    NotifyType.Script,
#endif
                    NotifyFlags.PreHalted,
                    new ObjectPair(result, cancelFlags), interpreter,
                    null, null, null, ref error);
            }
#endif

            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref error))
                        return ReturnCode.Error;

                    code = interpreter.InternalHaltEvaluate(
#if THREADING
                        engineContext,
#endif
                        result, cancelFlags, ref error);
                }
                else
                {
                    result = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

#if NOTIFY
            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
#if DEBUGGER
                    NotifyType.Debugger,
#else
                    NotifyType.Script,
#endif
                    NotifyFlags.Halted,
                    new ObjectTriplet(code, result, cancelFlags),
                    interpreter, null, null, null, ref error);
            }
#endif

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Cancellation Methods
        public static ReturnCode IsCanceled(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return InternalIsCanceled(interpreter,
#if THREADING
                null,
#endif
                cancelFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode InternalIsCanceled(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            CancelFlags cancelFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            InterruptType interruptType = InterruptType.None;

#if DEBUGGER
            BreakpointType breakpointType = BreakpointType.None;
#endif

            //
            // BUGFIX: Acquire the interpreter lock here;
            //         however, do not use the property
            //         because the interpreter may be
            //         disposed at this point.  We do not
            //         want to throw exceptions here
            //         primarily because we are called by
            //         Interpreter.Ready and that method
            //         checks for interpreter disposal
            //         already (although, not always at
            //         the right time to avoid a race
            //         condition).
            //
            // BUGBUG: This may not be the right fix for
            //         this issue.  It might be better to
            //         grab the interpreter lock inside of
            //         the Ready method while calling this
            //         method and only after checking if
            //         the interpreter has been disposed.
            //
            ReturnCode code;

            bool fireCallback = true;
            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref result))
                        return ReturnCode.Error;

                    code = interpreter.InternalIsCanceled(
#if THREADING
                        engineContext,
#endif
                        cancelFlags, ref interruptType,
#if DEBUGGER
                        ref breakpointType,
#endif
                        ref result);
                }
                else
                {
                    fireCallback = false; /* TODO: Nothing was done? */

                    result = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

            if (fireCallback && (code == ReturnCode.Error))
            {
                bool waitForLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.WaitForLock, true);

                ReturnCode fireCode;
                Result fireError = null;

                fireCode = interpreter.FireInterruptCallback(
                    interruptType, ClientData.Empty, waitForLock,
                    ref fireError);

                if (fireCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, fireCode, fireError);
            }

#if DEBUGGER && DEBUGGER_ENGINE
            //
            // HACK: Use the "fireCallback" bit here as well, because
            //       this breakpoint type should only fire when script
            //       cancellation was actually checked and found to be
            //       true.
            //
            if (fireCallback && (code == ReturnCode.Error))
            {
                EngineFlags engineFlags = EngineFlags.None;

                bool noBreakpoint = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoBreakpoint, true);

                if (noBreakpoint)
                    engineFlags |= EngineFlags.NoBreakpoint;

                if (DebuggerOps.CanHitBreakpoints(interpreter,
                        engineFlags, breakpointType))
                {
                    code = interpreter.CheckBreakpoints(
                        code, breakpointType, null,
                        null, null, null, null, null,
                        null, ref result);
                }
            }
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ResetCancel(
            Interpreter interpreter,
            CancelFlags cancelFlags
            )
        {
            Result error = null;

            return ResetCancel(interpreter, cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetCancel(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            bool reset = false;

            return ResetCancel(interpreter, cancelFlags, ref reset, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetCancel(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref bool reset,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            Result canceledResults = null;
            bool canceled = false;
            bool unwound = false;

            return ResetCancel(
                interpreter, cancelFlags, ref canceledResults, ref canceled,
                ref unwound, ref reset, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ResetCancel(
            Interpreter interpreter,
            CancelFlags cancelFlags,
            ref Result canceledResults,
            ref bool canceled,
            ref bool unwound,
            ref bool reset,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return InternalResetCancel(interpreter,
#if THREADING
                null,
#endif
                cancelFlags, ref canceledResults, ref canceled,
                ref unwound, ref reset, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode InternalResetCancel(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            CancelFlags cancelFlags,
            ref Result canceledResults,
            ref bool canceled,
            ref bool unwound,
            ref bool reset,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;

#if NOTIFY
            bool notify = FlagOps.HasFlags(
                cancelFlags, CancelFlags.Notify, true);
#endif

            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref error))
                        return ReturnCode.Error;

                    code = interpreter.InternalResetCancel(
#if THREADING
                        engineContext,
#endif
                        cancelFlags, ref canceledResults,
                        ref canceled, ref unwound, ref reset,
                        ref error);
                }
                else
                {
                    error = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

#if NOTIFY
            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
                    NotifyType.Script, NotifyFlags.Reset | NotifyFlags.Canceled,
                    new ObjectList(code, cancelFlags, canceledResults, canceled, unwound, reset),
                    interpreter, null, null, null, ref error);
            }
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CancelEvaluate(
            Interpreter interpreter,
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            return InternalCancelEvaluate(
                interpreter,
#if THREADING
                null,
#endif
                result, cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode InternalCancelEvaluate(
            Interpreter interpreter,
#if THREADING
            IEngineContext engineContext,
#endif
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;

#if NOTIFY
            bool notify = FlagOps.HasFlags(
                cancelFlags, CancelFlags.Notify, true);

            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
                    NotifyType.Script, NotifyFlags.PreCanceled,
                    new ObjectPair(result, cancelFlags), interpreter,
                    null, null, null, ref error);
            }
#endif

            bool locked = false;

            try
            {
                bool noLock = FlagOps.HasFlags(
                    cancelFlags, CancelFlags.NoLock, true);

                if (!noLock)
                {
                    bool tryLock = FlagOps.HasFlags(
                        cancelFlags, CancelFlags.TryLock, true);

                    if (tryLock)
                    {
                        interpreter.InternalHardTryLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                    else
                    {
                        interpreter.InternalLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (noLock || locked)
                {
                    if (!IsUsableNoLock(interpreter, ref error))
                        return ReturnCode.Error;

                    code = interpreter.InternalCancelEvaluate(
#if THREADING
                        engineContext,
#endif
                        result, cancelFlags, ref error);
                }
                else
                {
                    result = "unable to acquire lock";
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

#if NOTIFY
            if (notify)
            {
                /* IGNORED */
                interpreter.CheckNotification(
                    NotifyType.Script, NotifyFlags.Canceled,
                    new ObjectTriplet(code, result, cancelFlags),
                    interpreter, null, null, null, ref error);
            }
#endif

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Script Exception Methods
        #region Script Exception Flag Methods
        internal static bool SetErrorCodeSet( /* FOR [error], [exec], [return] USE ONLY */
            Interpreter interpreter,
            bool errorCodeSet
            )
        {
            if (interpreter != null)
            {
                if (errorCodeSet)
                    interpreter.ContextEngineFlags |= EngineFlags.ErrorCodeSet;
                else
                    interpreter.ContextEngineFlags &= ~EngineFlags.ErrorCodeSet;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetErrorInProgress( /* FOR [return] USE ONLY */
            Interpreter interpreter,
            bool errorInProgress
            )
        {
            if (interpreter != null)
            {
                if (errorInProgress)
                    interpreter.ContextEngineFlags |= EngineFlags.ErrorInProgress;
                else
                    interpreter.ContextEngineFlags &= ~EngineFlags.ErrorInProgress;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetErrorAlreadyLogged( /* FOR [error] USE ONLY */
            Interpreter interpreter,
            bool errorAlreadyLogged
            )
        {
            if (interpreter != null)
            {
                if (errorAlreadyLogged)
                    interpreter.ContextEngineFlags |= EngineFlags.ErrorAlreadyLogged;
                else
                    interpreter.ContextEngineFlags &= ~EngineFlags.ErrorAlreadyLogged;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static bool SetNoResetError( /* FOR [try] USE ONLY */
            Interpreter interpreter,
            bool noResetError
            )
        {
            if (interpreter != null)
            {
                if (noResetError)
                    interpreter.ContextEngineFlags |= EngineFlags.NoResetError;
                else
                    interpreter.ContextEngineFlags &= ~EngineFlags.NoResetError;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool ResetErrorFlags(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            EngineFlags engineFlags = interpreter.ContextEngineFlags;

            engineFlags &= ~EngineFlags.ErrorMask;

            interpreter.ContextEngineFlags = engineFlags;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Script Exception Stack Trace Methods
        private static void CheckStackOverflow(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: There is not much point in checking for a stack
                //       overflow if the interpreter is disposed.
                //
                if (!IsUsableNoLock(interpreter))
                    return;

                //
                // HACK: Reset the stack overflow flag now that we are at
                //       the outermost evaluation level.
                //
                if (interpreter.StackOverflow)
                {
                    string errorInfo = String.Format(
                        "{0}    ... truncated ..." +
                        "{0}    (stack overflow line {1})",
                        Environment.NewLine,
                        Interpreter.GetErrorLine(interpreter));

                    /* IGNORED */
                    interpreter.SetVariableValue( /* EXEMPT */
                        ErrorInfoVariableFlags | VariableFlags.AppendValue,
                        TclVars.Core.ErrorInfo, errorInfo, null);

                    interpreter.StackOverflow = false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetErrorInformation(
            Interpreter interpreter,
            bool waitForLock,
            bool failOnError,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool locked = false;

            try
            {
                if (waitForLock)
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */
                }
                else
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */
                }

                if (locked)
                {
                    Result localError = null; /* REUSED */

                    if (!IsUsableNoLock(interpreter, ref localError))
                    {
                        error = localError;
                        return ReturnCode.Error;
                    }

                    interpreter.ErrorCode = null;

                    localError = null;

                    if ((interpreter.SetVariableValue( /* EXEMPT */
                            ErrorCodeVariableFlags,
                            TclVars.Core.ErrorCode,
                            interpreter.ErrorCode, null,
                            ref localError) != ReturnCode.Ok) &&
                        failOnError)
                    {
                        error = localError;
                        return ReturnCode.Error;
                    }

                    interpreter.ErrorInfo = null;

                    localError = null;

                    if ((interpreter.SetVariableValue( /* EXEMPT */
                            ErrorInfoVariableFlags,
                            TclVars.Core.ErrorInfo,
                            interpreter.ErrorInfo, null,
                            ref localError) != ReturnCode.Ok) &&
                        failOnError)
                    {
                        error = localError;
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unable to acquire lock";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetExceptionErrorCode(
            Interpreter interpreter,
            Exception exception
            )
        {
            return SetExceptionErrorCode(
                interpreter, exception, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode SetExceptionErrorCode(
            Interpreter interpreter,
            Exception exception,
            ArgumentList arguments,
            MemberInfo memberInfo,
            Result result
            )
        {
            ReturnCode code;
            Result error = null;

            code = SetExceptionErrorCode(
                interpreter, exception, arguments, memberInfo, result,
                ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "SetExceptionErrorCode: failed, interpreter = {0}, " +
                    "exception = {1}, arguments = {2}, memberInfo = {3}, " +
                    "result = {4}, code = {5}, error = {6}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(exception),
                    FormatOps.WrapOrNull(arguments),
                    FormatOps.WrapOrNull(memberInfo),
                    FormatOps.WrapOrNull(result),
                    FormatOps.WrapOrNull(code),
                    FormatOps.WrapOrNull(error)),
                    typeof(Engine).Name, TracePriority.EngineError);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode SetExceptionErrorCode(
            Interpreter interpreter,
            Exception exception,
            ArgumentList arguments,
            MemberInfo memberInfo,
            Result result,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (exception == null)
            {
                error = "invalid exception";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Acquire the interpreter lock here; however, do not use
            //         the property because the interpreter may be disposed at
            //         this point.  We do not want to throw exceptions here
            //         primarily because we are called after a command has been
            //         executed (i.e. which may have arbitrary side-effects,
            //         including disposal of the interpreter).
            //
            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is unusable, we cannot continue.
                //
                if (!IsUsableNoLock(interpreter, ref error))
                    return ReturnCode.Error;

                //
                // NOTE: Figure out the extra data to pack into the exception
                //       saved into the interpreter, if any.
                //
                ResultList results = null;

                if (result != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(result);
                }

                if (memberInfo != null)
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(memberInfo.ToString());
                }

                //
                // NOTE: First, save the original exception that was seen into
                //       the per-thread state.
                //
                interpreter.Exception = new ScriptException(arguments,
                    ReturnCode.Exception, results, exception); /* per-thread */

                //
                // TODO: Fetch the innermost (i.e. the "root cause") exception.
                //       At some point, there might be a need to report other
                //       exceptions [from along the way]; however, for now this
                //       should provide some good error context information.
                //
                Exception baseException = ScriptOps.GetBaseException(exception);

                //
                // NOTE: *WARNING* This code currently assumes that this method
                //       is called for the "innermost" try/catch blocks inside
                //       the engine [and related dispatch mechanisms] only.  As
                //       such, it does not check if the error code has already
                //       been set by some other means.
                //
                /* IGNORED */
                interpreter.SetVariableValue( /* EXEMPT */
                    ErrorCodeVariableFlags, TclVars.Core.ErrorCode,
                    StringList.MakeList("EXCEPTION", baseException.GetType(),
                    FormatOps.ExceptionMethod(baseException, false)),
                    null);

                SetErrorCodeSet(interpreter, true);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void CopyErrorInformation(
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter,
            Result result
            )
        {
            if ((sourceInterpreter == null) || (targetInterpreter == null))
                return;

            Result localResult = null;

            if (sourceInterpreter.InternalCopyErrorInformation(
                    VariableFlags.None, false, ref localResult) == ReturnCode.Ok)
            {
                if (localResult != null)
                {
                    string errorInfo = localResult.ErrorInfo;

                    if (!String.IsNullOrEmpty(errorInfo))
                    {
                        int startIndex = errorInfo.IndexOf(
                            Environment.NewLine);

                        if (startIndex != Index.Invalid)
                        {
                            //
                            // HACK: Skip the initial error message as
                            //       that will already be present in the
                            //       passed result.
                            //
                            errorInfo = errorInfo.Substring(startIndex);
                        }
                        else
                        {
                            //
                            // HACK: The call below assumes there is a
                            //       new line at the start of the error
                            //       information; therefore, make sure
                            //       it does.
                            //
                            errorInfo = String.Format("{0}{1}",
                                Environment.NewLine, errorInfo);
                        }
                    }

                    string errorCode = localResult.ErrorCode;

                    AddErrorInformation(
                        targetInterpreter, result, errorCode, errorInfo);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void AddErrorInformation(
            Interpreter interpreter,
            Result result,
            string errorInfo
            )
        {
            AddErrorInformation(
                interpreter, result, null, errorInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void AddErrorInformation(
            Interpreter interpreter,
            Result result,
            string errorCode,
            string errorInfo
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                AddErrorInformation(
                    interpreter, interpreter.EngineFlags,
                    result, errorCode, errorInfo);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void AddErrorInformation(
            Interpreter interpreter,
            EngineFlags engineFlags,
            Result result,
            string errorCode,
            string errorInfo
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                if (!EngineFlagOps.HasErrorInProgress(engineFlags))
                {
                    SetErrorInProgress(interpreter, true);

                    /* IGNORED */
                    interpreter.SetVariableValue( /* EXEMPT */
                        ErrorInfoVariableFlags,
                        TclVars.Core.ErrorInfo, result, null);

                    if (!EngineFlagOps.HasErrorCodeSet(engineFlags))
                    {
                        if (errorCode == null)
                            errorCode = "NONE"; /* COMPAT: Tcl. */

                        /* IGNORED */
                        interpreter.SetVariableValue( /* EXEMPT */
                            ErrorCodeVariableFlags,
                            TclVars.Core.ErrorCode, errorCode, null);
                    }
                }

                //
                // HACK: *PERF* Skip excessive appending to the errorInfo
                //       variable when unwinding from a stack overflow.
                //
                if (interpreter.StackOverflow &&
                    ((interpreter.InternalLevels - interpreter.PreviousLevels)
                        >= ErrorInfoStackOverflowLevels) &&
                    (interpreter.ErrorFrames >= ErrorInfoStackOverflowFrames))
                {
                    return;
                }

                /* IGNORED */
                interpreter.SetVariableValue( /* EXEMPT */
                    ErrorInfoVariableFlags | VariableFlags.AppendValue,
                    TclVars.Core.ErrorInfo, errorInfo, null);

                interpreter.ErrorFrames++;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void SetErrorLine( /* NOTE: For use by [error] only. */
            Interpreter interpreter,
            bool force
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                IParseState parseState = interpreter.ParseState;

                if (parseState == null)
                    return;

                int errorLine = 0;

                CalculateErrorLine(
                    parseState.Text, parseState.CommandStart, ref errorLine);

                if (force || (errorLine != 0))
                    Interpreter.SetErrorLine(interpreter, errorLine);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void CalculateErrorLine(
            string text,      /* in */
            int commandStart, /* in */
            ref int errorLine /* out */
            )
        {
            if (text == null)
                return;

            int localErrorLine = 1;
            int length = Math.Min(commandStart, text.Length);

            for (int index = 0; index < length; index++)
                if (Parser.IsLineTerminator(text[index]))
                    localErrorLine++;

            errorLine = localErrorLine;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void LogCommandInformation(
            Interpreter interpreter,
            string text,
            int commandStart,
            int commandLength,
            EngineFlags engineFlags,
            Result result,
            ref int errorLine
            )
        {
            if (interpreter == null)
                return;

            //
            // NOTE: Already checked by [only] caller.
            //
            //if (EngineFlagOps.HasErrorAlreadyLogged(engineFlags))
            //    return;

            CalculateErrorLine(text, commandStart, ref errorLine);

            if (commandLength < 0)
                commandLength = text.Length - commandStart;

            string format;

            if (!EngineFlagOps.HasErrorInProgress(engineFlags))
                format = "{0}    while executing{0}\"{1}\"";
            else
                format = "{0}    invoked from within{0}\"{1}\"";

            string errorInfo = String.Format(format,
                Environment.NewLine, FormatOps.Ellipsis(
                    text, commandStart, commandLength,
                    ErrorInfoCommandLength, false));

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                AddErrorInformation(
                    interpreter, engineFlags, result, null,
                    errorInfo);

                SetErrorAlreadyLogged(interpreter, false);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Script Exception Return Code Methods
        private static ReturnCode ResetReturnCode(
            Interpreter interpreter,
            Result result,
            bool force
            )
        {
            bool reset = false;
            Result error = null;

            return ResetReturnCode(interpreter, result, force, ref reset, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ResetReturnCode(
            Interpreter interpreter,
            Result result,
            bool force,
            ref bool reset,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: If the interpreter has been disposed, skip
                //         checking its levels.
                //
                int levels;
                Result localError = null;

                if (IsUsableNoLock(interpreter, ref localError))
                {
                    //
                    // NOTE: The interpreter is not disposed, query its
                    //       levels.
                    //
                    levels = interpreter.InternalLevels;
                }
                else if (force)
                {
                    //
                    // NOTE: The interpreter is disposed and the force
                    //       flag is set, just use zero since the value
                    //       will be ignored.
                    //
                    levels = 0;
                }
                else
                {
                    //
                    // NOTE: The interpreter is disposed and the force
                    //       flag is not set, fail.
                    //
                    error = localError;
                    return ReturnCode.Error;
                }

                //
                // BUGFIX: Cannot reset a null result.
                //
                if ((result != null) && (force || (levels == 0)))
                {
                    ReturnCode returnCode = result.ReturnCode;

                    result.ReturnCode = ReturnCode.Ok;

                    reset = (returnCode != ReturnCode.Ok);
                }
                else
                {
                    reset = false;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode UpdateReturnInformation(
            Interpreter interpreter
            )
        {
            //
            // TODO: Figure out why the return code here defaults to "Ok".
            //
            ReturnCode code = ReturnCode.Ok;

            if (interpreter == null)
                return code;

            //
            // NOTE: Get the ReturnCode value used by the "exception"
            //       semantics and then reset it to Ok.
            //
            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: If the interpreter has been disposed, skip
                //         setting the return code.
                //
                if (!IsUsableNoLock(interpreter))
                    return code;

                code = interpreter.ReturnCode;
                interpreter.ReturnCode = ReturnCode.Ok;

                if (code == ReturnCode.Error)
                {
                    if (interpreter.ErrorCode != null)
                    {
                        /* IGNORED */
                        interpreter.SetVariableValue( /* EXEMPT */
                            ErrorCodeVariableFlags, TclVars.Core.ErrorCode,
                            interpreter.ErrorCode, null);

                        SetErrorCodeSet(interpreter, true);
                    }

                    if (interpreter.ErrorInfo != null)
                    {
                        /* IGNORED */
                        interpreter.SetVariableValue( /* EXEMPT */
                            ErrorInfoVariableFlags, TclVars.Core.ErrorInfo,
                            interpreter.ErrorInfo, null);

                        SetErrorInProgress(interpreter, true);
                    }
                }

                return code;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Script Exception/Result Reset Methods
        public static void ResetResult(
            Interpreter interpreter,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            ResetResult(interpreter, EngineFlags.None, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void ResetResult(
            Interpreter interpreter,
            EngineFlags engineFlags,
            ref Result result
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // BUGFIX: If the interpreter has already been disposed,
                //         just reset the string result and return.
                //
                if (!IsUsableNoLock(interpreter))
                {
                    result = null;
                    return;
                }

                EngineFlags localEngineFlags = CombineFlags(
                    interpreter, engineFlags, false, false);

                if (!EngineFlagOps.HasNoResetResult(localEngineFlags))
                {
                    //
                    // NOTE: Reset the string result.  This used to be
                    //       String.Empty; however, that does not seem
                    //       to be necessary here.
                    //
                    result = null;

                    if (!EngineFlagOps.HasNoResetError(localEngineFlags))
                    {
                        /* IGNORED */
                        ResetErrorFlags(interpreter);
                    }
                }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Methods
        #region Read Post-Script Methods
        private static void MaybeRemoveNonPostScriptBytes(
            ref ByteList bytes /* in, out */
            )
        {
            if (bytes == null)
                return;

            int index = bytes.IndexOf((byte)Characters.EndOfFile);

            if (index == Index.Invalid)
                return;

            bytes.RemoveRange(0, index + 1);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void ReadPostScriptBytes(
            ReadInt32Callback charCallback,  /* in */
            ReadBytesCallback bytesCallback, /* in */
            long streamLength,               /* in */
            bool seekSoftEof,                /* in */
            ref ByteList bytes               /* in, out */
            )
        {
            //
            // NOTE: If requested, skip all bytes of the stream until we
            //       hit the first "soft" end-of-file, if any.  In the
            //       event the stream is already pre-positioned, this
            //       step can be skipped at the request of the caller.
            //
            if (seekSoftEof)
            {
                //
                // NOTE: If the caller requested skipping all bytes to
                //       the first "soft" end-of-file and there is not
                //       a character callback available, fail.
                //
                if (charCallback == null)
                    return;

                //
                // NOTE: This loop must read a single byte at a time so
                //       it can stop immediately upon hitting the "soft"
                //       end-of-file indicator value.
                //
                while (true)
                {
                    //
                    // NOTE: Attempt to read a byte from the stream.  If
                    //       this throws an exception, our caller will be
                    //       responsible for catching it.
                    //
                    int readByte = charCallback(); /* throw */

                    //
                    // NOTE: Check for a "hard" end-of-stream.
                    //
                    if (readByte == ChannelStream.EndOfFile)
                        return;

                    //
                    // NOTE: Check for a "soft" end-of-file.
                    //
                    if (readByte == Characters.EndOfFile)
                        break;
                }
            }

            //
            // NOTE: If there is no callback specified to read the data
            //       bytes, we cannot read them.
            //
            if (bytesCallback == null)
                return;

            //
            // NOTE: Set the initial number of bytes to read at a time to
            //       the typical default.
            //
            int wantedToRead = ReadPostScriptBufferSize;

            //
            // NOTE: If the caller specified an overall stream length, use
            //       it to preallocate enough capacity for the resulting
            //       list of bytes.
            //
            ByteList localBytes;
            byte[] buffer;

            if (streamLength != Length.Invalid)
            {
                //
                // NOTE: Preallocate enough capacity to hold the entire
                //       contents of the stream (at least as much of it
                //       as we actually plan on reading).
                //
                localBytes = new ByteList((int)streamLength); /* throw */

                //
                // NOTE: If the chunk size is less than the overall stream
                //       length, use it; otherwise, use the overall stream
                //       length to avoid preallocating too much space.
                //
                if (wantedToRead <= streamLength)
                    buffer = new byte[wantedToRead]; /* throw */
                else
                    buffer = new byte[streamLength]; /* throw */
            }
            else
            {
                //
                // NOTE: Since the caller did not specify a stream length,
                //       just preallocate enough capacity to hold a single
                //       chunk.
                //
                localBytes = new ByteList(wantedToRead); /* throw */

                //
                // NOTE: Allocate a byte array buffer large enough to hold
                //       a single chunk.  It is possible for this to throw
                //       an exception under low-memory conditions; however,
                //       that should be fairly rare since this amount is
                //       fixed and relatively small.
                //
                buffer = new byte[wantedToRead]; /* throw */
            }

            //
            // NOTE: This loop will read N fixed-size chunks of bytes from
            //       the stream, where N may be zero.  Then, it may read a
            //       final chunk if the stream had a size not divisible by
            //       the chunk size.
            //
            while (true)
            {
                //
                // NOTE: If the caller specified an overall stream length,
                //       and that is less than the chunk size then reduce
                //       the chunk size to match it.
                //
                if ((streamLength != Length.Invalid) &&
                    (streamLength < wantedToRead))
                {
                    wantedToRead = (int)streamLength;
                }

                //
                // NOTE: Attempt to read the next chunk of bytes from the
                //       stream into the buffer.
                //
                /* throw */
                int actuallyRead = bytesCallback(buffer, 0, wantedToRead);

                //
                // NOTE: If no bytes were read, this is end-of-stream and
                //       we are now completely done with the stream.
                //
                if (actuallyRead == 0)
                    break;

                //
                // NOTE: If less bytes were read than requested, this is
                //       also end-of-stream; however, the bytes actually
                //       read must be copied into the result before we
                //       are done.
                //
                if (actuallyRead < wantedToRead)
                {
                    //
                    // NOTE: Get rid of excess bytes in the chunk buffer
                    //       so it can be added verbatim to the resulting
                    //       byte list.
                    //
                    Array.Resize(ref buffer, actuallyRead);

                    //
                    // NOTE: Add the entire (shrunken) contents of the
                    //       chunk buffer to the resulting byte list.
                    //
                    localBytes.AddRange(buffer);

                    //
                    // NOTE: We are now completely done with the stream.
                    //
                    break;
                }
                else
                {
                    //
                    // NOTE: An entire chunk was read.  Add the entire
                    //       contents of the chunk buffer to the resulting
                    //       byte list.
                    //
                    localBytes.AddRange(buffer);
                }

                //
                // NOTE: If the caller specified an overall stream length,
                //       adjust and check the remaining bytes to be read.
                //
                if (streamLength != Length.Invalid)
                {
                    streamLength -= actuallyRead;

                    if (streamLength == 0)
                        break;
                }
            }

            //
            // NOTE: Commit changes to the output parameter supplied by
            //       the caller.
            //
            if (bytes != null)
                bytes.AddRange(localBytes);
            else
                bytes = localBytes;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Script (Shared) Methods
        private static bool GetStreamLength(
            Stream stream,  /* in */
            out long length /* out */
            )
        {
            if ((stream != null) && stream.CanSeek)
            {
                length = stream.Length;
                return true;
            }

            length = Length.Invalid;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static long GetStreamLength(
            object reader /* in */
            )
        {
            long length;
            StreamReader streamReader = reader as StreamReader;

            if ((streamReader != null) && GetStreamLength(
                    streamReader.BaseStream, out length))
            {
                return length;
            }

            BinaryReader binaryReader = reader as BinaryReader;

            if ((binaryReader != null) && GetStreamLength(
                    binaryReader.BaseStream, out length))
            {
                return length;
            }

            return Length.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void GetStreamCallback(
            TextReader textReader,             /* in */
            ref ReadInt32Callback charCallback /* out */
            )
        {
            if (textReader == null)
                return;

            charCallback = textReader.Read;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void GetStreamCallbacks(
            TextReader textReader,              /* in */
            ref ReadInt32Callback charCallback, /* out */
            ref ReadCharsCallback charsCallback /* out */
            )
        {
            if (textReader == null)
                return;

            charCallback = textReader.Read;
            charsCallback = textReader.Read;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void GetStreamCallbacks(
            BinaryReader binaryReader,          /* in */
            ref ReadInt32Callback charCallback, /* out */
            ref ReadBytesCallback bytesCallback /* out */
            )
        {
            if (binaryReader == null)
                return;

            charCallback = binaryReader.Read;
            bytesCallback = binaryReader.Read;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void ReadScriptVia(
            ReadInt32Callback charCallback, /* in */
            StringBuilder builder,          /* in, out */
            StringBuilder originalBuilder,  /* in, out */
            bool forceSoftEof,              /* in */
            out int preSoftEofLength        /* out */
            )
        {
            bool hitSoftEof = false;
            int lastCharacter = Characters.Null;
            int character;

            //
            // NOTE: Initially, make sure the length passed by the
            //       caller has a well-defined (and invalid) value.
            //
            preSoftEofLength = Length.Invalid;

            //
            // NOTE: *WARNING* Do NOT "optimize" this code to use
            //       File.ReadAllText because that does not preserve
            //       the end-of-file character semantics for the
            //       [source] command (i.e. "scripted documents").
            //       Also, we must handle end-of-line translations
            //       (Cr/Lf --> Lf) here.  Keep going until we hit a
            //       "hard" end-of-stream (or file) -OR- until we hit
            //       a "soft" end-of-stream (or file) if that flag
            //       has been specified by the caller.
            //
            /* throw */
            while ((character = charCallback()) != ChannelStream.EndOfFile)
            {
                //
                // NOTE: Did we hit a "soft" end-of-file?
                //
                if (character == Characters.EndOfFile)
                {
                    //
                    // NOTE: If we are building the "original" buffer
                    //       we must keep going even after hitting a
                    //       "soft" end-of-file; otherwise, we can
                    //       stop reading now.  However, even if we
                    //       intend to keep going here, we set an
                    //       indicator to prevent the normal buffer
                    //       from being modified after this point.
                    //       Unless we are forbidden from doing any
                    //       of this special handling by the caller.
                    //
                    if (originalBuilder != null)
                    {
                        if (preSoftEofLength == Length.Invalid)
                            preSoftEofLength = originalBuilder.Length;

                        if (!forceSoftEof)
                        {
                            originalBuilder.Append((char)character);
                            hitSoftEof = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    //
                    // NOTE: If we previously hit a "soft" end-of-file,
                    //       skip changing the normal buffer; however,
                    //       always add all available characters to the
                    //       "original" buffer for use by the policy
                    //       engine.
                    //
                    if (!hitSoftEof && (builder != null))
                    {
                        //
                        // NOTE: If the current character is a line-feed
                        //       and the last character that we saw was
                        //       a carriage-return, strip the previous
                        //       character (the carriage-return) from
                        //       the buffer and then simply add the
                        //       current character (the line-feed).
                        //
                        if (lastCharacter == Characters.CarriageReturn)
                        {
                            int length = builder.Length - 1;

                            if (character == Characters.LineFeed)
                            {
                                //
                                // NOTE: To support the DOS end-of-line
                                //       convention we need to remove
                                //       the previous character, thus
                                //       collapsing the carriage-return
                                //       line-feed pair into a single
                                //       line-feed.
                                //
                                builder.Length = length;
                            }
                            else
                            {
                                //
                                // NOTE: To support the Mac end-of-line
                                //       convention we need to replace
                                //       the carriage-return character
                                //       with the line-feed character
                                //       (i.e. the Unix end-of-line
                                //       character).
                                //
                                builder[length] = Characters.LineFeed;
                            }
                        }

                        builder.Append((char)character);
                        lastCharacter = character;
                    }

                    //
                    // NOTE: When available, always add all the original
                    //       characters to the "original" buffer.
                    //
                    if (originalBuilder != null)
                        originalBuilder.Append((char)character);
                }
            }

            //
            // NOTE: Replace the final carriage-return, if any, with a
            //       line-feed.
            //
            if (builder != null)
            {
                int length = builder.Length;

                if ((length > 0) &&
                    (builder[length - 1] == Characters.CarriageReturn))
                {
                    builder[length - 1] = Characters.LineFeed;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void ReadScriptVia(
            ReadInt32Callback charCallback, /* in */
            long streamLength,              /* in */
            EngineFlags engineFlags,        /* in */
            ref string originalText,        /* out */
            ref string text                 /* out */
            )
        {
            int preSoftEofLength;

            ReadScriptVia(
                charCallback, streamLength, engineFlags,
                ref originalText, ref text, out preSoftEofLength);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void ReadScriptVia(
            ReadInt32Callback charCallback, /* in */
            long streamLength,              /* in */
            EngineFlags engineFlags,        /* in */
            ref string originalText,        /* out */
            ref string text,                /* out */
            out int preSoftEofLength        /* out */
            )
        {
            StringBuilder builder = (streamLength != Length.Invalid) ?
                StringOps.NewStringBuilder((int)streamLength) :
                StringOps.NewStringBuilder();

            StringBuilder originalBuilder = StringOps.NewStringBuilder(
                builder.Capacity);

            bool forceSoftEof = EngineFlagOps.HasForceSoftEof(engineFlags);

            //
            // NOTE: Perform the actual reading of the raw characters
            //       from the text reader.
            //
            ReadScriptVia(
                charCallback, builder, originalBuilder, forceSoftEof,
                out preSoftEofLength);

            //
            // NOTE: Get both the whole buffers as strings (i.e. both
            //       the original and line-ending modified ones).
            //
            originalText = originalBuilder.ToString();
            text = builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Script (XML) Methods
#if XML
        private static ReturnCode ReadScriptXmlNode(
            Interpreter interpreter,                 /* in */
            XmlNode node,                            /* in */
            Encoding encoding,                       /* in */
            ref EngineFlags engineFlags,             /* in, out */
            ref SubstitutionFlags substitutionFlags, /* in, out */
            ref EventFlags eventFlags,               /* in, out */
            ref ExpressionFlags expressionFlags,     /* in, out */
            ref string originalText,                 /* out */
            ref string text,                         /* out */
            ref Result error                         /* out */
            ) /* THREAD-SAFE */
        {
            XmlBlockType localBlockType;
            string localText;

            if (!ScriptXmlOps.TryGetAttributeValues(
                    node, XmlAttributeListType.Engine,
                    false, out localBlockType,
                    out localText, ref error))
            {
                return ReturnCode.Error;
            }

            switch (localBlockType)
            {
                case XmlBlockType.None:
                    {
                        //
                        // NOTE: Ok, give them nothing.
                        //
                        originalText = null;
                        text = null;

                        return ReturnCode.Ok;
                    }
                case XmlBlockType.Automatic:
                    {
                        //
                        // NOTE: Attempt to "automatically"
                        //       determine the type of block it
                        //       is.
                        //
                        if (StringOps.IsBase64(localText))
                            goto case XmlBlockType.Base64;
                        else if (PathOps.IsUri(localText))
                            goto case XmlBlockType.Uri;
                        else
                            goto case XmlBlockType.Text;
                    }
                case XmlBlockType.Text:
                    {
                        //
                        // NOTE: The element must contain the script text,
                        //       verbatim.
                        //
                        using (StringReader stringReader = new StringReader(
                                localText))
                        {
                            ReadInt32Callback charCallback = null;

                            GetStreamCallback(
                                stringReader, ref charCallback);

                            ReadScriptVia(
                                charCallback, Length.Invalid,
                                engineFlags, ref originalText,
                                ref text);
                        }

                        return ReturnCode.Ok;
                    }
                case XmlBlockType.Base64:
                    {
                        try
                        {
                            //
                            // NOTE: The element must contain the base64
                            //       encoded script in our system default
                            //       text encoding.
                            //
                            byte[] bytes = Convert.FromBase64String(
                                localText);

                            Encoding base64Encoding = (encoding != null) ?
                                encoding : StringOps.GuessOrGetEncoding(
                                    bytes, EncodingType.Base64);

                            if (base64Encoding == null)
                            {
                                error = "base64 encoding not available";
                                return ReturnCode.Error;
                            }

                            using (StringReader stringReader = new StringReader(
                                    base64Encoding.GetString(bytes)))
                            {
                                ReadInt32Callback charCallback = null;

                                GetStreamCallback(
                                    stringReader, ref charCallback);

                                ReadScriptVia(
                                    charCallback, Length.Invalid,
                                    engineFlags, ref originalText,
                                    ref text);
                            }

                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            error = String.Format(
                                "caught exception decoding base64 block: {0}",
                                e);

                            error.Exception = e;

                            SetExceptionErrorCode(interpreter, e);

#if NOTIFY
                            if ((interpreter != null) &&
                                !EngineFlagOps.HasNoNotify(engineFlags))
                            {
                                /* IGNORED */
                                interpreter.CheckNotification(
                                    NotifyType.Engine, NotifyFlags.Exception,
                                    new ObjectPair(node, localBlockType),
                                    interpreter, null, null, e, ref error);
                            }
#endif
                        }
                        break;
                    }
                case XmlBlockType.Uri:
                    {
                        //
                        // NOTE: The element must contain a URI (local
                        //       or remote) pointing to a script file
                        //       in our system default text encoding.
                        //
                        string fileName = localText;

                        //
                        // NOTE: Use the UTF-8 encoding [by default] in
                        //       this context, not the fallback used by
                        //       the Channel subsystem (ISO-8859-1).
                        //
                        Encoding uriEncoding = (encoding != null) ?
                            encoding : GetEncoding(fileName,
                                EncodingType.UnknownUri, null);

                        return ReadOrGetScriptFile(
                            interpreter, uriEncoding, ref fileName,
                            ref engineFlags, ref substitutionFlags,
                            ref eventFlags, ref expressionFlags,
                            ref originalText, ref text, ref error);
                    }
                default:
                    {
                        error = String.Format(
                            "unsupported xml block type {0}",
                            FormatOps.WrapOrNull(localBlockType));

                        break;
                    }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void AppendTextFromXmlNode(
            string text,              /* in */
            ref StringBuilder builder /* in, out */
            )
        {
            if (builder != null)
            {
                //
                // NOTE: If we have previously placed script text
                //       into the result, make 100% sure that it
                //       ends with an end-of-line character.
                //
                int length = builder.Length;

                if ((length > 0) &&
                    (builder[length - 1] != Characters.LineFeed))
                {
                    builder.Append(Characters.LineFeed);
                }
            }
            else
            {
                builder = StringOps.NewStringBuilder();
            }

            builder.Append(text);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool CanRetryScriptXml(
            Interpreter interpreter,  /* in: NOT USED */
            Result error,             /* in: OPTIONAL */
            XmlErrorTypes errorType,  /* in */
            XmlErrorTypes retryTypes, /* in */
            bool @default             /* in */
            )
        {
            return XmlOps.ShouldRetryError(
                error, errorType, retryTypes, @default);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static string GetScriptXml(
            string originalText, /* in */
            int preSoftEofLength /* in */
            )
        {
            if (String.IsNullOrEmpty(originalText))
                return originalText;

            if (preSoftEofLength == Length.Invalid)
                return originalText;

            return originalText.Substring(0, preSoftEofLength);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadScriptXml(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string xml,                              /* in */
            XmlErrorTypes retryTypes,                /* in */
            bool validate,                           /* in */
            bool relaxed,                            /* in */
            bool all,                                /* in */
            ref EngineFlags engineFlags,             /* in, out */
            ref SubstitutionFlags substitutionFlags, /* in, out */
            ref EventFlags eventFlags,               /* in, out */
            ref ExpressionFlags expressionFlags,     /* in, out */
            ref string text,                         /* out */
            ref bool canRetry,                       /* out */
            ref Result error                         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            IClientData clientData = null;
            IEnumerable<IScript> scripts = null;

            return ReadScriptXml(
                interpreter, encoding, xml, retryTypes, validate,
                relaxed, all, ref engineFlags, ref substitutionFlags,
                ref eventFlags, ref expressionFlags, ref clientData,
                ref scripts, ref text, ref canRetry, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadScriptXml(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string xml,                              /* in */
            XmlErrorTypes retryTypes,                /* in */
            bool validate,                           /* in */
            bool relaxed,                            /* in */
            bool all,                                /* in */
            ref EngineFlags engineFlags,             /* in, out */
            ref SubstitutionFlags substitutionFlags, /* in, out */
            ref EventFlags eventFlags,               /* in, out */
            ref ExpressionFlags expressionFlags,     /* in, out */
            ref IClientData clientData,              /* in, out */
            ref IEnumerable<IScript> scripts,        /* out */
            ref string text,                         /* out */
            ref bool canRetry,                       /* out */
            ref Result error                         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            XmlDocument document = null;

            if (XmlOps.LoadString(
                    xml, ref document,
                    ref error) != ReturnCode.Ok)
            {
                canRetry = CanRetryScriptXml(
                    interpreter, error,
                    XmlErrorTypes.LoadXml,
                    retryTypes, false);

                return ReturnCode.Error;
            }

            if (validate)
            {
                if (XmlOps.Validate(
                        null, null, document, relaxed,
                        ref error) != ReturnCode.Ok)
                {
                    canRetry = CanRetryScriptXml(
                        interpreter, error,
                        XmlErrorTypes.Validate,
                        retryTypes, false);

                    return ReturnCode.Error;
                }
            }

            if (encoding == null)
            {
                if (XmlOps.GetEncoding(
                        document, FlagOps.HasFlags(retryTypes,
                        XmlErrorTypes.StrictEncoding, true),
                        ref encoding, ref error) != ReturnCode.Ok)
                {
                    canRetry = CanRetryScriptXml(
                        interpreter, error,
                        XmlErrorTypes.Encoding,
                        retryTypes, false);

                    return ReturnCode.Error;
                }
            }

            XmlNodeList nodeList = null;

            if (XmlOps.GetScriptBlockNodeList(
                    document, ref nodeList,
                    ref error) != ReturnCode.Ok)
            {
                canRetry = CanRetryScriptXml(
                    interpreter, error,
                    XmlErrorTypes.Nodes,
                    retryTypes, false);

                return ReturnCode.Error;
            }

            if ((nodeList == null) || (nodeList.Count == 0))
            {
                error = "no script blocks were found";

                canRetry = CanRetryScriptXml(
                    interpreter, error,
                    XmlErrorTypes.Empty,
                    retryTypes, true);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            StringBuilder builder = null;
            IList<IScript> localScripts = null;

            foreach (XmlNode node in nodeList)
            {
                if (node == null)
                    continue;

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Xml, NotifyFlags.Read,
                        new ObjectTriplet(xml, node, builder),
                        interpreter, clientData, null, null,
                        ref error);
                }
#endif

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Attempt to create a script object from
                //       the XML node.
                //
                IScript script = Script.CreateFromXmlNode(
                    ScriptTypes.Block, node,
                    EngineMode.EvaluateScript, ScriptFlags.None,
                    engineFlags, substitutionFlags, eventFlags,
                    expressionFlags, clientData, ref error);

                //
                // NOTE: Make sure the script object creation
                //       succeeded.  It can fail due to policy,
                //       etc.  In that case, we will have to raise
                //       an error.
                //
                if (script == null)
                {
                    canRetry = CanRetryScriptXml(
                        interpreter, error,
                        XmlErrorTypes.Create,
                        retryTypes, true);

                    code = ReturnCode.Error;
                    break;
                }

                //
                // HACK: *SECURITY* Due to its use by the policy engine,
                //       an IScript created from XML cannot be modified.
                //
                script.MakeImmutable();

                ///////////////////////////////////////////////////////////////

                ReturnCode beforeScriptCode = ReturnCode.Ok;
                PolicyDecision beforeScriptDecision = PolicyDecision.None;
                Result beforeScriptPolicyResult = null;

                try
                {
                    #region Policy Checking: "Before Script"
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoPolicy(engineFlags))
                    {
                        beforeScriptDecision = interpreter.ScriptInitialDecision;

                        beforeScriptCode = interpreter.CheckScriptPolicies(
                            PolicyFlags.EngineBeforeScript, script,
                            encoding, null, ref beforeScriptDecision,
                            ref beforeScriptPolicyResult);

                        interpreter.ScriptFinalDecision = PolicyOps.FinalDecision(
                            PolicyFlags.EngineBeforeScript, beforeScriptCode,
                            beforeScriptDecision);

                        if (!PolicyOps.IsSuccess(
                                beforeScriptCode, beforeScriptDecision))
                        {
                            canRetry = CanRetryScriptXml(
                                interpreter, error,
                                XmlErrorTypes.Policy,
                                retryTypes, false);

                            if (beforeScriptPolicyResult != null)
                            {
                                error = beforeScriptPolicyResult;
                            }
                            else
                            {
                                error = String.Format(
                                    "script {0} cannot be used, denied by policy",
                                    FormatOps.WrapOrNull(EntityOps.GetId(script)));
                            }

                            code = ReturnCode.Error;
                            break;
                        }
                    }
                    #endregion
                }
                finally
                {
#if POLICY_TRACE
                    TraceOps.MaybeEmitPolicyTrace(
                        "ReadScriptXml", interpreter, "encoding", encoding,
                        "xml", xml, "retryTypes", retryTypes, "validate",
                        validate, "relaxed", relaxed, "all", all, "script",
                        script, "clientData", clientData, "engineFlags",
                        engineFlags, "substitutionFlags", substitutionFlags,
                        "eventFlags", eventFlags, "expressionFlags",
                        expressionFlags, "beforeScriptCode", beforeScriptCode,
                        "beforeScriptDecision", beforeScriptDecision,
                        "beforeScriptPolicyResult", beforeScriptPolicyResult,
                        "code", code, "canRetry", canRetry, "error", error);
#endif
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Ok, add this script to the list of scripts
                //       from this XML document.
                //
                if (localScripts == null)
                    localScripts = new List<IScript>();

                localScripts.Add(script);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(NotifyType.Xml |
                        NotifyType.Script, NotifyFlags.Added,
                        new ObjectTriplet(node, scripts, script),
                        interpreter, clientData, null, null,
                        ref error);
                }
#endif

                //
                // NOTE: Read the payload of the XML node.  If
                //       necessary, this will base64 decode it or
                //       fetch it from the specified remote URI.
                //
                string originalText = null;
                string localText = null;

                code = ReadScriptXmlNode(
                    interpreter, node, encoding, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref originalText,
                    ref localText, ref error);

                if (code != ReturnCode.Ok)
                {
                    canRetry = CanRetryScriptXml(
                        interpreter, error,
                        XmlErrorTypes.Read,
                        retryTypes, false);

                    break;
                }

                //
                // HACK: While the "FlattenText" flag is technically
                //       optional for an interpreter, this method is
                //       of limited usefulness without it, i.e. it
                //       will only return a logical list of IScript
                //       objects, which most callers will ignore.
                //
                if (FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.FlattenText, true))
                {
                    AppendTextFromXmlNode(localText, ref builder);
                }

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(NotifyType.Xml |
                        NotifyType.XmlBlock, NotifyFlags.Read,
                        new ObjectList(node, builder, originalText,
                        localText), interpreter, clientData, null,
                        null, ref error);
                }
#endif

                //
                // NOTE: Only load the first result?  If so, get ready
                //       to break out of this loop now.
                //
                if (!all)
                    break;
            }

            //
            // NOTE: Upon success, get the whole buffer as a string.
            //       Also, put the list of scripts into the client data.
            //
            if (code == ReturnCode.Ok)
            {
                //
                // HACK: While the "FlattenText" flag is technically
                //       optional for an interpreter, this method is
                //       of limited usefulness without it, i.e. it
                //       will only return a logical list of IScript
                //       objects, which most callers will ignore.
                //
                if (FlagOps.HasFlags(
                        retryTypes, XmlErrorTypes.FlattenText, true) &&
                    (builder != null))
                {
                    text = builder.ToString();
                }

                scripts = localScripts;

                canRetry = false; /* SUCCESS? */
            }

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Script (Stream) Methods
        public static ReturnCode ReadScriptStream(
            Interpreter interpreter, /* in */
            string name,             /* in */
            TextReader textReader,   /* in */
            int startIndex,          /* in */
            int characters,          /* in */
            ref string text,         /* out */
            ref Result error         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadInt32Callback charCallback = null;
            ReadCharsCallback charsCallback = null;

            GetStreamCallbacks(
                textReader, ref charCallback, ref charsCallback);

            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            if (ReadScriptStream(
                    interpreter, null, name, charCallback,
                    charsCallback, startIndex, characters,
                    ref engineFlags, ref substitutionFlags,
                    ref eventFlags, ref expressionFlags,
                    ref readScriptClientData, ref canRetry,
                    ref error) == ReturnCode.Ok)
            {
                text = readScriptClientData.Text;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadScriptStream(
            Interpreter interpreter,    /* in */
            string name,                /* in */
            TextReader textReader,      /* in */
            int startIndex,             /* in */
            int characters,             /* in */
            EngineFlags engineFlags,    /* in */
            ref IClientData clientData, /* in, out */
            ref string text,            /* out */
            ref Result error            /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags |= interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadInt32Callback charCallback = null;
            ReadCharsCallback charsCallback = null;

            GetStreamCallbacks(
                textReader, ref charCallback, ref charsCallback);

            ReadScriptClientData readScriptClientData =
                clientData as ReadScriptClientData;

            bool canRetry = false; /* NOT USED */

            if (ReadScriptStream(
                    interpreter, null, name, charCallback,
                    charsCallback, startIndex, characters,
                    ref engineFlags, ref substitutionFlags,
                    ref eventFlags, ref expressionFlags,
                    ref readScriptClientData, ref canRetry,
                    ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                text = readScriptClientData.Text;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadScriptStream(
            Interpreter interpreter,     /* in */
            string name,                 /* in */
            TextReader textReader,       /* in */
            int startIndex,              /* in */
            int characters,              /* in */
            ref EngineFlags engineFlags, /* in, out */
            ref string text,             /* out */
            ref bool canRetry,           /* out */
            ref Result error             /* out */
            ) /* THREAD-SAFE */
        {
            ReadInt32Callback charCallback = null;
            ReadCharsCallback charsCallback = null;

            GetStreamCallbacks(
                textReader, ref charCallback, ref charsCallback);

            ReadScriptClientData readScriptClientData = null;

            if (ReadScriptStream(
                    interpreter, name, charCallback, charsCallback,
                    startIndex, characters, ref engineFlags,
                    ref readScriptClientData, ref canRetry,
                    ref error) == ReturnCode.Ok)
            {
                text = readScriptClientData.Text;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadScriptStream(
            Interpreter interpreter,                       /* in */
            string name,                                   /* in */
            ReadInt32Callback charCallback,                /* in */
            ReadCharsCallback charsCallback,               /* in */
            int startIndex,                                /* in */
            int characters,                                /* in */
            ref EngineFlags engineFlags,                   /* in, out */
            ref ReadScriptClientData readScriptClientData, /* in, out */
            ref bool canRetry,                             /* out */
            ref Result error                               /* out */
            ) /* THREAD-SAFE */
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            return ReadScriptStream(
                interpreter, null, name, charCallback,
                charsCallback, startIndex, characters,
                ref engineFlags, ref substitutionFlags,
                ref eventFlags, ref expressionFlags,
                ref readScriptClientData, ref canRetry,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadScriptStream(
            Interpreter interpreter,                       /* in */
            IScript script,                                /* in */
            string name,                                   /* in */
            ReadInt32Callback charCallback,                /* in */
            ReadCharsCallback charsCallback,               /* in */
            int startIndex,                                /* in */
            int characters,                                /* in */
            ref EngineFlags engineFlags,                   /* in, out */
            ref SubstitutionFlags substitutionFlags,       /* in, out: NOT USED */
            ref EventFlags eventFlags,                     /* in, out */
            ref ExpressionFlags expressionFlags,           /* in, out: NOT USED */
            ref ReadScriptClientData readScriptClientData, /* out */
            ref bool canRetry,                             /* out */
            ref Result error                               /* out */
            ) /* THREAD-SAFE */
        {
            if ((charCallback == null) || (charsCallback == null))
            {
                error = "invalid stream callbacks";
                return ReturnCode.Error;
            }

            //
            // NOTE: This is the overall result of this method.  It is
            //       used to allow the finally block to detect success.
            //
            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: If we have an interpreter context, check to make
            //       sure this stream can be read (and then evaluated,
            //       presumably) according to any script stream
            //       policies that may be active.
            //
            ReturnCode beforeStreamCode = ReturnCode.Ok;
            ReturnCode beforeScriptCode = ReturnCode.Ok;
            ReturnCode afterStreamCode = ReturnCode.Ok;

            PolicyDecision beforeStreamDecision = PolicyDecision.None;
            PolicyDecision beforeScriptDecision = PolicyDecision.None;
            PolicyDecision afterStreamDecision = PolicyDecision.None;

            Result beforeStreamPolicyResult = null;
            Result beforeScriptPolicyResult = null;
            Result afterStreamPolicyResult = null;

            try
            {
                #region Policy Checking: "Before Stream"
                //
                // HACK: The "script" parameter should only be non-null
                //       when being called from the EvaluateScript method
                //       overload that accepts an IScript.  In that case,
                //       there is no underlying file, so all those policy
                //       checks must be skipped.  Similarly, this "name"
                //       parameter check here relies upon the fact that
                //       (eventually) the ExtractContextAndFileName helper
                //       method will always fail when a file name is null;
                //       therefore, there is (almost) no point in checking
                //       a stream policy for those cases.  Hence, a null
                //       "name" parameter is used to indiate that a script
                //       being read has no associated file name, e.g. from
                //       a memory stream, etc.
                //
                if ((interpreter != null) &&
                    (script == null) && (name != null) &&
                    !EngineFlagOps.HasNoPolicy(engineFlags))
                {
                    beforeStreamDecision = interpreter.StreamInitialDecision;

                    beforeStreamCode = interpreter.CheckBeforeStreamPolicies(
                        PolicyFlags.EngineBeforeStream, name, null, null,
                        ref beforeStreamDecision,
                        ref beforeStreamPolicyResult);

                    interpreter.StreamFinalDecision = PolicyOps.FinalDecision(
                        PolicyFlags.EngineBeforeStream, beforeStreamCode,
                        beforeStreamDecision);

                    if (!PolicyOps.IsSuccess(
                            beforeStreamCode, beforeStreamDecision))
                    {
                        canRetry = false;

                        if (beforeStreamPolicyResult != null)
                        {
                            error = beforeStreamPolicyResult;
                        }
                        else
                        {
                            error = String.Format(
                                "script stream {0} cannot be read, denied by policy",
                                FormatOps.DisplayName(name));
                        }

                        code = ReturnCode.Error;
                        return code;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if (characters < 0)
                {
                    //
                    // NOTE: Get both the whole buffers as strings (i.e.
                    //       both the original and line-ending modified
                    //       ones).
                    //
                    string originalText = null;
                    string localText = null;
                    int preSoftEofLength;

                    ReadScriptVia(
                        charCallback, Length.Invalid,
                        engineFlags, ref originalText,
                        ref localText, out preSoftEofLength);

                    ///////////////////////////////////////////////////////////

                    #region Optional Script Xml Handling
#if XML
                    //
                    // NOTE: Are we allowed to see if it is actually XML?
                    //       Check and see if the script text looks like
                    //       XML script document unless prevented from
                    //       doing so by our caller.
                    //
                    if (!EngineFlagOps.HasNoXml(engineFlags) &&
                        XmlOps.LooksLikeDocument(originalText))
                    {
                        XmlErrorTypes retryXml = XmlErrorTypes.None;
                        bool validateXml = false;
                        bool relaxedXml = false;
                        bool allXml = false;

                        if (interpreter != null)
                        {
                            interpreter.QueryXmlProperties(
                                ref retryXml, ref validateXml,
                                ref relaxedXml, ref allXml);
                        }

                        code = ReadScriptXml(
                            interpreter, null, GetScriptXml(
                                originalText, preSoftEofLength),
                            retryXml, validateXml, relaxedXml,
                            allXml, ref engineFlags,
                            ref substitutionFlags, ref eventFlags,
                            ref expressionFlags, ref localText,
                            ref canRetry, ref error);

                        if (code != ReturnCode.Ok)
                            return code;
                    }
#endif
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region Policy Checking: "Before Script"
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoPolicy(engineFlags) &&
                        EngineFlagOps.HasExternalScript(engineFlags))
                    {
                        //
                        // NOTE: Attempt to create a "stream-based" script
                        //       object for use by the policy engine -OR-
                        //       the pre-existing IScript provided by the
                        //       caller.
                        //
                        IScript localScript = null;
                        Result scriptCreateError = null;

                        if (script != null)
                        {
                            localScript = Script.CreateForPolicy(
                                script, originalText, ref scriptCreateError);

                            if (localScript == null)
                            {
                                canRetry = false;

                                if (scriptCreateError != null)
                                    error = scriptCreateError;
                                else
                                    error = "could not copy script for policy";

                                code = ReturnCode.Error;
                                return code;
                            }
                        }
                        else
                        {
                            localScript = Script.CreateForPolicy(
                                name, ScriptTypes.Stream, originalText,
                                engineFlags, substitutionFlags, eventFlags,
                                expressionFlags, ref scriptCreateError);

                            if (localScript == null)
                            {
                                canRetry = false;

                                if (scriptCreateError != null)
                                    error = scriptCreateError;
                                else
                                    error = "could not create script for policy";

                                code = ReturnCode.Error;
                                return code;
                            }
                        }

                        //
                        // HACK: *SECURITY* Due to its use by the policy
                        //       engine, this IScript cannot be modified.
                        //
                        localScript.MakeImmutable();

                        beforeScriptDecision = interpreter.ScriptInitialDecision;

                        beforeScriptCode = interpreter.CheckScriptPolicies(
                            PolicyFlags.EngineBeforeScript, localScript,
                            null, null, ref beforeScriptDecision,
                            ref beforeScriptPolicyResult);

                        interpreter.ScriptFinalDecision = PolicyOps.FinalDecision(
                            PolicyFlags.EngineBeforeScript, beforeScriptCode,
                            beforeScriptDecision);

                        if (!PolicyOps.IsSuccess(
                                beforeScriptCode, beforeScriptDecision))
                        {
                            canRetry = false;

                            if (beforeScriptPolicyResult != null)
                            {
                                error = beforeScriptPolicyResult;
                            }
                            else
                            {
                                error = String.Format(
                                    "script {0} cannot be used, denied by policy",
                                    FormatOps.WrapOrNull(EntityOps.GetId(localScript)));
                            }

                            code = ReturnCode.Error;
                            return code;
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    #region Policy Checking: "After Stream"
                    //
                    // NOTE: Did we succeed in post-processing the text,
                    //       if necessary?
                    //
                    // HACK: The "script" parameter should only be non-null
                    //       when being called from the EvaluateScript method
                    //       overload that accepts an IScript.  In that case,
                    //       there is no underlying file, so all those policy
                    //       checks must be skipped.  Similarly, this "name"
                    //       parameter check here relies upon the fact that
                    //       (eventually) the ExtractContextAndFileName helper
                    //       method will always fail when a file name is null;
                    //       therefore, there is (almost) no point in checking
                    //       a stream policy for those cases.  Hence, a null
                    //       "name" parameter is used to indiate that a script
                    //       being read has no associated file name, e.g. from
                    //       a memory stream, etc.
                    //
                    ReadScriptClientData localReadScriptClientData =
                        new ReadScriptClientData(name, originalText, localText,
                            null);

                    if ((interpreter != null) &&
                        (script == null) && (name != null) &&
                        !EngineFlagOps.HasNoPolicy(engineFlags))
                    {
                        afterStreamDecision = interpreter.StreamInitialDecision;

                        afterStreamCode = interpreter.CheckAfterStreamPolicies(
                            PolicyFlags.EngineAfterStream, name,
                            originalText, null, null,
                            ref afterStreamDecision,
                            ref afterStreamPolicyResult);

                        interpreter.StreamFinalDecision = PolicyOps.FinalDecision(
                            PolicyFlags.EngineAfterStream, afterStreamCode,
                            afterStreamDecision);

                        if (!PolicyOps.IsSuccess(
                                afterStreamCode, afterStreamDecision))
                        {
                            canRetry = false;

                            if (afterStreamPolicyResult != null)
                            {
                                error = afterStreamPolicyResult;
                            }
                            else
                            {
                                error = String.Format(
                                    "script {0} cannot be returned, denied by policy",
                                    FormatOps.DisplayName(name));
                            }

                            code = ReturnCode.Error;
                            return code;
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    readScriptClientData = localReadScriptClientData;

#if NOTIFY
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Stream, NotifyFlags.Read,
                            new ObjectList(charCallback, charsCallback,
                                startIndex, characters, readScriptClientData),
                            interpreter, null, null, null, ref error);
                    }
#endif

                    canRetry = false; /* SUCCESS? */
                    return code;
                }
                else if (characters > 0)
                {
                    char[] buffer = new char[characters];

                    int read = charsCallback(
                        buffer, 0, characters); /* throw */

                    if (read == characters)
                    {
                        //
                        // NOTE: Create a string from the buffer of
                        //       characters we read.
                        //
                        string originalText = new string(buffer);
                        string localText = originalText;

                        //
                        // NOTE: Were we able to read any characters?
                        //
                        if (!String.IsNullOrEmpty(localText))
                        {
                            //
                            // NOTE: Check for an embedded "soft"
                            //       end-of-file character.
                            //
                            int softEofIndex = localText.IndexOf(
                                Characters.EndOfFile, 0);

                            //
                            // NOTE: Create a string builder with
                            //       at least enough space to hold
                            //       the entire script.
                            //
                            StringBuilder builder;

                            //
                            // NOTE: If there is an embedded "soft"
                            //       end-of-file character, truncate
                            //       the script at that point.
                            //
                            if (softEofIndex != Index.Invalid)
                            {
                                builder = StringOps.NewStringBuilder(
                                    localText, 0, softEofIndex);
                            }
                            else
                            {
                                builder = StringOps.NewStringBuilder(
                                    localText, localText.Length);
                            }

                            //
                            // NOTE: Perform fixups and/or character
                            //       (or sub-string) replacements
                            //       within the text.  Typically,
                            //       this is only used to perform
                            //       end -of-line translations.
                            //
                            StringOps.FixupLineEndings(builder);

                            //
                            // NOTE: Get the whole buffer as a
                            //       string (i.e. the one with
                            //       its line-endings modified).
                            //
                            localText = builder.ToString();
                        }

                        ///////////////////////////////////////////////////////

                        #region Optional Script Xml Handling
#if XML
                        //
                        // NOTE: Are we allowed to see if it is actually XML?
                        //       Check and see if the script text looks like
                        //       XML script document unless prevented from
                        //       doing so by our caller.
                        //
                        if (!EngineFlagOps.HasNoXml(engineFlags) &&
                            XmlOps.LooksLikeDocument(originalText))
                        {
                            XmlErrorTypes retryXml = XmlErrorTypes.None;
                            bool validateXml = false;
                            bool relaxedXml = false;
                            bool allXml = false;

                            if (interpreter != null)
                            {
                                interpreter.QueryXmlProperties(
                                    ref retryXml, ref validateXml,
                                    ref relaxedXml, ref allXml);
                            }

                            code = ReadScriptXml(
                                interpreter, null, GetScriptXml(
                                    originalText, Length.Invalid),
                                retryXml, validateXml, relaxedXml,
                                allXml, ref engineFlags,
                                ref substitutionFlags, ref eventFlags,
                                ref expressionFlags, ref localText,
                                ref canRetry, ref error);

                            if (code != ReturnCode.Ok)
                                return code;
                        }
#endif
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Policy Checking: "Before Script"
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoPolicy(engineFlags) &&
                            EngineFlagOps.HasExternalScript(engineFlags))
                        {
                            //
                            // NOTE: Attempt to create a "stream-based" script
                            //       object for use by the policy engine -OR-
                            //       the pre-existing IScript provided by the
                            //       caller.
                            //
                            IScript localScript = null;
                            Result scriptCreateError = null;

                            if (script != null)
                            {
                                localScript = Script.CreateForPolicy(
                                    script, originalText, ref scriptCreateError);

                                if (localScript == null)
                                {
                                    canRetry = false;

                                    if (scriptCreateError != null)
                                        error = scriptCreateError;
                                    else
                                        error = "could not copy script for policy";

                                    code = ReturnCode.Error;
                                    return code;
                                }
                            }
                            else
                            {
                                localScript = Script.CreateForPolicy(
                                    name, ScriptTypes.Stream, originalText,
                                    engineFlags, substitutionFlags, eventFlags,
                                    expressionFlags, ref scriptCreateError);

                                if (localScript == null)
                                {
                                    canRetry = false;

                                    if (scriptCreateError != null)
                                        error = scriptCreateError;
                                    else
                                        error = "could not create script for policy";

                                    code = ReturnCode.Error;
                                    return code;
                                }
                            }

                            //
                            // HACK: *SECURITY* Due to its use by the policy
                            //       engine, this IScript cannot be modified.
                            //
                            localScript.MakeImmutable();

                            beforeScriptDecision = interpreter.ScriptInitialDecision;

                            beforeScriptCode = interpreter.CheckScriptPolicies(
                                PolicyFlags.EngineBeforeScript, localScript,
                                null, null, ref beforeScriptDecision,
                                ref beforeScriptPolicyResult);

                            interpreter.ScriptFinalDecision = PolicyOps.FinalDecision(
                                PolicyFlags.EngineBeforeScript, beforeScriptCode,
                                beforeScriptDecision);

                            if (!PolicyOps.IsSuccess(
                                    beforeScriptCode, beforeScriptDecision))
                            {
                                canRetry = false;

                                if (beforeScriptPolicyResult != null)
                                {
                                    error = beforeScriptPolicyResult;
                                }
                                else
                                {
                                    error = String.Format(
                                        "script {0} cannot be used, denied by policy",
                                        FormatOps.WrapOrNull(EntityOps.GetId(localScript)));
                                }

                                code = ReturnCode.Error;
                                return code;
                            }
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Policy Checking: "After Stream"
                        //
                        // NOTE: Did we succeed in post-processing the text,
                        //       if necessary?
                        //
                        // HACK: The "script" parameter should only be non-null
                        //       when being called from the EvaluateScript method
                        //       overload that accepts an IScript.  In that case,
                        //       there is no underlying file, so all those policy
                        //       checks must be skipped.  Similarly, this "name"
                        //       parameter check here relies upon the fact that
                        //       (eventually) the ExtractContextAndFileName helper
                        //       method will always fail when a file name is null;
                        //       therefore, there is (almost) no point in checking
                        //       a stream policy for those cases.  Hence, a null
                        //       "name" parameter is used to indiate that a script
                        //       being read has no associated file name, e.g. from
                        //       a memory stream, etc.
                        //
                        ReadScriptClientData localReadScriptClientData =
                            new ReadScriptClientData(name, originalText, localText,
                                null);

                        if ((interpreter != null) &&
                            (script == null) && (name != null) &&
                            !EngineFlagOps.HasNoPolicy(engineFlags))
                        {
                            afterStreamDecision = interpreter.StreamInitialDecision;

                            afterStreamCode = interpreter.CheckAfterStreamPolicies(
                                PolicyFlags.EngineAfterStream, name,
                                originalText, null, null,
                                ref afterStreamDecision,
                                ref afterStreamPolicyResult);

                            interpreter.StreamFinalDecision = PolicyOps.FinalDecision(
                                PolicyFlags.EngineAfterStream, afterStreamCode,
                                afterStreamDecision);

                            if (!PolicyOps.IsSuccess(
                                    afterStreamCode, afterStreamDecision))
                            {
                                canRetry = false;

                                if (afterStreamPolicyResult != null)
                                {
                                    error = afterStreamPolicyResult;
                                }
                                else
                                {
                                    error = String.Format(
                                        "script {0} cannot be returned, denied by policy",
                                        FormatOps.DisplayName(name));
                                }

                                code = ReturnCode.Error;
                                return code;
                            }
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        readScriptClientData = localReadScriptClientData;

#if NOTIFY
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Stream, NotifyFlags.Read,
                                new ObjectList(charCallback, charsCallback,
                                    startIndex, characters, readScriptClientData),
                                interpreter, null, null, null, ref error);
                        }
#endif

                        canRetry = false; /* SUCCESS? */
                        return code;
                    }
                    else
                    {
                        //
                        // NOTE: We did not read the right number
                        //       of characters (which they specified
                        //       exactly instead of using -1), this
                        //       is considered an error.
                        //
                        canRetry = false;

                        error = String.Format(
                            "unexpected end-of-stream, read {0} " +
                            "characters, wanted {1} characters, " +
                            "result discarded", read, characters);

                        code = ReturnCode.Error;
                        return code;
                    }
                }
                else
                {
                    //
                    // NOTE: Use zero characters?  Surely.
                    //
                    string originalText = String.Empty;
                    string localText = originalText;

                    ///////////////////////////////////////////////////////////

                    #region Policy Checking: "After Stream"
                    //
                    // HACK: The "script" parameter should only be non-null
                    //       when being called from the EvaluateScript method
                    //       overload that accepts an IScript.  In that case,
                    //       there is no underlying file, so all those policy
                    //       checks must be skipped.  Similarly, this "name"
                    //       parameter check here relies upon the fact that
                    //       (eventually) the ExtractContextAndFileName helper
                    //       method will always fail when a file name is null;
                    //       therefore, there is (almost) no point in checking
                    //       a stream policy for those cases.  Hence, a null
                    //       "name" parameter is used to indiate that a script
                    //       being read has no associated file name, e.g. from
                    //       a memory stream, etc.
                    //
                    ReadScriptClientData localReadScriptClientData =
                        new ReadScriptClientData(name, originalText, localText,
                            null);

                    if ((interpreter != null) &&
                        (script == null) && (name != null) &&
                        !EngineFlagOps.HasNoPolicy(engineFlags))
                    {
                        afterStreamDecision = interpreter.StreamInitialDecision;

                        afterStreamCode = interpreter.CheckAfterStreamPolicies(
                            PolicyFlags.EngineAfterStream, name,
                            originalText, null, null,
                            ref afterStreamDecision,
                            ref afterStreamPolicyResult);

                        interpreter.StreamFinalDecision = PolicyOps.FinalDecision(
                            PolicyFlags.EngineAfterStream, afterStreamCode,
                            afterStreamDecision);

                        if (!PolicyOps.IsSuccess(
                                afterStreamCode, afterStreamDecision))
                        {
                            canRetry = false;

                            if (afterStreamPolicyResult != null)
                            {
                                error = afterStreamPolicyResult;
                            }
                            else
                            {
                                error = String.Format(
                                    "script {0} cannot be returned, denied by policy",
                                    FormatOps.DisplayName(name));
                            }

                            code = ReturnCode.Error;
                            return code;
                        }
                    }
                    #endregion

                    ///////////////////////////////////////////////////////////

                    readScriptClientData = localReadScriptClientData;

#if NOTIFY
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Stream, NotifyFlags.Read,
                            new ObjectList(charCallback, charsCallback,
                                startIndex, characters, readScriptClientData),
                            interpreter, null, null, null, ref error);
                    }
#endif

                    canRetry = false; /* SUCCESS? */
                    return code;
                }
            }
            catch (Exception e)
            {
                error = String.Format(
                    "caught exception reading script stream: {0}",
                    e);

                error.Exception = e;

                SetExceptionErrorCode(interpreter, e);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectList(
                            name, charCallback, charsCallback,
                            startIndex, characters),
                        interpreter, null, null, e, ref error);
                }
#endif

                canRetry = false;
                code = ReturnCode.Error;

                return code;
            }
            finally
            {
#if POLICY_TRACE
                TraceOps.MaybeEmitPolicyTrace(
                    "ReadScriptStream", interpreter, "name", name,
                    "engineFlags", engineFlags, "substitutionFlags",
                    substitutionFlags, "eventFlags", eventFlags,
                    "expressionFlags", expressionFlags,
                    "readScriptClientData", readScriptClientData,
                    "beforeStreamCode", beforeStreamCode,
                    "beforeStreamDecision", beforeStreamDecision,
                    "beforeStreamPolicyResult", beforeStreamPolicyResult,
                    "beforeScriptCode", beforeScriptCode,
                    "beforeScriptDecision", beforeScriptDecision,
                    "beforeScriptPolicyResult", beforeScriptPolicyResult,
                    "afterStreamCode", afterStreamCode,
                    "afterStreamDecision", afterStreamDecision,
                    "afterStreamPolicyResult", afterStreamPolicyResult,
                    "code", code, "canRetry", canRetry, "error", error);
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Script (File) Methods
        internal static Encoding GetEncoding(
            string fileName,
            EncodingType type,
            bool? remoteUri
            )
        {
            int preambleSize = 0;

            return GetEncoding(
                fileName, type, remoteUri, ref preambleSize);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static Encoding GetEncoding(
            string fileName,
            EncodingType type,
            bool? remoteUri,
            ref int preambleSize
            )
        {
            bool localRemoteUri;

            if (remoteUri != null)
                localRemoteUri = (bool)remoteUri;
            else
                localRemoteUri = PathOps.IsRemoteUri(fileName);

            if (localRemoteUri || !File.Exists(fileName))
                return StringOps.GetEncoding(type);

#if XML
            if (XmlOps.CouldBeDocument(fileName))
            {
                Encoding encoding = null;

                if (XmlOps.GetEncoding(
                        fileName, null, null, false, true,
                        ref encoding) == ReturnCode.Ok)
                {
                    return encoding;
                }
            }
#endif

            int minimumCount = 0;
            int maximumCount = 0;

            /* NO RESULT */
            StringOps.GetPreambleSizes(
                ref minimumCount, ref maximumCount);

            int count = maximumCount;
            byte[] bytes = null;

            while (true)
            {
                if ((count <= 0) || (count < minimumCount))
                    break;

                bytes = FileOps.GetFileBytes(fileName, count);

                if (bytes != null)
                    break;

                count--;
            }

            return StringOps.GuessOrGetEncoding(
                bytes, type, ref preambleSize);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static Stream OpenScriptStream(
            Interpreter interpreter,
            string path,
            EngineFlags engineFlags,
            ref string fullPath,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                Uri uri = null;
                UriKind uriKind = UriKind.RelativeOrAbsolute;

                if (PathOps.TryCreateUri(path, ref uri, ref uriKind))
                {
                    try
                    {
                        //
                        // NOTE: First, try to acquire the stream from the
                        //       interpreter host.
                        //
                        HostStreamFlags hostStreamFlags =
                            HostStreamFlags.EngineScript;

                        Stream stream = null;
                        Result localError = null;

                        if ((interpreter != null) &&
                            (interpreter.GetStream(
                                path, FileMode.Open, FileAccess.Read,
                                ref hostStreamFlags, ref fullPath, ref stream,
                                ref localError) == ReturnCode.Ok))
                        {
                            //
                            // NOTE: Just in case the host returns Ok and a
                            //       null stream, make sure the real error
                            //       message, if any, is given to the caller.
                            //
                            if (stream == null)
                                error = localError;

                            return stream;
                        }
                        //
                        // NOTE: If the URI is relative, always treat it as
                        //       a local file name.
                        //
                        else if ((uriKind == UriKind.Relative) ||
                            PathOps.IsFileUriScheme(uri))
                        {
                            //
                            // NOTE: This file name is local, use a normal
                            //       stream object.
                            //
                            localError = null;

                            if (RuntimeOps.NewStream(
                                    interpreter, path, FileMode.Open,
                                    FileAccess.Read, ref hostStreamFlags, ref fullPath,
                                    ref stream, ref localError) == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Just in case the method returns Ok and
                                //       a null stream, make sure the real error
                                //       message, if any, is given to the caller.
                                //
                                if (stream == null)
                                    error = localError;

                                return stream;
                            }
                        }
                        else if (!EngineFlagOps.HasNoRemote(engineFlags))
                        {
#if NETWORK
#if TEST
                            if (EngineFlagOps.HasSetSecurityProtocol(engineFlags))
                            {
                                localError = null;

                                if (WebOps.SetSecurityProtocol(
                                        false, ref localError) != ReturnCode.Ok)
                                {
                                    error = localError;
                                    return null;
                                }
                            }
#endif

                            //
                            // NOTE: This file name is remote, use the
                            //       standard web client object to open a
                            //       stream on it.
                            //
                            localError = null;

                            using (WebClient webClient = WebOps.CreateClient(
                                    interpreter, "OpenScriptStream", null,
                                    WebOps.GetTimeout(interpreter),
                                    ref localError))
                            {
                                if (webClient != null)
                                    return webClient.OpenRead(uri);
                                else if (localError != null)
                                    error = localError;
                                else
                                    error = "could not create web client";
                            }
#else
                            error = "remote uri not supported";
#endif
                        }
                        else
                        {
                            error = "remote uri not allowed";
                        }
                    }
                    catch (Exception e)
                    {
                        error = String.Format(
                            "caught exception getting script stream: {0}",
                            e);

                        error.Exception = e;

                        SetExceptionErrorCode(interpreter, e);

#if NOTIFY
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Engine, NotifyFlags.Exception,
                                new ObjectList(path, uri, uriKind, fullPath),
                                interpreter, null, null, e, ref error);
                        }
#endif
                    }
                }
                else
                {
                    error = String.Format(
                        "invalid uri {0}", FormatOps.WrapOrNull(uri));
                }
            }
            else
            {
                error = "invalid path";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadScriptFile(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            ref string text,         /* out */
            ref Result error         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            if (ReadScriptFile(
                    interpreter, null, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref error) == ReturnCode.Ok)
            {
                text = readScriptClientData.Text;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadScriptFile(
            Interpreter interpreter,    /* in */
            string fileName,            /* in */
            ref IClientData clientData, /* in, out */
            ref string text,            /* out */
            ref Result error            /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadScriptClientData readScriptClientData =
                clientData as ReadScriptClientData;

            bool canRetry = false; /* NOT USED */

            if (ReadScriptFile(
                    interpreter, null, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                text = readScriptClientData.Text;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadScriptFile(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            EngineFlags engineFlags, /* in */
            ref string text,         /* out */
            ref Result error         /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags |= interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            if (ReadScriptFile(
                    interpreter, null, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref error) == ReturnCode.Ok)
            {
                text = readScriptClientData.Text;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadScriptFile(
            Interpreter interpreter,    /* in */
            string fileName,            /* in */
            EngineFlags engineFlags,    /* in */
            ref IClientData clientData, /* in, out */
            ref string text,            /* out */
            ref Result error            /* out */
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags |= interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadScriptClientData readScriptClientData =
                clientData as ReadScriptClientData;

            bool canRetry = false; /* NOT USED */

            if (ReadScriptFile(
                    interpreter, null, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                text = readScriptClientData.Text;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadScriptFile(
            Interpreter interpreter,                       /* in */
            Encoding encoding,                             /* in */
            string fileName,                               /* in */
            ref EngineFlags engineFlags,                   /* in, out */
            ref SubstitutionFlags substitutionFlags,       /* in, out */
            ref EventFlags eventFlags,                     /* in, out */
            ref ExpressionFlags expressionFlags,           /* in, out */
            ref ReadScriptClientData readScriptClientData, /* out */
            ref bool canRetry,                             /* out */
            ref Result error                               /* out */
            ) /* THREAD-SAFE */
        {
            string localFileName = fileName;

            if (String.IsNullOrEmpty(localFileName))
            {
                canRetry = true;
                error = "invalid file name";

                return ReturnCode.Error;
            }

            //
            // NOTE: Check if the file name is really a remote URI.
            //       Also, the file name may be changed as a result
            //       of environment and tilde substitution.
            //
            bool remoteUri = false;

            localFileName = PathOps.SubstituteOrResolvePath(
                interpreter, localFileName, false, ref remoteUri);

            //
            // NOTE: The file name may have changed.  Make sure it
            //       is still not null or an empty string.
            //
            if (String.IsNullOrEmpty(localFileName))
            {
                canRetry = true;
                error = "invalid file name";

                return ReturnCode.Error;
            }

            //
            // NOTE: The file name must refer to an existing local
            //       file -OR- it must be a remote URI.
            //
            if (!remoteUri && !File.Exists(localFileName))
            {
                canRetry = true;

                error = String.Format(
                    "couldn't read file {0}: no such file or directory",
                    FormatOps.DisplayName(localFileName));

                return ReturnCode.Error;
            }

            //
            // NOTE: If necessary, attempt to guess the encoding based
            //       on the first X bytes of the (local) file name,
            //       which may contain a byte order mark; otherwise,
            //       fallback to the default script encoding.
            //
            if (encoding == null)
            {
                encoding = GetEncoding(
                    localFileName, EncodingType.Script, remoteUri);
            }

            //
            // NOTE: At this point, there must be an encoding of some
            //       kind.
            //
            if (encoding == null)
            {
                canRetry = true;
                error = "script encoding not available";

                return ReturnCode.Error;
            }

            //
            // NOTE: This is the overall result of this method.  It is
            //       used to allow the finally block to detect success.
            //
            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: If we have an interpreter context, check to make
            //       sure this file can be read (and then evaluated,
            //       presumably) according to any script file policies
            //       that may be active.
            //
            ReturnCode beforeFileCode = ReturnCode.Ok;
            ReturnCode beforeScriptCode = ReturnCode.Ok;
            ReturnCode afterFileCode = ReturnCode.Ok;

            PolicyDecision beforeFileDecision = PolicyDecision.None;
            PolicyDecision beforeScriptDecision = PolicyDecision.None;
            PolicyDecision afterFileDecision = PolicyDecision.None;

            Result beforeFilePolicyResult = null;
            Result beforeScriptPolicyResult = null;
            Result afterFilePolicyResult = null;

            try
            {
                #region Policy Checking: "Before File"
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoPolicy(engineFlags))
                {
                    beforeFileDecision = interpreter.FileInitialDecision;

                    beforeFileCode = interpreter.CheckBeforeFilePolicies(
                        PolicyFlags.EngineBeforeFile, localFileName,
                        encoding, null, ref beforeFileDecision,
                        ref beforeFilePolicyResult);

                    interpreter.FileFinalDecision = PolicyOps.FinalDecision(
                        PolicyFlags.EngineBeforeFile, beforeFileCode,
                        beforeFileDecision);

                    if (!PolicyOps.IsSuccess(
                            beforeFileCode, beforeFileDecision))
                    {
                        canRetry = false;

                        if (beforeFilePolicyResult != null)
                        {
                            error = beforeFilePolicyResult;
                        }
                        else
                        {
                            error = String.Format(
                                "script file {0} cannot be read, denied by policy",
                                FormatOps.DisplayName(localFileName));
                        }

                        code = ReturnCode.Error;
                        return code;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Open the stream object for this "file" (which may
                //       actually be a web URI).  If the resulting stream
                //       object is null, the error argument will contain a
                //       reason why the open operation failed.
                //
                using (Stream stream = OpenScriptStream(
                        interpreter, localFileName, engineFlags,
                        ref localFileName, ref error))
                {
                    if (stream == null)
                    {
                        canRetry = false;
                        code = ReturnCode.Error;

                        return code;
                    }

                    //
                    // NOTE: Create stream reader for the stream we just
                    //       opened in order to read characters from it.
                    //
                    using (StreamReader streamReader = new StreamReader(
                            stream, encoding))
                    {
                        #region Optional Stream Length Detection
                        long streamLength = GetStreamLength(streamReader);
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Optional Post-Script Bytes Setup
                        //
                        // NOTE: Starting with the engine flags specified by
                        //       the caller, possibly modify them to include
                        //       the "ForceSoftEof" flag.  This must be done
                        //       if the "PostScriptBytes" flag is set, so we
                        //       can be at the correct position within the
                        //       stream to read the post-script bytes.
                        //
                        EngineFlags readEngineFlags = engineFlags;

                        bool postScriptBytes =
                            EngineFlagOps.HasPostScriptBytes(readEngineFlags);

                        if (postScriptBytes)
                            readEngineFlags |= EngineFlags.ForceSoftEof;
                        #endregion

                        ///////////////////////////////////////////////////////

                        ReadInt32Callback charCallback = null; /* REUSED */

                        GetStreamCallback(streamReader, ref charCallback);

                        ///////////////////////////////////////////////////////

                        #region Script Read Operation
                        //
                        // NOTE: Get both the whole buffers as strings
                        //       (i.e. both the original and line-ending
                        //       modified ones).
                        //
                        string localOriginalText = null;
                        string localText = null;
                        int preSoftEofLength;

                        ReadScriptVia(
                            charCallback, streamLength,
                            readEngineFlags, ref localOriginalText,
                            ref localText, out preSoftEofLength);
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Optional Post-Script Bytes Read Operation
                        //
                        // NOTE: The post-script bytes occur after the soft
                        //       end-of-file.  They are optionally read, by
                        //       request of the caller, and will be sent to
                        //       the "After File" policy check (below).
                        //
                        ByteList localBytes = null;

                        if (postScriptBytes)
                        {
                            //
                            // HACK: This method relies upon internals of the
                            //       .NET Framework (or Mono / .NET Core); it
                            //       is being used because there is no other
                            //       nice way to accomplish this task: using
                            //       a stream reader to obtain the raw bytes
                            //       already buffered by it when reading an
                            //       entire stream.  The other ways of doing
                            //       this would be less efficient.
                            //
                            if (!FileOps.TryGrabByteBuffer(
                                    streamReader, ref localBytes, ref error))
                            {
                                canRetry = false;
                                code = ReturnCode.Error;

                                return code;
                            }

                            //
                            // HACK: This call mutates the underlying buffer
                            //       grabbed from the stream reader to remove
                            //       all content except the post-script bytes
                            //       from it.  If soft end-of-file is absent,
                            //       this will do nothing.
                            //
                            // BUGBUG: Perhaps if the soft end-of-file is not
                            //         found, this should completely clear the
                            //         buffer?  Since these bytes are really
                            //         only used for policy checking, and end
                            //         up changing the cryptographic hash, the
                            //         current handling is probably fine.
                            //
                            /* NO RESULT */
                            MaybeRemoveNonPostScriptBytes(ref localBytes);

                            using (BinaryReader binaryReader = new BinaryReader(
                                    stream, encoding))
                            {
                                charCallback = null;

                                ReadBytesCallback bytesCallback = null;

                                GetStreamCallbacks(
                                    binaryReader, ref charCallback,
                                    ref bytesCallback);

                                ReadPostScriptBytes(
                                    charCallback, bytesCallback,
                                    Length.Invalid,
                                    EngineFlagOps.HasSeekSoftEof(
                                        readEngineFlags),
                                    ref localBytes);
                            }
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Optional Script Xml Handling
#if XML
                        //
                        // NOTE: Are we allowed to see if it is actually XML?
                        //       Check and see if the script text looks like
                        //       XML script document unless prevented from
                        //       doing so by our caller.
                        //
                        if (!EngineFlagOps.HasNoXml(engineFlags) &&
                            XmlOps.LooksLikeDocument(localOriginalText))
                        {
                            XmlErrorTypes retryXml = XmlErrorTypes.None;
                            bool validateXml = false;
                            bool relaxedXml = false;
                            bool allXml = false;

                            if (interpreter != null)
                            {
                                interpreter.QueryXmlProperties(
                                    ref retryXml, ref validateXml,
                                    ref relaxedXml, ref allXml);
                            }

                            code = ReadScriptXml(
                                interpreter, encoding, GetScriptXml(
                                    localOriginalText, preSoftEofLength),
                                retryXml, validateXml, relaxedXml,
                                allXml, ref engineFlags,
                                ref substitutionFlags, ref eventFlags,
                                ref expressionFlags, ref localText,
                                ref canRetry, ref error);

                            if (code != ReturnCode.Ok)
                                return code;
                        }
#endif
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Policy Checking: "Before Script"
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoPolicy(engineFlags) &&
                            EngineFlagOps.HasExternalScript(engineFlags))
                        {
                            //
                            // NOTE: Attempt to create a "stream-based" script
                            //       object for use by the policy engine.
                            //
                            IScript script;
                            Result scriptCreateError = null;

                            script = Script.CreateForPolicy(
                                localFileName, ScriptTypes.File,
                                localOriginalText, engineFlags,
                                substitutionFlags, eventFlags,
                                expressionFlags, ref scriptCreateError);

                            if (script == null)
                            {
                                canRetry = false;

                                if (scriptCreateError != null)
                                    error = scriptCreateError;
                                else
                                    error = "could not create script for policy";

                                code = ReturnCode.Error;
                                return code;
                            }

                            //
                            // HACK: *SECURITY* Due to its use by the policy
                            //       engine, this IScript cannot be modified.
                            //
                            script.MakeImmutable();

                            beforeScriptDecision = interpreter.ScriptInitialDecision;

                            beforeScriptCode = interpreter.CheckScriptPolicies(
                                PolicyFlags.EngineBeforeScript, script,
                                null, null, ref beforeScriptDecision,
                                ref beforeScriptPolicyResult);

                            interpreter.ScriptFinalDecision = PolicyOps.FinalDecision(
                                PolicyFlags.EngineBeforeScript, beforeScriptCode,
                                beforeScriptDecision);

                            if (!PolicyOps.IsSuccess(
                                    beforeScriptCode, beforeScriptDecision))
                            {
                                canRetry = false;

                                if (beforeScriptPolicyResult != null)
                                {
                                    error = beforeScriptPolicyResult;
                                }
                                else
                                {
                                    error = String.Format(
                                        "script {0} cannot be used, denied by policy",
                                        FormatOps.WrapOrNull(EntityOps.GetId(script)));
                                }

                                code = ReturnCode.Error;
                                return code;
                            }
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Policy Checking: "After File"
                        //
                        // NOTE: Did we succeed in post-processing the text,
                        //       if necessary?
                        //
                        ReadScriptClientData localReadScriptClientData =
                            new ReadScriptClientData(localFileName,
                                localOriginalText, localText, localBytes);

                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoPolicy(engineFlags))
                        {
                            afterFileDecision = interpreter.FileInitialDecision;

                            afterFileCode = interpreter.CheckAfterFilePolicies(
                                PolicyFlags.EngineAfterFile,
                                localFileName, localOriginalText,
                                encoding, localReadScriptClientData,
                                ref afterFileDecision,
                                ref afterFilePolicyResult);

                            interpreter.FileFinalDecision = PolicyOps.FinalDecision(
                                PolicyFlags.EngineAfterFile, afterFileCode,
                                afterFileDecision);

                            if (!PolicyOps.IsSuccess(
                                    afterFileCode, afterFileDecision))
                            {
                                canRetry = false;

                                if (afterFilePolicyResult != null)
                                {
                                    error = afterFilePolicyResult;
                                }
                                else
                                {
                                    error = String.Format(
                                        "script file {0} cannot be read, denied by policy",
                                        FormatOps.DisplayName(localFileName));
                                }

                                code = ReturnCode.Error;
                                return code;
                            }
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        readScriptClientData = localReadScriptClientData;

#if NOTIFY
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.File, NotifyFlags.Read,
                                new ObjectList(
                                    encoding, localReadScriptClientData),
                                interpreter, null, null, null, ref error);
                        }
#endif

                        canRetry = false; /* SUCCESS? */
                        return code;
                    }
                }
            }
            catch (Exception e)
            {
                error = String.Format(
                    "caught exception reading script file: {0}",
                    e);

                error.Exception = e;

                SetExceptionErrorCode(interpreter, e);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectPair(encoding, fileName), interpreter,
                        null, null, e, ref error);
                }
#endif

                canRetry = false;
                code = ReturnCode.Error;

                return code;
            }
            finally
            {
#if POLICY_TRACE
                TraceOps.MaybeEmitPolicyTrace(
                    "ReadScriptFile", interpreter, "encoding", encoding,
                    "fileName", fileName, "engineFlags", engineFlags,
                    "substitutionFlags", substitutionFlags, "eventFlags",
                    eventFlags, "expressionFlags", expressionFlags,
                    "readScriptClientData", readScriptClientData,
                    "localFileName", localFileName, "beforeFileCode",
                    beforeFileCode, "beforeFileDecision", beforeFileDecision,
                    "beforeFilePolicyResult", beforeFilePolicyResult,
                    "beforeScriptCode", beforeScriptCode,
                    "beforeScriptDecision", beforeScriptDecision,
                    "beforeScriptPolicyResult", beforeScriptPolicyResult,
                    "afterFileCode", afterFileCode, "afterFileDecision",
                    afterFileDecision, "afterFilePolicyResult",
                    afterFilePolicyResult, "code", code, "canRetry", canRetry,
                    "error", error);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static string GetReadScriptFileOriginalText(
            IClientData clientData /* in */
            )
        {
            if (clientData == null)
                return null;

            ReadScriptClientData readScriptClientData =
                clientData as ReadScriptClientData;

            if (readScriptClientData == null)
                return null;

            return readScriptClientData.OriginalText;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetScriptFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            ref ScriptFlags scriptFlags,
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref ReadScriptClientData readScriptClientData,
            ref bool canRetry,
            ref ResultList errors
            ) /* THREAD-SAFE */
        {
            bool silent = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.Silent, true);

            if (interpreter == null)
            {
                if (!silent)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid interpreter");
                }

                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(fileName))
            {
                if (!silent)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid file name");
                }

                return ReturnCode.Error;
            }

            //
            // NOTE: First, try for the exact file name given
            //       to us by our caller.
            //
            StringList names = new StringList(new string[] {
                fileName
            });

            //
            // BUGFIX: By default, when presented with fully
            //         qualified (i.e. absolute?) paths by
            //         our caller, do not attempt to search
            //         for alternate file names.
            //
            if (EngineFlagOps.HasIgnoreRootedFileName(engineFlags) ||
                !Path.IsPathRooted(fileName))
            {
                //
                // NOTE: If the file name is qualified with
                //       a directory name, try removing it.
                //       The caller can block this behavior
                //       by specifying the NoFileNameOnly
                //       engine flag.
                //
                if (!EngineFlagOps.HasNoFileNameOnly(engineFlags) &&
                    PathOps.HasDirectory(fileName))
                {
                    names.Add(Path.GetFileName(fileName));
                }

                //
                // NOTE: If the file name has an extension,
                //       try removing it.  The caller can
                //       block this behavior by specifying
                //       the NoRawName engine flag.
                //
                if (!EngineFlagOps.HasNoRawName(engineFlags) &&
                    PathOps.HasExtension(fileName))
                {
                    names.Add(Path.GetFileNameWithoutExtension(
                        fileName));
                }
            }

            //
            // NOTE: Try each candidate "file" name until
            //       we are able to get the script or we
            //       run out of options.
            //
            foreach (string name in names)
            {
                IClientData clientData = ClientData.Empty;
                Result localResult = null;

                if (interpreter.GetScript(
                        name, ref scriptFlags, ref clientData,
                        ref localResult) == ReturnCode.Ok)
                {
                    if (FlagOps.HasFlags(
                            scriptFlags, ScriptFlags.File, true))
                    {
                        string localFileName = localResult;
                        ReadScriptClientData localReadScriptClientData = null;
                        bool localCanRetry = false;

                        if (ReadScriptFile(
                                interpreter, encoding, localFileName,
                                ref engineFlags, ref substitutionFlags,
                                ref eventFlags, ref expressionFlags,
                                ref localReadScriptClientData,
                                ref localCanRetry,
                                ref localResult) == ReturnCode.Ok)
                        {
                            readScriptClientData = localReadScriptClientData;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (!silent)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localResult);
                            }

                            if (!localCanRetry)
                            {
                                canRetry = localCanRetry;
                                return ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        readScriptClientData = new ReadScriptClientData(
                            clientData, name, localResult, localResult,
                            null);

                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    if (!silent)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }
                }
            }

            //
            // NOTE: If this point is reached, the search
            //       failed to find any appropriate script
            //       content.
            //
            if (!silent)
            {
                if (errors == null)
                    errors = new ResultList();

                //
                // NOTE: Insert primary error message at
                //       the start of the list.
                //
                errors.Insert(0, String.Format(
                    "couldn't get file {0}: no such file or directory",
                    FormatOps.DisplayName(fileName)));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadOrGetScriptFile(
            Interpreter interpreter,
            Encoding encoding,
            ref string fileName,
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref string text,
            ref Result error
            ) /* THREAD-SAFE */
        {
            string originalText = null; /* NOT USED */

            return ReadOrGetScriptFile(
                interpreter, encoding, ref fileName, ref engineFlags,
                ref substitutionFlags, ref eventFlags, ref expressionFlags,
                ref originalText, ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadOrGetScriptFile(
            Interpreter interpreter,
            Encoding encoding,
            ref string fileName,
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref string originalText,
            ref string text,
            ref Result error
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ScriptFlags scriptFlags = ScriptOps.GetFlags(
                interpreter, interpreter.ScriptFlags, true, false);

            return ReadOrGetScriptFile(
                interpreter, encoding, ref scriptFlags, ref fileName,
                ref engineFlags, ref substitutionFlags, ref eventFlags,
                ref expressionFlags, ref originalText, ref text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadOrGetScriptFile(
            Interpreter interpreter,
            Encoding encoding,
            ref ScriptFlags scriptFlags,
            ref string fileName,
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref string text,
            ref Result error
            ) /* THREAD-SAFE */
        {
            string originalText = null; /* NOT USED */

            return ReadOrGetScriptFile(
                interpreter, encoding, ref scriptFlags, ref fileName, ref engineFlags,
                ref substitutionFlags, ref eventFlags, ref expressionFlags, ref originalText,
                ref text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ReadOrGetScriptFile(
            Interpreter interpreter,
            Encoding encoding,
            ref ScriptFlags scriptFlags,
            ref string fileName,
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref string originalText,
            ref string text,
            ref Result error
            ) /* THREAD-SAFE */
        {
            ReadScriptClientData readScriptClientData = null;
            ResultList errors = null;
            bool canRetry = false;
            Result localError = null;

            //
            // NOTE: First, try to read the file directly (either from a
            //       local file or from a remote host).
            //
            if (ReadScriptFile(
                    interpreter, encoding, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref canRetry, ref localError) == ReturnCode.Ok)
            {
                fileName = readScriptClientData.FileName;
                originalText = readScriptClientData.OriginalText;
                text = readScriptClientData.Text;

                return ReturnCode.Ok;
            }
            //
            // NOTE: *SECURITY* Block remote-to-local transition.  Also,
            //       block any script denied by policy.
            //
            else if (canRetry &&
                !EngineFlagOps.HasNoHost(engineFlags) &&
                !String.IsNullOrEmpty(fileName) &&
                !PathOps.IsRemoteUri(fileName))
            {
                //
                // NOTE: Now, try to get the script file by querying the
                //       interpreter host for it.
                //
                if (GetScriptFile(
                        interpreter, encoding, fileName,
                        ref scriptFlags, ref engineFlags,
                        ref substitutionFlags, ref eventFlags,
                        ref expressionFlags, ref readScriptClientData,
                        ref canRetry, ref errors) == ReturnCode.Ok)
                {
                    fileName = readScriptClientData.FileName;
                    originalText = readScriptClientData.OriginalText;
                    text = readScriptClientData.Text;

                    return ReturnCode.Ok;
                }
            }

            //
            // NOTE: Build the most complete error message we can for the
            //       caller.
            //
            if (EngineFlagOps.HasAllErrors(engineFlags) &&
                (errors != null))
            {
                if (localError != null)
                    errors.Insert(0, localError);

                error = errors;
            }
            else if (EngineFlagOps.HasNoDefaultError(engineFlags) &&
                (localError != null))
            {
                error = localError;
            }
            else
            {
                //
                // NOTE: Nobody gave us an error message -OR- we are not
                //       allowed to use it?  Ok, use the default one.
                //
                error = String.Format(
                    "couldn't read or get file {0}: no such file or directory",
                    FormatOps.DisplayName(fileName));
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Read Script (Bytes) Methods
        internal static ReturnCode ReadScriptBytes(
            Interpreter interpreter, /* in */
            string name,             /* in */
            byte[] bytes,            /* in */
            ref string text,         /* out */
            ref Result error         /* out */
            )
        {
            IClientData clientData = null;

            return ReadScriptBytes(
                interpreter, name, bytes, ref clientData, ref text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For now, this event is private only; however, it may
        //       eventually be exposed.
        //
        private static ReturnCode ReadScriptBytes(
            Interpreter interpreter,    /* in */
            string name,                /* in */
            byte[] bytes,               /* in */
            ref IClientData clientData, /* in, out */
            ref string text,            /* out */
            ref Result error            /* out */
            )
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            ReadScriptClientData readScriptClientData =
                clientData as ReadScriptClientData;

            bool canRetry = false; /* NOT USED */

            if (ReadScriptBytes(interpreter,
                    null, null, name, bytes, 0, Count.Invalid,
                    ref engineFlags, ref substitutionFlags,
                    ref eventFlags, ref expressionFlags,
                    ref readScriptClientData, ref canRetry,
                    ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                text = readScriptClientData.Text;

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadScriptBytes(
            Interpreter interpreter,                       /* in */
            IScript script,                                /* in */
            Encoding encoding,                             /* in */
            string name,                                   /* in */
            byte[] bytes,                                  /* in */
            int startIndex,                                /* in */
            int characters,                                /* in */
            ref EngineFlags engineFlags,                   /* in, out */
            ref SubstitutionFlags substitutionFlags,       /* in, out */
            ref EventFlags eventFlags,                     /* in, out */
            ref ExpressionFlags expressionFlags,           /* in, out */
            ref ReadScriptClientData readScriptClientData, /* in, out */
            ref bool canRetry,                             /* out */
            ref Result error                               /* out */
            ) /* THREAD-SAFE */
        {
            if (bytes == null)
            {
                error = "invalid bytes";
                return ReturnCode.Error;
            }

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                Encoding bytesEncoding = (encoding != null) ?
                    encoding : StringOps.GuessOrGetEncoding(
                        bytes, EncodingType.Script);

                if (bytesEncoding == null)
                {
                    error = "script encoding not available";
                    return ReturnCode.Error;
                }

                using (StreamReader streamReader = new StreamReader(
                        stream, bytesEncoding))
                {
                    ReadInt32Callback charCallback = null;
                    ReadCharsCallback charsCallback = null;

                    GetStreamCallbacks(
                        streamReader, ref charCallback, ref charsCallback);

                    return ReadScriptStream(
                        interpreter, script, name, charCallback,
                        charsCallback, startIndex, characters,
                        ref engineFlags, ref substitutionFlags,
                        ref eventFlags, ref expressionFlags,
                        ref readScriptClientData, ref canRetry,
                        ref error);
                }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Execution Methods
        #region ClientData Methods
        internal static IClientData GetClientData(
            Interpreter interpreter,
            IClientData clientData,
            bool useEmpty
            )
        {
            if (clientData != null)
                return clientData;

            if (interpreter != null)
            {
                IClientData localClientData =
                    interpreter.ContextClientData;

                if (localClientData != null)
                    return localClientData;
            }

            return useEmpty ? ClientData.Empty : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Profiler Methods
#if PROFILER
        private static IProfilerState GetProfilerAndStart(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            IProfilerState profiler = interpreter.Profiler;

            if (profiler != null)
            {
                if (profiler.Disposed)
                    return null; /* IMPOSSIBLE? */

                profiler.Start();
            }

            return profiler;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Result Limit Methods
#if RESULT_LIMITS
        internal static void CheckResultAgainstLimits(
            Interpreter interpreter,
            int length,
            int count,
            ref ReturnCode code,
            ref Result result
            )
        {
            CheckResultAgainstLimits(
                interpreter, length, 0, count, 0, ref code, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void CheckResultAgainstLimits(
            Interpreter interpreter,
            int baseLength,
            int extraLength,
            int baseCount,
            int extraCount,
            ref ReturnCode code,
            ref Result result
            )
        {
            if (interpreter == null)
                return;

            if ((baseLength == 0) && (extraLength == 0))
                return;

            if ((baseCount == 0) && (extraCount == 0))
                return;

            int executeResultLimit;
            int badLength;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                executeResultLimit = interpreter.InternalExecuteResultLimit;

                if (executeResultLimit != 0)
                {
                    int length = baseLength + extraLength;

                    if ((length < 0) || (length > executeResultLimit))
                    {
                        badLength = length;
                        goto error;
                    }

                    int count = baseCount + extraCount;

                    if ((count < 0) || (count > executeResultLimit))
                    {
                        badLength = count; /* HACK: Kinda makes sense. */
                        goto error;
                    }

                    int totalLength = length * count;

                    if ((totalLength < 0) ||
                        (totalLength > executeResultLimit))
                    {
                        badLength = totalLength;
                        goto error;
                    }
                }

                return;
            }

        error:

            result = String.Format(
                "cannot exceed length of {0} characters ({1})",
                executeResultLimit, badLength);

            code = ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void CheckResultAgainstLimits(
            int executeResultLimit,
            ref ReturnCode code,
            ref Result result
            )
        {
            if ((executeResultLimit != 0) && (result != null))
            {
                int length = result.Length;

                if (length > executeResultLimit)
                {
                    result.Reset(ResultFlags.ResetObject);

                    ObjectOps.CollectGarbage(); /* NOTE: Force memory cleanup. */

                    result = String.Format(
                        "maximum result length of {0} characters exceeded ({1})",
                        executeResultLimit, length);

                    code = ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static void CheckResultAgainstLimits(
            int executeResultLimit,
            ref ReturnCode code,
            ref Argument value,
            ref Result error
            )
        {
            if ((executeResultLimit != 0) && (value != null))
            {
                int length = value.Length;

                if (length > executeResultLimit)
                {
                    value.Reset(ArgumentFlags.ResetWithZero);

                    if (error != null)
                        error.Reset(ResultFlags.ResetObject);

                    ObjectOps.CollectGarbage(); /* NOTE: Force memory cleanup. */

                    error = String.Format(
                        "maximum result length of {0} characters exceeded ({1})",
                        executeResultLimit, length);

                    code = ReturnCode.Error;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Execution Statistics Methods
        private static void UpdateStatistics(
            Interpreter interpreter,
            IUsageData usageData,
            EngineFlags engineFlags,
            long microseconds
            )
        {
            //
            // NOTE: Keep track of how many times this particular
            //       entity has been used.
            //
            if ((usageData != null) &&
                !EngineFlagOps.HasNoUsageData(engineFlags))
            {
                if (microseconds != 0)
                {
                    /* IGNORED */
                    usageData.ProfileUsage(ref microseconds);
                }
                else
                {
                    long count = 1;

                    /* IGNORED */
                    usageData.CountUsage(ref count);
                }
            }

            //
            // NOTE: Keep track of how many commands are executed
            //       in this interpreter.
            //
            if (interpreter != null)
            {
                interpreter.IncrementOperationAndCommandCount(
                    usageData is ICommand);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Delegate Execution Methods
        internal static ReturnCode ExecuteDelegate(
            Delegate @delegate,
            object[] args,
            ref object returnValue,
            ref Result error
            )
        {
            try
            {
                returnValue = @delegate.DynamicInvoke(args);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteDelegate(
            Delegate @delegate,
            ArgumentList arguments,
            ref Result result
            )
        {
            object[] args = null;

            if (arguments != null)
            {
                int count = arguments.Count;

                if (count > 0)
                {
                    args = new object[count];

                    for (int index = 0; index < count; index++)
                    {
                        Argument argument = arguments[index];

                        args[index] = (argument != null) ?
                            argument.Value : null;
                    }
                }
            }

            object returnValue = null;

            if (ExecuteDelegate(
                    @delegate, args, ref returnValue,
                    ref result) == ReturnCode.Ok)
            {
                result = Result.FromObject(
                    returnValue, true, false, false);

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Member Execution Methods
        public static ReturnCode ExecuteMember(
            Interpreter interpreter,       /* in */
            Guid? id,                      /* in */
            FrameworkFlags frameworkFlags, /* in */
            BindingFlags bindingFlags,     /* in */
            string memberName,             /* in */
            object[] args,                 /* in */
            ref IClientData clientData,    /* in, out: NOT USED, RESERVED */
            ref Result result              /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            Result localResult = null; /* REUSED */

            if (interpreter.GetFramework(
                    id, frameworkFlags,
                    ref localResult) != ReturnCode.Ok)
            {
                result = localResult;
                return ReturnCode.Error;
            }

            object target;
            Type type;

            if (localResult.Value is Type)
            {
                target = null;
                type = (Type)localResult.Value;
            }
            else
            {
                target = localResult.Value;
                type = target.GetType();
            }

            IBinder binder = interpreter.InternalBinder;
            CultureInfo cultureInfo = interpreter.InternalCultureInfo;

            localResult = null;

            try
            {
                localResult = Result.FromObject(type.InvokeMember(
                    memberName, bindingFlags, binder as Binder,
                    target, args, cultureInfo), true, false,
                    false); /* throw */

                result = localResult;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                result = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Execution Methods
        #region Private IExecute Execution Methods
        private static ReturnCode PrivateExecuteIExecute(
            IExecute execute,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags, /* NOT USED */
            EventFlags eventFlags,
            ExpressionFlags expressionFlags, /* NOT USED */
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (execute != null)
                {
                    //
                    // NOTE: Execute the IExecute in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        //
                        // NOTE: Execute the IExecute in the context of the interpreter.
                        //       Commands executed via this method do NOT increment the
                        //       command count for the interpreter.  This behavior is by
                        //       design.
                        //
                        code = execute.Execute(
                            interpreter, clientData, arguments,
                            ref result); /* throw */
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref result);
#endif

                        UpdateStatistics(
                            interpreter, null, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXECUTE
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.IExecute, NotifyFlags.Executed,
                                new ObjectList(execute, code, engineFlags,
                                substitutionFlags, eventFlags, expressionFlags),
                                interpreter, clientData, arguments, null,
                                ref result);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    result = "invalid execute";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing: ",
                    TracePriority.EngineError);
#endif

                result = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                result = String.Format(
                    "caught exception while executing: {0}",
                    e);

                result.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            execute, interpreter,
                            clientData, arguments, e, ref result);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteIExecute(
            IExecute execute,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

#if DEBUGGER && DEBUGGER_EXECUTE
            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    engineFlags, BreakpointType.BeforeIExecute))
            {
                code = CheckBreakpoints(
                    code, BreakpointType.BeforeIExecute, null,
                    null, null, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, execute, null,
                    interpreter, clientData, arguments,
                    ref result);
            }
#endif

            if (code == ReturnCode.Ok)
            {
                code = PrivateExecuteIExecute(
                    execute, interpreter, clientData, arguments, engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit,
#endif
                    ref usable, ref exception, ref result);

#if DEBUGGER && DEBUGGER_EXECUTE
                if (usable && DebuggerOps.CanHitBreakpoints(interpreter,
                        engineFlags, BreakpointType.AfterIExecute))
                {
                    code = CheckBreakpoints(
                        code, BreakpointType.AfterIExecute, null,
                        null, null, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, execute, null,
                        interpreter, clientData, arguments,
                        ref result);
                }
#endif
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region SubCommand Execution Methods
        #region Private SubCommand Execution Methods
        private static ReturnCode PrivateExecuteSubCommand(
            ISubCommand subCommand,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (subCommand != null)
                {
                    //
                    // NOTE: Execute the sub-command in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        ExecuteCallback callback = subCommand.Callback;

                        if (callback != null)
                        {
                            code = callback(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                        else
                        {
                            code = subCommand.Execute(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref result);
#endif

                        UpdateStatistics(
                            interpreter, subCommand, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXECUTE
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.SubCommand, NotifyFlags.Executed,
                                new ObjectList(subCommand, code, engineFlags,
                                substitutionFlags, eventFlags, expressionFlags),
                                interpreter, clientData, arguments, null,
                                ref result);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    result = "invalid sub-command";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing sub-command: ",
                    TracePriority.EngineError);
#endif

                result = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                result = String.Format(
                    "caught exception while executing sub-command: {0}",
                    e);

                result.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            subCommand, interpreter,
                            clientData, arguments, e, ref result);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteSubCommand(
            ISubCommand subCommand,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

#if DEBUGGER && DEBUGGER_EXECUTE
            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    engineFlags, BreakpointType.BeforeSubCommand))
            {
                code = CheckBreakpoints(
                    code, BreakpointType.BeforeSubCommand, null,
                    null, null, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, subCommand,
                    null, interpreter, clientData, arguments,
                    ref result);
            }
#endif

            if (code == ReturnCode.Ok)
            {
                if ((interpreter != null) && (subCommand.Command == null))
                    interpreter.ReturnCode = ReturnCode.Ok;

                code = PrivateExecuteSubCommand(
                    subCommand, interpreter, clientData, arguments, engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit,
#endif
                    ref usable, ref exception, ref result);

                if (usable)
                {
#if DEBUGGER && DEBUGGER_EXECUTE
                    if (DebuggerOps.CanHitBreakpoints(interpreter,
                            engineFlags, BreakpointType.AfterSubCommand))
                    {
                        code = CheckBreakpoints(
                            code, BreakpointType.AfterSubCommand, null,
                            null, null, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags, subCommand,
                            null, interpreter, clientData, arguments,
                            ref result);
                    }
#endif

                    if ((code == ReturnCode.Return) &&
                        (interpreter != null) && (subCommand.Command == null))
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (!interpreter.InternalIsBusy)
                            {
                                code = UpdateReturnInformation(interpreter);
                            }
                        }
                    }
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Command Execution Methods
        #region Private Command Execution Methods
        private static ReturnCode PrivateExecuteCommand(
            ICommand command,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (command != null)
                {
                    //
                    // NOTE: Execute the command in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        ExecuteCallback callback = command.Callback;

                        if (callback != null)
                        {
                            code = callback(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                        else
                        {
                            code = command.Execute(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref result);
#endif

                        UpdateStatistics(
                            interpreter, command, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXECUTE
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Command, NotifyFlags.Executed,
                                new ObjectList(command, code, engineFlags,
                                substitutionFlags, eventFlags, expressionFlags),
                                interpreter, clientData, arguments, null,
                                ref result);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    result = "invalid command";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing command: ",
                    TracePriority.EngineError);
#endif

                result = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                result = String.Format(
                    "caught exception while executing command: {0}",
                    e);

                result.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            command, interpreter,
                            clientData, arguments, e, ref result);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteCommand(
            ICommand command,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

#if DEBUGGER && DEBUGGER_EXECUTE
            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    engineFlags, BreakpointType.BeforeCommand))
            {
                code = CheckBreakpoints(
                    code, BreakpointType.BeforeCommand, null,
                    null, null, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, command, null,
                    interpreter, clientData, arguments,
                    ref result);
            }
#endif

            if (code == ReturnCode.Ok)
            {
                if (interpreter != null)
                    interpreter.ReturnCode = ReturnCode.Ok;

                code = PrivateExecuteCommand(
                    command, interpreter, clientData, arguments, engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit,
#endif
                    ref usable, ref exception, ref result);

                if (usable)
                {
#if DEBUGGER && DEBUGGER_EXECUTE
                    if (DebuggerOps.CanHitBreakpoints(interpreter,
                            engineFlags, BreakpointType.AfterCommand))
                    {
                        code = CheckBreakpoints(
                            code, BreakpointType.AfterCommand, null,
                            null, null, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags, command, null,
                            interpreter, clientData, arguments,
                            ref result);
                    }
#endif

                    if ((code == ReturnCode.Return) && (interpreter != null))
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (!interpreter.InternalIsBusy)
                            {
                                code = UpdateReturnInformation(interpreter);
                            }
                        }
                    }
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Procedure Execution Methods
        #region Private Procedure Execution Methods
        private static ReturnCode PrivateExecuteProcedure(
            IProcedure procedure,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (procedure != null)
                {
                    //
                    // NOTE: Execute the procedure in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        ExecuteCallback callback = procedure.Callback;

                        if (callback != null)
                        {
                            code = callback(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                        else
                        {
                            code = procedure.Execute(
                                interpreter, clientData, arguments,
                                ref result); /* throw */
                        }
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref result);
#endif

                        UpdateStatistics(
                            interpreter, procedure, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXECUTE
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Procedure, NotifyFlags.Executed,
                                new ObjectList(procedure, code, engineFlags,
                                substitutionFlags, eventFlags, expressionFlags),
                                interpreter, clientData, arguments, null,
                                ref result);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    result = "invalid procedure";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing procedure: ",
                    TracePriority.EngineError);
#endif

                result = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                result = String.Format(
                    "caught exception while executing procedure: {0}",
                    e);

                result.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            procedure, interpreter,
                            clientData, arguments, e, ref result);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteProcedure(
            IProcedure procedure,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

#if DEBUGGER && DEBUGGER_EXECUTE
            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    engineFlags, BreakpointType.BeforeProcedure))
            {
                code = CheckBreakpoints(
                    code, BreakpointType.BeforeProcedure, null,
                    null, null, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, procedure,
                    null, interpreter, clientData, arguments,
                    ref result);
            }
#endif

            if (code == ReturnCode.Ok)
            {
                code = PrivateExecuteProcedure(
                    procedure, interpreter, clientData, arguments, engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit,
#endif
                    ref usable, ref exception, ref result);

#if DEBUGGER && DEBUGGER_EXECUTE
                if (usable && DebuggerOps.CanHitBreakpoints(interpreter,
                        engineFlags, BreakpointType.AfterProcedure))
                {
                    code = CheckBreakpoints(
                        code, BreakpointType.AfterProcedure, null,
                        null, null, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, procedure,
                        null, interpreter, clientData, arguments,
                        ref result);
                }
#endif
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Function Execution Methods
        #region Private Function Execution Methods
        private static ReturnCode PrivateExecuteFunction(
            IFunction function,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Argument value,
            ref Result error
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (function != null)
                {
                    //
                    // NOTE: Execute the function in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        code = function.Execute(
                            interpreter, clientData, arguments,
                            ref value, ref error); /* throw */
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref value, ref error);
#endif

                        UpdateStatistics(
                            interpreter, function, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXPRESSION
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Function, NotifyFlags.Executed,
                                new ObjectList(function, code, value,
                                engineFlags, substitutionFlags, eventFlags,
                                expressionFlags), interpreter, clientData,
                                arguments, null, ref error);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    error = "invalid function";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing function: ",
                    TracePriority.EngineError);
#endif

                error = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                error = String.Format(
                    "caught exception while executing function: {0}",
                    e);

                error.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            function, interpreter,
                            clientData, arguments, e, ref error);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Public Function Execution Methods
        internal static ReturnCode ExecuteFunction(
            IFunction function,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (!EntityOps.IsDisabled(function))
            {
                bool isSafe = false;

                if (EngineFlagOps.HasNoSafeFunction(engineFlags) ||
                    EntityOps.IsSafe(function) ||
                    !(isSafe = interpreter.InternalIsSafe()))
                {
#if DEBUGGER && DEBUGGER_EXPRESSION
                    if (DebuggerOps.CanHitBreakpoints(interpreter,
                            engineFlags, BreakpointType.BeforeFunction))
                    {
                        code = CheckBreakpoints(
                            code, BreakpointType.BeforeFunction, null,
                            null, null, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags, null, function,
                            interpreter, clientData, new ArgumentList(
                            arguments, value), ref error);
                    }
#endif

                    if (code == ReturnCode.Ok)
                    {
                        code = PrivateExecuteFunction(
                            function, interpreter, clientData, arguments, engineFlags,
                            substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                            executeResultLimit,
#endif
                            ref usable, ref exception, ref value, ref error);

#if DEBUGGER && DEBUGGER_EXPRESSION
                        if (usable && DebuggerOps.CanHitBreakpoints(interpreter,
                                engineFlags, BreakpointType.AfterFunction))
                        {
                            code = CheckBreakpoints(
                                code, BreakpointType.AfterFunction, null,
                                null, null, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, null, function,
                                interpreter, clientData, new ArgumentList(
                                arguments, value), ref error);
                        }
#endif
                    }
                }
                else
                {
                    error = String.Format(
                        "permission denied: {0}interpreter cannot use function {1}",
                        isSafe ? "safe " : String.Empty, FormatOps.DisplayName(
                        function));

                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "invalid function name {0}",
                    FormatOps.DisplayName(function));

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Operator Execution Methods
        #region Private Operator Execution Methods
        private static ReturnCode PrivateExecuteOperator(
            IOperator @operator,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Argument value,
            ref Result error
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                if (@operator != null)
                {
                    //
                    // NOTE: Execute the operator in the context of the interpreter.
                    //
                    long microseconds = 0;
#if PROFILER
                    IProfilerState profiler = GetProfilerAndStart(interpreter);
#endif
                    ReturnCode code;

                    try
                    {
                        //
                        // NOTE: Execute the operator in the context of the interpreter.
                        //
                        code = @operator.Execute(
                            interpreter, clientData, arguments,
                            ref value, ref error); /* throw */
                    }
                    finally
                    {
                        usable = IsUsableNoLock(interpreter);

#if PROFILER
                        if (IsUsableNoLock(profiler, usable))
                        {
                            microseconds = ConversionOps.ToLong(
                                Math.Round(profiler.Stop()));
                        }
#endif
                    }

                    if (usable)
                    {
#if RESULT_LIMITS
                        CheckResultAgainstLimits(
                            executeResultLimit, ref code, ref value, ref error);
#endif

                        UpdateStatistics(
                            interpreter, @operator, engineFlags,
                            microseconds);

#if NOTIFY && NOTIFY_EXPRESSION
                        if ((interpreter != null) &&
                            !EngineFlagOps.HasNoNotify(engineFlags))
                        {
                            /* IGNORED */
                            interpreter.CheckNotification(
                                NotifyType.Operator, NotifyFlags.Executed,
                                new ObjectList(@operator, code, value,
                                engineFlags, substitutionFlags, eventFlags,
                                expressionFlags), interpreter, clientData,
                                arguments, null, ref error);
                        }
#endif
                    }

                    return code;
                }
                else
                {
                    error = "invalid operator";
                }
            }
#if DEBUG || FORCE_TRACE
            catch (InterpreterDisposedException e)
#else
            catch (InterpreterDisposedException)
#endif
            {
                exception = true;

#if DEBUG || FORCE_TRACE
                TraceOps.DebugTrace(e, typeof(Engine).Name,
                    "interpreter was disposed while executing operator: ",
                    TracePriority.EngineError);
#endif

                error = Result.Copy(
                    InterpreterUnusableError, ResultFlags.CopyValue);
            }
            catch (Exception e)
            {
                exception = true;

                error = String.Format(
                    "caught exception while executing operator: {0}",
                    e);

                error.Exception = e;

                if (usable)
                {
                    SetExceptionErrorCode(interpreter, e);

#if NOTIFY && NOTIFY_EXCEPTION
                    if ((interpreter != null) &&
                        !EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Engine, NotifyFlags.Exception,
                            @operator, interpreter,
                            clientData, arguments, e, ref error);
                    }
#endif
                }
            }
            finally
            {
                /* IGNORED */
                GlobalState.PopActiveInterpreter();
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Public Operator Execution Methods
        internal static ReturnCode ExecuteOperator(
            IOperator @operator,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref bool exception,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (!EntityOps.IsDisabled(@operator))
            {
#if DEBUGGER && DEBUGGER_EXPRESSION
                if (DebuggerOps.CanHitBreakpoints(interpreter,
                        engineFlags, BreakpointType.BeforeOperator))
                {
                    code = CheckBreakpoints(
                        code, BreakpointType.BeforeOperator, null,
                        null, null, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, null, @operator,
                        interpreter, clientData, new ArgumentList(
                        arguments, value), ref error);
                }
#endif

                if (code == ReturnCode.Ok)
                {
                    code = PrivateExecuteOperator(
                        @operator, interpreter, clientData, arguments, engineFlags,
                        substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                        executeResultLimit,
#endif
                        ref usable, ref exception, ref value, ref error);

#if DEBUGGER && DEBUGGER_EXPRESSION
                    if (usable && DebuggerOps.CanHitBreakpoints(interpreter,
                            engineFlags, BreakpointType.AfterOperator))
                    {
                        code = CheckBreakpoints(
                            code, BreakpointType.AfterOperator, null,
                            null, null, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags, null, @operator,
                            interpreter, clientData, new ArgumentList(
                            arguments, value), ref error);
                    }
#endif
                }
            }
            else
            {
                error = String.Format(
                    "invalid operator name {0}",
                    FormatOps.DisplayName(@operator));

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region External Execution Methods
        internal static ReturnCode Execute(
            string name,
            IExecute execute,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref Result result
            )
        {
            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            return Execute(
                name, execute, interpreter,
                GetClientData(
                    interpreter, clientData, false),
                arguments, engineFlags,
                substitutionFlags, eventFlags,
                expressionFlags,
#if RESULT_LIMITS
                executeResultLimit,
#endif
                ref usable, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode Execute(
            string name,
            IExecute execute,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;

            ///////////////////////////////////////////////////////////////////////////////////
            //
            // NOTE: This function is called by the evaluation engine core and by external
            //       callers that have some kind of IExecute compatible object that needs to be
            //       executed.  Any semantic changes that need to be applied to each and every
            //       script command and procedure execution should be done in the function and
            //       ONLY in this function.
            //
            ///////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Did we succeed at finding something to execute?
            //
            if (execute != null)
            {
                if (interpreter != null)
                {
                    ICallFrame peekFrame = null;

                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if (interpreter.CanPeekCallFrame())
                            peekFrame = interpreter.PeekCallFrame();
                    }

                    bool exception = false;

                    GlobalState.PushActiveInterpreter(interpreter);

                    try
                    {
                        //
                        // NOTE: Cooperatively check for any pending asynchronous
                        //       events for this interpreter.  If an asynchronous
                        //       event returns an error, other events are skipped
                        //       and we also skip executing the command.
                        //
                        // WARNING: Please do not add calls to this function after
                        //          any kind of IExecute has been executed (here or
                        //          elsewhere) because the state of the interpreter
                        //          can no longer be relied upon and any processed
                        //          events could potentially mask the real results
                        //          of the IExecute.
                        //
                        code = CheckEvents(
                            interpreter, engineFlags, substitutionFlags,
                            eventFlags, expressionFlags, ref result);

                        //
                        // NOTE: If all the asynchronous events were processed
                        //       successfully (or there were none), attempt to
                        //       execute the command.
                        //
                        if (code == ReturnCode.Ok)
                        {
                            bool isSafe = false; /* REUSED */

                            if (execute is ICommand)
                            {
                                ICommand command = (ICommand)execute;

                                if (!EntityOps.IsDisabled(command))
                                {
                                    bool ignoreHidden = EngineFlagOps.HasIgnoreHidden(engineFlags);
                                    bool invokeHidden = EngineFlagOps.HasInvokeHidden(engineFlags);
                                    bool isHidden = EntityOps.IsHidden(command);

                                    if (ignoreHidden || (invokeHidden == isHidden))
                                    {
                                        code = ExecuteCommand(command, interpreter,
                                            (clientData != null) ? clientData : command.ClientData,
                                            arguments, engineFlags, substitutionFlags, eventFlags,
                                            expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit,
#endif
                                            ref usable, ref exception, ref result);
                                    }
                                    else if (isHidden)
                                    {
                                        //
                                        // NOTE: *POLICY* See if hidden command is allowed to be
                                        //       executed, based on whatever criteria the current
                                        //       policies evaluate.  However, if the interpreter is
                                        //       not "safe", the command was purposely hidden and
                                        //       will not be executed.
                                        //
                                        ReturnCode? commandCode = null;
                                        PolicyDecision commandDecision = interpreter.CommandInitialDecision;
                                        Result commandPolicyResult = null;

                                        if (!EngineFlagOps.HasNoPolicy(engineFlags) &&
                                            (isSafe = interpreter.InternalIsSafe()) &&
                                            ((commandCode = interpreter.CheckCommandPolicies(
                                                PolicyFlags.EngineBeforeCommand, command, arguments, null,
                                                ref commandDecision, ref commandPolicyResult)) == ReturnCode.Ok) &&
                                            PolicyContext.IsApproved(commandDecision))
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeCommand, commandCode,
                                                commandDecision);

                                            code = ExecuteCommand(command, interpreter,
                                                (clientData != null) ? clientData : command.ClientData,
                                                arguments, engineFlags, substitutionFlags, eventFlags,
                                                expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit,
#endif
                                                ref usable, ref exception, ref result);
                                        }
                                        else
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeCommand, commandCode,
                                                commandDecision);

                                            if (commandPolicyResult != null)
                                            {
                                                result = commandPolicyResult;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "permission denied: {0}interpreter cannot use {1}command {2}",
                                                    isSafe ? "safe " : String.Empty, !isSafe && isHidden ? "hidden " :
                                                    String.Empty, FormatOps.DisplayName(command, interpreter, arguments));
                                            }

                                            code = ReturnCode.Error;
                                        }

#if POLICY_TRACE
                                        TraceOps.MaybeEmitPolicyTrace(
                                            "Execute", interpreter, "name", name, "execute",
                                            execute, "clientData", clientData, "arguments",
                                            arguments, "engineFlags", engineFlags,
                                            "substitutionFlags", substitutionFlags, "eventFlags",
                                            eventFlags, "expressionFlags", expressionFlags,
                                            "ignoreHidden", ignoreHidden, "invokeHidden",
                                            invokeHidden, "isHidden", isHidden, "code", code,
                                            "commandDecision", commandDecision,
                                            "commandPolicyResult", commandPolicyResult, "usable",
                                            usable, "exception", exception, "result", result);
#endif
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "command {0} is {1}hidden",
                                            FormatOps.DisplayName(name),
                                            isHidden ? String.Empty : "not ");

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "command {0} is disabled",
                                        FormatOps.DisplayName(name));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (execute is ISubCommand)
                            {
                                ISubCommand subCommand = (ISubCommand)execute;

                                if (!EntityOps.IsDisabled(subCommand))
                                {
                                    bool ignoreHidden = EngineFlagOps.HasIgnoreHidden(engineFlags);
                                    bool invokeHidden = EngineFlagOps.HasInvokeHidden(engineFlags);
                                    bool isHidden = EntityOps.IsHidden(subCommand);

                                    if (ignoreHidden || (invokeHidden == isHidden))
                                    {
                                        code = ExecuteSubCommand(subCommand, interpreter,
                                            (clientData != null) ? clientData : subCommand.ClientData,
                                            arguments, engineFlags, substitutionFlags, eventFlags,
                                            expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit,
#endif
                                            ref usable, ref exception, ref result);
                                    }
                                    else if (isHidden)
                                    {
                                        //
                                        // NOTE: *POLICY* See if hidden sub-command is allowed to be
                                        //       executed, based on whatever criteria the current
                                        //       policies evaluate.  However, if the interpreter is
                                        //       not "safe", the sub-command was purposely hidden and
                                        //       will not be executed.
                                        //
                                        ReturnCode? commandCode = null;
                                        PolicyDecision commandDecision = interpreter.CommandInitialDecision;
                                        Result commandPolicyResult = null;

                                        if (!EngineFlagOps.HasNoPolicy(engineFlags) &&
                                            (isSafe = interpreter.InternalIsSafe()) &&
                                            ((commandCode = interpreter.CheckCommandPolicies(
                                                PolicyFlags.EngineBeforeSubCommand, subCommand, arguments,
                                                null, ref commandDecision, ref commandPolicyResult)) == ReturnCode.Ok) &&
                                            PolicyContext.IsApproved(commandDecision))
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeSubCommand, commandCode,
                                                commandDecision);

                                            code = ExecuteSubCommand(subCommand, interpreter,
                                                (clientData != null) ? clientData : subCommand.ClientData,
                                                arguments, engineFlags, substitutionFlags, eventFlags,
                                                expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit,
#endif
                                                ref usable, ref exception, ref result);
                                        }
                                        else
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeSubCommand, commandCode,
                                                commandDecision);

                                            if (commandPolicyResult != null)
                                            {
                                                result = commandPolicyResult;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "permission denied: {0}interpreter cannot use {1}sub-command {2}",
                                                    isSafe ? "safe " : String.Empty, !isSafe && isHidden ? "hidden " :
                                                    String.Empty, FormatOps.DisplayName(subCommand, interpreter, arguments));
                                            }

                                            code = ReturnCode.Error;
                                        }

#if POLICY_TRACE
                                        TraceOps.MaybeEmitPolicyTrace(
                                            "Execute", interpreter, "name", name, "execute",
                                            execute, "clientData", clientData, "arguments",
                                            arguments, "engineFlags", engineFlags,
                                            "substitutionFlags", substitutionFlags, "eventFlags",
                                            eventFlags, "expressionFlags", expressionFlags,
                                            "ignoreHidden", ignoreHidden, "invokeHidden",
                                            invokeHidden, "isHidden", isHidden, "code", code,
                                            "commandDecision", commandDecision,
                                            "commandPolicyResult", commandPolicyResult, "usable",
                                            usable, "exception", exception, "result", result);
#endif
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "sub-command {0} is {1}hidden",
                                            FormatOps.DisplayName(name),
                                            isHidden ? String.Empty : "not ");

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "sub-command {0} is disabled",
                                        FormatOps.DisplayName(name));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (execute is IProcedure)
                            {
                                IProcedure procedure = (IProcedure)execute;

                                if (!EntityOps.IsDisabled(procedure))
                                {
                                    bool ignoreHidden = EngineFlagOps.HasIgnoreHidden(engineFlags);
                                    bool invokeHidden = EngineFlagOps.HasInvokeHidden(engineFlags);
                                    bool isHidden = EntityOps.IsHidden(procedure);

                                    if (ignoreHidden || (invokeHidden == isHidden))
                                    {
                                        code = ExecuteProcedure(procedure, interpreter,
                                            (clientData != null) ? clientData : procedure.ClientData,
                                            arguments, engineFlags, substitutionFlags, eventFlags,
                                            expressionFlags,
#if RESULT_LIMITS
                                            executeResultLimit,
#endif
                                            ref usable, ref exception, ref result);
                                    }
                                    else if (isHidden)
                                    {
                                        //
                                        // NOTE: *POLICY* See if hidden procedure is allowed to be
                                        //       executed, based on whatever criteria the current
                                        //       policies evaluate.  However, if the interpreter is
                                        //       not "safe", the command was purposely hidden and
                                        //       will not be executed.
                                        //
                                        ReturnCode? commandCode = null;
                                        PolicyDecision commandDecision = interpreter.CommandInitialDecision;
                                        Result commandPolicyResult = null;

                                        if (!EngineFlagOps.HasNoPolicy(engineFlags) &&
                                            (isSafe = interpreter.InternalIsSafe()) &&
                                            ((commandCode = interpreter.CheckCommandPolicies(
                                                PolicyFlags.EngineBeforeProcedure, procedure, arguments,
                                                null, ref commandDecision, ref commandPolicyResult)) == ReturnCode.Ok) &&
                                            PolicyContext.IsApproved(commandDecision))
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeProcedure, commandCode,
                                                commandDecision);

                                            code = ExecuteProcedure(procedure, interpreter,
                                                (clientData != null) ? clientData : procedure.ClientData,
                                                arguments, engineFlags, substitutionFlags, eventFlags,
                                                expressionFlags,
#if RESULT_LIMITS
                                                executeResultLimit,
#endif
                                                ref usable, ref exception, ref result);
                                        }
                                        else
                                        {
                                            interpreter.CommandFinalDecision = PolicyOps.FinalDecision(
                                                PolicyFlags.EngineBeforeProcedure, commandCode,
                                                commandDecision);

                                            if (commandPolicyResult != null)
                                            {
                                                result = commandPolicyResult;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "permission denied: {0}interpreter cannot use {1}procedure {2}",
                                                    isSafe ? "safe " : String.Empty, !isSafe && isHidden ? "hidden " :
                                                    String.Empty, FormatOps.DisplayName(name));
                                            }

                                            code = ReturnCode.Error;
                                        }

#if POLICY_TRACE
                                        TraceOps.MaybeEmitPolicyTrace(
                                            "Execute", interpreter, "name", name, "execute",
                                            execute, "clientData", clientData, "arguments",
                                            arguments, "engineFlags", engineFlags,
                                            "substitutionFlags", substitutionFlags, "eventFlags",
                                            eventFlags, "expressionFlags", expressionFlags,
                                            "ignoreHidden", ignoreHidden, "invokeHidden",
                                            invokeHidden, "isHidden", isHidden, "code", code,
                                            "commandDecision", commandDecision,
                                            "commandPolicyResult", commandPolicyResult, "usable",
                                            usable, "exception", exception, "result", result);
#endif
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "procedure {0} is {1}hidden",
                                            FormatOps.DisplayName(name),
                                            isHidden ? String.Empty : "not ");

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    result = String.Format(
                                        "procedure {0} is disabled",
                                        FormatOps.DisplayName(name));

                                    code = ReturnCode.Error;
                                }
                            }
                            //
                            // NOTE: The IExecute interface is a strict sub-set of the
                            //       other interfaces; therefore, it must be last one
                            //       to be checked.
                            //
                            else if (execute is IExecute)
                            {
                                code = ExecuteIExecute(
                                    execute, interpreter, clientData, arguments,
                                    engineFlags, substitutionFlags, eventFlags,
                                    expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit,
#endif
                                    ref usable, ref exception, ref result);
                            }
                            else
                            {
                                result = String.Format(
                                    "unknown execution type for {0}",
                                    FormatOps.DisplayName(name));

                                code = ReturnCode.Error;
                            }
                        }

                        if (usable)
                        {
                            //
                            // NOTE: If the execution above succeeded, re-check the readiness
                            //       of the interpreter in case they executed [interp cancel]
                            //       or something similar that changed the state of the
                            //       interpreter.  There is not much point here in checking
                            //       if the interpreter is ready if we have just exited.
                            //
                            if ((code == ReturnCode.Ok) &&
                                !interpreter.ExitNoThrow &&
#if DEBUGGER
                                !interpreter.IsDebuggerExiting &&
#endif
                                !EngineFlagOps.HasNoReady(engineFlags))
                            {
                                code = Interpreter.EngineReady(
                                    interpreter, GetReadyFlags(engineFlags),
                                    ref result);
                            }
                        }
                    }
                    catch
                    {
                        exception = true;
                        throw;
                    }
                    finally
                    {
                        //
                        // NOTE: If an exception was thrown while executing
                        //       something, it may have cause the call stack
                        //       to be imbalanced.  Technically, the call
                        //       stack could be imbalanced even if an exception
                        //       was not thrown; however, that may simply be a
                        //       misuse of the library and the engine is not
                        //       designed to automatically correct anything in
                        //       that case.
                        //
                        if (usable && exception)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                //
                                // NOTE: Keep popping 'automatic' call frames until
                                //       the call stack is balanced again.  In the
                                //       general case, there should be exactly one
                                //       iteration of this loop.
                                //
                                // BUGFIX: Stop if (any) call frame encountered is
                                //         ever unsable (i.e. disposed).  This can
                                //         act as a "fail-safe" in case threading
                                //         rules are not followed.
                                //
                                while (Interpreter.ShouldPopAutomaticCallFrame(
                                        interpreter, peekFrame))
                                {
                                    /* IGNORED */
                                    Interpreter.PopAutomaticCallFrame(
                                        interpreter, ref usable);

                                    if (!usable)
                                        break;
                                }
                            }
                        }

                        /* IGNORED */
                        GlobalState.PopActiveInterpreter();
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid execute";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is somewhat special.  It is the only public method that can
        //       directly execute any executable entity in the core library without going
        //       through the evaluation engine (e.g. EvaluateScript).  Great care must be
        //       taken in this method to prevent exceptions from escaping.  Also, it must
        //       make sure that the call stack is balanced upon exit and that the previous
        //       engine flags are restored.
        //
        public static ReturnCode ExternalExecuteWithFrame( /* EXTERNAL USE ONLY */
            string name,
            IExecute execute,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: Is the interpreter usable at this point?  If not, return
            //       an error now.
            //
            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

#if RESULT_LIMITS
            int executeResultLimit = interpreter.InternalExecuteResultLimit;
#endif

            ICallFrame frame = interpreter.NewTrackingCallFrame(
                StringList.MakeList("external", name),
                CallFrameFlags.External);

            ReturnCode code;
            EngineFlags savedEngineFlags = EngineFlags.None;

            //
            // NOTE: Push a new call frame linked to the current one.  This
            //       call frame can be used to detect the external command
            //       execution in progress.  It will be automatically popped
            //       before returning from this method.
            //
            interpreter.PushCallFrame(frame);

            try
            {
                //
                // NOTE: Save the current engine flags and then enable the
                //       external execution flags.
                //
                savedEngineFlags = interpreter.BeginExternalExecution();

                try
                {
                    //
                    // NOTE: Execute the command using the engine flags having
                    //       been modified to include the flags necessary for
                    //       external command execution (i.e. command execution
                    //       outside of the engine).
                    //
                    code = Execute(
                        name, execute, interpreter,
                        GetClientData(
                            interpreter, clientData, false),
                        arguments, engineFlags,
                        substitutionFlags, eventFlags,
                        expressionFlags,
#if RESULT_LIMITS
                        executeResultLimit,
#endif
                        ref usable, ref result);
                }
                finally
                {
                    if (usable)
                    {
                        //
                        // NOTE: Restore the saved engine flags, masking off
                        //       the external execution flags as necessary.
                        //
                        /* IGNORED */
                        interpreter.EndAndCleanupExternalExecution(
                            savedEngineFlags);
                    }
                }
            }
            finally
            {
                if (usable)
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

            //
            // NOTE: Return the results to the caller.
            //
            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Argument List Execution Methods
        private static bool MaybeGetIExecuteViaArgument(
            Interpreter interpreter,
            Argument argument,
            out bool viaArgument,
            out string executeName,
            out IExecute execute
            )
        {
            viaArgument = (interpreter != null) ?
                interpreter.HasCacheViaArgument() : false;

            executeName = null;
            execute = null;

            if (argument != null)
            {
                executeName = argument; /* CONVERSION */

                if (viaArgument)
                {
                    execute = argument.CacheValue as IExecute;

                    if (execute != null)
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool MaybeCacheIExecuteViaArgument(
            Argument argument,
            bool viaArgument,
            IExecute execute
            )
        {
            if (viaArgument && (argument != null))
            {
                argument.CacheValue = execute;
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExecuteArguments(
            Interpreter interpreter,
            ArgumentList arguments,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
#endif
            ref bool usable,
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ///////////////////////////////////////////////////////////////////////////////////
            //
            // NOTE: This function is called directly by the evaluation engine core.
            //
            ///////////////////////////////////////////////////////////////////////////////////

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            if (arguments.Count == 0) // no command name or arguments.
                return code;

            //
            // NOTE: Fetch the first argument now; it should contain the
            //       name of the command to be looked up.
            //
            Argument firstArgument = arguments[0];

            //
            // NOTE: Figure out what our new (local) command resolution
            //       EngineFlags are.
            //
            engineFlags = GetResolveFlags(engineFlags, false);

            //
            // NOTE: Was the global call frame pushed by this method?
            //
            bool shouldPush = EngineFlagOps.HasEvaluateGlobal(engineFlags);
            bool didPush = false;

            if (shouldPush)
            {
                interpreter.PushGlobalCallFrame(true);
                didPush = true;
            }

            try
            {
                //
                // NOTE: Is [unknown] being used to locate the command?
                //
                bool useUnknown = false;

                //
                // HACK: Cheat, attempt to read the cached command from
                //       the Argument object itself?
                //
                bool viaArgument;
                string executeName;
                IExecute execute;

                if (MaybeGetIExecuteViaArgument(interpreter,
                        firstArgument, out viaArgument,
                        out executeName, out execute))
                {
                    goto execute;
                }

                bool ambiguous = false;
                Result error = null;

                //
                // NOTE: Resolve the command or procedure to execute.
                //
                code = interpreter.GetIExecuteViaResolvers(
                    engineFlags | EngineFlags.ToExecute,
                    executeName, arguments, LookupFlags.EngineDefault,
                    ref ambiguous, ref execute, ref error);

                //
                // NOTE: See if there is a callback configured to deal
                //       with unknown commands.
                //
                bool skipUnknown = false;

                if (code == ReturnCode.Ok)
                {
                    /* IGNORED */
                    MaybeCacheIExecuteViaArgument(
                        firstArgument, viaArgument, execute);
                }
                else
                {
                    UnknownCallback unknownCallback = interpreter.UnknownCallback;

                    if (unknownCallback != null)
                    {
                        code = unknownCallback(
                            interpreter, engineFlags | EngineFlags.ToExecute,
                            executeName, arguments, LookupFlags.EngineDefault,
                            ref ambiguous, ref execute, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Command may have been changed.
                            //       Refresh its name just in case.
                            //
                            if ((arguments != null) &&
                                (arguments.Count > 1))
                            {
                                executeName = firstArgument;
                            }
                            else
                            {
                                //
                                // BUGBUG: No command name?  This is
                                //         not a great situation.
                                //
                                executeName = null;
                            }
                        }
                        else if (code == ReturnCode.Break)
                        {
                            //
                            // NOTE: Command is effectively disabled,
                            //       do nothing -AND- return success.
                            //
                            return ReturnCode.Ok;
                        }
                        else if (code == ReturnCode.Continue)
                        {
                            //
                            // NOTE: Command is effectively disabled,
                            //       do nothing -AND- return failure,
                            //       while skipping further [unknown]
                            //       processing.
                            //
                            skipUnknown = true;

                            code = ReturnCode.Error;
                        }
                    }
                }

                //
                // NOTE: Did we fail to find the command (or procedure)?
                //
                if (code != ReturnCode.Ok)
                {
#if NOTIFY
                    if (!EngineFlagOps.HasNoNotify(engineFlags))
                    {
                        /* IGNORED */
                        interpreter.CheckNotification(
                            NotifyType.Resolver, NotifyFlags.NotFound,
                            new ObjectList(engineFlags | EngineFlags.ToExecute,
                            executeName, ambiguous, execute, error), interpreter,
                            null, arguments, null, ref result);
                    }
#endif

                    if (!skipUnknown)
                    {
                        //
                        // NOTE: If the command name was not ambiguous (i.e.
                        //       ambiguous commands are not considered to be
                        //       "unknown") and we are not explicitly forbidden
                        //       from using the unknown command handler, try to
                        //       look it up now.
                        //
                        // BUGFIX: Prevent infinite recursion via unknown (i.e.
                        //         if it ends up trying to call a nonexistent
                        //         command).
                        //
                        if (!ambiguous &&
                            !EngineFlagOps.HasNoUnknown(engineFlags))
                        {
                            code = interpreter.AttemptToUseUnknown(
                                code, engineFlags, LookupFlags.EngineNoVerbose,
                                ref arguments, ref execute, ref useUnknown);

                            if (code != ReturnCode.Ok)
                                result = error;
                        }
                        else
                        {
                            //
                            // NOTE: If we cannot find an unknown command handler
                            //       or we cannot use it then just give the caller
                            //       back the original invalid command name error.
                            //
                            result = error;
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Make sure the caller has access to the command
                        //       resolution error message.
                        //
                        result = error;
                    }
                }

            execute:

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: Did we succeed at finding some command (or procedure)
                    //       to execute (unknown or otherwise)?
                    //
                    if (useUnknown)
                        interpreter.EnterUnknownLevel();

                    try
                    {
                        //
                        // NOTE: Call the primary external execution entry point
                        //       so that we get all the necessary handling.
                        //
                        code = Execute(
                            executeName, execute, interpreter,
                            GetClientData(
                                interpreter, null, false),
                            arguments, engineFlags,
                            substitutionFlags,
                            eventFlags, expressionFlags,
#if RESULT_LIMITS
                            executeResultLimit,
#endif
                            ref usable, ref result);
                    }
                    finally
                    {
                        if (usable && useUnknown)
                            interpreter.ExitUnknownLevel();
                    }
                }
            }
            finally
            {
                //
                // NOTE: If we previously pushed the global call frame (above),
                //       we also need to pop any leftover scope call frames now;
                //       otherwise, the call stack will be imbalanced.
                //
                if (shouldPush && didPush && usable)
                    interpreter.PopGlobalCallFrame(true);
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation Methods
        #region Evaluation Cleanup Methods
        private static void CleanupObjectReferencesOrComplain(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                ReturnCode cleanupCode;
                Result cleanupError = null;

                cleanupCode = interpreter.CleanupObjectReferences(
                    false, ref cleanupError);

                if (cleanupCode != ReturnCode.Ok)
                {
                    DebugOps.Complain(
                        interpreter, cleanupCode, cleanupError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static void CleanupNamespacesOrComplain(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (!IsUsableNoLock(interpreter))
                    return;

                ReturnCode cleanupCode;
                Result cleanupResult = null;

                cleanupCode = interpreter.CleanupNamespaces(
                    VariableFlags.None, false, ref cleanupResult);

                if (cleanupCode != ReturnCode.Ok)
                {
                    DebugOps.Complain(
                        interpreter, cleanupCode, cleanupResult);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation Exit-Hook Methods
        private static ReturnCode EvaluateExited(
            Interpreter interpreter,             /* in */
            string fileName,                     /* in */
            int currentLine,                     /* in */
            string text,                         /* in */
            int startIndex,                      /* in */
            int characters,                      /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref ReturnCode code,                 /* in, out */
            ref Result result,                   /* in, out */
            ref int errorLine                    /* in, out */
            )
        {
#if DEBUGGER && DEBUGGER_ENGINE
            BreakpointType breakpointType =
                BreakpointType.Exit | BreakpointType.Evaluate;

            if (DebuggerOps.CanHitBreakpoints(
                    interpreter, engineFlags, breakpointType))
            {
                ReturnCode oldCode = code;

                Result oldResult = Result.Copy(
                    result, ResultFlags.CopyObject); /* COPY */

                code = CheckBreakpoints(
                    code, breakpointType, null,
                    null, null, engineFlags,
                    substitutionFlags, eventFlags,
                    expressionFlags, null, null,
                    interpreter, null, null,
                    ref result);

                //
                // TODO: What is the purpose of this if statement and the
                //       associated call to DebugOps.Complain?
                //
                // NOTE: It appears that the purpose of this check is to verify
                //       that the breakpoint, if any, did not cause the overall
                //       result of this script evaluation to be changed.
                //
                if ((code != oldCode) || !Result.Equals(result, oldResult))
                    DebugOps.Complain(interpreter, code, result);
            }
#endif

            if (interpreter != null)
            {
                int scriptLevels = interpreter.ScriptLevels;
                int levels = interpreter.InternalLevels;
                int previousLevels = interpreter.PreviousLevels;

#if NOTIFY
                if (!EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Script,
                        (levels == 0) ? NotifyFlags.Completed : NotifyFlags.Evaluated,
                        //
                        // NOTE: We do not include the code in the data triplet
                        //       directly because after an evaluation it is now
                        //       guaranteed to be accessible via the ReturnCode
                        //       property of the Result object.
                        //
                        // BUGBUG: In order to use this class for notification
                        //         parameters, it really should probably be
                        //         made public.
                        //
                        new ObjectList(fileName, currentLine, text, startIndex, characters),
                        interpreter, null, null, null, ref result);
                }
#endif

                if (scriptLevels == 0)
                {
                    //
                    // NOTE: Cleanup any object references that may no longer
                    //       be needed (e.g. temporary).
                    //
                    CleanupObjectReferencesOrComplain(interpreter);
                }

                if (levels == 0)
                {
                    //
                    // NOTE: If appropriate, populate the error information for
                    //       the current result.
                    //
                    interpreter.MaybePopulateResultErrorProperties(
                        "current", code, result, errorLine);

                    //
                    // NOTE: Cleanup any namespaces that are pending deletion
                    //       or complain if we are unable to.
                    //
                    CleanupNamespacesOrComplain(interpreter);

                    //
                    // NOTE: Reset the stack overflow flag for the interpreter
                    //       now, if necessary.
                    //
                    CheckStackOverflow(interpreter);

#if DEBUGGER
                    //
                    // NOTE: Reset the skip-ready flag for the interpreter.
                    //
                    CheckIsDebuggerExiting(interpreter);
#endif

                    //
                    // NOTE: Reset the number of errorInfo frames to zero.
                    //
                    interpreter.ErrorFrames = 0;
                }
                else if (levels == previousLevels)
                {
                    //
                    // NOTE: Reset the stack overflow flag for the interpreter
                    //       now, if necessary.
                    //
                    CheckStackOverflow(interpreter);

#if DEBUGGER
                    //
                    // NOTE: Reset the skip-ready flag for the interpreter.
                    //
                    CheckIsDebuggerExiting(interpreter);
#endif
                }
            }

            //
            // NOTE: Finally, reset the result return code, if necessary.
            //
            ResetReturnCode(interpreter, result,
                EngineFlagOps.HasResetReturnCode(engineFlags));

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation Helper Methods
#if DEBUGGER && DEBUGGER_BREAKPOINTS
        internal static bool HasArgumentLocation(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.HasArgumentLocation();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetToken(
            IParseState parseState, /* in */
            ref IToken token,       /* in, out */
            ref int tokenIndex,     /* in, out */
            ref Result error        /* out */
            )
        {
            if (parseState != null)
            {
                if (token != null)
                    tokenIndex += (token.Components + 1);

                if ((tokenIndex >= 0) &&
                    (tokenIndex < parseState.Tokens.Count))
                {
                    token = parseState.Tokens[tokenIndex];

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid token index";
                }
            }
            else
            {
                error = "invalid parser state";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode ToBoolean(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            Result localError = null; /* NOT USED */

            if (Value.GetBoolean3(
                    getValue, ValueFlags.AnyNumberAnyRadix | ValueFlags.Fast,
                    cultureInfo, ref value, ref localError) == ReturnCode.Ok)
            {
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "expected boolean value but got {0}", (getValue != null) ?
                    FormatOps.DisplayName(getValue.String) : FormatOps.DisplayNull);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (IToken) Methods
#if DEBUGGER && DEBUGGER_BREAKPOINTS
        private static void CheckTokenLines(
            IToken token,
            ref int startLine,
            ref int endLine
            )
        {
            if ((startLine == Parser.UnknownLine) ||
                ((token.StartLine != Parser.UnknownLine) &&
                (token.StartLine < startLine)))
            {
                startLine = token.StartLine;
            }

            if ((endLine == Parser.UnknownLine) ||
                ((token.EndLine != Parser.UnknownLine) &&
                (token.EndLine > endLine)))
            {
                endLine = token.EndLine;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode EvaluateTokens(
            Interpreter interpreter,
            IParseState parseState,
            int startTokenIndex,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            int tokenCount,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            )
        {
            int startLine = Parser.UnknownLine;
            int endLine = Parser.UnknownLine;

            return EvaluateTokens(
                interpreter, parseState, startTokenIndex,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                tokenCount, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref startLine, ref endLine, ref result);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTokenVariableValue(
            Interpreter interpreter,
            string varName,
            string varIndex,
            ref Result result
            )
        {
            ReturnCode code;
            Result value = null;
            Result error = null;

            //
            // BUGFIX: Passing the same variable for both reference parameters
            //         here causes serious problems with the cross-AppDomain
            //         marshalling; therefore, avoid doing that.
            //
            code = interpreter.GetVariableValue2(
                VariableFlags.SkipToString, varName, varIndex, ref value,
                ref error);

            //
            // BUGFIX: Callers of this method cannot handle a null value or
            //         error message because they will interpret that to
            //         mean "use all the parse state text"; therefore, fix
            //         it up to be an empty string instead.
            //
            if (code == ReturnCode.Ok)
                result = (value != null) ? value : (Result)String.Empty;
            else
                result = (error != null) ? error : (Result)String.Empty;

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_BREAKPOINTS
        private
#else
        internal
#endif
        static ReturnCode EvaluateTokens(
            Interpreter interpreter,
            IParseState parseState,
            int startTokenIndex,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            int tokenCount,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
            ref int startLine,
            ref int endLine,
#endif
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;
            string text = parseState.Text;
            CommandBuilder evalResult = null;
            int startTokenCount = tokenCount;

            for (int tokenIndex = startTokenIndex;
                    tokenCount > 0;
                    tokenCount--, tokenIndex++)
            {
                int index = Index.Invalid;
                int length = 0;
                int thisTokenCount;
                IToken token = parseState.Tokens[tokenIndex];
                Result localResult = null;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                CheckTokenLines(token, ref startLine, ref endLine);
#endif

                switch (token.Type)
                {
                    case TokenType.Text:
                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            BreakpointType breakpointType = BreakpointType.Token |
                                BreakpointType.BeforeText;

                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    engineFlags, breakpointType))
                            {
                                code = CheckBreakpoints(
                                    code, breakpointType, null,
                                    token, null, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, null, null,
                                    interpreter, null, null,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }
#endif

                            index = token.Start;
                            length = token.Length;
                            thisTokenCount = 1;

                            break;
                        }
                    case TokenType.Backslash:
                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            BreakpointType breakpointType = BreakpointType.Token |
                                BreakpointType.BeforeBackslash;

                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    engineFlags, breakpointType))
                            {
                                code = CheckBreakpoints(
                                    code, breakpointType, null,
                                    token, null, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, null, null,
                                    interpreter, null, null,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }
#endif

                            char? character1 = null;
                            char? character2 = null;

                            Parser.ParseBackslash(
                                text, token.Start, token.Length,
                                ref character1, ref character2);

                            localResult = Result.FromCharacters(character1, character2);
                            thisTokenCount = 1;

                            break;
                        }
                    case TokenType.Command:
                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            BreakpointType breakpointType = BreakpointType.Token |
                                BreakpointType.BeforeCommand;

                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    engineFlags, breakpointType))
                            {
                                code = CheckBreakpoints(
                                    code, breakpointType, null,
                                    token, null, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, null, null,
                                    interpreter, null, null,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }
#endif

                            code = CheckEvents(
                                interpreter, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, ref localResult);

                            if (code == ReturnCode.Ok)
                                code = EvaluateScript(
                                    interpreter, token.FileName, token.StartLine, text,
                                    token.Start + 1, token.Length - 2, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit, nestedResultLimit,
#endif
                                    sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                    argumentLocation,
#endif
                                    ref localResult);

                            if (code != ReturnCode.Ok)
                            {
                                result = localResult;
                                goto done;
                            }

                            thisTokenCount = 1;

                            break;
                        }
                    case TokenType.Variable:
                    case TokenType.VariableNameOnly:
                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            BreakpointType breakpointType = BreakpointType.Token |
                                BreakpointType.BeforeVariableGet;

                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    engineFlags, breakpointType))
                            {
                                code = CheckBreakpoints(
                                    code, breakpointType, null,
                                    token, null, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, null, null,
                                    interpreter, null, null,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }
#endif

                            string varName;
                            string varIndex = null;

                            if (token.Components > 1)
                            {
                                code = CheckEvents(
                                    interpreter, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref localResult);

                                if (code == ReturnCode.Ok)
                                {
                                    code = EvaluateTokens(
                                        interpreter, parseState,
                                        tokenIndex + 2,
#if RESULT_LIMITS
                                        executeResultLimit,
                                        nestedResultLimit,
#endif
                                        token.Components - 1,
                                        engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags,
                                        sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                        argumentLocation,
                                        ref startLine, ref endLine,
#endif
                                        ref localResult);
                                }

                                if (code != ReturnCode.Ok)
                                {
                                    result = localResult;
                                    goto done;
                                }

                                varIndex = localResult;
                            }

                            varName = text.Substring(
                                parseState.Tokens[tokenIndex + 1].Start,
                                parseState.Tokens[tokenIndex + 1].Length);

                            if (token.Type == TokenType.VariableNameOnly)
                            {
                                localResult = FormatOps.VariableName(
                                    varName, varIndex);
                            }
                            else
                            {
                                if (GetTokenVariableValue(
                                        interpreter, varName, varIndex,
                                        ref localResult) != ReturnCode.Ok)
                                {
                                    result = localResult;
                                    code = ReturnCode.Error;
                                    goto done;
                                }
                            }

                            tokenCount -= token.Components;
                            tokenIndex += token.Components;
                            thisTokenCount = token.Components;

                            break;
                        }
                    default:
                        {
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            BreakpointType breakpointType = BreakpointType.Token |
                                BreakpointType.BeforeUnknown;

                            if (DebuggerOps.CanHitBreakpoints(interpreter,
                                    engineFlags, breakpointType))
                            {
                                code = CheckBreakpoints(
                                    code, breakpointType, null,
                                    token, null, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, null, null,
                                    interpreter, null, null,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }
#endif

                            result = String.Format(
                                "unexpected token type {0} for evaluation",
                                token.Type);

                            code = ReturnCode.Error;
                            goto done;
                        }
                }

                //
                // NOTE: If there was only one "token", just return the result now.
                //
                if (thisTokenCount >= startTokenCount)
                {
                    if (index == Index.Invalid)
                    {
                        if (localResult != null) // INTL: do not change to String.IsNullOrEmpty
                        {
#if RESULT_LIMITS
                            if (!CommandBuilder.StaticHaveEnoughCapacity(
                                    nestedResultLimit, localResult, ref result))
                            {
                                code = ReturnCode.Error;
                                goto done;
                            }
#endif

                            result = localResult;
                        }
                    }
                    else
                    {
#if RESULT_LIMITS
                        if (!CommandBuilder.StaticHaveEnoughCapacity(
                                nestedResultLimit, length, ref result))
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        result = text.Substring(index, length);
                    }

                    code = ReturnCode.Ok;
                    goto done;
                }

                //
                // NOTE: If there was no result, there is now.
                //
                if (evalResult == null)
                    evalResult = CommandBuilder.Create();

                if (index == Index.Invalid)
                {
                    if (localResult != null) // INTL: do not change to String.IsNullOrEmpty
                    {
#if RESULT_LIMITS
                        if (!evalResult.HaveEnoughCapacity(
                                nestedResultLimit, localResult, ref result))
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }
#endif

                        evalResult.Add(localResult);
                    }
                }
                else
                {
#if RESULT_LIMITS
                    if (!evalResult.HaveEnoughCapacity(
                            nestedResultLimit, length, ref result))
                    {
                        code = ReturnCode.Error;
                        goto done;
                    }
#endif

                    evalResult.Add(text, index, length);
                }
            }

            if (evalResult != null)
                result = Result.FromCommandBuilder(evalResult);
            else
                ResetResult(interpreter, engineFlags, ref result);

        done:
            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (IScript) Methods
        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            IScript script,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateScript(
                interpreter, script, ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            IScript script,
            ref Result result,
            ref int errorLine
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            EngineFlags engineFlags = EngineFlags.None;
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;
#if RESULT_LIMITS
            int executeResultLimit = 0;
            int nestedResultLimit = 0;
#endif

            if (script != null)
            {
                engineFlags = script.EngineFlags;
                substitutionFlags = script.SubstitutionFlags;
                eventFlags = script.EventFlags;
                expressionFlags = script.ExpressionFlags;

                if ((interpreter != null) &&
                    EngineFlagOps.HasUseInterpreter(engineFlags))
                {
                    engineFlags |= interpreter.EngineFlags;
                    substitutionFlags |= interpreter.SubstitutionFlags;
                    eventFlags |= interpreter.EngineEventFlags;
                    expressionFlags |= interpreter.ExpressionFlags;

#if RESULT_LIMITS
                    executeResultLimit = interpreter.InternalExecuteResultLimit;
                    nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif
                }
            }
            else if (interpreter != null)
            {
                engineFlags = interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;

#if RESULT_LIMITS
                executeResultLimit = interpreter.InternalExecuteResultLimit;
                nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif
            }

            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            return EvaluateScript(
                interpreter, script, 0, Length.Invalid,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref engineFlags, ref substitutionFlags, ref eventFlags,
                ref expressionFlags, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            IScript script,
            int startIndex,
            int characters,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref EngineFlags engineFlags,
            ref SubstitutionFlags substitutionFlags,
            ref EventFlags eventFlags,
            ref ExpressionFlags expressionFlags,
            ref Result result,
            ref int errorLine
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            if (script == null)
            {
                result = "invalid script";
                return ReturnCode.Error;
            }

            string originalText = script.Text;

            if (originalText == null)
            {
                result = "invalid script";
                return ReturnCode.Error;
            }

            string text = null;

            try
            {
                using (StringReader stringReader = new StringReader(
                        originalText))
                {
                    ReadInt32Callback charCallback = null;
                    ReadCharsCallback charsCallback = null;

                    GetStreamCallbacks(
                        stringReader, ref charCallback, ref charsCallback);

                    engineFlags |= EngineFlags.ExternalScript;

                    ReadScriptClientData readScriptClientData = null;
                    bool canRetry = false; /* NOT USED */

                    if (ReadScriptStream(interpreter,
                            script, script.Name, charCallback,
                            charsCallback, startIndex, characters,
                            ref engineFlags, ref substitutionFlags,
                            ref eventFlags, ref expressionFlags,
                            ref readScriptClientData, ref canRetry,
                            ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    text = readScriptClientData.Text;
                }
            }
            catch (Exception e)
            {
                result = String.Format(
                    "caught exception reading script stream: {0}",
                    e);

                result.Exception = e;

                SetExceptionErrorCode(interpreter, e);

#if NOTIFY
                if ((interpreter != null) &&
                    !EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Engine, NotifyFlags.Exception,
                        new ObjectTriplet(script, startIndex, characters),
                        interpreter, null, null, e, ref result);
                }
#endif

                return ReturnCode.Error;
            }

            string fileName = script.FileName;

            if (fileName == null)
                fileName = script.Name;

            bool newFrame = EngineFlagOps.HasExtraCallFrame(
                engineFlags);

            if (newFrame)
            {
                ICallFrame frame = interpreter.NewEngineCallFrame(
                    StringList.MakeList("script", fileName),
                    CallFrameFlags.Engine);

                interpreter.PushAutomaticCallFrame(frame);
            }

            try
            {
                bool pushed = false;

                interpreter.PushScriptLocation(fileName, true, ref pushed);

                try
                {
                    ReturnCode code = EvaluateScript(
                        interpreter, text, startIndex, characters, engineFlags,
                        substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                        executeResultLimit, nestedResultLimit,
#endif
                        sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                        argumentLocation,
#endif
                        ref result, ref errorLine);

                    if (code == ReturnCode.Return)
                    {
                        code = UpdateReturnInformation(interpreter);
                    }
                    else if (code == ReturnCode.Error)
                    {
                        AddErrorInformation(interpreter, result,
                            String.Format(
                                "{0}    (script \"{1}\" line {2})",
                                Environment.NewLine,
                                FormatOps.Ellipsis(fileName),
                                errorLine));
                    }

                    return code;
                }
                finally
                {
                    interpreter.PopScriptLocation(true, ref pushed);
                }
            }
            finally
            {
                if (newFrame)
                {
                    //
                    // NOTE: Pop the original call frame that we
                    //       pushed above and any intervening scope
                    //       call frames that may be leftover (i.e.
                    //       they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (Text) Methods
        #region Specialized Evaluation (Text) Methods
        //
        // WARNING: This method creates and disposes its own "single-use"
        //          interpreter object.  Before using this method, make
        //          sure that is what you want.  This method is custom
        //          tailored to work from inside SQL Server.
        //
        public static int /* ReturnCode */ EvaluateOneScript(
            string text,
            ref string result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;
            Result localResult = null;

            using (Interpreter interpreter = Interpreter.Create(
                    null, CreateFlags.SingleUse, HostCreateFlags.SingleUse,
                    ref localResult))
            {
                if (interpreter != null)
                    code = EvaluateScript(
                        interpreter, text, ref localResult);
                else
                    code = ReturnCode.Error;
            }

            result = localResult;
            return (int)code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateScript(
                interpreter, text, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            ref Result result,
            ref int errorLine
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateScript(
                interpreter, text, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.Default,
                ExpressionFlags.Default, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
#if RESULT_LIMITS
            int executeResultLimit = 0;
            int nestedResultLimit = 0;

            if (interpreter != null)
            {
                executeResultLimit = interpreter.InternalExecuteResultLimit;
                nestedResultLimit = interpreter.InternalNestedResultLimit;
            }
#endif

            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            return EvaluateScript(
                interpreter, text, 0, Length.Invalid, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateScript(
                interpreter, text, startIndex, characters,
                engineFlags, substitutionFlags, eventFlags,
                expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result,
            ref int errorLine
            )
        {
#if RESULT_LIMITS
            int executeResultLimit = 0;
            int nestedResultLimit = 0;

            if (interpreter != null)
            {
                executeResultLimit = interpreter.InternalExecuteResultLimit;
                nestedResultLimit = interpreter.InternalNestedResultLimit;
            }
#endif

            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            return EvaluateScript(
                interpreter, text, 0, Length.Invalid, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result,
            ref int errorLine
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            string fileName = null;
            int currentLine = Parser.StartLine;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            if (ScriptOps.GetLocation(
                    interpreter, false, false, ref fileName,
                    ref currentLine, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            return EvaluateScript(
                interpreter, fileName, currentLine, text, startIndex, characters,
                engineFlags, substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string fileName,
            int currentLine,
            string text,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateScript(
                interpreter, fileName, currentLine, text, startIndex, characters,
                engineFlags, substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string fileName,
            int currentLine,
            string text,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result,
            ref int errorLine
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code = ReturnCode.Ok;
            EngineFlags localEngineFlags = engineFlags;

            if (interpreter == null)
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;

                goto exit;
            }

            if (text == null) // INTL: do not change to String.IsNullOrEmpty
            {
                result = "invalid script";
                code = ReturnCode.Error;

                goto exit;
            }

            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            /* IGNORED */
            interpreter.EnterEngineScriptLevel();

            try
            {
                localEngineFlags = CombineFlags(interpreter, engineFlags, true, true);

                if (EngineFlagOps.HasNoEvaluate(localEngineFlags))
                {
                    result = "interpreter not accepting scripts to evaluate";
                    code = ReturnCode.Error;

                    goto exit;
                }

                bool noReady = EngineFlagOps.HasNoReady(localEngineFlags);
                bool noCacheArgument = false;

#if ARGUMENT_CACHE
                if (EngineFlagOps.HasNoCacheArgument(localEngineFlags))
                    noCacheArgument = true;
#endif

#if CALLBACK_QUEUE
                bool callbackQueue = true;

                //
                // NOTE: Has callback support been disabled for this script or for
                //       the interpreter?
                //
                if (EngineFlagOps.HasNoCallbackQueue(localEngineFlags))
                    callbackQueue = false;
#endif

                if (characters < 0)
                    characters = text.Length;

                /*
                 * Reset the canceled flag of the interpreter, if required.
                 */

                ResetCancel(interpreter, GetCancelFlags(localEngineFlags));

                /*
                 * Reset the result passed in by the caller now.
                 */

                ResetResult(interpreter, localEngineFlags, ref result);

                /*
                 * Reset the last return code for the interpreter, if required.
                 */

                ResetReturnCode(interpreter, result,
                    EngineFlagOps.HasResetReturnCode(localEngineFlags));

                /*
                 * Are we going to evaluate the script in the global context?
                 */

                bool global = false;

                if (EngineFlagOps.HasEvaluateGlobal(localEngineFlags))
                {
                    interpreter.PushGlobalCallFrame(true);
                    global = true; // pushed.
                }

                int index = startIndex;
                int charactersLeft = characters;
                bool nested = EngineFlagOps.HasBracketTerminator(localEngineFlags);

                int terminator;
                int nextIndex;

                //
                // NOTE: Prevent a null result from being added as an argument to
                //       a subsequent command?  For full backward compatibility,
                //       e.g. with native Tcl, etc, this flag should be used.
                //
                bool noNullArgument = interpreter.HasNoNullArgument(localEngineFlags);

                //
                // NOTE: Disallow use of the Thread.ResetAbort() method when script
                //       is running?
                //
                bool noResetAbort = EngineFlagOps.HasNoResetAbort(localEngineFlags);

                IParseState parseState = new ParseState(
                    localEngineFlags, substitutionFlags, fileName, currentLine);

                interpreter.ParseState = parseState; /* NOTE: Per-thread. */
                ArgumentList arguments = new ArgumentList();

#if PREVIOUS_RESULT
                Result previousResult = null;
#endif

                do
                {
                    /*
                     * Attempt to parse the command.  This can fail in a number of
                     * ways, including being canceled.
                     */

                    if (Parser.ParseCommand(
                            interpreter, text, index,
                            charactersLeft, nested, parseState,
                            noReady, ref result) != ReturnCode.Ok)
                    {
                        code = ReturnCode.Error;
                        goto error;
                    }

                    terminator = parseState.Terminator;

                    if (nested && (terminator == characters))
                    {
                        code = ReturnCode.Error;
                        goto error;
                    }

                    int commandWords = parseState.CommandWords;

                    if (commandWords > 0)
                    {
                        //
                        // NOTE: Build the argument list of the command to execute,
                        //       recursively evaluating terms as necessary.
                        //
                        arguments.Clear();

                        if (arguments.Capacity < commandWords)
                            arguments.Capacity = commandWords;

                        //
                        // NOTE: Initialize token related variables.
                        //
                        IToken token = null;
                        int tokenIndex = 0;

                        for (int wordsUsed = 0; wordsUsed < commandWords; wordsUsed++)
                        {
                            //
                            // NOTE: Get the first token from the parse state.
                            //
                            code = GetToken(
                                parseState, ref token, ref tokenIndex,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto error;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            int startLine = Parser.UnknownLine;
                            int endLine = Parser.UnknownLine;
#endif

                            Result localResult = null;

                            try
                            {
                                code = EvaluateTokens(
                                    interpreter, parseState,
                                    tokenIndex + 1,
#if RESULT_LIMITS
                                    executeResultLimit,
                                    nestedResultLimit,
#endif
                                    token.Components, localEngineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                    argumentLocation,
                                    ref startLine, ref endLine,
#endif
                                    ref localResult);
                            }
#if true
                            catch (ThreadAbortException)
                            {
                                if (!noResetAbort)
                                    Thread.ResetAbort();

                                localResult = ThreadAbortException;
                                code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                //
                                // NOTE: This may not actually work.  In that case,
                                //       there is not much else we can do.
                                //
                                DebugOps.Complain(interpreter, code, localResult);
#endif
                            }
#endif
#if true
                            catch (ThreadInterruptedException)
                            {
                                localResult = ThreadInterruptedException;
                                code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                //
                                // NOTE: This may not actually work.  In that case,
                                //       there is not much else we can do.
                                //
                                DebugOps.Complain(interpreter, code, localResult);
#endif
                            }
#endif
#if true
                            catch (StackOverflowException)
                            {
                                localResult = StackOverflowException;
                                code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                //
                                // NOTE: We should (almost) never get here, complain.
                                //       This may not actually work.  In that case,
                                //       there is not much else we can do.
                                //
                                DebugOps.Complain(interpreter, code, localResult);
#endif
                            }
                            catch (OutOfMemoryException e)
                            {
                                try
                                {
                                    //
                                    // HACK: Try to free up some memory.  This is
                                    //       unlikely to work.
                                    //
                                    ObjectOps.CollectGarbage(
                                        GarbageFlags.ForEngine); /* throw */

                                    localResult = e;
                                    code = ReturnCode.Error;
                                }
                                catch (OutOfMemoryException)
                                {
                                    localResult = OutOfMemoryException;
                                    code = ReturnCode.Error;
                                }
                                catch (Exception ex)
                                {
                                    localResult = ex;
                                    code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                    //
                                    // NOTE: This may not actually work.  In that case,
                                    //       there is not much else we can do.
                                    //
                                    DebugOps.Complain(interpreter, code, localResult);
#endif
                                }
                            }
#endif
                            catch (ScriptEngineException e)
                            {
                                localResult = e;
                                code = ReturnCode.Error;
                            }
#if true
                            catch (Exception e)
                            {
                                localResult = e;
                                code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                //
                                // NOTE: We should never get here, complain.
                                //       This may not actually work.  In that case,
                                //       there is not much else we can do.
                                //
                                DebugOps.Complain(interpreter, code, localResult);
#endif
                            }
#endif

                            //
                            // NOTE: If there was any kind of error or exception,
                            //       bail out now.
                            //
                            if (code != ReturnCode.Ok)
                            {
                                result = localResult;
                                goto error;
                            }

                            //
                            // HACK: When preventing null results, reset them to
                            //       an empty string instead.  This is something
                            //       of a hack.  In general, commands should not
                            //       return a null result.  One interesting case
                            //       is that of an empty procedure body, i.e. a
                            //       procedure body with no commands to execute.
                            //       That appears to produce a null result, for
                            //       reasons that are not entirely clear.  If it
                            //       has an initial null result and no commands
                            //       are executed, it stays null?
                            //
                            if (noNullArgument && (localResult == null))
                                localResult = String.Empty;

                            //
                            // NOTE: Grab the engine data associated with the
                            //       result, if any.  If this engine data is
                            //       valid, it will prevent the (new) argument
                            //       from being cached.
                            //
                            object engineData = null;

                            if (localResult != null)
                                engineData = localResult.EngineData;

                            //
                            // NOTE: Append the result value to the list of
                            //       arguments for the command to be executed.
                            //
                            Argument argument;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            if (argumentLocation)
                            {
                                argument = Argument.GetOrCreate(
                                    interpreter, localResult, fileName,
                                    startLine, endLine, false,
                                    noCacheArgument || (engineData != null));
                            }
                            else
#endif
                            {
                                argument = Argument.GetOrCreate(
                                    interpreter, localResult,
                                    noCacheArgument || (engineData != null));
                            }

                            if ((argument != null) && (engineData != null))
                            {
                                argument.SetEngineDataForIHaveStringBuilder(
                                    engineData, arguments);
                            }

                            arguments.Add(argument);
                        }

                        bool exit = false;
                        int levels = interpreter.EnterEngineLevel(); /* REALLY: Command level? */

                        try
                        {
#if HISTORY
                            //
                            // BUGFIX: Is the interpreter configured to track command history?
                            //         Also, has command history been disabled for this script
                            //         by our caller?  Unfortunately, we cannot simply fetch
                            //         this value upon entry into this method and continue to
                            //         use it [like we used to] because any command execution
                            //         could cause it to change and we want those changes to
                            //         be effective immediately.
                            //
                            if (!EngineFlagOps.HasNoHistory(localEngineFlags) &&
                                interpreter.CanAddHistory())
                            {
                                if (HistoryOps.MatchData(levels,
                                        HistoryFlags.Engine, interpreter.HistoryEngineFilter))
                                {
                                    code = interpreter.AddHistory(arguments, levels,
                                        HistoryFlags.Engine, ref result);

                                    if (code != ReturnCode.Ok)
                                        goto error;
                                }
                            }
#endif

                            // if (arguments.Count > 0)
                            {
#if DEBUGGER && DEBUGGER_ARGUMENTS
                                //
                                // NOTE: Notify the script debugger, if any, of the current
                                //       command name and arguments.
                                //
                                if (!EngineFlagOps.HasNoDebuggerArguments(localEngineFlags))
                                    SetDebuggerExecuteArguments(interpreter, arguments);
#endif

                                //
                                // BUGFIX: *HACK* If this is NOT the primary AppDomain for the
                                //         interpreter being used (i.e. remoting is involved),
                                //         then refresh the "cached" ParseState.  This is
                                //         necessary because the "cached" ParseState for the
                                //         interpreter is modified by ParseCommand without
                                //         entering this method again -AND- must be completely
                                //         up-to-date prior to executing each command (e.g.
                                //         [error]).
                                //
                                if (!sameAppDomain)
                                    interpreter.ParseState = parseState; /* NOTE: Per-thread. */

#if SCRIPT_ARGUMENTS
                                bool pushed = false;

                                interpreter.PushScriptArguments(arguments, ref pushed);

                                try
                                {
#endif
                                    //
                                    // NOTE: Execute the command.  The command could do practically
                                    //       anything at this point; therefore, we need to be very
                                    //       careful about making assumptions about the state of the
                                    //       interpreter after this point.
                                    //
                                    code = ExecuteArguments(
                                        interpreter, arguments, localEngineFlags, substitutionFlags,
                                        eventFlags, expressionFlags,
#if RESULT_LIMITS
                                        executeResultLimit,
#endif
                                        ref usable, ref result);
#if SCRIPT_ARGUMENTS
                                }
                                finally
                                {
                                    interpreter.PopScriptArguments(ref pushed);
                                }
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
                                if (usable && !EngineFlagOps.HasNoNotify(localEngineFlags))
                                {
                                    /* IGNORED */
                                    interpreter.CheckNotification(
                                        NotifyType.Engine, NotifyFlags.Executed,
                                        new ObjectList(code, localEngineFlags, substitutionFlags,
                                        eventFlags, expressionFlags, usable), interpreter,
                                        null, arguments, null, ref result);
                                }
#endif
                            }

                            //
                            // BUGFIX: We cannot use various properties of the interpeter if
                            //         it has been disposed.
                            //
                            if (!usable)
                                goto unusable;

                            exit = interpreter.ExitNoThrow;

#if CALLBACK_QUEUE
                            //
                            // NOTE: We only want to execute queued callbacks if we have not
                            //       exited (or been canceled, etc) and if we are on the way
                            //       out of this evaluation.
                            //
                            if (callbackQueue && (code == ReturnCode.Ok) && (levels == 1) && !exit)
                            {
                                //
                                // NOTE: Check for and execute any queued callbacks.  This is
                                //       currently used primarily to implement tailcall-like
                                //       functionality (see TIP #327).
                                //
                                code = ExecuteCallbackQueue(
                                    interpreter, localEngineFlags, substitutionFlags, eventFlags,
                                    expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit,
#endif
                                    ref usable, ref result);

                                if (!usable)
                                    goto unusable;
                            }
#endif

                            //
                            // BUGFIX: Prevent null command result from causing an exception
                            //         when we try to store the return code.
                            //
                            // NOTE: This used to place String.Empty in the result if it was
                            //       null and then set the return code; however, that seems
                            //       wasteful.
                            //
                            if (result != null) result.ReturnCode = code;

                            //
                            // HACK: Yes, this is a bit ugly; however, it can help make things
                            //       like [append] go (much) faster.
                            //
                            // arguments.ResetForIHaveStringBuilder(interpreter);
                        }
#if true
                        catch (StackOverflowException)
                        {
                            result = StackOverflowException;
                            code = ReturnCode.Error;

#if DEBUG && VERBOSE
                            //
                            // NOTE: We should (almost) never get here, complain.
                            //       This may not actually work.  In that case,
                            //       there is not much else we can do.
                            //
                            DebugOps.Complain(interpreter, code, result);
#endif
                        }
                        catch (OutOfMemoryException e)
                        {
                            try
                            {
                                //
                                // HACK: Try to free up some memory.  This is
                                //       unlikely to work.
                                //
                                ObjectOps.CollectGarbage(
                                    GarbageFlags.ForEngine); /* throw */

                                result = e;
                                code = ReturnCode.Error;
                            }
                            catch (OutOfMemoryException)
                            {
                                result = OutOfMemoryException;
                                code = ReturnCode.Error;
                            }
                            catch (Exception ex)
                            {
                                result = ex;
                                code = ReturnCode.Error;

#if DEBUG && VERBOSE
                                //
                                // NOTE: This may not actually work.  In that case,
                                //       there is not much else we can do.
                                //
                                DebugOps.Complain(interpreter, code, result);
#endif
                            }
                        }
#endif
                        catch (ScriptEngineException e)
                        {
                            result = e;
                            code = ReturnCode.Error;
                        }
#if true
                        catch (Exception e)
                        {
                            result = e;
                            code = ReturnCode.Error;

#if DEBUG && VERBOSE
                            //
                            // NOTE: We should never get here, complain.
                            //       This may not actually work.  In that case,
                            //       there is not much else we can do.
                            //
                            DebugOps.Complain(interpreter, code, result);
#endif
                        }
#endif
                        finally
                        {
                            if (usable)
                            {
                                /* IGNORED */
                                interpreter.ExitEngineLevel();
                            }
                        }

#if PREVIOUS_RESULT
                        //
                        // TODO: Evaluate how much this block impacts the overall
                        //       performance of script evaluation.
                        //
                        if (!EngineFlagOps.HasNoPreviousResult(localEngineFlags))
                        {
                            //
                            // NOTE: Copy result to previous for use by debugger.
                            //
                            previousResult = Result.Copy(
                                result, ResultFlags.CopyObject); /* COPY */

                            //
                            // NOTE: At this point, the error line value will be
                            //       whatever the caller passed in, which is most
                            //       likely zero.  It will be made accurate after
                            //       going to the "error" label, if applicable.
                            //
                            interpreter.MaybePopulateResultErrorProperties(
                                "previous", code, previousResult, errorLine);

                            Interpreter.SetPreviousResult(
                                interpreter, previousResult);
                        }
#endif

                        //
                        // NOTE: Return is also considered an "exception" here because it
                        //       halts evaluation of the current procedure.
                        //
                        if (code != ReturnCode.Ok)
                            goto error;

                        //
                        // NOTE: If the command marked the interpreter as "exited", bail
                        //       out now.
                        //
                        if (exit)
                            goto ok;
                    }

                    //
                    // NOTE: Advance to the next command in the script.
                    //
                    nextIndex = parseState.CommandStart + parseState.CommandLength;
                    charactersLeft -= (nextIndex - index);
                    index = nextIndex;
                    parseState.Tokens.Clear();

                    if (nested &&
                        (terminator < text.Length) && // TEST: Test this.
                        (text[terminator] == Characters.CloseBracket))
                    {
                        //
                        // NOTE: If we previously pushed the global call frame (above),
                        //       we also need to pop any leftover scope call frames now;
                        //       otherwise, the call stack will be imbalanced.
                        //
                        if (global)
                            interpreter.PopGlobalCallFrame(true);

                        code = ReturnCode.Ok;

                        goto exit;
                    }
                } while (charactersLeft > 0);

                if (nested)
                {
                    code = ReturnCode.Error;
                    goto error;
                }

            ok:

                //
                // NOTE: If we previously pushed the global call frame (above), we also
                //       need to pop any leftover scope call frames now; otherwise, the
                //       call stack will be imbalanced.
                //
                if (global)
                    interpreter.PopGlobalCallFrame(true);

                code = ReturnCode.Ok;

                goto exit;

            error:

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if ((code == ReturnCode.Return) && !interpreter.InternalIsBusy)
                        code = UpdateReturnInformation(interpreter);

                    //
                    // WARNING: The engine flags in the interpreter must be checked here
                    //          because the command we just executed above may have just
                    //          changed them (i.e. the [error] command).
                    //
                    localEngineFlags = CombineFlags(interpreter, localEngineFlags, false, false);

                    if ((code == ReturnCode.Error) &&
                        !EngineFlagOps.HasErrorAlreadyLogged(localEngineFlags))
                    {
                        terminator = parseState.Terminator;

                        int commandStart = parseState.CommandStart;
                        int commandLength = parseState.CommandLength;

                        if (terminator == (commandStart + commandLength - 1))
                            commandLength--; // back off trailing command terminator...

                        LogCommandInformation(interpreter, text, commandStart,
                            commandLength, localEngineFlags, result, ref errorLine);

#if PREVIOUS_RESULT
                        if (previousResult != null) previousResult.ErrorLine = errorLine;
#endif
                    }
                }

                //
                // NOTE: If we previously pushed the global call frame (above), we also
                //       need to pop any leftover scope call frames now; otherwise, the
                //       call stack will be imbalanced.
                //
                if (global)
                    interpreter.PopGlobalCallFrame(true);

                nextIndex = parseState.CommandStart + parseState.CommandLength;
                charactersLeft -= (nextIndex - index);
                index = nextIndex;

                if (!nested)
                    goto exit;

                terminator = parseState.Terminator;
                nextIndex = Index.Invalid;

                while ((terminator < text.Length) && // TEST: Test this.
                       (charactersLeft > 0) &&
                       (text[terminator] != Characters.CloseBracket))
                {
                    if (Parser.ParseCommand(
                            interpreter, text, index,
                            charactersLeft, nested, parseState,
                            noReady, ref result) != ReturnCode.Ok)
                    {
                        goto exit;
                    }

                    terminator = parseState.Terminator;
                    nextIndex = parseState.CommandStart + parseState.CommandLength;
                    charactersLeft -= (nextIndex - index);
                    index = nextIndex;
                }

                if (terminator == characters)
                {
                    result = "missing close-bracket";
                }
                else if ((terminator < text.Length) && // TEST: Test this.
                         (text[terminator] != Characters.CloseBracket))
                {
                    result = "missing close-bracket";
                }

                code = ReturnCode.Error;
            }
            finally
            {
                if (usable)
                {
                    /* IGNORED */
                    interpreter.ExitEngineScriptLevel();
                }
            }

        exit:

            return EvaluateExited(
                interpreter, fileName, currentLine,
                text, startIndex, characters,
                localEngineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref code,
                ref result, ref errorLine);

        unusable:

            result = Result.Copy(
                InterpreterUnusableError, ResultFlags.CopyValue);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Scope Call Frame Methods
        internal static ReturnCode EvaluateScriptWithScopeFrame(
            Interpreter interpreter,             /* in */
            string text,                         /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            )
        {
            ReturnCode code;
            ICallFrame frame = null;

            code = EvaluateScriptWithScopeFrame(
                interpreter, text, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref frame, ref result,
                ref errorLine);

            /* IGNORED */
            CallFrameOps.MaybeStoreInto(frame, true, ref result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScriptWithScopeFrame(
            Interpreter interpreter,             /* in */
            string text,                         /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref ICallFrame frame,                /* in, out */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            if ((frame != null) &&
                !interpreter.IsScopeCallFrame(frame, ref result))
            {
                return ReturnCode.Error;
            }

            ICallFrame localFrame = null;
            ICallFrame newFrame = null;

            if (frame != null)
            {
                localFrame = frame;
            }
            else
            {
                newFrame = CallFrameOps.NewEngineScope(interpreter);

                if (interpreter.AddScope(newFrame,
                        ClientData.Empty, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                localFrame = newFrame;
            }

            interpreter.PushCallFrame(localFrame);

            try
            {
                return EvaluateScript(interpreter,
                    text, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, ref result,
                    ref errorLine);
            }
            finally
            {
                usable = IsUsableNoLock(interpreter);

                if (usable)
                {
                    //
                    // NOTE: Pop all scope call frames, including the brand
                    //       new one that we pushed above, just in case any
                    //       scope call frames were left open by the script
                    //       being evaluated.
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFrames();

                    //
                    // NOTE: Finally, return reference to the newly created
                    //       call frame to our caller.  It is possible that
                    //       this frame was actually deleted by the script.
                    //
                    if (newFrame != null)
                        frame = newFrame;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Asynchronous Methods
        public static ReturnCode EvaluateScript(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            AsynchronousCallback callback, /* NOTE: May be null for "fire-and-forget" type scripts. */
            IClientData clientData,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT, ASYNCHRONOUS */
        {
            if (interpreter != null)
            {
                if (text != null)
                {
                    try
                    {
                        if (ScriptOps.HasFlags(
                                interpreter, InterpreterFlags.UseNewEngineThread,
                                true))
                        {
                            Thread thread = CreateThread(
                                interpreter, EngineThreadStart, 0,
                                true, false, true);

                            if (thread == null)
                            {
                                error = "failed to create engine thread";
                                return ReturnCode.Error;
                            }

                            eventFlags &= ~EventFlags.DisposeThread;

                            thread.Start(new AsynchronousContext(
                                GlobalState.GetCurrentSystemThreadId(),
                                EngineMode.EvaluateScript, interpreter,
                                text, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, callback,
                                clientData));

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            eventFlags |= EventFlags.DisposeThread;

                            if (QueueWorkItem(interpreter, EngineThreadStart,
                                    new AsynchronousContext(
                                        GlobalState.GetCurrentSystemThreadId(),
                                        EngineMode.EvaluateScript, interpreter,
                                        text, engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags, callback,
                                        clientData)))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "could not queue work item";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid script";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (Stream) Methods
        public static ReturnCode EvaluateStream(
            Interpreter interpreter,
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateStream(
                interpreter, name, textReader, startIndex, characters,
                engineFlags, substitutionFlags, eventFlags, expressionFlags,
                ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is somewhat special.  It is the only place where the stream
        //       evaluation pipeline (via EvaluateStream) ends up calling into the string
        //       evaluation pipeline (via EvaluateScript).  Therefore, "special handling"
        //       for that transition (e.g. call frame management) should happen here [and
        //       only here].
        //
        public static ReturnCode EvaluateStream(
            Interpreter interpreter,
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result,
            ref int errorLine
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReadInt32Callback charCallback = null;
            ReadCharsCallback charsCallback = null;

            GetStreamCallbacks(
                textReader, ref charCallback, ref charsCallback);

            ReturnCode code;
            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            code = ReadScriptStream(
                interpreter, null, name, charCallback, charsCallback,
                startIndex, characters, ref engineFlags,
                ref substitutionFlags, ref eventFlags,
                ref expressionFlags, ref readScriptClientData,
                ref canRetry, ref result);

            if (code == ReturnCode.Ok)
            {
                string text = readScriptClientData.Text;

                bool newFrame = EngineFlagOps.HasExtraCallFrame(
                    engineFlags);

                if (newFrame)
                {
                    ICallFrame frame = interpreter.NewEngineCallFrame(
                        StringList.MakeList("stream", name),
                        CallFrameFlags.Engine);

                    interpreter.PushAutomaticCallFrame(frame);
                }

                try
                {
#if RESULT_LIMITS
                    int executeResultLimit = interpreter.InternalExecuteResultLimit;
                    int nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif

                    //
                    // BUGFIX: We need to know if this is the primary AppDomain
                    //         for the interpreter so we can check (potentially
                    //         many times) if the "cached" ParseState for the
                    //         interpreter needs to be manually refreshed from
                    //         within the main command loop (below).
                    //
                    bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                    bool argumentLocation = HasArgumentLocation(interpreter);
#endif

                    code = EvaluateScript(
                        interpreter, text, startIndex, characters,
                        engineFlags, substitutionFlags, eventFlags,
                        expressionFlags,
#if RESULT_LIMITS
                        executeResultLimit, nestedResultLimit,
#endif
                        sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                        argumentLocation,
#endif
                        ref result, ref errorLine);

                    if (code == ReturnCode.Return)
                    {
                        code = UpdateReturnInformation(interpreter);
                    }
                    else if (code == ReturnCode.Error)
                    {
                        AddErrorInformation(interpreter, result,
                            String.Format(
                                "{0}    (stream \"{1}\" line {2})",
                                Environment.NewLine,
                                FormatOps.Ellipsis(name),
                                errorLine));
                    }
                }
                finally
                {
                    if (newFrame)
                    {
                        //
                        // NOTE: Pop the original call frame that we
                        //       pushed above and any intervening scope
                        //       call frames that may be leftover (i.e.
                        //       they were not explicitly closed).
                        //
                        /* IGNORED */
                        interpreter.PopScopeCallFramesAndOneMore();
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Scope Call Frame Methods
        internal static ReturnCode EvaluateStreamWithScopeFrame(
            Interpreter interpreter,             /* in */
            string name,                         /* in */
            TextReader textReader,               /* in */
            int startIndex,                      /* in */
            int characters,                      /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;
            ICallFrame frame = null;

            code = EvaluateStreamWithScopeFrame(
                interpreter, name, textReader, startIndex,
                characters, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref frame,
                ref result, ref errorLine);

            /* IGNORED */
            CallFrameOps.MaybeStoreInto(frame, true, ref result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateStreamWithScopeFrame(
            Interpreter interpreter,             /* in */
            string name,                         /* in */
            TextReader textReader,               /* in */
            int startIndex,                      /* in */
            int characters,                      /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref ICallFrame frame,                /* in, out */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            if ((frame != null) &&
                !interpreter.IsScopeCallFrame(frame, ref result))
            {
                return ReturnCode.Error;
            }

            ICallFrame localFrame = null;
            ICallFrame newFrame = null;

            if (frame != null)
            {
                localFrame = frame;
            }
            else
            {
                newFrame = CallFrameOps.NewEngineScope(interpreter);

                if (interpreter.AddScope(newFrame,
                        ClientData.Empty, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                localFrame = newFrame;
            }

            interpreter.PushCallFrame(localFrame);

            try
            {
                return EvaluateStream(
                    interpreter, name, textReader, startIndex,
                    characters, engineFlags, substitutionFlags,
                    eventFlags, expressionFlags, ref result,
                    ref errorLine);
            }
            finally
            {
                usable = IsUsableNoLock(interpreter);

                if (usable)
                {
                    //
                    // NOTE: Pop all scope call frames, including the brand
                    //       new one that we pushed above, just in case any
                    //       scope call frames were left open by the script
                    //       being evaluated.
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFrames();

                    //
                    // NOTE: Finally, return reference to the newly created
                    //       call frame to our caller.  It is possible that
                    //       this frame was actually deleted by the script.
                    //
                    if (newFrame != null)
                        frame = newFrame;
                }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (File) Methods
        public static ReturnCode EvaluateFile(
            Interpreter interpreter,
            string fileName,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateFile(
                interpreter, fileName, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateFile(
                interpreter, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result,
            ref int errorLine
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateFile(
                interpreter, null, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateFile(
                interpreter, encoding, fileName, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            int errorLine = 0;

            ReturnCode code = EvaluateFile(
                interpreter, encoding, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result, ref errorLine);

            if (errorLine != 0)
                Interpreter.SetErrorLine(interpreter, errorLine);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is somewhat special.  It is the only place where the file
        //       evaluation pipeline (via EvaluateFile) ends up calling into the string
        //       evaluation pipeline (via EvaluateScript).  Therefore, "special handling"
        //       for that transition (e.g. call frame management) should happen here [and
        //       only here].
        //
        internal static ReturnCode EvaluateFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result,
            ref int errorLine
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;

            if (interpreter != null)
            {
                string text = null;

                code = ReadOrGetScriptFile(
                    interpreter, encoding, ref fileName,
                    ref engineFlags, ref substitutionFlags,
                    ref eventFlags, ref expressionFlags,
                    ref text, ref result);

                if (code == ReturnCode.Ok)
                {
                    bool newFrame = EngineFlagOps.HasExtraCallFrame(
                        engineFlags);

                    if (newFrame)
                    {
                        ICallFrame frame = interpreter.NewEngineCallFrame(
                            StringList.MakeList("file", fileName),
                            CallFrameFlags.Engine);

                        interpreter.PushAutomaticCallFrame(frame);
                    }

                    try
                    {
                        bool pushed = false;

                        interpreter.PushScriptLocation(fileName, true, ref pushed);

                        try
                        {
#if RESULT_LIMITS
                            int executeResultLimit = interpreter.InternalExecuteResultLimit;
                            int nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif

                            //
                            // BUGFIX: We need to know if this is the primary AppDomain
                            //         for the interpreter so we can check (potentially
                            //         many times) if the "cached" ParseState for the
                            //         interpreter needs to be manually refreshed from
                            //         within the main command loop (below).
                            //
                            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

                            code = EvaluateScript(
                                interpreter, fileName, Parser.StartLine,
                                text, 0, Length.Invalid, engineFlags,
                                substitutionFlags, eventFlags,
                                expressionFlags,
#if RESULT_LIMITS
                                executeResultLimit, nestedResultLimit,
#endif
                                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                argumentLocation,
#endif
                                ref result, ref errorLine);

                            if (code == ReturnCode.Return)
                            {
                                code = UpdateReturnInformation(interpreter);
                            }
                            else if (code == ReturnCode.Error)
                            {
                                AddErrorInformation(interpreter, result,
                                    String.Format(
                                        "{0}    (file \"{1}\" line {2})",
                                        Environment.NewLine,
                                        FormatOps.Ellipsis(fileName),
                                        errorLine));
                            }
                        }
                        finally
                        {
                            interpreter.PopScriptLocation(true, ref pushed);
                        }
                    }
                    finally
                    {
                        if (newFrame)
                        {
                            //
                            // NOTE: Pop the original call frame that we
                            //       pushed above and any intervening scope
                            //       call frames that may be leftover (i.e.
                            //       they were not explicitly closed).
                            //
                            /* IGNORED */
                            interpreter.PopScopeCallFramesAndOneMore();
                        }
                    }
                }

#if NOTIFY
                if (!EngineFlagOps.HasNoNotify(engineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.File, NotifyFlags.Evaluated,
                        new ObjectTriplet(encoding, fileName, code),
                        interpreter, null, null, null, ref result);
                }
#endif
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Scope Call Frame Methods
        internal static ReturnCode EvaluateFileWithScopeFrame(
            Interpreter interpreter,             /* in */
            Encoding encoding,                   /* in */
            string fileName,                     /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;
            ICallFrame frame = null;

            code = EvaluateFileWithScopeFrame(
                interpreter, encoding, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref frame, ref result, ref errorLine);

            /* IGNORED */
            CallFrameOps.MaybeStoreInto(frame, true, ref result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateFileWithScopeFrame(
            Interpreter interpreter,             /* in */
            Encoding encoding,                   /* in */
            string fileName,                     /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref ICallFrame frame,                /* in, out */
            ref Result result,                   /* out */
            ref int errorLine                    /* out */
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            if ((frame != null) &&
                !interpreter.IsScopeCallFrame(frame, ref result))
            {
                return ReturnCode.Error;
            }

            ICallFrame localFrame = null;
            ICallFrame newFrame = null;

            if (frame != null)
            {
                localFrame = frame;
            }
            else
            {
                newFrame = CallFrameOps.NewEngineScope(interpreter);

                if (interpreter.AddScope(newFrame,
                        ClientData.Empty, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                localFrame = newFrame;
            }

            interpreter.PushCallFrame(localFrame);

            try
            {
                return EvaluateFile(
                    interpreter, encoding, fileName, engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
                    ref result, ref errorLine);
            }
            finally
            {
                usable = IsUsableNoLock(interpreter);

                if (usable)
                {
                    //
                    // NOTE: Pop all scope call frames, including the brand
                    //       new one that we pushed above, just in case any
                    //       scope call frames were left open by the script
                    //       being evaluated.
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFrames();

                    //
                    // NOTE: Finally, return reference to the newly created
                    //       call frame to our caller.  It is possible that
                    //       this frame was actually deleted by the script.
                    //
                    if (newFrame != null)
                        frame = newFrame;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Asynchronous Methods
        public static ReturnCode EvaluateFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            AsynchronousCallback callback, /* NOTE: May be null for "fire-and-forget" type scripts. */
            IClientData clientData,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT, ASYNCHRONOUS */
        {
            if (interpreter != null)
            {
                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        if (ScriptOps.HasFlags(
                                interpreter, InterpreterFlags.UseNewEngineThread,
                                true))
                        {
                            Thread thread = CreateThread(
                                interpreter, EngineThreadStart, 0,
                                true, false, true);

                            if (thread == null)
                            {
                                error = "failed to create engine thread";
                                return ReturnCode.Error;
                            }

                            eventFlags &= ~EventFlags.DisposeThread;

                            thread.Start(new AsynchronousContext(
                                GlobalState.GetCurrentSystemThreadId(),
                                EngineMode.EvaluateFile, interpreter,
                                fileName, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, callback,
                                clientData));

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            eventFlags |= EventFlags.DisposeThread;

                            if (QueueWorkItem(interpreter, EngineThreadStart,
                                    new AsynchronousContext(
                                        GlobalState.GetCurrentSystemThreadId(),
                                        EngineMode.EvaluateFile, interpreter,
                                        fileName, engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags, callback,
                                        clientData)))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "could not queue work item";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Evaluation (Expression) Methods
        internal static ReturnCode EvaluateExpressionWithErrorInfo( /* INTERNAL USE ONLY */
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            string errorInfo,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            //
            // FIXME: The expression parser does not know the line where
            //        the error happened unless it evaluates a command
            //        contained within the expression.
            //
            Interpreter.SetErrorLine(interpreter, 0);

            ReturnCode code = EvaluateExpression(
                interpreter, text, engineFlags, substitutionFlags,
                eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result);

            if (code == ReturnCode.Error)
            {
                AddErrorInformation(interpreter, result,
                    String.Format(errorInfo, Environment.NewLine,
                        Interpreter.GetErrorLine(interpreter)));
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateExpression(
            Interpreter interpreter,
            string text,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return EvaluateExpression(
                interpreter, text, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateExpression(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            string fileName = null;
            int currentLine = Parser.StartLine;

#if DEBUGGER && DEBUGGER_EXPRESSION && DEBUGGER_BREAKPOINTS
            if (ScriptOps.GetLocation(
                    interpreter, false, false, ref fileName,
                    ref currentLine, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

#if RESULT_LIMITS
            int executeResultLimit = interpreter.InternalExecuteResultLimit;
            int nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif

            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            return EvaluateExpression(
                interpreter, fileName, currentLine, text,
                engineFlags, substitutionFlags, eventFlags,
                expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateExpression(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            string fileName = null;
            int currentLine = Parser.StartLine;

#if DEBUGGER && DEBUGGER_EXPRESSION && DEBUGGER_BREAKPOINTS
            if (ScriptOps.GetLocation(
                    interpreter, false, false, ref fileName,
                    ref currentLine, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

            return EvaluateExpression(
                interpreter, fileName, currentLine, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateExpression(
            Interpreter interpreter,
            string fileName,
            int currentLine,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            bool usable = IsUsableNoLock(interpreter, ref result);

            if (!usable)
                return ReturnCode.Error;

            EngineFlags localEngineFlags = CombineFlags(
                interpreter, engineFlags, true, true);

            if (EngineFlagOps.HasNoEvaluate(localEngineFlags))
            {
                result = "interpreter not accepting expressions to evaluate";
                return ReturnCode.Error;
            }

            bool noReady = EngineFlagOps.HasNoReady(localEngineFlags);

            /*
             * Reset the canceled flag of the interpreter, if required.
             */

            ResetCancel(interpreter, GetCancelFlags(localEngineFlags));

            /*
             * Reset the last return code for the interpreter, if required.
             */

            ResetReturnCode(interpreter, result,
                EngineFlagOps.HasResetReturnCode(localEngineFlags));

            ReturnCode code;

            code = CheckEvents(
                interpreter, localEngineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref result);

            if (code != ReturnCode.Ok)
                return code;

            IParseState parseState = null;

            /*
             * NOTE: This code is part of an experimental effort to
             *       improve performance and may be modified and/or
             *       removed later.
             */
#if PARSE_CACHE
            if (!interpreter.GetCachedParseState(text, ref parseState))
#endif
            {
                Result localError = null;

                parseState = new ParseState(
                    localEngineFlags, substitutionFlags, fileName, currentLine);

                code = ExpressionParser.ParseExpression(
                    interpreter, text, 0, Length.Invalid, parseState,
                    noReady, ref localError);

                if (code == ReturnCode.Ok)
                {
#if PARSE_CACHE
                    if (!EngineFlagOps.HasNoCacheParseState(localEngineFlags))
                    {
                        parseState.MakeImmutable();

                        /* IGNORED */
                        interpreter.AddCachedParseState(parseState);
                    }
#endif
                }
                else
                {
                    result = localError;
                    return code;
                }
            }

#if (DEBUGGER && DEBUGGER_EXPRESSION) || (NOTIFY && NOTIFY_EXPRESSION)
            IClientData parseStateClientData = null;
            ArgumentList arguments = null;
#endif

#if DEBUGGER && DEBUGGER_EXPRESSION
            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    localEngineFlags, BreakpointType.BeforeExpression))
            {
                arguments = new ArgumentList("text", text);

                code = CheckBreakpoints(
                    code, BreakpointType.BeforeExpression, null,
                    null, null, localEngineFlags, substitutionFlags,
                    eventFlags, expressionFlags, null, null,
                    interpreter, ClientData.WrapOrReplace(
                        parseStateClientData, parseState),
                    arguments, ref result);

                if (code != ReturnCode.Ok)
                    return code;
            }
#endif

            int savedExpressionLevels = 0;
            bool exception = false; /* NOT USED */
            Argument value = null;
            Result error = null;

            interpreter.PushSubExpression(ref savedExpressionLevels);

            try
            {
                code = ExpressionEvaluator.EvaluateSubExpression(
                    interpreter, parseState, 0, localEngineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
#if RESULT_LIMITS
                    executeResultLimit, nestedResultLimit,
#endif
                    noReady, sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                    argumentLocation,
#endif
                    ref usable, ref exception, ref value, ref error);
            }
            finally
            {
                interpreter.PopSubExpression(ref savedExpressionLevels);
            }

            if (code == ReturnCode.Ok)
                result = value;
            else
                result = error;

            if (usable)
            {
#if DEBUGGER && DEBUGGER_EXPRESSION
                if (DebuggerOps.CanHitBreakpoints(interpreter,
                        localEngineFlags, BreakpointType.AfterExpression))
                {
                    arguments = new ArgumentList(
                        "text", text, "value", value, "error", error);

                    code = CheckBreakpoints(
                        code, BreakpointType.AfterExpression, null,
                        null, null, localEngineFlags, substitutionFlags,
                        eventFlags, expressionFlags, null, null,
                        interpreter, ClientData.WrapOrReplace(
                            parseStateClientData, parseState),
                        arguments, ref result);
                }
#endif

#if NOTIFY && NOTIFY_EXPRESSION
                if (!EngineFlagOps.HasNoNotify(localEngineFlags))
                {
                    /* IGNORED */
                    interpreter.CheckNotification(
                        NotifyType.Expression, NotifyFlags.Evaluated,
                        new ObjectList(fileName, currentLine, text,
                        localEngineFlags, substitutionFlags, eventFlags,
                        expressionFlags, code), interpreter,
                        ClientData.WrapOrReplace(
                            parseStateClientData, parseState),
                        arguments, null, ref result);
                }
#endif
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Substitution Methods
        #region Substitution Exit-Hook Methods
#if (DEBUGGER && DEBUGGER_ENGINE) || NOTIFY
        private static ReturnCode SubstituteExited(
            Interpreter interpreter,             /* in */
            string fileName,                     /* in */
            int currentLine,                     /* in */
            string text,                         /* in */
            EngineFlags engineFlags,             /* in */
            SubstitutionFlags substitutionFlags, /* in */
            EventFlags eventFlags,               /* in */
            ExpressionFlags expressionFlags,     /* in */
            ref ReturnCode code,                 /* in, out */
            ref Result result                    /* in, out */
            )
        {
#if DEBUGGER && DEBUGGER_ENGINE
            BreakpointType breakpointType =
                BreakpointType.Exit | BreakpointType.Substitute;

            if (DebuggerOps.CanHitBreakpoints(interpreter,
                    engineFlags, breakpointType))
            {
                ReturnCode oldCode = code;

                Result oldResult = Result.Copy(
                    result, ResultFlags.CopyObject); /* COPY */

                code = CheckBreakpoints(
                    code, breakpointType, null,
                    null, null, engineFlags,
                    substitutionFlags, eventFlags,
                    expressionFlags, null, null,
                    interpreter, null, null,
                    ref result);

                //
                // TODO: What is the purpose of this if statement and the
                //       associated call to DebugOps.Complain?
                //
                // NOTE: It appears that the purpose of this check is to verify
                //       that the breakpoint, if any, did not cause the overall
                //       result of this script evaluation to be changed.
                //
                if ((code != oldCode) || !Result.Equals(result, oldResult))
                    DebugOps.Complain(interpreter, code, result);
            }
#endif

#if NOTIFY
            if ((interpreter != null) &&
                !EngineFlagOps.HasNoNotify(engineFlags))
            {
                /* IGNORED */
                interpreter.CheckNotification(
                    NotifyType.String, NotifyFlags.Substituted,
                    //
                    // BUGBUG: In order to use this class for notification
                    //         parameters, it really should probably be
                    //         made public.
                    //
                    new ObjectList(fileName, currentLine, text,
                    engineFlags, substitutionFlags, eventFlags,
                    expressionFlags, code), interpreter, null,
                    null, null, ref result);
            }
#endif

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Substitution (Token) Methods
        private static ReturnCode SubstituteTokens(
            Interpreter interpreter,
            IParseState parseState,
            int startTokenIndex,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref int tokenCount,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            /*
             * Each pass through this loop will substitute one token, and its
             * components, if any.
             */

            string text = parseState.Text;
            CommandBuilder substResult = null;

            for (int tokenIndex = startTokenIndex;
                    (tokenCount > 0) && (code == ReturnCode.Ok);
                    tokenCount--, tokenIndex++)
            {
                int index = Index.Invalid;
                int length = 0;
                IToken token = parseState.Tokens[tokenIndex];
                Result localResult = null;

                switch (token.Type)
                {
                    case TokenType.Text:
                        {
                            index = token.Start;
                            length = token.Length;

                            break;
                        }
                    case TokenType.Backslash:
                        {
                            char? character1 = null;
                            char? character2 = null;

                            Parser.ParseBackslash(
                                text, token.Start, token.Length,
                                ref character1, ref character2);

                            localResult = Result.FromCharacters(character1, character2);

                            break;
                        }
                    case TokenType.Command:
                        {
                            code = CheckEvents(
                                interpreter, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, ref localResult);

                            if (code == ReturnCode.Ok)
                            {
                                code = EvaluateScript(
                                    interpreter, text, token.Start + 1,
                                    token.Length - 2, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags,
#if RESULT_LIMITS
                                    executeResultLimit, nestedResultLimit,
#endif
                                    sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                    argumentLocation,
#endif
                                    ref localResult);
                            }

                            if (code == ReturnCode.Error)
                                result = localResult;

                            break;
                        }
                    case TokenType.Variable:
                    case TokenType.VariableNameOnly:
                        {
                            string varName = null;
                            string varIndex = null;

                            if (token.Components > 1)
                            {
                                code = CheckEvents(
                                    interpreter, engineFlags, substitutionFlags,
                                    eventFlags, expressionFlags, ref localResult);

                                if (code == ReturnCode.Ok)
                                {
                                    code = EvaluateTokens(
                                        interpreter, parseState,
                                        tokenIndex + 2,
#if RESULT_LIMITS
                                        executeResultLimit,
                                        nestedResultLimit,
#endif
                                        token.Components - 1,
                                        engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags,
                                        sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                        argumentLocation,
#endif
                                        ref localResult);
                                }

                                if (code == ReturnCode.Ok)
                                    varIndex = localResult;
                            }

                            if (code == ReturnCode.Ok)
                            {
                                varName = text.Substring(
                                    parseState.Tokens[tokenIndex + 1].Start,
                                    parseState.Tokens[tokenIndex + 1].Length);

                                code = GetTokenVariableValue(interpreter,
                                    varName, varIndex, ref localResult);
                            }

                            switch (code)
                            {
                                case ReturnCode.Ok:       /* Got value */
                                    {
                                        break;
                                    }
                                case ReturnCode.Error:    /* Give error message to caller. */
                                    {
                                        result = localResult;
                                        break;
                                    }
                                case ReturnCode.Break:    /* Will not substitute anyway */
                                case ReturnCode.Continue: /* Will not substitute anyway */
                                    {
                                        break;
                                    }
                                default:
                                    {
                                        /*
                                         * All other return codes, we will subst the result from the
                                         * code-throwing evaluation.
                                         */
                                        break;
                                    }
                            }

                            tokenCount -= token.Components;
                            tokenIndex += token.Components;

                            break;
                        }
                    default:
                        {
                            result = String.Format(
                                "unexpected token type {0} for substitution",
                                token.Type);

                            return ReturnCode.Error;
                        }
                }

                if ((code == ReturnCode.Break) || (code == ReturnCode.Continue))
                {
                    /*
                     * Inhibit substitution.
                     */
                    continue;
                }

                //
                // NOTE: If there was no result, there is now.
                //
                if (substResult == null)
                    substResult = CommandBuilder.Create();

                if (localResult != null) // INTL: do not change to String.IsNullOrEmpty
                {
#if RESULT_LIMITS
                    if (!substResult.HaveEnoughCapacity(
                            nestedResultLimit, localResult, ref result))
                    {
                        return ReturnCode.Error;
                    }
#endif

                    substResult.Add(localResult);
                }
                else if (index != Index.Invalid)
                {
#if RESULT_LIMITS
                    if (!substResult.HaveEnoughCapacity(
                            nestedResultLimit, length, ref result))
                    {
                        return ReturnCode.Error;
                    }
#endif

                    substResult.Add(text, index, length);
                }
            }

            if (code != ReturnCode.Error)
            {
                if (substResult != null)
                    result = Result.FromCommandBuilder(substResult);
                else
                    ResetResult(interpreter, engineFlags, ref result);
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Substitution (String) Methods
        //
        // WARNING: This method creates and disposes its own "single-use"
        //          interpreter object.  Before using this method, make
        //          sure that is what you want.  This method is custom
        //          tailored to work from inside SQL Server.
        //
        public static int /* ReturnCode */ SubstituteOneString(
            string text,
            ref string result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;
            Result localResult = null;

            using (Interpreter interpreter = Interpreter.Create(
                    null, CreateFlags.SingleUse, HostCreateFlags.SingleUse,
                    ref localResult))
            {
                if (interpreter != null)
                    code = SubstituteString(
                        interpreter, text, SubstitutionFlags.Default,
                        ref localResult);
                else
                    code = ReturnCode.Error;
            }

            result = localResult;
            return (int)code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SubstituteString(
            Interpreter interpreter,
            string text,
            SubstitutionFlags substitutionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return SubstituteString(
                interpreter, text, EngineFlags.None,
                substitutionFlags, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SubstituteString(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            string fileName = null;
            int currentLine = Parser.StartLine;

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            if (ScriptOps.GetLocation(
                    interpreter, false, false, ref fileName,
                    ref currentLine, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }
#endif

#if RESULT_LIMITS
            int executeResultLimit = 0;
            int nestedResultLimit = 0;

            if (interpreter != null)
            {
                executeResultLimit = interpreter.InternalExecuteResultLimit;
                nestedResultLimit = interpreter.InternalNestedResultLimit;
            }
#endif

            //
            // BUGFIX: We need to know if this is the primary AppDomain
            //         for the interpreter so we can check (potentially
            //         many times) if the "cached" ParseState for the
            //         interpreter needs to be manually refreshed from
            //         within the main command loop (below).
            //
            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

            return SubstituteString(
                interpreter, fileName, currentLine, text,
                engineFlags, substitutionFlags, eventFlags,
                expressionFlags,
#if RESULT_LIMITS
                executeResultLimit, nestedResultLimit,
#endif
                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                argumentLocation,
#endif
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode SubstituteString(
            Interpreter interpreter,
            string fileName,
            int currentLine,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
#if RESULT_LIMITS
            int executeResultLimit,
            int nestedResultLimit,
#endif
            bool sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
            bool argumentLocation,
#endif
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;
            EngineFlags localEngineFlags = engineFlags;

            if (interpreter != null)
            {
                localEngineFlags = CombineFlags(interpreter, engineFlags, true, true);

                if (!EngineFlagOps.HasNoSubstitute(localEngineFlags))
                {
                    bool noReady = EngineFlagOps.HasNoReady(localEngineFlags);

                    if (text != null) // INTL: do not change to String.IsNullOrEmpty
                    {
                        IParseState parseState = null;
                        Result error = null;
                        int index = 0;
                        int length = text.Length;

                        Parser.Initialize(
                            interpreter, fileName, currentLine, text,
                            index, length, localEngineFlags, substitutionFlags,
                            ref parseState);

                        /*
                         * Reset the canceled flag of the interpreter, if required.
                         */

                        ResetCancel(interpreter, GetCancelFlags(localEngineFlags));

                        /*
                         * Reset the last return code for the interpreter, if required.
                         */

                        ResetReturnCode(interpreter, result,
                            EngineFlagOps.HasResetReturnCode(localEngineFlags));

                        /*
                         * First parse the string rep of objPtr, as if it were enclosed as a
                         * "-quoted word in a normal Tcl command. Honor flags that selectively
                         * inhibit types of substitution.
                         */

                        if (Parser.ParseTokens(
                                interpreter, index, length, CharacterType.None,
                                parseState, noReady, ref result) != ReturnCode.Ok)
                        {
                            /*
                             * There was a parse error. Save the error message for possible
                             * reporting later.
                             */

                            error = result;

                            /*
                             * We need to re-parse to get the portion of the string we can [subst]
                             * before the parse error. Sadly, all the Tcl_Token's created by the
                             * first parse attempt are gone, freed according to the public spec
                             * for the Tcl_Parse* routines. The only clue we have is parse.term,
                             * which points to either the unmatched opener, or to characters that
                             * follow a close brace or close quote.
                             *
                             * Call ParseTokens again, working on the string up to parse.term.
                             * Keep repeating until we get a good parse on a prefix.
                             */

                            do
                            {
                                parseState.Tokens.Clear();
                                parseState.Characters = parseState.Terminator;
                                parseState.Incomplete = false;
                                parseState.ParseError = ParseError.Success;
                            }
                            while ((Parser.ParseTokens(
                                        interpreter, index, parseState.Characters,
                                        CharacterType.None, parseState,
                                        noReady, ref result) != ReturnCode.Ok)
                                    && !parseState.NotReady); // BUGFIX: Must be ready.

                            //
                            // BUGFIX: Make sure we completed all parsing successfully and were
                            //         not interrupted.
                            //
                            if (!parseState.NotReady)
                            {
                                /*
                                 * The good parse will have to be followed by {, (, or [.
                                 */

                                switch (text[parseState.Terminator])
                                {
                                    case Characters.OpenBrace:
                                        {
                                            /*
                                             * Parse error was a missing } in a ${varname} variable
                                             * substitution at the toplevel. We will subst everything up to
                                             * that broken variable substitution before reporting the parse
                                             * error. Substituting the leftover '$' will have no side-effects,
                                             * so the current token stream is fine.
                                             */
                                            break;
                                        }
                                    case Characters.OpenParenthesis:
                                        {
                                            /*
                                             * Parse error was during the parsing of the index part of an
                                             * array variable substitution at the toplevel.
                                             */

                                            if (text[parseState.Terminator - 1] == Characters.DollarSign)
                                            {
                                                /*
                                                 * Special case where removing the array index left us with
                                                 * just a dollar sign (array variable with name the empty
                                                 * string as its name), instead of with a scalar variable
                                                 * reference.
                                                 *
                                                 * As in the previous case, existing token stream is OK.
                                                 */
                                            }
                                            else
                                            {
                                                /*
                                                 * The current parse includes a successful parse of a scalar
                                                 * variable substitution where there should have been an array
                                                 * variable substitution. We remove that mistaken part of the
                                                 * parse before moving on. A scalar variable substitution is
                                                 * two tokens.
                                                 */

                                                if ((parseState.Tokens[parseState.Tokens.Last - 1].Type != TokenType.Variable) &&
                                                    (parseState.Tokens[parseState.Tokens.Last - 1].Type != TokenType.VariableNameOnly))
                                                {
                                                    result = String.Format(
                                                        "unexpected token type {0}", FormatOps.WrapOrNull(
                                                        parseState.Tokens[parseState.Tokens.Last - 1].Type));

                                                    code = ReturnCode.Error;

                                                    goto exit;
                                                }

                                                if (parseState.Tokens[parseState.Tokens.Last].Type != TokenType.Text)
                                                {
                                                    result = String.Format(
                                                        "unexpected token type {0}", FormatOps.WrapOrNull(
                                                        parseState.Tokens[parseState.Tokens.Last].Type));

                                                    code = ReturnCode.Error;

                                                    goto exit;
                                                }

                                                parseState.Tokens.RemoveAt(parseState.Tokens.Last, 2);
                                            }
                                            break;
                                        }
                                    case Characters.OpenBracket:
                                        {
                                            /*
                                             * Parse error occurred during parsing of a toplevel command
                                             * substitution.
                                             */

                                            parseState.Characters = index + length;
                                            index = parseState.Terminator + 1;
                                            length = parseState.Terminator - index;

                                            if (length == 0)
                                            {
                                                /*
                                                 * No commands, just an unmatched [. As in previous cases,
                                                 * existing token stream is OK.
                                                 */
                                            }
                                            else
                                            {
                                                /*
                                                 * We want to add the parsing of as many commands as we can
                                                 * within that substitution until we reach the actual parse
                                                 * error. We'll do additional parsing to determine what length
                                                 * to claim for the final TCL_TOKEN_COMMAND token.
                                                 */

                                                int lastTerminator = parseState.Terminator;

                                                IParseState nestedParseState = new ParseState(
                                                    localEngineFlags, substitutionFlags, parseState.FileName,
                                                    parseState.CurrentLine);

                                                while (Parser.ParseCommand(
                                                        interpreter, text, index,
                                                        length, noReady, nestedParseState,
                                                        false, ref result) == ReturnCode.Ok)
                                                {
                                                    index = nestedParseState.Terminator +
                                                        ConversionOps.ToInt(nestedParseState.Terminator < nestedParseState.Characters);

                                                    length = nestedParseState.Characters - index;

                                                    if ((length == 0) && (nestedParseState.Terminator == nestedParseState.Characters))
                                                    {
                                                        /*
                                                         * If we run out of string, blame the missing close
                                                         * bracket on the last command, and do not evaluate it
                                                         * during substitution.
                                                         */
                                                        break;
                                                    }

                                                    lastTerminator = nestedParseState.Terminator;
                                                }

                                                if (lastTerminator == parseState.Terminator)
                                                {
                                                    /*
                                                     * Parse error in first command. No commands to subst, add
                                                     * no more tokens.
                                                     */
                                                    break;
                                                }

                                                /*
                                                 * Create a command substitution token for whatever commands
                                                 * got parsed.
                                                 */

                                                IToken token = ParseToken.FromState(interpreter, parseState);

                                                token.Start = parseState.Terminator;
                                                token.Components = 0;
                                                token.Type = TokenType.Command;
                                                token.Length = lastTerminator - token.Start + 1;

                                                parseState.Tokens.Add(token);
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            result = String.Format(
                                                "bad parse in SubstituteString: {0}",
                                                text[parseState.Terminator]);

                                            code = ReturnCode.Error;

                                            goto exit;
                                        }
                                }
                            }
                            else
                            {
                                //
                                // NOTE: Not ready, canceled, etc.  The result already contains
                                //       the error message.
                                //
                                code = ReturnCode.Error;

                                goto exit;
                            }
                        }

                        /*
                         * Next, substitute the parsed tokens just as in normal Tcl evaluation.
                         */

                        int tokensLeft = parseState.Tokens.Count;
                        Result localResult = null;

                        code = SubstituteTokens(
                            interpreter, parseState,
                            parseState.Tokens.Count - tokensLeft,
#if RESULT_LIMITS
                            executeResultLimit,
                            nestedResultLimit,
#endif
                            sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            argumentLocation,
#endif
                            ref tokensLeft, localEngineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, ref localResult);

                        if (code == ReturnCode.Ok)
                        {
                            if (error != null) // INTL: do not change to String.IsNullOrEmpty
                            {
                                result = error;
                                code = ReturnCode.Error;

                                goto exit;
                            }

                            result = localResult;
                            code = ReturnCode.Ok; // NOTE: Redundant.

                            goto exit;
                        }

                        CommandBuilder substResult = CommandBuilder.Create();

                        while (true)
                        {
                            switch (code)
                            {
                                case ReturnCode.Error:
                                    {
                                        result = localResult;
                                        code = ReturnCode.Error;

                                        goto exit;
                                    }
                                case ReturnCode.Break:
                                    {
                                        tokensLeft = 0; /* Halt substitution */
                                        goto default; // FALL-THROUGH
                                    }
                                default:
                                    {
#if RESULT_LIMITS
                                        if (!substResult.HaveEnoughCapacity(
                                                nestedResultLimit, localResult,
                                                ref result))
                                        {
                                            code = ReturnCode.Error;
                                            goto exit;
                                        }
#endif

                                        substResult.Add(localResult);
                                        break;
                                    }
                            }

                            if (tokensLeft == 0)
                            {
                                if ((error != null) && (code != ReturnCode.Break)) // INTL: do not change to String.IsNullOrEmpty
                                {
                                    result = error;
                                    code = ReturnCode.Error;

                                    goto exit;
                                }

                                result = Result.FromCommandBuilder(substResult);
                                code = ReturnCode.Ok;

                                goto exit;
                            }

                            code = SubstituteTokens(
                                interpreter, parseState,
                                parseState.Tokens.Count - tokensLeft,
#if RESULT_LIMITS
                                executeResultLimit,
                                nestedResultLimit,
#endif
                                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                argumentLocation,
#endif
                                ref tokensLeft, localEngineFlags,
                                substitutionFlags, eventFlags,
                                expressionFlags, ref localResult);
                        }
                    }
                    else
                    {
                        result = "invalid string";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "interpreter not accepting text to substitute";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

        exit:

#if (DEBUGGER && DEBUGGER_ENGINE) || NOTIFY
            return SubstituteExited(
                interpreter, fileName, currentLine,
                text, localEngineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref code,
                ref result);
#else
            return code;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Asynchronous Methods
        public static ReturnCode SubstituteString(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            AsynchronousCallback callback, /* NOTE: May be null for "fire-and-forget" type scripts. */
            IClientData clientData,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT, ASYNCHRONOUS */
        {
            if (interpreter != null)
            {
                if (text != null)
                {
                    try
                    {
                        if (ScriptOps.HasFlags(
                                interpreter, InterpreterFlags.UseNewEngineThread,
                                true))
                        {
                            Thread thread = CreateThread(
                                interpreter, EngineThreadStart, 0,
                                true, false, true);

                            if (thread == null)
                            {
                                error = "failed to create engine thread";
                                return ReturnCode.Error;
                            }

                            eventFlags &= ~EventFlags.DisposeThread;

                            thread.Start(new AsynchronousContext(
                                GlobalState.GetCurrentSystemThreadId(),
                                EngineMode.SubstituteString, interpreter,
                                text, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, callback,
                                clientData));

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            eventFlags |= EventFlags.DisposeThread;

                            if (QueueWorkItem(interpreter, EngineThreadStart,
                                    new AsynchronousContext(
                                        GlobalState.GetCurrentSystemThreadId(),
                                        EngineMode.SubstituteString, interpreter,
                                        text, engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags, callback,
                                        clientData)))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "could not queue work item";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid script";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Substitution (Stream) Methods
        //
        // NOTE: This method is somewhat special.  It is the only place where the stream
        //       substitution pipeline (via SubstituteStream) ends up calling into the
        //       string substitution pipeline (via SubstituteString).  Therefore,
        //       "special handling" for that transition (e.g. call frame management)
        //       should happen here [and only here].
        //
        public static ReturnCode SubstituteStream(
            Interpreter interpreter,
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReadInt32Callback charCallback = null;
            ReadCharsCallback charsCallback = null;

            GetStreamCallbacks(
                textReader, ref charCallback, ref charsCallback);

            ReturnCode code;
            ReadScriptClientData readScriptClientData = null;
            bool canRetry = false; /* NOT USED */

            code = ReadScriptStream(
                interpreter, null, name, charCallback, charsCallback,
                startIndex, characters, ref engineFlags,
                ref substitutionFlags, ref eventFlags,
                ref expressionFlags, ref readScriptClientData,
                ref canRetry, ref result);

            if (code == ReturnCode.Ok)
            {
                string text = readScriptClientData.Text;

                bool newFrame = EngineFlagOps.HasExtraCallFrame(
                    engineFlags);

                if (newFrame)
                {
                    ICallFrame frame = interpreter.NewEngineCallFrame(
                        StringList.MakeList("stream", name),
                        CallFrameFlags.Engine);

                    interpreter.PushAutomaticCallFrame(frame);
                }

                try
                {
#if RESULT_LIMITS
                    int executeResultLimit = interpreter.InternalExecuteResultLimit;
                    int nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif

                    //
                    // BUGFIX: We need to know if this is the primary AppDomain
                    //         for the interpreter so we can check (potentially
                    //         many times) if the "cached" ParseState for the
                    //         interpreter needs to be manually refreshed from
                    //         within the main command loop (below).
                    //
                    bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                    bool argumentLocation = HasArgumentLocation(interpreter);
#endif

                    code = SubstituteString(
                        interpreter, name, Parser.StartLine, text,
                        engineFlags, substitutionFlags, eventFlags,
                        expressionFlags,
#if RESULT_LIMITS
                        executeResultLimit, nestedResultLimit,
#endif
                        sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                        argumentLocation,
#endif
                        ref result);
                }
                finally
                {
                    if (newFrame)
                    {
                        //
                        // NOTE: Pop the original call frame that we
                        //       pushed above and any intervening scope
                        //       call frames that may be leftover (i.e.
                        //       they were not explicitly closed).
                        //
                        /* IGNORED */
                        interpreter.PopScopeCallFramesAndOneMore();
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Substitution (File) Methods
        public static ReturnCode SubstituteFile(
            Interpreter interpreter,
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return SubstituteFile(
                interpreter, fileName, EngineFlags.None,
                substitutionFlags, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SubstituteFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return SubstituteFile(
                interpreter, null, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SubstituteFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            return SubstituteFile(
                interpreter, encoding, fileName, EngineFlags.None,
                substitutionFlags, EventFlags.Default,
                ExpressionFlags.Default, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is somewhat special.  It is the only place where the file
        //       substitution pipeline (via SubstituteFile) ends up calling into the
        //       string substitution pipeline (via SubstituteString).  Therefore,
        //       "special handling" for that transition (e.g. call frame management)
        //       should happen here [and only here].
        //
        public static ReturnCode SubstituteFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            ReturnCode code;

            if (interpreter != null)
            {
                string text = null;

                code = ReadOrGetScriptFile(
                    interpreter, encoding, ref fileName,
                    ref engineFlags, ref substitutionFlags,
                    ref eventFlags, ref expressionFlags, ref text,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    bool newFrame = EngineFlagOps.HasExtraCallFrame(
                        engineFlags);

                    if (newFrame)
                    {
                        ICallFrame frame = interpreter.NewEngineCallFrame(
                            StringList.MakeList("file", fileName),
                            CallFrameFlags.Engine);

                        interpreter.PushAutomaticCallFrame(frame);
                    }

                    try
                    {
                        bool pushed = false;

                        interpreter.PushScriptLocation(fileName, true, ref pushed);

                        try
                        {
#if RESULT_LIMITS
                            int executeResultLimit = interpreter.InternalExecuteResultLimit;
                            int nestedResultLimit = interpreter.InternalNestedResultLimit;
#endif

                            //
                            // BUGFIX: We need to know if this is the primary AppDomain
                            //         for the interpreter so we can check (potentially
                            //         many times) if the "cached" ParseState for the
                            //         interpreter needs to be manually refreshed from
                            //         within the main command loop (below).
                            //
                            bool sameAppDomain = AppDomainOps.IsSame(interpreter);

#if DEBUGGER && DEBUGGER_BREAKPOINTS
                            bool argumentLocation = HasArgumentLocation(interpreter);
#endif

                            //
                            // FIXME: The [subst] parser does not know the line
                            //        where the error happened.
                            //
                            Interpreter.SetErrorLine(interpreter, 0);

                            code = SubstituteString(
                                interpreter, fileName, Parser.StartLine, text,
                                engineFlags, substitutionFlags, eventFlags,
                                expressionFlags,
#if RESULT_LIMITS
                                executeResultLimit, nestedResultLimit,
#endif
                                sameAppDomain,
#if DEBUGGER && DEBUGGER_BREAKPOINTS
                                argumentLocation,
#endif
                                ref result);

                            if (code == ReturnCode.Return)
                            {
                                code = UpdateReturnInformation(interpreter);
                            }
                            else if (code == ReturnCode.Error)
                            {
                                AddErrorInformation(interpreter, result,
                                    String.Format("{0}    (file \"{1}\" line {2})",
                                        Environment.NewLine, FormatOps.Ellipsis(fileName),
                                        Interpreter.GetErrorLine(interpreter)));
                            }
                        }
                        finally
                        {
                            interpreter.PopScriptLocation(true, ref pushed);
                        }
                    }
                    finally
                    {
                        if (newFrame)
                        {
                            //
                            // NOTE: Pop the original call frame that we
                            //       pushed above and any intervening scope
                            //       call frames that may be leftover (i.e.
                            //       they were not explicitly closed).
                            //
                            /* IGNORED */
                            interpreter.PopScopeCallFramesAndOneMore();
                        }
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Asynchronous Methods
        public static ReturnCode SubstituteFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            AsynchronousCallback callback, /* NOTE: May be null for "fire-and-forget" type scripts. */
            IClientData clientData,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT, ASYNCHRONOUS */
        {
            if (interpreter != null)
            {
                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        if (ScriptOps.HasFlags(
                                interpreter, InterpreterFlags.UseNewEngineThread,
                                true))
                        {
                            Thread thread = CreateThread(
                                interpreter, EngineThreadStart, 0,
                                true, false, true);

                            if (thread == null)
                            {
                                error = "failed to create engine thread";
                                return ReturnCode.Error;
                            }

                            eventFlags &= ~EventFlags.DisposeThread;

                            thread.Start(new AsynchronousContext(
                                GlobalState.GetCurrentSystemThreadId(),
                                EngineMode.SubstituteFile, interpreter,
                                fileName, engineFlags, substitutionFlags,
                                eventFlags, expressionFlags, callback,
                                clientData));

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            eventFlags |= EventFlags.DisposeThread;

                            if (QueueWorkItem(interpreter, EngineThreadStart,
                                    new AsynchronousContext(
                                        GlobalState.GetCurrentSystemThreadId(),
                                        EngineMode.SubstituteFile, interpreter,
                                        fileName, engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags, callback,
                                        clientData)))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "could not queue work item";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion
        #endregion
    }
}
