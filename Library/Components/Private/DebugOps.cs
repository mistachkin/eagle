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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if TEST
using IBufferedTraceListener = Eagle._Tests.Default.IBufferedTraceListener;
using ScriptTraceListener = Eagle._Tests.Default.ScriptTraceListener;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("1d388444-db3b-41b5-a23e-b25084d1c94b")]
    internal static class DebugOps
    {
        #region Public Constants
        public static readonly string DefaultCategory =
            System.Diagnostics.Debugger.DefaultCategory;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly string ListenerName =
            typeof(Interpreter).FullName + ".LogFile";

        ///////////////////////////////////////////////////////////////////////

        private const string TraceLogFileDataName = "TraceLogFileName";
        private const string TraceLogDataName = "TraceLogName";

        ///////////////////////////////////////////////////////////////////////

        private const string TraceLogInterpreterDataName = "TraceLogInterpreter";
        private const string TraceLogEncodingDataName = "TraceLogEncoding";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TextWriteExceptionFormat =
            "write of text failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string HostWriteExceptionFormat =
            "write to host failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TestWriteExceptionFormat =
            "write via test failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TextWriterDisposedFormat =
            "{0} text writer for interpreter {1} was disposed and is now " +
            "disabled{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string BreakIsDisabled =
            "breaking into debugger was disabled via environment variable " +
            "\"{0}\": {1}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly char[] StackTrimChars = {
            Characters.CarriageReturn, Characters.LineFeed
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int ComplainRetryLimit = 3;
        private static int ComplainRetryMilliseconds = 750;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultSameTraceListenerTypeOnly = true;

        ///////////////////////////////////////////////////////////////////////

#if TEST
        //
        // NOTE: These are the format strings used when building the test
        //       trace log file name.
        //
        private const string TraceBareLogFileFormat = "trace-{1}-";
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
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The current number of calls to Complain() that are active
        //       on this thread.  This number should always be zero or one.
        //
        // BUGFIX: Previously, this was a global value, not per thread, and
        //         that was wrong.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static int complainLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       per-thread and never reset.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static long complainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       global (AppDomain) and never reset.
        //
        private static long globalComplainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called while
        //       quiet mode is enabled.  It is per-thread and never reset.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static long complainQuietCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called while
        //       quiet mode is enabled.  It is global (AppDomain) and never
        //       reset.
        //
        private static long globalComplainQuietCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The most recent complaint message seen by this subsystem.
        //
        private static string globalComplaint = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, failsafe write calls will also
        //       output to the trace listeners, if any.
        //
        private static bool UseTraceForWithoutFail = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, failsafe write calls will also
        //       output to the specified IDebugHost, if any.
        //
        private static bool UseHostForWithoutFail = false;

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
        private static bool IgnoreOnCallbackThrow = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        private static bool AllowComplainViaTrace = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        private static bool AllowComplainViaTest = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        private static bool SkipCurrentForComplainViaTest = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        private static bool IgnoreQuietForComplainViaTest = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        private static bool AutoFlushOnWrite = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        private static bool AutoFlushOnClear = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, calls to the Flush() method will
        //       be performed at appropriate times.  Generally, this will be
        //       used with instances of the TextWriter class.
        //
        private static bool AutoFlushOnClose = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is non-zero, ALWAYS emit trace messages to
        //       all active trace listeners.
        //
        private static bool ForceToListeners = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Stack Trace Methods
        private static bool ContainsMethodName(
            StringList skipNames,
            string name
            )
        {
            if (skipNames == null)
                return false;

            //
            // TODO: *PERF* Should this take into account case?  If not,
            //       the alternative Contains method overload could be
            //       used; however, it will not perform as well.
            //
            return skipNames.Contains(name);
        }

        ///////////////////////////////////////////////////////////////////////

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetStackTraceString()
        {
            return GetStackTraceString(1, null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static bool ShouldSkipMethodType(
            MethodBase methodBase,    /* in */
            bool skipDebug,           /* in */
            ref Type methodType,      /* out */
            ref string methodBaseName /* out */
            )
        {
            if (methodBase == null)
                return true;

            Type localMethodType = methodBase.DeclaringType;

            if (localMethodType == null)
                return true;

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

        private static bool ShouldSkipMethodName(
            StringList skipNames,     /* in */
            Type methodType,          /* in */
            string methodBaseName,    /* in */
            ref string methodFullName /* out */
            )
        {
            //
            // NOTE: Format the method name with its full type name,
            //       with and without the namespace name.
            //
            string localMethodFullName;
            string localMethodName;

            if (FormatOps.IsSameAssembly(methodType))
            {
                localMethodFullName = methodBaseName;
                localMethodName = methodBaseName;
            }
            else
            {
                localMethodFullName = FormatOps.MethodQualifiedFullName(
                    methodType, methodBaseName);

                localMethodName = FormatOps.MethodQualifiedName(
                    methodType, methodBaseName);
            }

            //
            // NOTE: Does the method name, using any of the formats
            //       we have, match something in the skip list?
            //
            if ((skipNames != null) &&
                (ContainsMethodName(skipNames, methodBaseName) ||
                ContainsMethodName(skipNames, localMethodFullName) ||
                ContainsMethodName(skipNames, localMethodName)))
            {
                return true;
            }

            methodFullName = localMethodFullName;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetMethodName(
            int skipFrames,        /* in */
            StringList skipNames,  /* in */
            bool skipDebug,        /* in */
            bool nameOnly,         /* in */
            string defaultName,    /* in */
            out bool thisAssembly, /* out */
            out string typeName,   /* out */
            out string methodName  /* out */
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
                {
                    thisAssembly = false;
                    typeName = null;
                    methodName = defaultName;

                    return;
                }

                //
                // NOTE: Always skip this method (i.e. we start with at
                //       least one, not zero).
                //
                int count = stackTrace.FrameCount;

                for (int index = skipFrames + 1; index < count; index++)
                {
                    //
                    // NOTE: Get the stack frame for the current index.
                    //
                    StackFrame stackFrame = stackTrace.GetFrame(index);

                    if (stackFrame == null)
                        continue;

                    //
                    // NOTE: Skip this method (based on its declaring
                    //       type)?
                    //
                    Type methodType = null;
                    string methodBaseName = null;

                    if (ShouldSkipMethodType(
                            stackFrame.GetMethod(), skipDebug,
                            ref methodType, ref methodBaseName))
                    {
                        continue;
                    }

                    //
                    // NOTE: Skip this method (based on the name and/or
                    //       the type qualified method name)?
                    //
                    string methodFullName = null;

                    if (ShouldSkipMethodName(
                            skipNames, methodType, methodBaseName,
                            ref methodFullName))
                    {
                        continue;
                    }

                    //
                    // NOTE: Return only the bare method name -OR- the
                    //       method name formatted with its declaring
                    //       type.
                    //
                    if (methodType != null)
                    {
                        thisAssembly = GlobalState.IsAssembly(
                            methodType.Assembly);

                        typeName = methodType.FullName;
                    }
                    else
                    {
                        thisAssembly = false;
                        typeName = null;
                    }

                    if (nameOnly)
                        methodName = methodBaseName;
                    else
                        methodName = methodFullName;

                    return;
                }
            }
            catch
            {
                // do nothing.
            }

            thisAssembly = false;
            typeName = null;
            methodName = defaultName;
        }

        ///////////////////////////////////////////////////////////////////////

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
            bool thisAssembly; /* NOT USED */
            string typeName; /* NOT USED */
            string methodName;

            GetMethodName(
                2, skipNames, true, false, defaultName, out thisAssembly,
                out typeName, out methodName);

            return methodName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Stack Trace Methods
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
        private static long GetComplaintId()
        {
            return GlobalState.NextComplaintId();
        }

        ///////////////////////////////////////////////////////////////////////

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
                    interpreter.InternalSoftTryLock(
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
                        return interpreter.Host; /* throw */
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

        public static string SafeGetGlobalComplaint()
        {
            return Interlocked.CompareExchange(
                ref globalComplaint, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SafeSetGlobalComplaint(
            string complaint
            )
        {
            /* IGNORED */
            Interlocked.Exchange(ref globalComplaint, complaint);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SafeUnsetGlobalComplaint() /* FOR TEST USE ONLY */
        {
            /* IGNORED */
            Interlocked.Exchange(ref globalComplaint, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Output Support Methods
#if NATIVE
        public static void Output(
            string message
            )
        {
            NativeOps.OutputDebugMessage(String.Format(
                "{0}{1}", message, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Output(
            Exception exception
            )
        {
            if (exception == null)
                return;

            Output(String.Format(
                "{0}{1}", exception, Environment.NewLine));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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

        //
        // WARNING: This method is called from places where the interpreter
        //          host may have failed to emit output; therefore, it must
        //          never attempt to use the interpreter host.
        //
        private static void WriteWithoutFail(
            string value
            )
        {
            WriteWithoutFail(null, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value
            )
        {
            WriteWithoutFail(
                debugHost, value, UseHostForWithoutFail);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaHost
            )
        {
            WriteWithoutFail(
                debugHost, value, Build.Debug, true, viaHost);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaOutput,
            bool viaTrace,
            bool viaHost
            )
        {
#if NATIVE
            if (viaOutput)
                Output(value);
#endif

            ///////////////////////////////////////////////////////////////////

            if (viaTrace)
                WriteViaDebugAndOrTrace(value);

            ///////////////////////////////////////////////////////////////////

            if (viaHost && (debugHost != null))
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
                        /* IGNORED */
                        debugHost.WriteResult(
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
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void TextWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TextWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void HostWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                HostWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void TestWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TestWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsComplainPending()
        {
            return Interlocked.CompareExchange(ref complainLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method must *NOT* throw any exceptions.
        //
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
            bool nullResult = (result == null);

            ComplainCallback callback = SafeGetComplainCallback(interpreter);

            long id = GetComplaintId();

            bool stack = nullResult || SafeGetTraceStack(interpreter,
                GetDefaultTraceStack(SafeGetDefaultTraceStack(interpreter)));

            string stackTrace = stack ? GetStackTraceString() : null;

            bool viaTrace = SafeGetComplainViaTrace(interpreter, false);
            bool viaTest = SafeGetComplainViaTest(interpreter, false);

            bool quiet = SafeGetQuiet(interpreter,
                GetDefaultQuiet(SafeGetDefaultQuiet(interpreter)));

            bool disposed = false;

            Complain(
                callback, interpreter, SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), id, code, result, stackTrace,
                viaTrace, viaTest, quiet, ref disposed);

            if (disposed)
                DebugTextWriterWasDisposed(interpreter);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Complaint Reporting Methods
        private static bool IsComplainPossible()
        {
            return !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

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
                        if (textWriter != null)
                        {
                            try
                            {
                                lock (textWriter) /* TRANSACTIONAL */
                                {
                                    textWriter.WriteLine(
                                        formatted); /* throw */

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
                                TextWriteException(id, e);
                            }
                        }

                        ///////////////////////////////////////////////////////

                    retryHost:

                        if (debugHost != null)
                        {
                            //
                            // BUGFIX: The host may have been disposed at this
                            //         point and we do NOT want to throw an
                            //         exception; therefore, wrap the host
                            //         access in a try block.  If the host does
                            //         throw an exception for any reason, we
                            //         will simply null out the host and retry
                            //         using our default handling.
                            //
                            try
                            {
                                if (IsHostUsable(
                                        debugHost, HostFlags.Complain))
                                {
                                    debugHost.WriteErrorLine(
                                        formatted); /* throw */
                                }
                            }
                            catch (Exception e)
                            {
                                HostWriteException(id, e);

                                debugHost = null;

                                goto retryHost;
                            }
                        }
#if WINFORMS
                        else
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
                    ReturnCode.Error, e));
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
                    ReturnCode.Error, e));
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

        private static Interpreter GetTraceLogInterpreter(
            IClientData clientData /* in */
            )
        {
            Interpreter interpreter;

            if (clientData is IAnyClientData)
            {
                IAnyClientData anyClientData = (IAnyClientData)clientData;
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

        private static Encoding GetTraceLogEncoding(
            Interpreter interpreter, /* in */
            IClientData clientData   /* in */
            )
        {
            Encoding encoding;

            if (clientData is IAnyClientData)
            {
                IAnyClientData anyClientData = (IAnyClientData)clientData;
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

        private static string GetTraceLogName(
            IClientData clientData /* in */
            )
        {
            string name;

            if (clientData is IAnyClientData)
            {
                IAnyClientData anyClientData = (IAnyClientData)clientData;
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

            if (clientData is IAnyClientData)
            {
                IAnyClientData anyClientData = (IAnyClientData)clientData;

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

        public static TraceListener NewTraceListener(
            TraceListenerType listenerType,
            IClientData clientData
            )
        {
            Result error = null;

            return NewTraceListener(listenerType, clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

                        return NewTestTraceListener(
                            logName, logFileName, encoding);
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
        private static TraceListener NewNativeTraceListener()
        {
            return new _Tests.Default.NativeTraceListener();
        }

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewNativeTraceListener(
            string name
            )
        {
            return new _Tests.Default.NativeTraceListener(name);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewTestTraceListener()
        {
            return new _Tests.Default.Listener();
        }

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewTestTraceListener(
            string name
            )
        {
            return new _Tests.Default.Listener(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListener NewTestTraceListener(
            string name,
            string fileName
            )
        {
            return NewTestTraceListener(name, fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewTestTraceListener(
            string name,
            string fileName,
            Encoding encoding
            )
        {
            return NewTestTraceListener(
                name, fileName, encoding, 0, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewTestTraceListener(
            string name,
            string fileName,
            Encoding encoding,
            int bufferSize,
            bool expandBuffer,
            bool zeroBuffer
            )
        {
            return new _Tests.Default.Listener(
                name, fileName, encoding, bufferSize,
                expandBuffer, zeroBuffer);
        }

        ///////////////////////////////////////////////////////////////////////

        private static TraceListener NewBufferedTraceListener()
        {
            Result error = null; /* NOT USED */

            return _Tests.Default.BufferedTraceListener.Create(
                null, BufferedTraceFlags.None, 0, 0, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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
                    Type type = listener.GetType();

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

        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return AddTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return RemoveTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode SetupTraceLogFile(
            string name,                /* in: OPTIONAL */
            string fileName,            /* in */
            Encoding encoding,          /* in: OPTIONAL */
            bool trace,                 /* in */
            bool debug,                 /* in */
            bool console,               /* in */
            bool verbose,               /* in */
            bool typeOnly,              /* in */
            ref Result error            /* out */
            )
        {
            TraceListener listener = null;

            return SetupTraceLogFile(
                name, fileName, encoding, trace, debug, console,
                verbose, typeOnly, ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetupTraceLogFile(
            string name,                /* in: OPTIONAL */
            string fileName,            /* in */
            Encoding encoding,          /* in: OPTIONAL */
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
                    name, fileName, encoding);

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
                Output(ResultOps.Format(code, error));
            }
#endif

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsAttached()
        {
            return System.Diagnostics.Debugger.IsAttached;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Fail(
            string message,
            string detailMessage
            )
        {
            Debug.Fail(message, detailMessage);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        public static TraceListenerCollection GetDebugListeners()
        {
            return Debug.Listeners;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static TraceListenerCollection GetTraceListeners()
        {
            return Trace.Listeners;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static void Log(
            int level,
            string message
            )
        {
            System.Diagnostics.Debugger.Log(level, DefaultCategory, message);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static void Log(
            string message
            )
        {
            System.Diagnostics.Debugger.Log(0, DefaultCategory, message);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Log(
            int level,
            string category,
            string message
            )
        {
            System.Diagnostics.Debugger.Log(level, category, message);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            object value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Debug.Write(value); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            string message
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Debug.Write(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            string message,
            string category
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (category != null)
                    Debug.Write(message, category); /* throw */
                else
                    Debug.Write(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWriteLine(
            object value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Debug.WriteLine(value); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWriteLine(
            string message
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Debug.WriteLine(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DebugWriteLine(
            string message,
            string category
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (category != null)
                    Debug.WriteLine(message, category); /* throw */
                else
                    Debug.WriteLine(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DebugFlush()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Debug.Flush();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldForceToListeners()
        {
            //
            // NOTE: If the "ForceToListeners" field is non-zero, ALWAYS
            //       emit trace messages to all active trace listeners.
            //
            if (GetForceToListeners())
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
        public static bool GetForceToListeners()
        {
            lock (syncRoot)
            {
                return ForceToListeners;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by TraceOps.ForceEnabledOrDisabled only.
        //
        public static void SetForceToListeners(
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                ForceToListeners = enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by TraceOps.ResetStatus only.
        //
        public static void ResetForceToListeners()
        {
            lock (syncRoot)
            {
                ForceToListeners = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void TraceWrite( /* RESTRICTED */
            object value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Trace.Write(value); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWrite( /* RESTRICTED */
            string message
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Trace.Write(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void TraceWrite( /* RESTRICTED */
            string message,
            string category
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (category != null)
                    Trace.Write(message, category); /* throw */
                else
                    Trace.Write(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine( /* RESTRICTED */
            object value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Trace.WriteLine(value); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine( /* RESTRICTED */
            string message
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Trace.WriteLine(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine( /* RESTRICTED */
            string message,
            string category
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (category != null)
                    Trace.WriteLine(message, category); /* throw */
                else
                    Trace.WriteLine(message); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void TraceFlush()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Trace.Flush();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Integration Methods
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
        public static bool IsBreakDisabled()
        {
            return CommonOps.Environment.DoesVariableExist(EnvVars.NoBreak);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Break()
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(null);
                return;
            }

            System.Diagnostics.Debugger.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeBreak()
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(null);
                return;
            }

            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeBreak(
            string message
            )
        {
            if (IsBreakDisabled())
            {
                ReportBreakIsDisabled(message);
                return;
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Log(0, DefaultCategory, message);
                System.Diagnostics.Debugger.Break();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ReportBreakIsDisabled(
            string message
            )
        {
            string formatted = String.Format(
                BreakIsDisabled, EnvVars.NoBreak,
                FormatOps.DisplayString(message));

            WriteWithoutFail(formatted);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Log(
                    0, DefaultCategory, formatted);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Miscellaneous Debugging Methods
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
                    AppDomainOps.GetId(appDomain),
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
