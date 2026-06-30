/*
 * DebugOps.cs --
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

#if REMOTING
using System.Runtime.Remoting;
#endif

using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using SDD = System.Diagnostics.Debugger;

using ComplaintTriplet = Eagle._Components.Public.AnyTriplet<
    long, long, Eagle._Components.Public.Result>;

#if TEST
using IBufferedTraceListener = Eagle._Tests.Default.IBufferedTraceListener;
using ScriptTraceListener = Eagle._Tests.Default.ScriptTraceListener;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private debugging, diagnostic, and complaint
    /// reporting support used throughout the Eagle core.  It centralizes the
    /// logic for capturing stack traces and method names, recording and
    /// emitting complaints (internal error reports) to a variety of output
    /// sinks (trace listeners, the interpreter host, a text writer, and the
    /// test "puts" channel), managing trace listeners and the trace log file,
    /// and breaking into an attached debugger.  All members are static and the
    /// class is not intended to be instantiated.
    /// </summary>
    [ObjectId("1d388444-db3b-41b5-a23e-b25084d1c94b")]
    internal static class DebugOps
    {
        #region Public Constants
        /// <summary>
        /// The default trace and debug category name, as provided by the
        /// underlying System.Diagnostics.Debugger.DefaultCategory value.
        /// </summary>
        public static readonly string DefaultCategory = SDD.DefaultCategory;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // HACK: This is the name of the private field within the
        //       TextWriterTraceListener class that contains the
        //       fully qualified name of the associated log file.
        //       Ideally, this should not be necessary; however,
        //       apparently Microsoft does not grasp this.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The name of the private file-name field within the
        /// TextWriterTraceListener class on the .NET Framework.
        /// </summary>
        private static string TextWriterFileNameFieldName1 =
            "fileName"; /* .NET Framework */

        /// <summary>
        /// The name of the private file-name field within the
        /// TextWriterTraceListener class on .NET Core.
        /// </summary>
        private static string TextWriterFileNameFieldName2 =
            "_fileName"; /* .NET Core */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name used for the trace listener associated with the
        /// interpreter log file.
        /// </summary>
        private static readonly string ListenerName =
            typeof(Interpreter).FullName + ".LogFile";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the AppDomain data slot that stores the trace log file
        /// name.
        /// </summary>
        private const string TraceLogFileDataName = "TraceLogFileName";
        /// <summary>
        /// The name of the AppDomain data slot that stores the trace log name.
        /// </summary>
        private const string TraceLogDataName = "TraceLogName";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the AppDomain data slot that stores the interpreter
        /// associated with the trace log.
        /// </summary>
        private const string TraceLogInterpreterDataName = "TraceLogInterpreter";
        /// <summary>
        /// The name of the AppDomain data slot that stores the encoding used
        /// for the trace log.
        /// </summary>
        private const string TraceLogEncodingDataName = "TraceLogEncoding";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to report a failed write of text, including
        /// the complaint identifier, the exception, and a trailing line
        /// terminator.
        /// </summary>
        private static readonly string TextWriteExceptionFormat =
            "write of text failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to report a failed write to the interpreter
        /// host, including the complaint identifier, the exception, and a
        /// trailing line terminator.
        /// </summary>
        private static readonly string HostWriteExceptionFormat =
            "write to host failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to report a failed write via the test "puts"
        /// channel, including the complaint identifier, the exception, and a
        /// trailing line terminator.
        /// </summary>
        private static readonly string TestWriteExceptionFormat =
            "write via test failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to report that a text writer for an
        /// interpreter was disposed and has therefore been disabled.
        /// </summary>
        private static readonly string TextWriterDisposedFormat =
            "{0} text writer for interpreter {1} was disposed and is now " +
            "disabled{2}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to report that breaking into the debugger
        /// was disabled via an environment variable.
        /// </summary>
        private static readonly string BreakIsDisabled =
            "breaking into debugger was disabled via environment variable " +
            "\"{0}\": {1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The characters trimmed from the start and end of a captured stack
        /// trace string.
        /// </summary>
        private static readonly char[] StackTrimChars = {
            Characters.CarriageReturn, Characters.LineFeed
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The maximum number of times the complaint subsystem will retry
        /// acquiring its lock before giving up.
        /// </summary>
        private static int ComplainRetryLimit = 3;
        /// <summary>
        /// The number of milliseconds to wait between successive complaint lock
        /// retry attempts.
        /// </summary>
        private static int ComplainRetryMilliseconds = 750;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, trace listener operations consider only listeners of
        /// the same type by default.
        /// </summary>
        private static bool DefaultSameTraceListenerTypeOnly = true;

        ///////////////////////////////////////////////////////////////////////

#if TEST
        //
        // NOTE: These are the format strings used when building the test
        //       trace log file name.
        //
        /// <summary>
        /// The format string used to build the test trace log file name when no
        /// log name is present.
        /// </summary>
        private const string TraceBareLogFileFormat = "trace-{1}-";
        /// <summary>
        /// The format string used to build the test trace log file name when a
        /// log name is present.
        /// </summary>
        private const string TraceNameLogFileFormat = "trace-{0}-{1}-";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // HACK: This synchronization object is used ONLY while writing to
        //       the collections of trace listeners.  Hopefully, this will
        //       prevent trace messages from being improperly interleaved
        //       in the resulting output.
        //
        /// <summary>
        /// The synchronization object used to serialize writes to the
        /// collections of trace listeners and to the recorded complaints.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The current number of calls to Complain() that are active
        //       on this thread.  This number should always be zero or one.
        //
        // BUGFIX: Previously, this was a global value, not per thread, and
        //         that was wrong.
        //
        /// <summary>
        /// The current number of active calls to Complain() on this thread.
        /// This value should always be zero or one.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static int complainLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       per-thread and never reset.
        //
        /// <summary>
        /// The number of times Complain() has been called on this thread.  It
        /// is per-thread and is never reset.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static long complainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       global (AppDomain) and never reset.
        //
        /// <summary>
        /// The number of times Complain() has been called.  It is global (per
        /// AppDomain) and is never reset.
        /// </summary>
        private static long globalComplainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called while
        //       quiet mode is enabled.  It is per-thread and never reset.
        //
        /// <summary>
        /// The number of times Complain() has been called while quiet mode was
        /// enabled.  It is per-thread and is never reset.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static long complainQuietCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called while
        //       quiet mode is enabled.  It is global (AppDomain) and never
        //       reset.
        //
        /// <summary>
        /// The number of times Complain() has been called while quiet mode was
        /// enabled.  It is global (per AppDomain) and is never reset.
        /// </summary>
        private static long globalComplainQuietCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The most recent complaint message seen by this subsystem.
        //
        /// <summary>
        /// The most recent complaint message seen by this subsystem.
        /// </summary>
        private static string globalComplaint = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, failsafe write calls will also
        //       output to the trace listeners, if any.
        //
        /// <summary>
        /// When non-zero, failsafe write calls will also output to the trace
        /// listeners, if any.
        /// </summary>
        private static bool UseTraceForWithoutFail = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, failsafe write calls will also
        //       output to the specified IDebugHost, if any.
        //
        /// <summary>
        /// When non-zero, failsafe write calls will also output to the
        /// specified IDebugHost, if any.
        /// </summary>
        private static bool UseHostForWithoutFail = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, the interpreter host will be
        //       used to emit a complaint; otherwise, it will be skipped.
        //
        /// <summary>
        /// When non-zero, the interpreter host will be used to emit a
        /// complaint; otherwise, it will be skipped.
        /// </summary>
        private static bool UseHostForComplain = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, the text write will be used
        //       to emit a complaint; otherwise, it will be skipped.
        //
        /// <summary>
        /// When non-zero, the text writer will be used to emit a complaint;
        /// otherwise, it will be skipped.
        /// </summary>
        private static bool UseTextWriterForComplain = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, all complaints will be treated
        //       as trace messages instead of using the complaint handling
        //       subsystem.  It should be noted that the complaint counts
        //       will still be updated, the complaint callback will still
        //       be called (if set), and the infinite recursion prevention
        //       will still be used.
        //
        /// <summary>
        /// When non-zero, all complaints are treated as trace messages instead
        /// of using the complaint handling subsystem.  The complaint counts are
        /// still updated, the complaint callback is still called (if set), and
        /// the infinite recursion prevention is still used.
        /// </summary>
        private static bool UseOnlyTraceForComplain = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, exceptions thrown by the complain
        //       callback are simply ignored; otherwise, the default complain
        //       mechanism will be used after an exception is caught from the
        //       callback.
        //
        /// <summary>
        /// When non-zero, exceptions thrown by the complaint callback are
        /// simply ignored; otherwise, the default complaint mechanism is used
        /// after an exception is caught from the callback.
        /// </summary>
        private static bool IgnoreOnCallbackThrow = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// When non-zero, complaints are permitted to be emitted via the trace
        /// subsystem.
        /// </summary>
        private static bool AllowComplainViaTrace = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// When non-zero, complaints are permitted to be emitted via the test
        /// "puts" channel.
        /// </summary>
        private static bool AllowComplainViaTest = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// When non-zero, the current interpreter is skipped when selecting an
        /// interpreter to use for complaint output via the test "puts" channel.
        /// </summary>
        private static bool SkipCurrentForComplainViaTest = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// When non-zero, the quiet setting is ignored when emitting complaint
        /// output via the test "puts" channel.
        /// </summary>
        private static bool IgnoreQuietForComplainViaTest = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        /// <summary>
        /// When non-zero, the Flush() method is called after a write operation.
        /// This is generally used with instances of the TextWriter class.
        /// </summary>
        private static bool AutoFlushOnWrite = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        /// <summary>
        /// When non-zero, the Flush() method is called after a clear operation.
        /// This is generally used with instances of the TextWriter class.
        /// </summary>
        private static bool AutoFlushOnClear = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        /// <summary>
        /// When non-zero, the Flush() method is called before a close
        /// operation.  This is generally used with instances of the TextWriter
        /// class.
        /// </summary>
        private static bool AutoFlushOnClose = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is non-zero, ALWAYS emit trace messages to
        //       all active trace listeners.
        //
        /// <summary>
        /// When non-zero, trace messages are always emitted to all active trace
        /// listeners.
        /// </summary>
        private static bool ForceToListeners = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the static lock,
        /// or zero if no thread holds it.
        /// </summary>
        private static long lockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Keep track of all complaints that have been seen by this
        //       class.
        //
        /// <summary>
        /// The list that keeps track of all complaints that have been seen by
        /// this class.
        /// </summary>
        private static readonly ComplaintList complaints = new ComplaintList();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Diagnostic Methods
        /// <summary>
        /// This method returns the identifier of the thread that is currently
        /// believed to hold the static lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread that holds the static lock, or zero if
        /// no thread holds it.
        /// </returns>
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(
                ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the current thread holds the static lock,
        /// but only when the lock was actually acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock was acquired by the current thread.
        /// </param>
        private static void MaybeSomebodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that no thread holds the static lock, but only
        /// when the lock was actually held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock was held by the current thread.
        /// </param>
        private static void MaybeNobodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        //
        // NOTE: Attempts to acquire the lock without waiting.
        //       This is used by callers that must never block
        //       (e.g. trace output, complaint recording).
        //
        /// <summary>
        /// This method attempts to acquire the static lock without waiting.  It
        /// is used by callers that must never block (e.g. trace output or
        /// complaint recording).
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter is set to non-zero if the lock was
        /// acquired by the current thread.
        /// </param>
        private static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
            MaybeSomebodyHasLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Attempts to acquire the lock with the standard
        //       wait timeout.  This is used by callers that can
        //       tolerate a brief wait (e.g. textWriter output
        //       in Complain).
        //
        /// <summary>
        /// This method attempts to acquire the static lock using the standard
        /// wait timeout.  It is used by callers that can tolerate a brief wait
        /// (e.g. text writer output in Complain).
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter is set to non-zero if the lock was
        /// acquired by the current thread.
        /// </param>
        private static void TryLockWithWait(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));

            MaybeSomebodyHasLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the static lock if it is currently held by the
        /// current thread.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock is held by the current thread.  Upon
        /// return, this parameter is set to false once the lock is released.
        /// </param>
        private static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasLock(locked);
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Stack Trace Methods
        /// <summary>
        /// This method determines whether the specified method name matches any
        /// of the supplied names to skip.
        /// </summary>
        /// <param name="methodName">
        /// The method name to test.  This parameter may be null.
        /// </param>
        /// <param name="skipNames">
        /// The list of names to match against.  This parameter may be null.
        /// </param>
        /// <param name="anywhere">
        /// Non-zero to match a name that appears anywhere within the method
        /// name; otherwise, only a match at the start or end of the method name
        /// is considered.
        /// </param>
        /// <returns>
        /// True if the method name matches one of the supplied names;
        /// otherwise, false.
        /// </returns>
        private static bool MatchAnyMethodName(
            string methodName,
            StringList skipNames,
            bool anywhere
            )
        {
            if ((methodName == null) || (skipNames == null))
                return false;

            int length = methodName.Length;

            foreach (string skipName in skipNames)
            {
                if (String.IsNullOrEmpty(skipName))
                    continue;

                int index = methodName.IndexOf(skipName);

                if (index == Index.Invalid)
                    continue;

                if (anywhere) /* e.g. Contains */
                    return true;

                if (index == 0) /* e.g. StartsWith */
                    return true;

                int skipLength = skipName.Length;

                if (index == (length - skipLength)) /* e.g. EndsWith */
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method captures a stack trace for the current thread, always
        /// skipping this method itself.
        /// </summary>
        /// <param name="skipFrames">
        /// The number of additional stack frames to skip, beyond this method,
        /// when capturing the stack trace.
        /// </param>
        /// <returns>
        /// The captured stack trace.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StackTrace GetStackTrace(
            int skipFrames
            )
        {
            //
            // NOTE: Always skip this method.
            //
            return new StackTrace(skipFrames + 1, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method captures the current stack trace and returns it as a
        /// string, skipping this method itself.
        /// </summary>
        /// <returns>
        /// The string form of the captured stack trace, or null if it could
        /// not be obtained.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetStackTraceString()
        {
            return GetStackTraceString(1, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method captures the current stack trace and returns it as a
        /// string, trimming any surrounding line terminators.
        /// </summary>
        /// <param name="skipFrames">
        /// The number of additional stack frames to skip, beyond this method,
        /// when capturing the stack trace.
        /// </param>
        /// <param name="default">
        /// The value to return if the stack trace cannot be captured.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The string form of the captured stack trace, or the supplied
        /// default value if it could not be obtained.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetStackTraceString(
            int skipFrames,
            string @default
            )
        {
            try
            {
                StackTrace stackTrace = GetStackTrace(skipFrames + 1);

                if (stackTrace == null)
                    return @default;

                string result = stackTrace.ToString();

                if ((result != null) && (StackTrimChars != null))
                    result = result.Trim(StackTrimChars);

                return result;
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified method should be
        /// skipped based on its declaring type (e.g. compiler-generated
        /// methods, framework methods, or this library's own diagnostic
        /// helpers).
        /// </summary>
        /// <param name="methodBase">
        /// The method to test.  This parameter may be null.
        /// </param>
        /// <param name="skipDebug">
        /// Non-zero to also skip methods declared by this library's own
        /// diagnostic helper classes.
        /// </param>
        /// <param name="methodType">
        /// Upon success, this parameter receives the declaring type of the
        /// method when it should not be skipped.
        /// </param>
        /// <param name="methodBaseName">
        /// Upon success, this parameter receives the name of the method when it
        /// should not be skipped.
        /// </param>
        /// <returns>
        /// True if the method should be skipped; otherwise, false.
        /// </returns>
        private static bool ShouldSkipMethodType(
            MethodBase methodBase,    /* in */
            bool skipDebug,           /* in */
            ref Type methodType,      /* out */
            ref string methodBaseName /* out */
            )
        {
            if (methodBase == null)
                return true;

            if (methodBase.IsSpecialName)
                return true;

            Type localMethodType = methodBase.DeclaringType;

            if (localMethodType == null)
                return true;

            string namespaceName = localMethodType.Namespace;

            if (SharedStringOps.SystemEquals(namespaceName, "Microsoft") ||
                SharedStringOps.SystemStartsWith(namespaceName, "Microsoft.") ||
                SharedStringOps.SystemEquals(namespaceName, "System") ||
                SharedStringOps.SystemStartsWith(namespaceName, "System."))
            {
                return true;
            }

            if (skipDebug)
            {
                if (localMethodType == typeof(DebugOps))
                    return true;

                if (localMethodType == typeof(FormatOps))
                    return true;

                if (localMethodType == typeof(TraceOps))
                    return true;

                if (localMethodType == typeof(Utility))
                    return true;
            }

            methodType = localMethodType;
            methodBaseName = methodBase.Name;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified method should be
        /// skipped based on its name (and the names supplied by the caller),
        /// computing the fully qualified method name in the process.
        /// </summary>
        /// <param name="skipNames">
        /// The list of method names to skip.  This parameter may be null.
        /// </param>
        /// <param name="methodType">
        /// The declaring type of the method.
        /// </param>
        /// <param name="methodBaseName">
        /// The bare name of the method.
        /// </param>
        /// <param name="anywhere">
        /// Non-zero to match a name that appears anywhere within the method
        /// name; otherwise, only a match at the start or end of the method name
        /// is considered.
        /// </param>
        /// <param name="methodFullName">
        /// Upon success, this parameter receives the fully qualified method
        /// name when it should not be skipped.
        /// </param>
        /// <returns>
        /// True if the method should be skipped; otherwise, false.
        /// </returns>
        private static bool ShouldSkipMethodName(
            StringList skipNames,     /* in */
            Type methodType,          /* in */
            string methodBaseName,    /* in */
            bool anywhere,            /* in */
            ref string methodFullName /* out */
            )
        {
            bool sameAssembly = FormatOps.IsSameAssembly(
                methodType);

            string localMethodFullName;

            if (sameAssembly)
            {
                localMethodFullName = methodBaseName;
            }
            else
            {
                localMethodFullName =
                    FormatOps.MethodQualifiedFullName(
                        methodType, methodBaseName);
            }

            if (skipNames != null)
            {
                if (MatchAnyMethodName(
                        methodBaseName, skipNames, anywhere))
                {
                    return true;
                }

                if (MatchAnyMethodName(
                        localMethodFullName, skipNames, anywhere))
                {
                    return true;
                }

                string localMethodName;

                if (sameAssembly)
                {
                    localMethodName = methodBaseName;
                }
                else
                {
                    localMethodName =
                        FormatOps.MethodQualifiedName(
                            methodType, methodBaseName);
                }

                if (MatchAnyMethodName(
                        localMethodName, skipNames, anywhere))
                {
                    return true;
                }
            }

            methodFullName = localMethodFullName;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the output values describing a method name,
        /// either using a default (empty) result or the supplied method type
        /// and names.
        /// </summary>
        /// <param name="methodType">
        /// The declaring type of the method.  This parameter may be null.
        /// </param>
        /// <param name="defaultName">
        /// The default method name to use when the empty result is requested.
        /// This parameter may be null.
        /// </param>
        /// <param name="methodBaseName">
        /// The bare name of the method.  This parameter may be null.
        /// </param>
        /// <param name="methodFullName">
        /// The fully qualified name of the method.  This parameter may be null.
        /// </param>
        /// <param name="emptyOnly">
        /// Non-zero to produce the default (empty) result instead of using the
        /// supplied method type and names.
        /// </param>
        /// <param name="nameOnly">
        /// Non-zero to use only the bare method name; otherwise, the fully
        /// qualified method name is used.
        /// </param>
        /// <param name="isThisAssembly">
        /// Upon return, this parameter is set to non-zero if the method belongs
        /// to this assembly.
        /// </param>
        /// <param name="typeName">
        /// Upon return, this parameter receives the full name of the declaring
        /// type, or null if none.
        /// </param>
        /// <param name="methodName">
        /// Upon return, this parameter receives the resulting method name.
        /// </param>
        private static void PopulateMethodName(
            Type methodType,         /* in */
            string defaultName,      /* in */
            string methodBaseName,   /* in */
            string methodFullName,   /* in */
            bool emptyOnly,          /* in */
            bool nameOnly,           /* in */
            out bool isThisAssembly, /* out */
            out string typeName,     /* out */
            out string methodName    /* out */
            )
        {
            if (emptyOnly)
            {
                isThisAssembly = false;
                typeName = null;
                methodName = defaultName;
            }
            else
            {
                //
                // NOTE: Return only the bare method name
                //       -OR- the method name formatted
                //       with its declaring type.
                //
                if (methodType != null)
                {
                    isThisAssembly = GlobalState.IsAssembly(
                        methodType.Assembly);

                    typeName = methodType.FullName;
                }
                else
                {
                    isThisAssembly = false;
                    typeName = null;
                }

                if (nameOnly)
                    methodName = methodBaseName;
                else
                    methodName = methodFullName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method walks the current execution stack and determines the
        /// name of the first relevant method, skipping framework methods, this
        /// library's diagnostic helpers, and any names requested by the caller.
        /// </summary>
        /// <param name="skipFrames">
        /// The number of additional stack frames to skip, beyond this method,
        /// when walking the stack.
        /// </param>
        /// <param name="skipNames">
        /// The list of method names to skip.  This parameter may be null.
        /// </param>
        /// <param name="skipDebug">
        /// Non-zero to also skip methods declared by this library's own
        /// diagnostic helper classes.
        /// </param>
        /// <param name="nameOnly">
        /// Non-zero to use only the bare method name; otherwise, the fully
        /// qualified method name is used.
        /// </param>
        /// <param name="defaultName">
        /// The default method name to use when no suitable method is found.
        /// This parameter may be null.
        /// </param>
        /// <param name="anywhere">
        /// Non-zero to match a skip name that appears anywhere within a method
        /// name; otherwise, only a match at the start or end is considered.
        /// </param>
        /// <param name="isThisAssembly">
        /// Upon return, this parameter is set to non-zero if the resulting
        /// method belongs to this assembly.
        /// </param>
        /// <param name="typeName">
        /// Upon return, this parameter receives the full name of the declaring
        /// type, or null if none.
        /// </param>
        /// <param name="methodName">
        /// Upon return, this parameter receives the resulting method name.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetMethodName(
            int skipFrames,          /* in */
            StringList skipNames,    /* in */
            bool skipDebug,          /* in */
            bool nameOnly,           /* in */
            string defaultName,      /* in */
            bool anywhere,           /* in */
            out bool isThisAssembly, /* out */
            out string typeName,     /* out */
            out string methodName    /* out */
            )
        {
            PopulateMethodName(
                null, defaultName, null, null, true,
                nameOnly, out isThisAssembly, out typeName,
                out methodName);

            try
            {
                //
                // NOTE: Create a new stack trace based on the current
                //       execution stack.
                //
                StackTrace stackTrace = GetStackTrace(0);

                if (stackTrace == null)
                    return;

                //
                // NOTE: Always skip this method (i.e. we start with at
                //       least one, not zero).
                //
                int count = stackTrace.FrameCount;

                for (int index = skipFrames + 1; index < count; index++)
                {
                    StackFrame stackFrame = stackTrace.GetFrame(index);

                    if (stackFrame == null)
                        continue;

                    Type methodType = null;
                    string methodBaseName = null;

                    if (ShouldSkipMethodType(
                            stackFrame.GetMethod(), skipDebug,
                            ref methodType, ref methodBaseName))
                    {
                        continue;
                    }

                    string methodFullName = null;

                    if (ShouldSkipMethodName(
                            skipNames, methodType, methodBaseName,
                            anywhere, ref methodFullName))
                    {
                        continue;
                    }

                    PopulateMethodName(
                        methodType, defaultName, methodBaseName,
                        methodFullName, false, nameOnly,
                        out isThisAssembly, out typeName,
                        out methodName);

                    return;
                }
            }
            catch
            {
                // do nothing.
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the name of the method that called the direct
        /// caller, optionally skipping a particular method.
        /// </summary>
        /// <param name="skipMethodBase">
        /// The method to skip when walking the stack, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="defaultName">
        /// The default method name to use when no suitable method is found.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The resulting method name, or the supplied default name if none was
        /// found.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetMethodName(
            MethodBase skipMethodBase,
            string defaultName
            )
        {
            //
            // NOTE: Does the caller want to skip a particular method?
            //
            StringList skipNames = null;

            if (skipMethodBase != null)
            {
                skipNames = new StringList(FormatOps.MethodName(
                    skipMethodBase.DeclaringType, skipMethodBase.Name));
            }

            //
            // NOTE: We are doing this on behalf of the direct caller;
            //       therefore, skip this method AND the calling method.
            //
            bool isThisAssembly; /* NOT USED */
            string typeName; /* NOT USED */
            string methodName;

            GetMethodName(
                2, skipNames, true, false, defaultName, false,
                out isThisAssembly, out typeName, out methodName);

            return methodName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Stack Trace Methods
        /// <summary>
        /// This method returns the method associated with a particular frame of
        /// the current execution stack, always skipping this method itself.
        /// </summary>
        /// <param name="skipFrames">
        /// The number of additional stack frames to skip, beyond this method,
        /// when locating the desired frame.
        /// </param>
        /// <returns>
        /// The method associated with the selected stack frame, or null if it
        /// could not be obtained.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MethodBase GetMethod(
            int skipFrames /* in */
            )
        {
            try
            {
                //
                // NOTE: Create a new stack trace based on the current
                //       execution stack.
                //
                StackTrace stackTrace = GetStackTrace(0);

                if (stackTrace == null)
                    return null;

                //
                // NOTE: Always skip this method (i.e. we start with at
                //       least one, not zero).
                //
                StackFrame stackFrame = stackTrace.GetFrame(skipFrames + 1);

                if (stackFrame == null)
                    return null;

                return stackFrame.GetMethod();
            }
            catch
            {
                // do nothing.
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Interpreter Helper Methods
        /// <summary>
        /// This method returns the next unique complaint identifier.
        /// </summary>
        /// <returns>
        /// The next unique complaint identifier.
        /// </returns>
        private static long GetComplaintId()
        {
            return GlobalState.NextComplaintId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the configured complaint callback,
        /// acquiring the interpreter static lock without throwing.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The configured complaint callback, or null if it could not be
        /// obtained.
        /// </returns>
        private static ComplainCallback SafeGetComplainCallback(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.ComplainCallback;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the default trace stack setting,
        /// acquiring the interpreter static lock without throwing.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The default trace stack setting, or false if it could not be
        /// obtained.
        /// </returns>
        private static bool SafeGetDefaultTraceStack(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.DefaultTraceStack;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the default quiet setting, acquiring
        /// the interpreter static lock without throwing.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The default quiet setting, or false if it could not be obtained.
        /// </returns>
        private static bool SafeGetDefaultQuiet(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.DefaultQuiet;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the quiet setting for the specified
        /// interpreter without throwing, falling back to an environment
        /// variable and then a supplied default value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose quiet setting is queried.  This parameter may
        /// be null.
        /// </param>
        /// <param name="default">
        /// The default value to return when the setting cannot otherwise be
        /// determined.
        /// </param>
        /// <returns>
        /// The quiet setting for the interpreter, or the supplied default
        /// value.
        /// </returns>
        private static bool SafeGetQuiet(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    //
                    // BUGFIX: *HACK* Normally, the InternalSoftTryLock
                    //         method would be used here; however, that
                    //         actually ended up being a problem during
                    //         test suite runs.  Other threads may grab
                    //         the interpreter lock, which then causes
                    //         the interpreter quiet flag to be ignored,
                    //         thus blowing up the release process, due
                    //         to MSBuild seeing any "error message" as
                    //         a build failure.
                    //
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.InternalQuietNoLock; /* throw? */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            //
            // HOOK: Allow the test suite (and others components) to override
            //       the quietness setting even if the interpreter is not
            //       available (or has already been disposed).
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.Quiet))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the "trace to host" setting for the
        /// specified interpreter without throwing, falling back to an
        /// environment variable and then a supplied default value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setting is queried.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The default value to return when the setting cannot otherwise be
        /// determined.
        /// </param>
        /// <returns>
        /// The "trace to host" setting for the interpreter, or the supplied
        /// default value.
        /// </returns>
        public static bool SafeGetTraceToHost(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.HasTraceToHost();
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(EnvVars.TraceToHost))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the "complain via trace" setting for
        /// the specified interpreter without throwing, falling back to an
        /// environment variable and then a supplied default value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setting is queried.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The default value to return when the setting cannot otherwise be
        /// determined.
        /// </param>
        /// <returns>
        /// The "complain via trace" setting for the interpreter, or the
        /// supplied default value.
        /// </returns>
        private static bool SafeGetComplainViaTrace(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.HasComplainViaTrace();
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ComplainViaTrace))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the "complain via test" setting for the
        /// specified interpreter without throwing, falling back to an
        /// environment variable and then a supplied default value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setting is queried.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The default value to return when the setting cannot otherwise be
        /// determined.
        /// </param>
        /// <returns>
        /// The "complain via test" setting for the interpreter, or the supplied
        /// default value.
        /// </returns>
        private static bool SafeGetComplainViaTest(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.HasComplainViaTest();
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ComplainViaTest))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the "trace stack" setting for the
        /// specified interpreter without throwing, falling back to an
        /// environment variable and then a supplied default value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setting is queried.  This parameter may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The default value to return when the setting cannot otherwise be
        /// determined.
        /// </param>
        /// <returns>
        /// The "trace stack" setting for the interpreter, or the supplied
        /// default value.
        /// </returns>
        private static bool SafeGetTraceStack(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.HasTraceStack();
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(EnvVars.TraceStack))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the debug text writer for the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer is queried.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The debug text writer for the interpreter, or null if it could not
        /// be obtained.
        /// </returns>
        private static TextWriter SafeGetDebugTextWriter(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.DebugTextWriter; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely sets the debug text writer for the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer is set.  This parameter may
        /// be null.
        /// </param>
        /// <param name="textWriter">
        /// The text writer to associate with the interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the debug text writer was set; otherwise, false.
        /// </returns>
        private static bool SafeSetDebugTextWriter(
            Interpreter interpreter,
            TextWriter textWriter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                    {
                        interpreter.DebugTextWriter = textWriter;
                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the trace text writer for the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trace text writer is queried.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The trace text writer for the interpreter, or null if it could not
        /// be obtained.
        /// </returns>
        private static TextWriter SafeGetTraceTextWriter(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.TraceTextWriter; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely sets the trace text writer for the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trace text writer is set.  This parameter may
        /// be null.
        /// </param>
        /// <param name="textWriter">
        /// The text writer to associate with the interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the trace text writer was set; otherwise, false.
        /// </returns>
        private static bool SafeSetTraceTextWriter(
            Interpreter interpreter,
            TextWriter textWriter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                    {
                        interpreter.TraceTextWriter = textWriter;
                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the interpreter host for the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host is queried.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The interpreter host, or null if it could not be obtained.
        /// </returns>
        private static IDebugHost SafeGetHost(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.InternalHost;
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the most recent complaint recorded on
        /// the specified interpreter without throwing, even if the interpreter
        /// has been disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose complaint is queried.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The most recent complaint recorded on the interpreter, or null if it
        /// could not be obtained.
        /// </returns>
        public static string SafeGetComplaint(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.Complaint; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely records the specified complaint on the specified
        /// interpreter without throwing, even if the interpreter has been
        /// disposed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on which the complaint is recorded.  This parameter
        /// may be null.
        /// </param>
        /// <param name="complaint">
        /// The complaint message to record.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the complaint was recorded; otherwise, false.
        /// </returns>
        public static bool SafeSetComplaint(
            Interpreter interpreter,
            string complaint
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        interpreter.Complaint = complaint; /* throw */
                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely clears the most recent complaint recorded on the
        /// specified interpreter without throwing.  It is intended for test use
        /// only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose complaint is cleared.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the complaint was cleared; otherwise, false.
        /// </returns>
        public static bool SafeUnsetComplaint( /* FOR TEST USE ONLY */
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        interpreter.Complaint = null; /* throw */
                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely retrieves the most recent global complaint
        /// message seen by this subsystem.
        /// </summary>
        /// <returns>
        /// The most recent global complaint message, or null if none.
        /// </returns>
        public static string SafeGetGlobalComplaint()
        {
            return Interlocked.CompareExchange(
                ref globalComplaint, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely records the specified message as the most recent
        /// global complaint seen by this subsystem.
        /// </summary>
        /// <param name="complaint">
        /// The complaint message to record.  This parameter may be null.
        /// </param>
        public static void SafeSetGlobalComplaint(
            string complaint
            )
        {
            /* IGNORED */
            Interlocked.Exchange(ref globalComplaint, complaint);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely clears the most recent global complaint message
        /// seen by this subsystem.  It is intended for test use only.
        /// </summary>
        public static void SafeUnsetGlobalComplaint() /* FOR TEST USE ONLY */
        {
            /* IGNORED */
            Interlocked.Exchange(ref globalComplaint, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Output Support Methods
#if NATIVE
        /// <summary>
        /// This method emits the specified message to the native debug output,
        /// appending a line terminator.
        /// </summary>
        /// <param name="message">
        /// The message to emit.
        /// </param>
        /// <param name="priority">
        /// The optional debug priority associated with the message.
        /// </param>
        public static void Output(
            string message,         /* in */
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            NativeOps.OutputDebugMessage(String.Format(
                "{0}{1}", message, Environment.NewLine),
                priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified exception to the native debug
        /// output, appending a line terminator.
        /// </summary>
        /// <param name="exception">
        /// The exception to emit.  This parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The optional debug priority associated with the message.
        /// </param>
        public static void Output(
            Exception exception,    /* in */
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            if (exception == null)
                return;

            Output(String.Format(
                "{0}{1}", exception, Environment.NewLine),
                priority);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter can be used
        /// to emit complaint output via the test "puts" channel, checking that
        /// it is usable, runs on the current primary thread, has the needed
        /// command or channel, and is not busy.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to test.  This parameter may be null.
        /// </param>
        /// <param name="ignoreLevels">
        /// Non-zero to ignore whether the interpreter is currently in use by
        /// the script engine, expression engine, or parser.
        /// </param>
        /// <returns>
        /// True if the interpreter can be used for complaint output via the
        /// test "puts" channel; otherwise, false.
        /// </returns>
        private static bool IsUsableForComplainViaTest(
            Interpreter interpreter,
            bool ignoreLevels
            )
        {
            //
            // NOTE: The interpreter cannot be deleted or disposed.
            //
            if (!EntityOps.IsUsable(interpreter))
                return false;

            //
            // NOTE: Ignore the interpreter if its primary thread is not
            //       the current thread.  This helps to avoid deadlocks
            //       during the test suite in some situations.
            //
            if ((interpreter == null) || !interpreter.IsPrimarySystemThread())
                return false;

            //
            // NOTE: If the interpreter appears to be missing the needed
            //       command or channel, there isn't much point in trying
            //       to use it for Complain() output.
            //
            if (!TestOps.CanMaybeTryWriteViaPuts(interpreter))
                return false;

            //
            // NOTE: The interpreter cannot be in use by the script engine,
            //       the expression engine, or the script parser.  This is
            //       not a hard requirement; however, it's a failsafe that
            //       will hopefully prevented unwanted recursion back into
            //       the Complain() pipeline.  The caller can specify that
            //       these levels should be ignored.
            //
            if (!ignoreLevels &&
                ((interpreter == null) || interpreter.HasReadyLevels()))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects an interpreter suitable for emitting complaint
        /// output via the test "puts" channel, considering the supplied
        /// interpreter, its test and parent interpreters, and the first known
        /// interpreter, in that order.
        /// </summary>
        /// <param name="interpreter">
        /// The preferred interpreter to use.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A usable interpreter, or null if none is suitable.
        /// </returns>
        private static Interpreter GetInterpreterForComplainViaTest(
            Interpreter interpreter
            )
        {
            if (!SkipCurrentForComplainViaTest &&
                IsUsableForComplainViaTest(interpreter, false))
            {
                return interpreter;
            }

            Interpreter localInterpreter = EntityOps.FollowTest(
                interpreter, true);

            if (IsUsableForComplainViaTest(localInterpreter, false))
                return localInterpreter;

            localInterpreter = EntityOps.FollowParent(interpreter, true);

            if (IsUsableForComplainViaTest(localInterpreter, false))
                return localInterpreter;

            localInterpreter = GlobalState.GetFirstInterpreter();

            if (IsUsableForComplainViaTest(localInterpreter, true))
                return localInterpreter;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified complaint value via the test "puts"
        /// channel, selecting a suitable interpreter and appending a line
        /// terminator.
        /// </summary>
        /// <param name="interpreter">
        /// The preferred interpreter to use.  This parameter may be null.
        /// </param>
        /// <param name="id">
        /// The complaint identifier, used when reporting a write failure.
        /// </param>
        /// <param name="value">
        /// The complaint value to emit.
        /// </param>
        /// <returns>
        /// True if the value was written; otherwise, false.
        /// </returns>
        private static bool ComplainViaTest(
            Interpreter interpreter,
            long id,
            string value
            )
        {
            try
            {
                return TestOps.TryWriteViaPuts(
                    GetInterpreterForComplainViaTest(interpreter),
                    String.Format("{0}{1}", value, Environment.NewLine),
                    IgnoreQuietForComplainViaTest, /* noComplain */ true);
            }
            catch (Exception e)
            {
                TestWriteException(id, e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the debug and/or trace
        /// output, depending on the build configuration, suppressing any
        /// exceptions thrown in the process.
        /// </summary>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void WriteViaDebugAndOrTrace(
            string value
            )
        {
#if DEBUG
            //
            // BUGFIX: Use a try/catch here to prevent exceptions thrown
            //         by Debug.WriteLine from ever escaping this method.
            //
            try
            {
                DebugWriteLine(value, null); /* throw */
            }
            catch
            {
                // do nothing.
            }
#endif

#if TRACE
            //
            // BUGFIX: Use a try/catch here to prevent exceptions thrown
            //         by Trace.WriteLine from ever escaping this method.
            //
            try
            {
                TraceWriteLine(value, null); /* EXEMPT */ /* throw */
            }
            catch
            {
                // do nothing.
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the supplied debug host,
        /// if it is usable, suppressing any exceptions thrown in the process.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to write to.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <returns>
        /// True if the value was written to the debug host; otherwise, false.
        /// </returns>
        private static bool MaybeWriteViaDebugHost(
            IDebugHost debugHost,
            string value
            )
        {
            try
            {
                if (IsHostUsable(
                        debugHost, HostFlags.Debug))
                {
                    //
                    // NOTE: Since our caller has no way
                    //       to indicate if the output to
                    //       be written is associated with
                    //       "success" or "failure", use a
                    //       sane default ("neutral").
                    //
                    return debugHost.WriteResult(
                        ReturnCode.Break, value, true,
                        true);
                }
            }
            catch (Exception e)
            {
                //
                // HACK: This will end up calling right
                //       back into this method; however,
                //       the IDebugHost will be null in
                //       that case and this block will
                //       not be entered again.
                //
                /* RECURSIVE */
                HostWriteException(0, e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is called from places where the interpreter
        //          host may have failed to emit output; therefore, it must
        //          never attempt to use the interpreter host.
        //
        /// <summary>
        /// This method writes the specified value using the failsafe output
        /// mechanism, without using any interpreter host.
        /// </summary>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void WriteWithoutFail(
            string value
            )
        {
            WriteWithoutFail(null, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value using the failsafe output
        /// mechanism, optionally using the supplied debug host.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to write to, if enabled.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value
            )
        {
            WriteWithoutFail(
                debugHost, value, false, UseHostForWithoutFail);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value using the failsafe output
        /// mechanism, controlling whether the native output and debug host are
        /// used.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to write to, if enabled.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="viaOutput">
        /// Non-zero to also emit the value to the native debug output.
        /// </param>
        /// <param name="viaHost">
        /// Non-zero to also emit the value to the supplied debug host.
        /// </param>
        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaOutput,
            bool viaHost
            )
        {
            WriteWithoutFail(
                debugHost, value, viaOutput || Build.Debug, true, viaHost);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value using the failsafe output
        /// mechanism, controlling whether the native output, the debug and/or
        /// trace output, and the debug host are used.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to write to, if enabled.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="viaOutput">
        /// Non-zero to also emit the value to the native debug output.
        /// </param>
        /// <param name="viaTrace">
        /// Non-zero to also emit the value to the debug and/or trace output.
        /// </param>
        /// <param name="viaHost">
        /// Non-zero to also emit the value to the supplied debug host.
        /// </param>
        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaOutput,
            bool viaTrace,
            bool viaHost
            )
        {
#if NATIVE
            if (viaOutput)
                Output(value, DebugPriority.FromSelf);
#endif

            ///////////////////////////////////////////////////////////////////

            if (viaTrace)
                WriteViaDebugAndOrTrace(value);

            ///////////////////////////////////////////////////////////////////

            if (viaHost && (debugHost != null) &&
                !AppDomainOps.IsTransparentProxy(debugHost))
            {
                /* IGNORED */
                MaybeWriteViaDebugHost(debugHost, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failed write of text using the failsafe output
        /// mechanism.
        /// </summary>
        /// <param name="id">
        /// The complaint identifier associated with the failed write.
        /// </param>
        /// <param name="e">
        /// The exception that caused the failed write.
        /// </param>
        private static void TextWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TextWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failed write to the interpreter host using the
        /// failsafe output mechanism.
        /// </summary>
        /// <param name="id">
        /// The complaint identifier associated with the failed write.
        /// </param>
        /// <param name="e">
        /// The exception that caused the failed write.
        /// </param>
        private static void HostWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                HostWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failed write via the test "puts" channel using
        /// the failsafe output mechanism.
        /// </summary>
        /// <param name="id">
        /// The complaint identifier associated with the failed write.
        /// </param>
        /// <param name="e">
        /// The exception that caused the failed write.
        /// </param>
        private static void TestWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TestWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the case where the debug text writer for an
        /// interpreter was disposed, clearing it and reporting that it has been
        /// disabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer was disposed.  This
        /// parameter may be null.
        /// </param>
        private static void DebugTextWriterWasDisposed(
            Interpreter interpreter
            )
        {
            //
            // HACK: If the interpreter is being disposed -OR- the
            //       text writer was disposed (?), null it out now
            //       so we do not repeatedly trip exceptions when
            //       attempting to write to it.
            //
            SafeSetDebugTextWriter(interpreter, null);

            WriteWithoutFail(String.Format(
                TextWriterDisposedFormat, "debug",
                FormatOps.InterpreterNoThrow(interpreter),
                Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the case where the trace text writer for an
        /// interpreter was disposed, clearing it and reporting that it has been
        /// disabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trace text writer was disposed.  This
        /// parameter may be null.
        /// </param>
        private static void TraceTextWriterWasDisposed(
            Interpreter interpreter
            )
        {
            //
            // HACK: If the interpreter is being disposed -OR- the
            //       text writer was disposed (?), null it out now
            //       so we do not repeatedly trip exceptions when
            //       attempting to write to it.
            //
            SafeSetTraceTextWriter(interpreter, null);

            WriteWithoutFail(String.Format(
                TextWriterDisposedFormat, "trace",
                FormatOps.InterpreterNoThrow(interpreter),
                Environment.NewLine));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Complaint Reporting Methods
        /// <summary>
        /// This method returns the default quiet setting to use during
        /// interpreter creation, allowing an environment variable to override
        /// the supplied default value.
        /// </summary>
        /// <param name="default">
        /// The default value to return when the environment variable is not
        /// present.
        /// </param>
        /// <returns>
        /// The default quiet setting.
        /// </returns>
        public static bool GetDefaultQuiet(
            bool @default
            )
        {
            //
            // HOOK: Allow the test suite (and others components) to override
            //       the quietness setting during interpreter creation and to
            //       be able to specify the default fallback value.
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.DefaultQuiet))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default trace stack setting to use during
        /// interpreter creation, allowing an environment variable to override
        /// the supplied default value.
        /// </summary>
        /// <param name="default">
        /// The default value to return when the environment variable is not
        /// present.
        /// </param>
        /// <returns>
        /// The default trace stack setting.
        /// </returns>
        public static bool GetDefaultTraceStack(
            bool @default
            )
        {
            //
            // HOOK: Allow the test suite (and others components) to override
            //       the stack trace setting during interpreter creation and
            //       to be able to specify the default fallback value.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.DefaultTraceStack))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the various complaint counts maintained by
        /// this subsystem.
        /// </summary>
        /// <param name="thread">
        /// Non-zero to retrieve the per-thread complaint counts.
        /// </param>
        /// <param name="global">
        /// Non-zero to retrieve the global (per AppDomain) complaint counts.
        /// </param>
        /// <param name="threadCount">
        /// Upon return, this parameter receives the per-thread complaint count,
        /// when requested.
        /// </param>
        /// <param name="globalCount">
        /// Upon return, this parameter receives the global complaint count,
        /// when requested.
        /// </param>
        /// <param name="threadQuietCount">
        /// Upon return, this parameter receives the per-thread quiet complaint
        /// count, when requested.
        /// </param>
        /// <param name="globalQuietCount">
        /// Upon return, this parameter receives the global quiet complaint
        /// count, when requested.
        /// </param>
        public static void GetComplainCounts(
            bool thread,               /* in */
            bool global,               /* in */
            ref long threadCount,      /* out */
            ref long globalCount,      /* out */
            ref long threadQuietCount, /* out */
            ref long globalQuietCount  /* out */
            )
        {
            if (thread)
            {
                threadCount = Interlocked.CompareExchange(
                    ref complainCount, 0, 0);

                threadQuietCount = Interlocked.CompareExchange(
                    ref complainQuietCount, 0, 0);
            }

            if (global)
            {
                globalCount = Interlocked.CompareExchange(
                    ref globalComplainCount, 0, 0);

                globalQuietCount = Interlocked.CompareExchange(
                    ref globalComplainQuietCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a call to Complain() is currently
        /// active on this thread.
        /// </summary>
        /// <returns>
        /// True if a complaint is currently being processed on this thread;
        /// otherwise, false.
        /// </returns>
        public static bool IsComplainPending()
        {
            return Interlocked.CompareExchange(ref complainLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified bytes to the supplied stream,
        /// optionally flushing it afterward.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.  This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The bytes to write.  This parameter may be null.
        /// </param>
        /// <param name="flush">
        /// Non-zero to flush the stream after writing.
        /// </param>
        /// <returns>
        /// True if any bytes were written; otherwise, false.
        /// </returns>
        private static bool WriteBytes(
            Stream stream, /* in */
            byte[] bytes,  /* in */
            bool flush     /* in */
            )
        {
            if ((stream == null) || (bytes == null))
                return false;

            int length = bytes.Length;

            if (length == 0)
                return false;

            stream.Write(bytes, 0, length); /* throw */

            if (flush)
                stream.Flush(); /* throw */

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single complaint, followed by two line
        /// terminators and a form feed, to the supplied stream, incrementing
        /// the count of complaints written.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.  This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The bytes of the complaint to write.  This parameter may be null.
        /// </param>
        /// <param name="written">
        /// Upon success, this parameter is incremented to reflect the complaint
        /// that was written.
        /// </param>
        /// <returns>
        /// True if the complaint was written; otherwise, false.
        /// </returns>
        private static bool WriteComplaint(
            Stream stream,  /* in */
            byte[] bytes,   /* in */
            ref int written /* in, out */
            )
        {
            if (!WriteBytes(stream, bytes, false))
                return false;

            if (!WriteBytes(stream, Characters.DoesNewLineBytes, false))
                return false;

            if (!WriteBytes(stream, Characters.DoesNewLineBytes, false))
                return false;

            if (!WriteBytes(stream, Characters.FormFeedBytes, true))
                return false;

            written++;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method dumps the recorded complaints, either to a file or via
        /// the failsafe output mechanism, optionally filtering by interpreter
        /// and optionally clearing the complaints that are written.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose complaints should be dumped, or null to dump
        /// the complaints from all interpreters.  This parameter is optional.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when writing complaints to a file.  When null,
        /// the default encoding is used.  This parameter is optional.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to write the complaints to, or null to use the
        /// failsafe output mechanism.  This parameter is optional.
        /// </param>
        /// <param name="message">
        /// An optional message to write before the complaints.  This parameter
        /// is optional.
        /// </param>
        /// <param name="clear">
        /// Non-zero to remove each complaint that is successfully written.
        /// </param>
        /// <returns>
        /// The number of complaints written, or an invalid count if the
        /// operation could not be performed.
        /// </returns>
        public static int DumpComplaints(
            Interpreter interpreter, /* in: OPTIONAL */
            Encoding encoding,       /* in: OPTIONAL */
            string fileName,         /* in: OPTIONAL */
            string message,          /* in: OPTIONAL */
            bool clear               /* in */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked);

                if (locked)
                {
                    if (complaints == null)
                        return Count.Invalid;

                    long interpreterId = 0;

                    if (interpreter != null)
                        interpreterId = interpreter.IdNoThrow;

                    int written = 0;
                    int count = complaints.Count;

                    if (fileName != null)
                    {
                        if (encoding == null)
                        {
                            encoding = StringOps.GetEncoding(
                                EncodingType.Default);

                            if (encoding == null)
                                return Count.Invalid;
                        }

                        using (FileStream stream = new FileStream(
                                fileName, FileMode.CreateNew, FileAccess.Write,
                                FileShare.Read))
                        {
                            byte[] bytes; /* REUSED */

                            if (!String.IsNullOrEmpty(message))
                            {
                                bytes = encoding.GetBytes(message);

                                if (bytes != null)
                                {
                                    /* IGNORED */
                                    WriteComplaint(stream, bytes, ref written);
                                }
                            }

                            for (int index = count - 1; index >= 0; index--)
                            {
                                ComplaintTriplet triplet = complaints[index];

                                if (triplet == null)
                                    continue;

                                if ((interpreterId != 0) &&
                                    (triplet.X != interpreterId))
                                {
                                    continue;
                                }

                                bytes = encoding.GetBytes(triplet.Z);

                                if (bytes == null)
                                    continue;

                                /* IGNORED */
                                WriteComplaint(stream, bytes, ref written);

                                if (clear)
                                    complaints.RemoveAt(index);
                            }
                        }
                    }
                    else
                    {
                        for (int index = count - 1; index >= 0; index--)
                        {
                            ComplaintTriplet triplet = complaints[index];

                            if (triplet == null)
                                continue;

                            if ((interpreterId != 0) &&
                                (triplet.X != interpreterId))
                            {
                                continue;
                            }

                            WriteWithoutFail(triplet.Z);
                            written++;

                            if (clear)
                                complaints.RemoveAt(index);
                        }
                    }

                    return written;
                }
                else
                {
                    TraceOps.LockTrace(
                        "DumpComplaints",
                        typeof(DebugOps).Name, true,
                        TracePriority.LockError3,
                        MaybeWhoHasLock());
                }

                return Count.Invalid;
            }
            finally
            {
                ExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method must *NOT* throw any exceptions.
        //
        /// <summary>
        /// This method records a complaint for the currently active
        /// interpreter.  It must never throw an exception.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the complaint.
        /// </param>
        /// <param name="result">
        /// The result (message) associated with the complaint.  This parameter
        /// may be null.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            ReturnCode code,
            Result result
            )
        {
            if (!IsComplainPossible())
                return;

            Complain(Interpreter.GetActive(), code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method must *NOT* throw any exceptions.
        //
        /// <summary>
        /// This method records a complaint for the specified interpreter,
        /// capturing a stack trace and method name as appropriate and routing
        /// the complaint to the configured output sinks.  It must never throw
        /// an exception.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the complaint.  This parameter may
        /// be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the complaint.
        /// </param>
        /// <param name="result">
        /// The result (message) associated with the complaint.  This parameter
        /// may be null.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            )
        {
            if (!IsComplainPossible())
                return;

            //
            // HACK: If this method is called with a null result, something
            //       is almost certainly very wrong.  Make sure that we end
            //       up with a full stack trace (see just below).
            //
            Result localResult = result;
            bool nullResult = (localResult == null);

            ComplainCallback callback = SafeGetComplainCallback(interpreter);

            long id = GetComplaintId();

            bool stack = nullResult || SafeGetTraceStack(interpreter,
                GetDefaultTraceStack(SafeGetDefaultTraceStack(interpreter)));

            string stackTrace = stack ? GetStackTraceString() : null;

            if (stackTrace == null)
            {
                //
                // HACK: Since there is no stack trace for us to use, try
                //       very hard to obtain a method name for the final
                //       complaint.
                //
                string methodName = GetMethodName(null, null);

                if (methodName != null)
                {
                    if (localResult != null)
                    {
                        localResult = String.Format(
                            "[{0}]: {1}", methodName, localResult);
                    }
                    else
                    {
                        localResult = methodName;
                    }
                }
            }

            bool viaTrace = SafeGetComplainViaTrace(interpreter, false);
            bool viaTest = SafeGetComplainViaTest(interpreter, false);

            bool quiet = SafeGetQuiet(interpreter,
                GetDefaultQuiet(SafeGetDefaultQuiet(interpreter)));

            bool disposed = false;

            Complain(
                callback, interpreter, SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), id, code, localResult, stackTrace,
                viaTrace, viaTest, quiet, ref disposed);

            if (disposed)
                DebugTextWriterWasDisposed(interpreter);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Complaint Reporting Methods
        /// <summary>
        /// This method determines whether it is currently possible to process a
        /// complaint (e.g. the AppDomain is not in the process of stopping).
        /// </summary>
        /// <returns>
        /// True if a complaint can be processed; otherwise, false.
        /// </returns>
        private static bool IsComplainPossible()
        {
            return !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified debug host is usable
        /// for the selected features, checking its flags, exception state, and
        /// open state.
        /// </summary>
        /// <param name="debugHost">
        /// The debug host to test.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The host flags that the debug host is required to support.
        /// </param>
        /// <returns>
        /// True if the debug host is usable for the selected features;
        /// otherwise, false.
        /// </returns>
        private static bool IsHostUsable(
            IDebugHost debugHost,
            HostFlags hasFlags
            ) /* throw */
        {
            if (debugHost == null)
                return false;

            //
            // NOTE: Grab the flags for this debug host.
            //
            HostFlags flags = debugHost.GetHostFlags(); /* throw */

            //
            // NOTE: The debug host is not usable if it failed a call to read
            //       or write with an exception.
            //
            if (FlagOps.HasFlags(flags, HostFlags.ExceptionMask, false))
                return false;

            //
            // NOTE: The debug host is not usable if it does not support the
            //       selected features.
            //
            if (!FlagOps.HasFlags(flags, hasFlags, true))
                return false;

            //
            // HACK: Currently, all debug host method calls within this class
            //       are write operations; therefore, if the host is not open
            //       it cannot be used.
            //
            if (!debugHost.IsOpen())
                return false;

            //
            // NOTE: If we get to this point, the debug host should be usable
            //       for the selected features (e.g. writing a complaint to
            //       the console).
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the specified complaint in the internal list of
        /// complaints seen by this class.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the complaint.  This parameter is
        /// optional.
        /// </param>
        /// <param name="complaintId">
        /// The unique identifier of the complaint.
        /// </param>
        /// <param name="complaint">
        /// The complaint message to record.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the complaint was recorded; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool RecordComplaint(
            Interpreter interpreter, /* in: OPTIONAL */
            long complaintId,        /* in */
            string complaint         /* in */
            )
        {
            if (complaint == null)
                return false;

            long interpreterId = 0;

            if (interpreter != null)
                interpreterId = interpreter.IdNoThrow;

            bool locked = false;

            try
            {
                TryLock(ref locked);

                if (locked)
                {
                    if (complaints == null) /* IMPOSSIBLE (?) */
                        return false;

                    complaints.Add(new ComplaintTriplet(
                        interpreterId, complaintId, complaint));

                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "RecordComplaint",
                        typeof(DebugOps).Name, true,
                        TracePriority.LockError3,
                        MaybeWhoHasLock());

                    return false;
                }
            }
            finally
            {
                ExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the core complaint handling: it invokes the
        /// complaint callback (if any), formats the complaint, records it, and
        /// emits it to the configured output sinks (trace, test "puts" channel,
        /// text writer, and debug host) while preventing unwanted recursion.
        /// </summary>
        /// <param name="callback">
        /// The complaint callback to invoke, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the complaint.  This parameter may
        /// be null.
        /// </param>
        /// <param name="textWriter">
        /// The text writer to emit the complaint to, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to emit the complaint to, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="id">
        /// The unique identifier of the complaint.
        /// </param>
        /// <param name="code">
        /// The return code associated with the complaint.
        /// </param>
        /// <param name="result">
        /// The result (message) associated with the complaint.  This parameter
        /// may be null.
        /// </param>
        /// <param name="stackTrace">
        /// The captured stack trace associated with the complaint, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="viaTrace">
        /// Non-zero to emit the complaint via the trace subsystem.
        /// </param>
        /// <param name="viaTest">
        /// Non-zero to emit the complaint via the test "puts" channel.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to inhibit use of the debug host and the console.
        /// </param>
        /// <param name="disposed">
        /// Upon return, this parameter is set to non-zero if the supplied text
        /// writer was found to have been disposed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Complain(
            ComplainCallback callback,
            Interpreter interpreter,
            TextWriter textWriter,
            IDebugHost debugHost,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool viaTrace,
            bool viaTest,
            bool quiet, // NOTE: Inhibit use of IDebugHost and the Console?
            ref bool disposed // NOTE: Was "textWriter" disposed?
            )
        {
            /* IGNORED */
            Interlocked.Increment(ref complainCount);

            /* IGNORED */
            Interlocked.Increment(ref globalComplainCount);

            ///////////////////////////////////////////////////////////////////

            int retry = 0;

        retryLevels:

            int levels = Interlocked.Increment(ref complainLevels);

            ///////////////////////////////////////////////////////////////////

            try
            {
                if (callback != null)
                {
                    //
                    // NOTE: Invoke the callback now.  If this ends up throwing
                    //       an exception, it will be caught by this method and
                    //       the remaining complaint handling will be skipped.
                    //
                    callback(interpreter, id, code, result, stackTrace, quiet,
                        retry, levels); /* throw */
                }

                ///////////////////////////////////////////////////////////////

                if (levels == 1)
                {
                    string formatted = FormatOps.Complaint(
                        id, code, result, stackTrace);

                    /* IGNORED */
                    RecordComplaint(interpreter, id, formatted);

                    /* IGNORED */
                    SafeSetComplaint(interpreter, formatted);

                    /* NO RESULT */
                    SafeSetGlobalComplaint(formatted);

                    ///////////////////////////////////////////////////////////

                    //
                    // BUGBUG: Maybe this should not be done if "quiet" mode
                    //         is enabled?
                    //
                    if (viaTrace && AllowComplainViaTrace)
                    {
                        bool locked = false;

                        try
                        {
                            TraceOps.TryLock(ref locked); /* TRANSACTIONAL */

                            if (locked)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "COMPLAINT: {0}", formatted),
                                    typeof(DebugOps).Name,
                                    TracePriority.ComplainError);

                                //
                                // NOTE: If "use only trace for complain"
                                //       flag is set, skip other reporting
                                //       except to the tracing subsystem.
                                //
                                if (UseOnlyTraceForComplain)
                                    return;
                            }
                        }
                        finally
                        {
                            TraceOps.ExitLock(ref locked); /* TRANSACTIONAL */
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // BUGBUG: Maybe this should not be done if "quiet" mode
                    //         is enabled?
                    //
                    /* NO RESULT */
                    WriteWithoutFail(
                        debugHost, formatted, true, UseTraceForWithoutFail,
                        UseHostForWithoutFail);

                    ///////////////////////////////////////////////////////////

                    //
                    // BUGBUG: Maybe this should not be done if "quiet" mode
                    //         is enabled?
                    //
                    if (viaTest && AllowComplainViaTest)
                    {
                        /* IGNORED */
                        ComplainViaTest(interpreter, id, formatted);
                    }

                    ///////////////////////////////////////////////////////////

                    if (quiet)
                    {
                        /* IGNORED */
                        Interlocked.Increment(ref complainQuietCount);

                        /* IGNORED */
                        Interlocked.Increment(ref globalComplainQuietCount);
                    }
                    else
                    {
                        //
                        // NOTE: Attempt to use the debug host first, since
                        //       it handles colors and locking properly via
                        //       the WriteCore pipeline.  Only fallback to
                        //       the direct TextWriter path if the host is
                        //       unavailable or fails.  Using both paths
                        //       concurrently causes a race condition where
                        //       the TextWriter write could interleave with
                        //       another thread writing with color enabled
                        //       in the WriteCore pipeline, causing colors
                        //       to "bleed" across lines.
                        //
                        bool wroteViaHost = false;

                    retryHost:

                        if (UseHostForComplain && (debugHost != null))
                        {
                            //
                            // BUGFIX: The host may have been disposed at
                            //         this point and we do NOT want to
                            //         throw an exception; therefore, wrap
                            //         the host access in a try block.  If
                            //         the host does throw an exception for
                            //         any reason, we will simply null out
                            //         the host and retry using our default
                            //         handling.
                            //
                            try
                            {
                                if (IsHostUsable(
                                        debugHost,
                                        HostFlags.Complain))
                                {
                                    debugHost.WriteErrorLine(
                                        formatted); /* throw */

                                    wroteViaHost = true;
                                }
                            }
                            catch (Exception e)
                            {
                                HostWriteException(id, e);

                                debugHost = null;

                                goto retryHost;
                            }
                        }

                        ///////////////////////////////////////////////////////

                        //
                        // NOTE: Only use the direct TextWriter path if the
                        //       interpreter host was unavailable or failed.
                        //       Avoids the color race condition described
                        //       above.
                        //
                        if (!wroteViaHost &&
                            UseTextWriterForComplain && (textWriter != null))
                        {
                            bool locked = false;

                            try
                            {
                                TryLockWithWait(
                                    ref locked); /* TRANSACTIONAL */

                                if (locked)
                                {
                                    textWriter.WriteLine(
                                        formatted); /* throw */

                                    if (AutoFlushOnWrite)
                                        textWriter.Flush(); /* throw */
                                }
                                else
                                {
                                    TraceOps.LockTrace(
                                        "Complain",
                                        typeof(DebugOps).Name,
                                        true,
                                        TracePriority.LockWarning,
                                        MaybeWhoHasLock());
                                }
                            }
#if DEBUG
                            catch (ObjectDisposedException e)
#else
                            catch (ObjectDisposedException)
#endif
                            {
#if DEBUG
                                TextWriteException(0, e);
#endif

                                disposed = true;
                            }
#if REMOTING
#if DEBUG
                            catch (RemotingException e)
#else
                            catch (RemotingException)
#endif
                            {
#if DEBUG
                                TextWriteException(0, e);
#endif

                                disposed = true;
                            }
#endif
                            catch (Exception e)
                            {
                                TextWriteException(id, e);
                            }
                            finally
                            {
                                ExitLock(
                                    ref locked); /* TRANSACTIONAL */
                            }
                        }
#if WINFORMS
                        else if (!wroteViaHost)
                        {
                            try
                            {
                                FormOps.Complain(formatted);
                            }
                            catch (Exception e)
                            {
                                TextWriteException(id, e);
                            }
                        }
#elif CONSOLE
                        else
                        {
                            try
                            {
                                TextWriter localTextWriter = Console.Error;

                                if (localTextWriter == null)
                                    localTextWriter = Console.Out;

                                if (localTextWriter != null)
                                {
                                    lock (localTextWriter) /* TRANSACTIONAL */
                                    {
                                        localTextWriter.WriteLine(
                                            formatted); /* throw */

                                        if (AutoFlushOnWrite)
                                            localTextWriter.Flush(); /* throw */
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                TextWriteException(id, e);
                            }
                        }
#endif
                    }
                }
                else
                {
                    //
                    // NOTE: Have we reached the limit on the number of times
                    //       we should retry the complaint?
                    //
                    if (Interlocked.Increment(ref retry) < ComplainRetryLimit)
                    {
                        //
                        // NOTE: *IMPORTANT* Sleep for a bit; this can throw
                        //       an exception, e.g. ThreadAbortException.
                        //
                        /* IGNORED */
                        HostOps.ThreadSleep(
                            ComplainRetryMilliseconds); /* throw */

                        //
                        // NOTE: After waiting a bit, try again to escape the
                        //       nested complaint level (i.e. one from another
                        //       thread).
                        //
                        goto retryLevels;
                    }

                    //
                    // NOTE: This method has been called recursively -AND- we
                    //       are out of retries.  That is not a good sign.
                    //       Allow the attached debugger to see this.
                    //
                    MaybeBreak();
                }
            }
            catch
            {
                //
                // NOTE: If there is a valid callback, we might want to do
                //       nothing, as it may have simple wanted to abort the
                //       complaint processing; however, if necessary, reset
                //       the callback to null and retry.
                //
                if (callback == null)
                {
                    throw;
                }
                else if (!IgnoreOnCallbackThrow)
                {
                    //
                    // HACK: Change the callback to null (only locally) and
                    //       then try to handle this complaint using only
                    //       the default handling.  This code may look bad;
                    //       however, apparently, jumping out of the middle
                    //       of a catch block is perfectly fine and still
                    //       executes the finally block correctly.
                    //
                    callback = null;
                    goto retryLevels;
                }
                else
                {
                    //
                    // NOTE: Really do nothing.  There is a valid callback
                    //       and the "ignoreOnCallbackThrow" flag is set.
                    //
                }
            }
            finally
            {
                Interlocked.Decrement(ref complainLevels);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Host Output Methods
        /// <summary>
        /// This method writes the specified value, as a line, to the debug
        /// text writer and debug host associated with the supplied
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer and debug host should be
        /// used; this value may be null.
        /// </param>
        /// <param name="value">
        /// The value to be written.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when not running a
        /// debug build.
        /// </param>
        public static void WriteTo(
            Interpreter interpreter,
            string value,
            bool force
            )
        {
            bool disposed = false;

            WriteTo(
                SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), value, force,
                ref disposed);

            if (disposed)
                DebugTextWriterWasDisposed(interpreter);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Host Output Methods
        /// <summary>
        /// This method writes the specified value, as a line, to the specified
        /// text writer only.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="value">
        /// The value to be written.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when not running a
        /// debug build.
        /// </param>
        /// <param name="disposed">
        /// Upon return, this value is set to non-zero if the text writer was
        /// discovered to have been disposed.
        /// </param>
        private static void WriteTo(
            TextWriter textWriter,
            string value,
            bool force,
            ref bool disposed
            )
        {
            WriteTo(textWriter, null, value, force, ref disposed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value, as a line, to the specified
        /// text writer and/or debug host.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to be written to; this value may be null.
        /// </param>
        /// <param name="value">
        /// The value to be written.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when not running a
        /// debug build.
        /// </param>
        /// <param name="disposed">
        /// Upon return, this value is set to non-zero if the text writer was
        /// discovered to have been disposed.
        /// </param>
        private static void WriteTo(
            TextWriter textWriter,
            IDebugHost debugHost,
            string value,
            bool force,
            ref bool disposed
            )
        {
            #region Part 1: Write to TextWriter
#if DEBUG
            if (textWriter != null)
#else
            if (force && (textWriter != null))
#endif
            {
                try
                {
                    lock (textWriter) /* TRANSACTIONAL */
                    {
                        textWriter.WriteLine(value); /* throw */

                        if (AutoFlushOnWrite)
                            textWriter.Flush(); /* throw */
                    }
                }
#if DEBUG
                catch (ObjectDisposedException e)
#else
                catch (ObjectDisposedException)
#endif
                {
#if DEBUG
                    TextWriteException(0, e);
#endif

                    disposed = true;
                }
#if REMOTING
#if DEBUG
                catch (RemotingException e)
#else
                catch (RemotingException)
#endif
                {
#if DEBUG
                    TextWriteException(0, e);
#endif

                    disposed = true;
                }
#endif
                catch (Exception e)
                {
                    TextWriteException(0, e);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Part 2: Write to IDebugHost
#if DEBUG
            if (debugHost != null)
#else
            if (force && (debugHost != null))
#endif
            {
                //
                // BUGFIX: The host may have been disposed at this point
                //         -AND- we do NOT want to throw an exception;
                //         therefore, wrap host access in a try block.
                //
                try
                {
                    if (IsHostUsable(
                            debugHost, HostFlags.Debug))
                    {
                        debugHost.WriteDebugLine(value);
                    }
                }
                catch (Exception e)
                {
                    HostWriteException(0, e);
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Debug "Break" Methods
        /// <summary>
        /// This method records a complaint describing a debug break and then,
        /// optionally, breaks into an attached debugger, using the debug text
        /// writer and debug host associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer and debug host should be
        /// used; this value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the break even when not running a debug build.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Break(
            Interpreter interpreter,
            MethodBase skipMethod,
            bool force
            )
        {
            bool disposed = false;

            Break(SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), skipMethod, force,
                ref disposed);

            if (disposed)
                DebugTextWriterWasDisposed(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records a complaint describing a debug break and then,
        /// optionally, breaks into an attached debugger, using the specified
        /// text writer and debug host.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to be written to; this value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the break even when not running a debug build.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Break(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            bool force
            )
        {
            bool disposed = false; /* NOT USED */

            Break(
                textWriter, debugHost, skipMethod, force,
                ref disposed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records a complaint describing a debug break and then,
        /// optionally, breaks into an attached debugger, using the specified
        /// text writer and debug host.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to be written to; this value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the break even when not running a debug build.
        /// </param>
        /// <param name="disposed">
        /// Upon return, this value is set to non-zero if the text writer was
        /// discovered to have been disposed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Break(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            bool force,
            ref bool disposed
            )
        {
            ComplainCallback callback = SafeGetComplainCallback(null);

            Result result = FormatOps.BreakOrFail(
                GetMethodName(skipMethod, null), "debug break invoked");

            //
            // NOTE: There is no need for a full stack trace here.
            //
            Complain(
                callback, null, textWriter, debugHost, GetComplaintId(),
                ReturnCode.Error, result, null, true, false, false,
                ref disposed);

#if !DEBUG
            if (force)
#endif
                Break();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Debug "Fail" Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method records a complaint describing a debug failure and then,
        /// optionally, reports it via the framework assertion mechanism, using
        /// the text writer and debug host associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debug text writer and host are used; this
        /// value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="message">
        /// The primary failure message; this value may be null.
        /// </param>
        /// <param name="detailMessage">
        /// The detailed failure message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the failure to be reported even when not running a
        /// debug build.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(
            Interpreter interpreter,
            MethodBase skipMethod,
            string message,
            string detailMessage,
            bool force
            )
        {
            bool disposed = false;

            Fail(SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), skipMethod,
                message, detailMessage, force,
                ref disposed);

            if (disposed)
                DebugTextWriterWasDisposed(interpreter);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records a complaint describing a debug failure and then,
        /// optionally, reports it via the framework assertion mechanism, using
        /// the specified text writer and debug host.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to be written to; this value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="message">
        /// The primary failure message; this value may be null.
        /// </param>
        /// <param name="detailMessage">
        /// The detailed failure message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the failure to be reported even when not running a
        /// debug build.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            string message,
            string detailMessage,
            bool force
            )
        {
            bool disposed = false; /* NOT USED */

            Fail(
                textWriter, debugHost, skipMethod,
                message, detailMessage, force,
                ref disposed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records a complaint describing a debug failure and then,
        /// optionally, reports it via the framework assertion mechanism, using
        /// the specified text writer and debug host.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to be written to; this value may be null.
        /// </param>
        /// <param name="debugHost">
        /// The debug host to be written to; this value may be null.
        /// </param>
        /// <param name="skipMethod">
        /// The method to skip when determining the calling method name to
        /// include in the diagnostic message; this value may be null.
        /// </param>
        /// <param name="message">
        /// The primary failure message; this value may be null.
        /// </param>
        /// <param name="detailMessage">
        /// The detailed failure message; this value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the failure to be reported even when not running a
        /// debug build.
        /// </param>
        /// <param name="disposed">
        /// Upon return, this value is set to non-zero if the text writer was
        /// discovered to have been disposed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            string message,
            string detailMessage,
            bool force,
            ref bool disposed
            )
        {
            ComplainCallback callback = SafeGetComplainCallback(null);

            Result result = FormatOps.BreakOrFail(
                GetMethodName(skipMethod, null), "debug fail invoked",
                message, detailMessage);

            Complain(
                callback, null, textWriter, debugHost, GetComplaintId(),
                ReturnCode.Error, result, GetStackTraceString(), true,
                false, false, ref disposed);

#if !DEBUG
            if (force)
#endif
                Fail(message, detailMessage);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Listener Handling Methods
        /// <summary>
        /// This method determines the default trace listener type to use,
        /// based on the current build configuration and platform.
        /// </summary>
        /// <returns>
        /// The trace listener type that should be used by default.
        /// </returns>
        private static TraceListenerType GetTraceListenerType()
        {
            if (Build.Debug)
            {
#if NATIVE && WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: When running on Windows with a debug
                    //       build, if a native console window is
                    //       open, use it; otherwise, fallback to
                    //       the native listener.
                    //
                    if (!NativeConsole.IsOpen())
                        return TraceListenerType.Native;
                }
#endif

                //
                // NOTE: Not running with native Windows support,
                //       assume running on an interactive console;
                //       since this is a debug build, also assume
                //       user wants to see all diagnostic output.
                //
                return TraceListenerType.Console;
            }
#if NATIVE && WINDOWS
            else
            {
                //
                // HACK: Release builds should almost never use
                //       the console trace listener by default;
                //       therefore, use the native listener on
                //       Windows and the default one otherwise.
                //
                if (PlatformOps.IsWindowsOperatingSystem())
                    return TraceListenerType.Native;
            }
#endif

            return TraceListenerType.Default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps a nullable console preference to the corresponding
        /// trace listener type.
        /// </summary>
        /// <param name="console">
        /// Non-zero to request the console listener type; zero to request the
        /// default listener type; null to request automatic detection.
        /// </param>
        /// <returns>
        /// The trace listener type that corresponds to the specified
        /// preference.
        /// </returns>
        public static TraceListenerType GetTraceListenerType(
            bool? console
            )
        {
            if (console == null)
                return TraceListenerType.Automatic;

            return (bool)console ?
                TraceListenerType.Console : TraceListenerType.Default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the managed type that implements the specified
        /// trace listener type.
        /// </summary>
        /// <param name="listenerType">
        /// The trace listener type to resolve.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the resolved type; otherwise, null.
        /// </returns>
        private static Type GetTraceListenerType(
            TraceListenerType listenerType,
            ref Result error
            )
        {
        retry:

            switch (listenerType)
            {
                case TraceListenerType.Default:
                    {
                        return typeof(DefaultTraceListener);
                    }
                case TraceListenerType.Console:
                    {
#if CONSOLE
#if !NET_STANDARD_20
                        return typeof(ConsoleTraceListener);
#else
                        return typeof(TextWriterTraceListener);
#endif
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.Native:
                    {
#if TEST && NATIVE
                        return typeof(_Tests.Default.NativeTraceListener);
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.RawLogFile:
                    {
                        return typeof(TextWriterTraceListener);
                    }
                case TraceListenerType.TestLogFile:
                    {
#if TEST
                        return typeof(_Tests.Default.Listener);
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.StatusForm:
                    {
#if TEST && WINFORMS
                        return typeof(_Tests.Default.StatusFormTraceListener);
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.Automatic:
                    {
                        listenerType = GetTraceListenerType();

                        if (listenerType == TraceListenerType.Automatic)
                        {
                            error = String.Format(
                                "trace listener type {0} detection failed",
                                listenerType);

                            return null;
                        }

                        goto retry;
                    }
                default:
                    {
                        error = String.Format(
                            "unrecognized trace listener type {0}",
                            listenerType);

                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes the specified trace listener, ignoring (or
        /// reporting) any exception that is raised.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to flush; this value may be null.
        /// </param>
        /// <returns>
        /// True if the listener was non-null and was flushed successfully;
        /// otherwise, false.
        /// </returns>
        private static bool FlushTraceListener(
            TraceListener listener
            )
        {
            try
            {
                if (listener != null)
                {
                    listener.Flush(); /* throw */
                    return true;
                }
            }
#if NATIVE
            catch (Exception e)
            {
                Output(ResultOps.Format(
                    ReturnCode.Error, e),
                    DebugPriority.FromSelf);
            }
#else
            catch
            {
                // do nothing.
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified trace listener, ignoring (or
        /// reporting) any exception that is raised.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to dispose; upon successful disposal it is set to
        /// null.  This value may be null.
        /// </param>
        private static void DisposeTraceListener(
            ref TraceListener listener
            )
        {
            try
            {
                if (listener != null)
                {
                    listener.Dispose(); /* throw */
                    listener = null;
                }
            }
#if NATIVE
            catch (Exception e)
            {
                Output(ResultOps.Format(
                    ReturnCode.Error, e),
                    DebugPriority.FromSelf);
            }
#else
            catch
            {
                // do nothing.
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method saves the trace listeners currently present in the
        /// selected collection and, optionally, replaces them with a single
        /// listener.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="listener">
        /// The replacement trace listener to install; this value may be null.
        /// </param>
        /// <param name="savedListeners">
        /// Upon return, this will contain the array of trace listeners that
        /// were previously present in the collection.
        /// </param>
        public static void PushTraceListener(
            bool debug,
            TraceListener listener,
            ref TraceListener[] savedListeners
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            if (listeners != null)
            {
                int count = listeners.Count;

                savedListeners = new TraceListener[count];

                for (int index = 0; index < count; index++)
                    savedListeners[index] = listeners[index];

                if (listener != null)
                {
                    listeners.Clear();
                    listeners.Add(listener);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores a previously saved set of trace listeners into
        /// the selected collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="savedListeners">
        /// The array of trace listeners to restore; upon return it is set to
        /// null.  This value may be null.
        /// </param>
        public static void RestoreTraceListeners(
            bool debug,
            ref TraceListener[] savedListeners
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            if (listeners != null)
            {
                listeners.Clear();

                if (savedListeners != null)
                {
                    int count = savedListeners.Length;

                    for (int index = 0; index < count; index++)
                        listeners.Add(savedListeners[index]);

                    savedListeners = null;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the log file name associated with the
        /// specified text writer trace listener, via reflection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to query; this value may be null.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this will contain the log file name.
        /// </param>
        /// <returns>
        /// True if the log file name was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetTraceLogFileName(
            TextWriterTraceListener listener, /* in */
            out string fileName               /* out */
            )
        {
            fileName = null;

            if (listener == null)
                return false;

            Type type = listener.GetType();

            if (type == null) /* IMPOSSIBLE (?) */
                return false;

            BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.PrivateInstanceGetField, true);

            try
            {
                foreach (string fieldName in new string[] {
                        TextWriterFileNameFieldName1, /* .NET Framework */
                        TextWriterFileNameFieldName2  /* .NET Core */
                    })
                {
                    if (fieldName == null)
                        continue;

                    FieldInfo fieldInfo = type.GetField(
                        fieldName, bindingFlags);

                    if (fieldInfo == null)
                        continue;

                    fileName = (string)fieldInfo.GetValue(
                        listener);

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(DebugOps).Name,
                    TracePriority.TraceError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method attempts to obtain the log file name associated with the
        /// specified test trace listener.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to query; this value may be null.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this will contain the log file name.
        /// </param>
        /// <returns>
        /// True if the log file name was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetTraceLogFileName(
            _Tests.Default.Listener listener, /* in */
            out string fileName               /* out */
            )
        {
            fileName = null;

            if (listener == null)
                return false;

            try
            {
                fileName = listener.Path;
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(DebugOps).Name,
                    TracePriority.TraceError);
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the log file names associated with the trace
        /// listeners in the selected collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="fileNames">
        /// Upon success, this will contain the list of extracted log file
        /// names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if at least one log file name was extracted; otherwise, false.
        /// </returns>
        public static bool ExtractTraceLogFileNames(
            bool debug,               /* in */
            ref StringList fileNames, /* in, out */
            ref Result error          /* out */
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            return ExtractTraceLogFileNames(
                listeners, ref fileNames, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the log file names associated with the trace
        /// listeners in the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <param name="fileNames">
        /// Upon success, this will contain the list of extracted log file
        /// names.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if at least one log file name was extracted; otherwise, false.
        /// </returns>
        private static bool ExtractTraceLogFileNames(
            TraceListenerCollection listeners, /* in */
            ref StringList fileNames,          /* in, out */
            ref Result error                   /* out */
            )
        {
            if (listeners == null)
            {
                error = "invalid trace listener collection";
                return false;
            }

            int count = 0;

            foreach (TraceListener listener in listeners)
            {
                string fileName;

                if (listener == null)
                {
                    continue;
                }
                else if (listener is TextWriterTraceListener)
                {
                    if (TryGetTraceLogFileName(
                            (TextWriterTraceListener)listener,
                            out fileName) && (fileName != null))
                    {
                        if (fileNames == null)
                            fileNames = new StringList();

                        fileNames.Add(fileName);
                        count++;
                    }
                }
#if TEST
                else if (listener is _Tests.Default.Listener)
                {
                    if (TryGetTraceLogFileName(
                            (_Tests.Default.Listener)listener,
                            out fileName) && (fileName != null))
                    {
                        if (fileNames == null)
                            fileNames = new StringList();

                        fileNames.Add(fileName);
                        count++;
                    }
                }
#endif
            }

            return (count > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates and normalizes the specified trace log file
        /// name, requiring it to be an absolute path.
        /// </summary>
        /// <param name="fileName">
        /// The trace log file name to validate; upon success it may be modified
        /// to its expanded form.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the file name is valid; otherwise, false.
        /// </returns>
        private static bool VerifyTraceLogFileName(
            ref string fileName, /* in, out */
            ref Result error     /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid trace log file name (1)";
                return false;
            }

            fileName = CommonOps.Environment.ExpandVariables(fileName);

            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid trace log file name (2)";
                return false;
            }

            if (!PathOps.ValidatePathAsFile(fileName, true, false))
            {
                error = "could not validate trace log file name";
                return false;
            }

            if (PathOps.GetPathType(fileName) != PathType.Absolute)
            {
                error = "trace log file name must be absolute";
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the interpreter to associate with a trace
        /// log, based on the specified client data.
        /// </summary>
        /// <param name="clientData">
        /// The optional client data that may specify the interpreter; this
        /// value may be null.
        /// </param>
        /// <returns>
        /// The associated interpreter, or the active interpreter when none is
        /// specified.
        /// </returns>
        private static Interpreter GetTraceLogInterpreter(
            IClientData clientData /* in */
            )
        {
            Interpreter interpreter;
            IAnyClientData anyClientData = clientData as IAnyClientData;

            if (anyClientData != null)
            {
                Result error = null;

                /* IGNORED */
                anyClientData.TryGetInterpreter(
                    Interpreter.GetActive(), TraceLogInterpreterDataName,
                    true, out interpreter, ref error);
            }
            else
            {
                interpreter = Interpreter.GetActive();
            }

            return interpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the text encoding to use for a trace log,
        /// based on the specified client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use; this value may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data that may specify the encoding; this value
        /// may be null.
        /// </param>
        /// <returns>
        /// The associated encoding, or null when none is specified.
        /// </returns>
        private static Encoding GetTraceLogEncoding(
            Interpreter interpreter, /* in */
            IClientData clientData   /* in */
            )
        {
            Encoding encoding;
            IAnyClientData anyClientData = clientData as IAnyClientData;

            if (anyClientData != null)
            {
                Result error = null;

                /* IGNORED */
                anyClientData.TryGetEncoding(
                    interpreter, TraceLogEncodingDataName, true,
                    out encoding, ref error);
            }
            else
            {
                encoding = null;
            }

            return encoding;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the name to associate with a trace log,
        /// incorporating the current system thread identifier.
        /// </summary>
        /// <param name="clientData">
        /// The optional client data that may specify the base name; this value
        /// may be null.
        /// </param>
        /// <returns>
        /// The constructed trace log name.
        /// </returns>
        private static string GetTraceLogName(
            IClientData clientData /* in */
            )
        {
            string name;
            IAnyClientData anyClientData = clientData as IAnyClientData;

            if (anyClientData != null)
            {
                Result error = null;

                /* IGNORED */
                anyClientData.TryGetString(
                    TraceLogDataName, true, out name, ref error);
            }
            else
            {
                name = null;
            }

            return String.Format(
                "{0}:{1}", (name != null) ? name : ListenerName,
                GlobalState.GetCurrentSystemThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the log flags to use for a trace log, based
        /// on the specified client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use; this value may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data that may specify the log flags; this value
        /// may be null.
        /// </param>
        /// <returns>
        /// The associated log flags, or null when none are specified.
        /// </returns>
        private static LogFlags? GetTraceLogFlags(
            Interpreter interpreter, /* in */
            IClientData clientData   /* in */
            )
        {
            LogFlags? flags;
            IAnyClientData anyClientData = clientData as IAnyClientData;

            if (anyClientData != null)
            {
                Enum enumValue;
                Result error = null;

                if (anyClientData.TryGetEnum(
                        interpreter, TraceLogEncodingDataName,
                        typeof(LogFlags), true, out enumValue,
                        ref error))
                {
                    flags = (LogFlags)enumValue;
                }
                else
                {
                    flags = null;
                }
            }
            else
            {
                flags = null;
            }

            return flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines and validates the trace log file name to use,
        /// based on the specified client data.
        /// </summary>
        /// <param name="clientData">
        /// The client data that may specify the log file name; this value may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the validated trace log file name; otherwise, null.
        /// </returns>
        private static string GetTraceLogFileName(
            IClientData clientData, /* in */
            ref Result error        /* out */
            )
        {
            if (clientData == null)
            {
                error = "invalid clientData";
                return null;
            }

            string fileName; /* REUSED */
            Result localError; /* REUSED */
            ResultList errors = null;
            IAnyClientData anyClientData = clientData as IAnyClientData;

            if (anyClientData != null)
            {
                localError = null;

                if (!anyClientData.TryGetString(
                        TraceLogFileDataName, true, out fileName,
                        ref localError))
                {
                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    goto fallback;
                }

                localError = null;

                if (VerifyTraceLogFileName(ref fileName, ref localError))
                    return fileName;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

        fallback:

            if (clientData.Data is string)
            {
                fileName = (string)clientData.Data;

                localError = null;

                if (VerifyTraceLogFileName(ref fileName, ref localError))
                    return fileName;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
                error = errors;
            else
                error = "trace log file name unavailable";

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace listener of the specified type.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// Upon success, the new trace listener; otherwise, null.
        /// </returns>
        public static TraceListener NewTraceListener(
            TraceListenerType listenerType,
            IClientData clientData
            )
        {
            Result error = null;

            return NewTraceListener(listenerType, clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace listener of the specified type.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the new trace listener; otherwise, null.
        /// </returns>
        public static TraceListener NewTraceListener(
            TraceListenerType listenerType, /* in */
            IClientData clientData,         /* in: OPTIONAL */
            ref Result error                /* out */
            )
        {
        retry:

            switch (listenerType)
            {
                case TraceListenerType.Default:
                    {
                        return new DefaultTraceListener();
                    }
                case TraceListenerType.Console:
                    {
#if CONSOLE
#if !NET_STANDARD_20
                        return new ConsoleTraceListener();
#else
                        return new TextWriterTraceListener(Console.Out);
#endif
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.Native:
                    {
#if TEST && NATIVE
                        return new _Tests.Default.NativeTraceListener();
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.RawLogFile:
                    {
                        string logFileName = GetTraceLogFileName(
                            clientData, ref error);

                        if (logFileName == null)
                            return null;

                        string logName = GetTraceLogName(clientData);

                        try
                        {
                            return new TextWriterTraceListener(
                                logFileName, logName);
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return null;
                        }
                    }
                case TraceListenerType.TestLogFile:
                    {
#if TEST
                        string logFileName = GetTraceLogFileName(
                            clientData, ref error);

                        if (logFileName == null)
                            return null;

                        string logName = GetTraceLogName(clientData);

                        Interpreter interpreter = GetTraceLogInterpreter(
                            clientData);

                        Encoding encoding = GetTraceLogEncoding(
                            interpreter, clientData);

                        LogFlags? flags = GetTraceLogFlags(
                            interpreter, clientData);

                        return NewTestTraceListener(
                            logName, logFileName, encoding, flags);
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.StatusForm:
                    {
#if TEST && WINFORMS
                        return NewStatusFormTraceListener();
#else
                        error = String.Format(
                            "unimplemented trace listener type {0}",
                            listenerType);

                        return null;
#endif
                    }
                case TraceListenerType.Automatic:
                    {
                        listenerType = GetTraceListenerType();

                        if (listenerType == TraceListenerType.Automatic)
                        {
                            error = String.Format(
                                "trace listener type {0} detection failed",
                                listenerType);

                            return null;
                        }

                        goto retry;
                    }
                default:
                    {
                        error = String.Format(
                            "unrecognized trace listener type {0}",
                            listenerType);

                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
#if NATIVE
        /// <summary>
        /// This method creates a new native trace listener.
        /// </summary>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewNativeTraceListener()
        {
            return new _Tests.Default.NativeTraceListener();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, named native trace listener.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new trace listener; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewNativeTraceListener(
            string name
            )
        {
            return new _Tests.Default.NativeTraceListener(name);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new test trace listener.
        /// </summary>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewTestTraceListener()
        {
            return new _Tests.Default.Listener();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, named test trace listener.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new trace listener; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewTestTraceListener(
            string name
            )
        {
            return new _Tests.Default.Listener(name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new test trace listener that writes to the
        /// specified log file.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new trace listener; this value may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        public static TraceListener NewTestTraceListener(
            string name,
            string fileName
            )
        {
            return NewTestTraceListener(name, fileName, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new test trace listener that writes to the
        /// specified log file, using the specified encoding and log flags.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new trace listener; this value may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the log file; this value may be null.
        /// </param>
        /// <param name="flags">
        /// The log flags to use; this value may be null.
        /// </param>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewTestTraceListener(
            string name,
            string fileName,
            Encoding encoding,
            LogFlags? flags
            )
        {
            return NewTestTraceListener(
                name, fileName, encoding, 0, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new test trace listener that writes to the
        /// specified log file, using the specified encoding, buffer size, and
        /// log flags.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the new trace listener; this value may be
        /// null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the log file; this value may be null.
        /// </param>
        /// <param name="bufferSize">
        /// The size of the buffer to use for the log file, in bytes.
        /// </param>
        /// <param name="flags">
        /// The log flags to use; this value may be null.
        /// </param>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        private static TraceListener NewTestTraceListener(
            string name,
            string fileName,
            Encoding encoding,
            int bufferSize,
            LogFlags? flags
            )
        {
            return new _Tests.Default.Listener(
                name, fileName, encoding, bufferSize, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new buffered trace listener.
        /// </summary>
        /// <returns>
        /// Upon success, the new trace listener; otherwise, null.
        /// </returns>
        private static TraceListener NewBufferedTraceListener()
        {
            Result error = null; /* NOT USED */

            return _Tests.Default.BufferedTraceListener.Create(
                null, BufferedTraceFlags.None, 0, 0, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if WINFORMS
        /// <summary>
        /// This method creates a new status form trace listener.
        /// </summary>
        /// <returns>
        /// The new trace listener.
        /// </returns>
        public static TraceListener NewStatusFormTraceListener()
        {
            return new _Tests.Default.StatusFormTraceListener(
                Interpreter.GetAny(), null, null);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two trace listeners are considered to
        /// be the same, optionally comparing only their types.
        /// </summary>
        /// <param name="listener1">
        /// The first trace listener to compare; this value may be null.
        /// </param>
        /// <param name="listener2">
        /// The second trace listener to compare; this value may be null.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider the listeners equal when their types match;
        /// otherwise, they must be the same object instance.
        /// </param>
        /// <returns>
        /// True if the listeners are considered to be the same; otherwise,
        /// false.
        /// </returns>
        private static bool IsSameTraceListener(
            TraceListener listener1,
            TraceListener listener2,
            bool typeOnly
            )
        {
            //
            // NOTE: If either trace listener is null, both must
            //       be null for this method to return true.
            //
            if ((listener1 == null) || (listener2 == null))
                return (listener1 == null) && (listener2 == null);

            //
            // NOTE: First, compare the types.  If they are not a
            //       match, we are done.  If they are a match, we
            //       might be done.
            //
            Type type1 = listener1.GetType();
            Type type2 = listener2.GetType();

            if (!Object.ReferenceEquals(type1, type2))
                return false;

            //
            // NOTE: At least one listener of this type is present
            //       in the list.  If the caller only cares about
            //       type, just return now.
            //
            if (typeOnly)
                return true;

            //
            // NOTE: If these trace listener are the same object,
            //       return true; otherwise, return false.
            //
            return Object.ReferenceEquals(listener1, listener2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a trace listener of the specified
        /// type is present in the selected collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="listenerType">
        /// The trace listener type to look for; this value may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used when constructing a comparison
        /// listener; this value may be null.
        /// </param>
        /// <returns>
        /// True if a matching trace listener is present; otherwise, false.
        /// </returns>
        public static bool HasTraceListener(
            bool debug,                      /* in */
            TraceListenerType? listenerType, /* in */
            IClientData clientData           /* in: OPTIONAL */
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            return HasTraceListener(listeners, listenerType, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a trace listener of the specified
        /// type is present in the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <param name="listenerType">
        /// The trace listener type to look for; this value may be null, which
        /// matches any console or default listener.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used when constructing a comparison
        /// listener; this value may be null.
        /// </param>
        /// <returns>
        /// True if a matching trace listener is present; otherwise, false.
        /// </returns>
        public static bool HasTraceListener(
            TraceListenerCollection listeners, /* in */
            TraceListenerType? listenerType,   /* in: OPTIONAL, null = ANY */
            IClientData clientData             /* in: OPTIONAL */
            )
        {
            if (listeners != null)
            {
                TraceListener listener; /* REUSED */

                if (listenerType != null)
                {
                    listener = null;

                    try
                    {
                        listener = NewTraceListener(
                            (TraceListenerType)listenerType, clientData);

                        if (FindTraceListener(
                                listeners, listener, true) != Index.Invalid)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        if (AutoFlushOnClose)
                            FlushTraceListener(listener);

                        DisposeTraceListener(ref listener);
                    }
                }
                else
                {
#if CONSOLE
                    listener = null;

                    try
                    {
                        listener = NewTraceListener(
                            TraceListenerType.Console, clientData);

                        if (FindTraceListener(
                                listeners, listener, true) != Index.Invalid)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        if (AutoFlushOnClose)
                            FlushTraceListener(listener);

                        DisposeTraceListener(ref listener);
                    }
#endif

                    listener = null;

                    try
                    {
                        listener = NewTraceListener(
                            TraceListenerType.Default, clientData);

                        if (FindTraceListener(
                                listeners, listener, true) != Index.Invalid)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        if (AutoFlushOnClose)
                            FlushTraceListener(listener);

                        DisposeTraceListener(ref listener);
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
#if NATIVE
        /// <summary>
        /// This method determines whether a native trace listener is present in
        /// the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// True if a native trace listener is present; otherwise, false.
        /// </returns>
        public static bool HasNativeTraceListener(
            TraceListenerCollection listeners /* in */
            )
        {
            if (listeners != null)
            {
                TraceListener listener = null;

                try
                {
                    listener = NewNativeTraceListener();

                    if (FindTraceListener(
                            listeners, listener, true) != Index.Invalid)
                    {
                        return true;
                    }
                }
                finally
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(listener);

                    DisposeTraceListener(ref listener);
                }
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a test trace listener is present in
        /// the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// True if a test trace listener is present; otherwise, false.
        /// </returns>
        public static bool HasTestTraceListener(
            TraceListenerCollection listeners /* in */
            )
        {
            if (listeners != null)
            {
                TraceListener listener = null;

                try
                {
                    listener = NewTestTraceListener();

                    if (FindTraceListener(
                            listeners, listener, true) != Index.Invalid)
                    {
                        return true;
                    }
                }
                finally
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(listener);

                    DisposeTraceListener(ref listener);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a buffered trace listener is present
        /// in the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// True if a buffered trace listener is present; otherwise, false.
        /// </returns>
        public static bool HasBufferedTraceListener(
            TraceListenerCollection listeners /* in */
            )
        {
            if (listeners != null)
            {
                TraceListener listener = null;

                try
                {
                    listener = NewBufferedTraceListener();

                    if (FindTraceListener(
                            listeners, listener, true) != Index.Invalid)
                    {
                        return true;
                    }
                }
                finally
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(listener);

                    DisposeTraceListener(ref listener);
                }
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified collection for a trace listener
        /// matching the one provided.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to search; this value may be null.
        /// </param>
        /// <param name="listener">
        /// The trace listener to search for; this value may be null.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to match based on listener type; otherwise, the same object
        /// instance is required.
        /// </param>
        /// <returns>
        /// The index of the matching trace listener, or
        /// <see cref="Index.Invalid" /> if no match was found.
        /// </returns>
        private static int FindTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            bool typeOnly
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    int count = listeners.Count;

                    for (int index = 0; index < count; index++)
                    {
                        TraceListener localListener = listeners[index];

                        if (localListener == null)
                            continue;

                        if (IsSameTraceListener(
                                localListener, listener, typeOnly))
                        {
                            return index;
                        }
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified trace listener is present in
        /// the selected collection, adding it if necessary.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to ensure is present; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode EnsureTraceListener(
            TraceListener listener,
            bool debug,
            bool typeOnly,
            ref Result error
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            return EnsureTraceListener(
                listeners, listener, typeOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified trace listener is present in
        /// the specified collection, adding it if necessary.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to operate on; this value may be
        /// null.
        /// </param>
        /// <param name="listener">
        /// The trace listener to ensure is present; this value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode EnsureTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            ref Result error
            )
        {
            return EnsureTraceListener(listeners,
                listener, DefaultSameTraceListenerTypeOnly,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified trace listener is present in
        /// the specified collection, adding it if necessary.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to operate on; this value may be
        /// null.
        /// </param>
        /// <param name="listener">
        /// The trace listener to ensure is present; this value may be null.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode EnsureTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            bool typeOnly,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    //
                    // NOTE: We succeeded.  At least one listener of this
                    //       type is already present in the list.
                    //
                    if (FindTraceListener(
                            listeners, listener, typeOnly) != Index.Invalid)
                    {
                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: No listeners of this type are present in the
                    //       list, add one now (i.e. the one provided by
                    //       the caller).
                    //
                    /* IGNORED */
                    listeners.Add(listener);

                    //
                    // NOTE: We succeeded (the listener has been added).
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid trace listener";
                }
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces an existing trace listener in the specified
        /// collection with a new one, optionally disposing the old one.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to operate on; this value may be
        /// null.
        /// </param>
        /// <param name="oldListener">
        /// The trace listener to remove; this value may be null.
        /// </param>
        /// <param name="newListener">
        /// The trace listener to add; this value may be null.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to match the old listener based on type; otherwise, the
        /// same object instance is required.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose the old listener after removing it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ReplaceTraceListener(
            TraceListenerCollection listeners,
            TraceListener oldListener,
            TraceListener newListener,
            bool typeOnly,
            bool dispose,
            ref Result error
            )
        {
            if (listeners == null)
            {
                error = "invalid trace listener collection";
                return ReturnCode.Error;
            }

            if (oldListener != null)
            {
                int index = FindTraceListener(
                    listeners, oldListener, typeOnly);

                if (index != Index.Invalid)
                {
                    /* NO RESULT */
                    listeners.RemoveAt(index);
                }

                if (dispose)
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(oldListener);

                    DisposeTraceListener(ref oldListener);
                }
            }

            if (newListener != null)
            {
                /* IGNORED */
                listeners.Add(newListener);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the indicator flags describing the trace
        /// listeners currently present in the trace listener collection.
        /// </summary>
        /// <returns>
        /// The calculated trace indicator flags.
        /// </returns>
        public static TraceIndicatorFlags CalculateListeners()
        {
            return CalculateListeners(GetTraceListeners());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the indicator flags describing the trace
        /// listeners present in the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to examine; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// The calculated trace indicator flags.
        /// </returns>
        private static TraceIndicatorFlags CalculateListeners(
            TraceListenerCollection listeners
            )
        {
            TraceIndicatorFlags flags = TraceIndicatorFlags.None;

            if (listeners == null)
                return flags;

            foreach (TraceListener listener in listeners)
            {
                if (listener == null)
                    continue;

                if (listener is DefaultTraceListener)
                    flags |= TraceIndicatorFlags.HaveDefault;

#if CONSOLE && !NET_STANDARD_20
                if (listener is ConsoleTraceListener)
                    flags |= TraceIndicatorFlags.HaveConsole;
#endif

#if TEST && NATIVE
                if (listener is _Tests.Default.NativeTraceListener)
                    flags |= TraceIndicatorFlags.HaveNative;
#endif

                if (listener is TextWriterTraceListener)
                    flags |= TraceIndicatorFlags.HaveRawLogFile;

#if TEST
                if (listener is _Tests.Default.Listener)
                    flags |= TraceIndicatorFlags.HaveTestLogFile;
#endif

#if TEST
                if (listener is IBufferedTraceListener)
                    flags |= TraceIndicatorFlags.HaveBuffered;
#endif
            }

            return flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes all of the trace listeners present in the
        /// specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to flush; this value may be null.
        /// </param>
        /// <returns>
        /// True if all listeners were flushed successfully; otherwise, false.
        /// </returns>
        private static bool FlushTraceListeners(
            TraceListenerCollection listeners
            )
        {
            if (listeners == null)
                return false;

            int errorCount = 0;

            foreach (TraceListener listener in listeners)
            {
                if (listener == null)
                    continue;

                if (!FlushTraceListener(listener))
                    errorCount++;
            }

            return (errorCount == 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the normal and/or debug trace listener
        /// collections.
        /// </summary>
        /// <param name="trace">
        /// Non-zero to clear the normal trace listener collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to clear the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        public static void ClearTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose
            )
        {
            Result error = null;

            /* IGNORED */
            ClearTraceListeners(trace, debug, console, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the normal and/or debug trace listener
        /// collections.
        /// </summary>
        /// <param name="trace">
        /// Non-zero to clear the normal trace listener collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to clear the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ClearTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            ref Result error
            )
        {
            try
            {
                int count = (trace ? 1 : 0) + (debug ? 1 : 0);

                //
                // NOTE: Do they want to clear normal trace listeners?
                //
                if (trace)
                {
                    if (AutoFlushOnClear)
                    {
                        /* NO RESULT */
                        TraceFlush();
                    }

                    if (ClearTraceListeners(
                            GetTraceListeners(), false, console,
                            verbose, ref error) == ReturnCode.Ok)
                    {
                        count--;
                    }
                }

#if !NET_STANDARD_20
                //
                // NOTE: Do they want to clear debug trace listeners
                //       as well?
                //
                if (debug)
                {
                    if (AutoFlushOnClear)
                    {
                        /* NO RESULT */
                        DebugFlush();
                    }

                    if (ClearTraceListeners(
                            GetDebugListeners(), true, console,
                            verbose, ref error) == ReturnCode.Ok)
                    {
                        count--;
                    }
                }
#endif

                if (count == 0)
                    return ReturnCode.Ok;
                else
                    error = "one or more trace listeners could not be cleared";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the specified trace listener collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to clear; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero if the specified collection is the debug listener
        /// collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ClearTraceListeners(
            TraceListenerCollection listeners,
            bool debug,
            bool console,
            bool verbose,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (AutoFlushOnClear)
                {
                    /* IGNORED */
                    FlushTraceListeners(listeners);
                }

                /* NO RESULT */
                listeners.Clear();

#if CONSOLE
                ConsoleOps.MaybeWritePrompt(debug ?
                    _Constants.Prompt.NoDebugTrace :
                    _Constants.Prompt.NoTrace,
                    console, verbose);
#endif

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace listener of the specified type and
        /// adds it to the specified collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to add to; this value may be null.
        /// </param>
        /// <param name="listenerType">
        /// The type of trace listener to create and add.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <param name="force">
        /// Non-zero to always add the listener; otherwise, it is only added
        /// when one of its type is not already present.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode AddTraceListener(
            TraceListenerCollection listeners,
            TraceListenerType listenerType,
            IClientData clientData,
            bool force,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;
            TraceListener listener = null;
            Result localError; /* REUSED */

            try
            {
                localError = null;

                listener = NewTraceListener(
                    listenerType, clientData, ref localError);

                if (listener != null)
                {
                    localError = null;

                    if (force)
                    {
                        code = AddTraceListener(
                            listeners, listener, ref localError);
                    }
                    else
                    {
                        code = EnsureTraceListener(
                            listeners, listener, ref localError);
                    }

                    if (code != ReturnCode.Ok)
                        error = localError;
                }
                else
                {
                    error = localError;
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (code != ReturnCode.Ok)
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(listener);

                    DisposeTraceListener(ref listener);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified trace listener to the selected
        /// collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to add; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return AddTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified trace listener to the selected
        /// collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to add; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug,
            ref Result error
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            return AddTraceListener(listeners, listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified trace listener to the specified
        /// collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to add to; this value may be null.
        /// </param>
        /// <param name="listener">
        /// The trace listener to add; this value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode AddTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    /* IGNORED */
                    listeners.Add(listener);

                    //
                    // NOTE: We succeeded (the listener has been added).
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid trace listener";
                }
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method parses the specified value into a script trace listener
        /// specification, creates the resulting listener, and ensures it is
        /// present in the selected collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use; this value may be null.
        /// </param>
        /// <param name="value">
        /// The value describing the script trace listener to create.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode AddScriptTraceListener(
            Interpreter interpreter,
            string value,
            bool debug,
            bool typeOnly,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, value, 0, Length.Invalid, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string text = null;

            if (list.Count > 0)
                text = list[0];

            string argument = null;

            if (list.Count > 1)
                argument = list[1];

            TraceListener listener = ScriptTraceListener.Create(
                interpreter, text, argument, ref error);

            if (listener == null)
                return ReturnCode.Error;

            return EnsureTraceListener(
                listener, debug, typeOnly, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified trace listener from the selected
        /// collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to remove; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return RemoveTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified trace listener from the selected
        /// collection.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to remove; this value may be null.
        /// </param>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug,
            ref Result error
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            return RemoveTraceListener(listeners, listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified trace listener from the specified
        /// collection.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to remove from; this value may be
        /// null.
        /// </param>
        /// <param name="listener">
        /// The trace listener to remove; this value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode RemoveTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    /* NO RESULT */
                    listeners.Remove(listener);

                    //
                    // NOTE: We succeeded (the listener has been removed)?
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid trace listener";
                }
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the first trace listener of the specified type
        /// from the specified collection, optionally disposing it.
        /// </summary>
        /// <param name="listeners">
        /// The collection of trace listeners to remove from; this value may be
        /// null.
        /// </param>
        /// <param name="listenerType">
        /// The type of trace listener to remove.
        /// </param>
        /// <param name="dispose">
        /// Non-zero to dispose the removed listener.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode RemoveTraceListener(
            TraceListenerCollection listeners,
            TraceListenerType listenerType,
            bool dispose,
            ref Result error
            )
        {
            if (listeners == null)
            {
                error = "invalid trace listener collection";
                return ReturnCode.Error;
            }

            Type type = GetTraceListenerType(
                listenerType, ref error);

            if (type == null)
                return ReturnCode.Error;

            TraceListener removeListener = null;
            int count = listeners.Count;

            for (int index = 0; index < count; index++)
            {
                TraceListener listener = listeners[index];

                if (listener == null)
                    continue;

                if (Object.ReferenceEquals(listener.GetType(), type))
                {
                    removeListener = listener;
                    break;
                }
            }

            if (removeListener != null)
            {
                listeners.Remove(removeListener);

                if (dispose)
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(removeListener);

                    DisposeTraceListener(ref removeListener);
                }

                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "unmatched trace listener type {0}",
                    listenerType);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        //
        // WARNING: For use by child classes of the Eagle._Tests.Default
        //          class only.
        //
        /// <summary>
        /// This method removes the specified trace listener from both the
        /// debug and trace listener collections, ignoring any exception.
        /// </summary>
        /// <param name="listener">
        /// The trace listener to remove; this value may be null.
        /// </param>
        public static void RemoveTraceListener(
            TraceListener listener /* in */
            )
        {
#if !NET_STANDARD_20
            //
            // HACK: Remove this object instance from the
            //       collections of debug listeners to prevent
            //       ObjectDisposedException from being thrown
            //       (i.e. during later calls to Debug.Write,
            //       etc).
            //
            try
            {
                Debug.Listeners.Remove(listener);
            }
            catch
            {
                //
                // NOTE: There is nothing much we can do here.
                //       We cannot even call DebugOps.Complain
                //       because it could use Debug.WriteLine,
                //       and that may end up calling into this
                //       object instance.
                //
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Remove this object instance from the
            //       collections of trace listeners to prevent
            //       ObjectDisposedException from being thrown
            //       (i.e. during later calls to Trace.Write,
            //       etc).
            //
            try
            {
                Trace.Listeners.Remove(listener);
            }
            catch
            {
                //
                // NOTE: There is nothing much we can do here.
                //       We cannot even call DebugOps.Complain
                //       because it could use Trace.WriteLine,
                //       and that may end up calling into this
                //       object instance.
                //
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified trace listener is present in
        /// the normal and/or debug trace listener collections, writing prompt
        /// output as appropriate.
        /// </summary>
        /// <param name="context">
        /// A short description of the listener being set up, used in prompt
        /// output.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="listener">
        /// The trace listener to set up; this value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode PrivateSetupTraceListeners(
            string context,             /* in */
            bool trace,                 /* in */
            bool debug,                 /* in */
            bool console,               /* in */
            bool verbose,               /* in */
            bool typeOnly,              /* in */
            ref TraceListener listener, /* in, out */
            ref Result error            /* out */
            )
        {
            ResultList errors = null;

            try
            {
                int count = (trace ? 1 : 0) + (debug ? 1 : 0);
                Result localError; /* REUSED */

                //
                // NOTE: Do they want to add a normal trace listener?
                //
                if (trace)
                {
                    localError = null;

                    if (EnsureTraceListener(
                            listener, false, typeOnly,
                            ref localError) == ReturnCode.Ok)
                    {
                        count--;

#if CONSOLE
                        ConsoleOps.MaybeWritePrompt(String.Format(
                            _Constants.Prompt.Trace, context),
                            console, verbose);
#endif
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);

#if CONSOLE
                            //
                            // TODO: Can this actually happen?
                            //
                            ConsoleOps.MaybeWritePrompt(String.Format(
                                _Constants.Prompt.TraceError, localError),
                                console, verbose);
#endif
                        }
                    }
                }

                //
                // NOTE: Do they want to add a debug trace listener?
                //
                if (debug)
                {
                    localError = null;

                    if (EnsureTraceListener(
                            listener, true, typeOnly,
                            ref localError) == ReturnCode.Ok)
                    {
                        count--;

#if CONSOLE
                        ConsoleOps.MaybeWritePrompt(
                            _Constants.Prompt.DebugTrace,
                            console, verbose);
#endif
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);

#if CONSOLE
                            //
                            // TODO: Can this actually happen?
                            //
                            ConsoleOps.MaybeWritePrompt(String.Format(
                                _Constants.Prompt.DebugTraceError, localError),
                                console, verbose);
#endif
                        }
                    }
                }

                if (count == 0)
                    return ReturnCode.Ok;

                if (errors == null)
                    errors = new ResultList();

                errors.Insert(0,
                    "one or more trace listeners could not be added");
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by ProcessStartupOptions only.
        //
        /// <summary>
        /// This method creates a new trace listener of the specified type and
        /// adds it to the normal and/or debug trace listener collections.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create and add.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        public static void SetupTraceListeners(
            TraceListenerType listenerType, /* in */
            IClientData clientData,         /* in: OPTIONAL */
            bool trace,                     /* in */
            bool debug,                     /* in */
            bool console,                   /* in */
            bool verbose                    /* in */
            )
        {
            TraceListener listener = null; /* NOT USED */
            Result error = null; /* NOT USED */

            /* IGNORED */
            SetupTraceListeners(
                listenerType, clientData, trace,
                debug, console, verbose,
                DefaultSameTraceListenerTypeOnly,
                ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by PrivateShellMainCore only.
        //
        /// <summary>
        /// This method creates a new trace listener of the specified type and
        /// adds it to the normal and/or debug trace listener collections.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create and add.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetupTraceListeners(
            TraceListenerType listenerType, /* in */
            IClientData clientData,         /* in: OPTIONAL */
            bool trace,                     /* in */
            bool debug,                     /* in */
            bool console,                   /* in */
            bool verbose,                   /* in */
            ref Result error                /* out */
            )
        {
            TraceListener listener = null; /* NOT USED */

            return SetupTraceListeners(
                listenerType, clientData, trace,
                debug, console, verbose,
                DefaultSameTraceListenerTypeOnly,
                ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new trace listener of the specified type and
        /// adds it to the normal and/or debug trace listener collections,
        /// returning the created listener to the caller.
        /// </summary>
        /// <param name="listenerType">
        /// The type of trace listener to create and add.
        /// </param>
        /// <param name="clientData">
        /// The optional client data used during creation; this value may be
        /// null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="listener">
        /// Upon success, this will contain the trace listener that was created
        /// and added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetupTraceListeners(
            TraceListenerType listenerType, /* in */
            IClientData clientData,         /* in: OPTIONAL */
            bool trace,                     /* in */
            bool debug,                     /* in */
            bool console,                   /* in */
            bool verbose,                   /* in */
            bool typeOnly,                  /* in */
            ref TraceListener listener,     /* out */
            ref Result error                /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            TraceListener localListener = null;

            try
            {
                if (trace || debug)
                {
                    localListener = NewTraceListener(
                        listenerType, clientData, ref error);

                    if (localListener != null)
                    {
                        code = PrivateSetupTraceListeners(
                            "listeners", trace, debug, console,
                            verbose, typeOnly, ref localListener,
                            ref error);

                        if (code == ReturnCode.Ok)
                            listener = localListener;
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (code != ReturnCode.Ok)
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(localListener);

                    DisposeTraceListener(ref localListener);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method generates a unique trace log file name, preferring the
        /// directory used for the primary test log file when an interpreter is
        /// available.
        /// </summary>
        /// <param name="interpreter">
        /// The optional interpreter context to use; this value may be null.
        /// </param>
        /// <param name="name">
        /// An optional name to embed in the generated file name; this value may
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Upon success, the generated trace log file name; otherwise, null.
        /// </returns>
        public static string GetTraceLogFileName(
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            //
            // NOTE: By default, when there is an interpreter context,
            //       attempt to use the same directory for test suite
            //       tracing log files as the primary test log file;
            //       fallback to using the system temporary directory.
            //
            string directory = null;

            if (interpreter != null)
            {
                Result value = null;

                if (interpreter.GetVariableValue(
                        VariableFlags.None, Vars.Core.TestLog,
                        ref value, ref error) == ReturnCode.Ok)
                {
                    directory = Path.GetDirectoryName(value);
                }
            }

            if (directory == null)
                directory = PathOps.GetTempPath(interpreter);

            string format;

            if (!String.IsNullOrEmpty(name))
                format = TraceNameLogFileFormat;
            else
                format = TraceBareLogFileFormat;

            return PathOps.GetUniquePath(
                interpreter, directory, String.Format(format, name,
                ProcessOps.GetId()), FileExtension.Log, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method deletes the specified trace log file, but only when it
        /// exists and is empty, ignoring any exception that is raised.
        /// </summary>
        /// <param name="fileName">
        /// The name of the trace log file to delete; this value may be null.
        /// </param>
        public static void MaybeDeleteTraceLogFile(
            string fileName /* in: OPTIONAL */
            )
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        long size = Size.Invalid;

                        if ((FileOps.GetFileSize(fileName,
                                ref size) == ReturnCode.Ok) &&
                            (size == 0))
                        {
                            File.Delete(fileName); /* throw */
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(DebugOps).Name,
                        TracePriority.CleanupError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new log file trace listener and adds it to the
        /// normal and/or debug trace listener collections.
        /// </summary>
        /// <param name="name">
        /// An optional name to assign to the new trace listener; this value may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the log file; this value may be null.
        /// </param>
        /// <param name="flags">
        /// The log flags to use; this value may be null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetupTraceLogFile(
            string name,       /* in: OPTIONAL */
            string fileName,   /* in */
            Encoding encoding, /* in: OPTIONAL */
            LogFlags? flags,   /* in */
            bool trace,        /* in */
            bool debug,        /* in */
            bool console,      /* in */
            bool verbose,      /* in */
            bool typeOnly,     /* in */
            ref Result error   /* out */
            )
        {
            TraceListener listener = null;

            return SetupTraceLogFile(
                name, fileName, encoding, flags, trace, debug, console,
                verbose, typeOnly, ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new log file trace listener and adds it to the
        /// normal and/or debug trace listener collections, returning the
        /// created listener to the caller.
        /// </summary>
        /// <param name="name">
        /// An optional name to assign to the new trace listener; this value may
        /// be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the log file to write to.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the log file; this value may be null.
        /// </param>
        /// <param name="flags">
        /// The log flags to use; this value may be null.
        /// </param>
        /// <param name="trace">
        /// Non-zero to add the listener to the normal trace listener
        /// collection.
        /// </param>
        /// <param name="debug">
        /// Non-zero to add the listener to the debug trace listener collection.
        /// </param>
        /// <param name="console">
        /// Non-zero if running with an interactive console available.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose prompt output.
        /// </param>
        /// <param name="typeOnly">
        /// Non-zero to consider an existing listener of the same type to be a
        /// match; otherwise, the same object instance is required.
        /// </param>
        /// <param name="listener">
        /// Upon success, this will contain the trace listener that was created
        /// and added.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode SetupTraceLogFile(
            string name,                /* in: OPTIONAL */
            string fileName,            /* in */
            Encoding encoding,          /* in: OPTIONAL */
            LogFlags? flags,            /* in */
            bool trace,                 /* in */
            bool debug,                 /* in */
            bool console,               /* in */
            bool verbose,               /* in */
            bool typeOnly,              /* in */
            ref TraceListener listener, /* out */
            ref Result error            /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            TraceListener localListener = null;

            try
            {
                localListener = NewTestTraceListener(
                    name, fileName, encoding, flags);

                if (localListener != null)
                {
                    code = PrivateSetupTraceListeners(
                        "log file", trace, debug, console,
                        verbose, typeOnly, ref localListener,
                        ref error);

                    if (code == ReturnCode.Ok)
                        listener = localListener;
                }
                else
                {
                    error = "could not create log trace listener";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (code != ReturnCode.Ok)
                {
                    if (AutoFlushOnClose)
                        FlushTraceListener(localListener);

                    DisposeTraceListener(ref localListener);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes the buffers of all buffered trace listeners in
        /// the selected collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode FlushBufferedTraceListeners(
            bool debug /* in */
            )
        {
            ReturnCode code;
            Result error = null;
            int count = 0; /* NOT USED */

            code = FlushBufferedTraceListeners(debug, ref count, ref error);

#if NATIVE
            if (code != ReturnCode.Ok)
            {
                //
                // HACK: If we get to this point, something went really
                //       wrong.  We cannot easily complain because that
                //       subsystem may make assumptions that may not be
                //       true at this point.
                //
                Output(ResultOps.Format(code, error), DebugPriority.FromSelf);
            }
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes the buffers of all buffered trace listeners in
        /// the selected collection, accumulating a count of the listeners that
        /// were flushed.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to operate on the debug listener collection; otherwise, the
        /// trace listener collection is used.
        /// </param>
        /// <param name="count">
        /// Upon return, this is incremented by the number of buffered trace
        /// listeners that were flushed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode FlushBufferedTraceListeners(
            bool debug,      /* in */
            ref int count,   /* in, out */
            ref Result error /* out */
            )
        {
            TraceListenerCollection listeners = GetListeners(debug);

            if (listeners != null)
            {
                int localCount = 0;

                foreach (TraceListener listener in listeners)
                {
                    if (listener == null)
                        continue;

                    IBufferedTraceListener bufferedTraceListener =
                        listener as IBufferedTraceListener;

                    if (bufferedTraceListener == null)
                        continue;

                    if (bufferedTraceListener.MaybeFlushBuffers())
                        localCount++;
                }

                count += localCount;
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Framework Wrapper Methods
        /// <summary>
        /// This method determines whether a debugger is currently attached to
        /// the process.
        /// </summary>
        /// <returns>
        /// True if a debugger is attached; otherwise, false.
        /// </returns>
        public static bool IsAttached()
        {
            return SDD.IsAttached;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a debug failure via the framework assertion
        /// mechanism.
        /// </summary>
        /// <param name="message">
        /// The primary failure message; this value may be null.
        /// </param>
        /// <param name="detailMessage">
        /// The detailed failure message; this value may be null.
        /// </param>
        public static void Fail(
            string message,
            string detailMessage
            )
        {
            Debug.Fail(message, detailMessage);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// This method gets the collection of active debug trace listeners.
        /// </summary>
        /// <returns>
        /// The collection of active debug trace listeners.
        /// </returns>
        public static TraceListenerCollection GetDebugListeners()
        {
            return Debug.Listeners;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the collection of active trace listeners.
        /// </summary>
        /// <returns>
        /// The collection of active trace listeners.
        /// </returns>
        public static TraceListenerCollection GetTraceListeners()
        {
            return Trace.Listeners;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the selected trace listener collection.
        /// </summary>
        /// <param name="debug">
        /// Non-zero to return the debug listener collection; otherwise, the
        /// trace listener collection is returned.
        /// </param>
        /// <returns>
        /// The selected trace listener collection.
        /// </returns>
        public static TraceListenerCollection GetListeners(
            bool debug
            )
        {
#if !NET_STANDARD_20
            return debug ? GetDebugListeners() : GetTraceListeners();
#else
            return GetTraceListeners();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method writes a message to the system diagnostics log using the
        /// default category and the specified level.
        /// </summary>
        /// <param name="level">
        /// The diagnostic level to use.
        /// </param>
        /// <param name="message">
        /// The message to log; this value may be null.
        /// </param>
        private static void Log(
            int level,
            string message
            )
        {
            SDD.Log(level, DefaultCategory, message);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a message to the system diagnostics log using the
        /// default category and level.
        /// </summary>
        /// <param name="message">
        /// The message to log; this value may be null.
        /// </param>
        public static void Log(
            string message
            )
        {
            SDD.Log(0, DefaultCategory, message);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a message to the system diagnostics log using the
        /// specified category and level.
        /// </summary>
        /// <param name="level">
        /// The diagnostic level to use.
        /// </param>
        /// <param name="category">
        /// The diagnostic category to use; this value may be null.
        /// </param>
        /// <param name="message">
        /// The message to log; this value may be null.
        /// </param>
        public static void Log(
            int level,
            string category,
            string message
            )
        {
            SDD.Log(level, category, message);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the active debug
        /// listeners.
        /// </summary>
        /// <param name="value">
        /// The value to be written; this value may be null.
        /// </param>
        public static void DebugWrite(
            object value
            )
        {
            Debug.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message to the active debug
        /// listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        public static void DebugWrite(
            string message
            )
        {
            Debug.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, under the specified
        /// category, to the active debug listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        /// <param name="category">
        /// The category to be written; this value may be null.
        /// </param>
        public static void DebugWrite(
            string message,
            string category
            )
        {
            if (category != null)
                Debug.Write(message, category); /* throw */
            else
                Debug.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value, as a line, to the active
        /// debug listeners.
        /// </summary>
        /// <param name="value">
        /// The value to be written; this value may be null.
        /// </param>
        public static void DebugWriteLine(
            object value
            )
        {
            Debug.WriteLine(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, as a line, to the active
        /// debug listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        public static void DebugWriteLine(
            string message
            )
        {
            Debug.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, as a line, under the
        /// specified category, to the active debug listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        /// <param name="category">
        /// The category to be written; this value may be null.
        /// </param>
        private static void DebugWriteLine(
            string message,
            string category
            )
        {
            if (category != null)
                Debug.WriteLine(message, category); /* throw */
            else
                Debug.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes the active debug listeners.
        /// </summary>
        private static void DebugFlush()
        {
            Debug.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether trace messages should always be
        /// emitted to all active trace listeners.
        /// </summary>
        /// <returns>
        /// True if trace messages should always be emitted to all active trace
        /// listeners; otherwise, false.
        /// </returns>
        private static bool ShouldForceToListeners()
        {
            //
            // NOTE: If the "ForceToListeners" field is non-zero, ALWAYS
            //       emit trace messages to all active trace listeners.
            //
            bool? forceToListeners = GetForceToListeners();

            if (forceToListeners == null)
                return false;

            if ((bool)forceToListeners)
                return true;

            //
            // NOTE: If the "TraceToListeners" environment variable has
            //       been set, ALWAYS emit trace messages to all active
            //       trace listeners.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.TraceToListeners))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by TraceOps.QueryStatus only.
        //
        /// <summary>
        /// This method gets the current value of the flag that forces trace
        /// messages to all active trace listeners.
        /// </summary>
        /// <returns>
        /// The current flag value, or null if the lock could not be acquired.
        /// </returns>
        public static bool? GetForceToListeners()
        {
            bool locked = false;

            try
            {
                TryLock(ref locked);

                if (locked)
                    return ForceToListeners;
                else
                    return null;
            }
            finally
            {
                ExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by TraceOps.ForceEnabledOrDisabled only.
        //
        /// <summary>
        /// This method sets the flag that forces trace messages to all active
        /// trace listeners.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to force trace messages to all active trace listeners.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        public static bool SetForceToListeners(
            bool enabled /* in */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    ForceToListeners = enabled;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by TraceOps.ResetStatus only.
        //
        /// <summary>
        /// This method resets the flag that forces trace messages to all active
        /// trace listeners to its default value.
        /// </summary>
        /// <returns>
        /// True if the value was reset; otherwise, false.
        /// </returns>
        public static bool ResetForceToListeners()
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    ForceToListeners = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method moves a leading new line from the trace message to the
        /// trace category, when present.
        /// </summary>
        /// <param name="message">
        /// The trace message, possibly modified upon return; this value may be
        /// null.
        /// </param>
        /// <param name="category">
        /// The trace category, possibly modified upon return; this value may be
        /// null.
        /// </param>
        private static void MaybeModifyTraceMessageAndCategory(
            ref string message, /* in, out */
            ref string category /* in, out */
            )
        {
            if ((message == null) || (category == null))
                return;

            string newLine = Environment.NewLine;

            if ((newLine != null) && message.StartsWith(newLine))
            {
                message = message.Substring(newLine.Length);
                category = String.Format("{0}{1}", newLine, category);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to all active trace
        /// listeners.
        /// </summary>
        /// <param name="value">
        /// The value to be written; this value may be null.
        /// </param>
        public static void TraceWrite( /* RESTRICTED */
            object value
            )
        {
            Trace.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message to all active trace
        /// listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        public static void TraceWrite( /* RESTRICTED */
            string message
            )
        {
            Trace.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a trace message, honoring any configured trace
        /// text writer for the specified interpreter and falling back to the
        /// active trace listeners.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use; this value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write; this value may be null.
        /// </param>
        /// <param name="category">
        /// The trace category to use; this value may be null.
        /// </param>
        /// <returns>
        /// True if the active trace listeners were used; otherwise, false.
        /// </returns>
        public static bool TraceWrite(
            Interpreter interpreter,
            string message,
            string category
            )
        {
            //
            // HACK: Disallow displaying categories that have non-alphanumeric
            //       characters.  That probably means it is from an obfuscated
            //       assembly and there is not much point in cluttering trace
            //       output with them.
            //
            if (!TraceOps.CanDisplayCategory(category))
                category = null;

            //
            // HACK: This method is used to move a leading new line character
            //       from the message to the category.
            //
            /* NO RESULT */
            MaybeModifyTraceMessageAndCategory(ref message, ref category);

            //
            // HOOK: Allow the test suite (and others components) to override
            //       our destination [where all trace output generated by the
            //       core library is sent], including for requests originating
            //       from external callers via the "Utility.DebugTrace" method
            //       overloads).
            //
            TextWriter textWriter = SafeGetTraceTextWriter(interpreter);

            if (textWriter != null)
            {
                //
                // NOTE: Perform basic formatting of the trace message.  The
                //       only task this generally should handle is including
                //       or excluding the category (i.e. if it is null).
                //
                string formatted = FormatOps.TraceWrite(message, category);

                //
                // WARNING: It is very important that the method overload used
                //          here does not use the optional IDebugHost instance
                //          as that could lead to infinite [mutual] recursion.
                //
                bool disposed = false;

                /* NO RESULT */
                WriteTo(textWriter, formatted, true, ref disposed);

                if (disposed)
                {
                    /* NO RESULT */
                    TraceTextWriterWasDisposed(interpreter);
                }
                else
                {
                    /* NO RESULT */
                    TraceOps.TraceWasLogged(
                        interpreter, message, category, null);
                }

                if (!ShouldForceToListeners())
                {
                    if (disposed)
                    {
                        //
                        // HACK: Writing to the TraceTextWriter failed due to
                        //       its disposal AND we are not going to use the
                        //       trace listeners; therefore, this message has
                        //       technically been dropped.
                        //
                        /* NO RESULT */
                        TraceOps.TraceWasDropped(
                            interpreter, message, category, null);
                    }

                    return false; /* NOTE: "Trace.Listeners" were not used. */
                }
            }

            //
            // NOTE: There is no configured TraceTextWriter for the specified
            //       interpreter -OR- the configuration requires us to always
            //       emit messages to all trace listeners.  Either way, send
            //       the message to all trace listeners, via the Trace.Write
            //       method.
            //
            /* NO RESULT */
            TraceWrite(message, category); /* EXEMPT */

            return true; /* NOTE: "Trace.Listeners" were used. */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, under the specified
        /// category, to all active trace listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        /// <param name="category">
        /// The category to be written; this value may be null.
        /// </param>
        public static void TraceWrite( /* RESTRICTED */
            string message,
            string category
            )
        {
            if (category != null)
                Trace.Write(message, category); /* throw */
            else
                Trace.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value, as a line, to all active
        /// trace listeners.
        /// </summary>
        /// <param name="value">
        /// The value to be written; this value may be null.
        /// </param>
        public static void TraceWriteLine( /* RESTRICTED */
            object value
            )
        {
            Trace.WriteLine(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, as a line, to all active
        /// trace listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        public static void TraceWriteLine( /* RESTRICTED */
            string message
            )
        {
            Trace.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, as a line, under the
        /// specified category, to all active trace listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        /// <param name="category">
        /// The category to be written; this value may be null.
        /// </param>
        public static void TraceWriteLine( /* RESTRICTED */
            string message,
            string category
            )
        {
            if (category != null)
                Trace.WriteLine(message, category); /* throw */
            else
                Trace.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified message, as a line, prefixed with
        /// the current system thread identifier, to all active trace listeners.
        /// </summary>
        /// <param name="message">
        /// The message to be written; this value may be null.
        /// </param>
        /// <param name="category">
        /// The category to be written; this value may be null.
        /// </param>
        public static void TraceWriteLineFormatted( /* RESTRICTED */
            string message,
            string category
            )
        {
            string formatted = String.Format(
                "{0}: {1}", GlobalState.GetCurrentSystemThreadId(),
                message);

            TraceWriteLine(formatted, category); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes all active trace listeners.
        /// </summary>
        public static void TraceFlush()
        {
            Trace.Flush();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Integration Methods
        /// <summary>
        /// This method flushes both the active trace listeners and the active
        /// debug listeners, ignoring any exception that is raised.
        /// </summary>
        public static void Flush()
        {
            try
            {
                TraceFlush(); /* throw */
            }
            catch
            {
                //
                // BUGBUG: Maybe complain here?  Break
                //         into the debugger, etc?
                //
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            try
            {
                DebugFlush(); /* throw */
            }
            catch
            {
                //
                // BUGBUG: Maybe complain here?  Break
                //         into the debugger, etc?
                //
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Break-Into-Debugger Methods
        /// <summary>
        /// This method determines whether breaking into the debugger has been
        /// disabled via the environment.
        /// </summary>
        /// <returns>
        /// True if breaking into the debugger is disabled; otherwise, false.
        /// </returns>
        public static bool IsBreakDisabled()
        {
            return CommonOps.Environment.DoesVariableExist(EnvVars.NoBreak);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method breaks into an attached debugger, unless breaking into
        /// the debugger has been disabled.
        /// </summary>
        public static void Break()
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(null);
                return;
            }

            SDD.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method breaks into the debugger only when one is attached and
        /// breaking into the debugger has not been disabled.
        /// </summary>
        public static void MaybeBreak()
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(null);
                return;
            }

            if (SDD.IsAttached)
                SDD.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method logs the specified message and breaks into the debugger,
        /// but only when one is attached and breaking into the debugger has not
        /// been disabled.
        /// </summary>
        /// <param name="message">
        /// The message to log prior to breaking; this value may be null.
        /// </param>
        public static void MaybeBreak(
            string message
            )
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(message);
                return;
            }

            if (SDD.IsAttached)
            {
                SDD.Log(0, DefaultCategory, message);
                SDD.Break();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports that breaking into the debugger has been
        /// disabled.
        /// </summary>
        /// <param name="message">
        /// An optional message describing the context; this value may be null.
        /// </param>
        private static void ReportBreakIsDisabled(
            string message
            )
        {
            string formatted = String.Format(
                BreakIsDisabled, EnvVars.NoBreak,
                FormatOps.DisplayString(message));

            WriteWithoutFail(formatted);

            if (SDD.IsAttached)
                SDD.Log(0, DefaultCategory, formatted);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Miscellaneous Debugging Methods
        /// <summary>
        /// This method writes diagnostic information about the specified
        /// application domain and its loaded assemblies to the active trace
        /// listeners.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to dump; this value may be null.
        /// </param>
        public static void DumpAppDomain(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                TraceWriteLineFormatted(String.Format(
                    "DumpAppDomain: Id = {0}, FriendlyName = {1}, " +
                    "BaseDirectory = {2}, RelativeSearchPath = {3}, " +
                    "DynamicDirectory = {4}, ShadowCopyFiles = {5}",
                    AppDomainOps.GetIdString(appDomain, true),
                    FormatOps.WrapOrNull(appDomain.FriendlyName),
                    FormatOps.WrapOrNull(appDomain.BaseDirectory),
                    FormatOps.WrapOrNull(appDomain.RelativeSearchPath),
                    FormatOps.WrapOrNull(appDomain.DynamicDirectory),
                    appDomain.ShadowCopyFiles),
                    typeof(DebugOps).Name); /* EXEMPT */

                Assembly[] assemblies = appDomain.GetAssemblies();

                if (assemblies != null)
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        string name = null;
                        string location = null;

                        if (assembly != null)
                        {
                            AssemblyName assemblyName = assembly.GetName();

                            if (assemblyName != null)
                                name = assemblyName.ToString();

                            try
                            {
                                location = assembly.Location;
                            }
                            catch (NotSupportedException)
                            {
                                // do nothing.
                            }
                        }

                        TraceWriteLineFormatted(String.Format(
                            "DumpAppDomain: assemblyName = {0}, " +
                            "location = {1}", FormatOps.WrapOrNull(name),
                            FormatOps.WrapOrNull(location)),
                            typeof(DebugOps).Name); /* EXEMPT */
                    }
                }
            }
            else
            {
                TraceWriteLineFormatted(
                    "DumpAppDomain: invalid application domain",
                    typeof(DebugOps).Name); /* EXEMPT */
            }
        }
        #endregion
    }
}
