/*
 * TestOps.cs --
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    [ObjectId("c2335b96-f944-44c5-97dc-abdb6dd08525")]
    internal static class TestOps
    {
        #region Private Constants
        private static readonly string KnownBugConstraint = "knownBug";
        private static readonly string NotKnownBugConstraint = "!knownBug";
        private static readonly string FailConstraintPrefix = "fail.";

        private static readonly MatchMode NameMatchMode = StringOps.DefaultMatchMode;
        internal static readonly RegexOptions RegExOptions = StringOps.DefaultRegExTestOptions;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        internal static readonly string TestToken = "%test%";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string IsolationPrefix = "-isolation";
        private static readonly int MinimumArgumentCount = 3;
        private static readonly string MonoExecutableName = "mono";
        private static readonly string LogFileOption = "-logFile"; /* NOTE: For "test.eagle" package. */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string DotNetCoreExecutableArgument = "exec"; // TODO: Not official?
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These file names are skipped by the interactive "#test" command
        //       prior to any other pattern matching.
        //
        private static readonly MatchMode skipFileNameMatchMode = MatchMode.Exact;

        private static readonly StringList skipFileNames = new StringList(new string[] {
            "epilogue.eagle", "prologue.eagle"
        });

        private const string fileNameWarningVarIndex = "warningForAllEagle";
        private const string directoryWarningVarIndex = "warningForTestsPath";
        private const string suiteFileName = "all.eagle";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        private static bool IgnoreQuietForWarning = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        private static string NewLine = Environment.NewLine; /* COMPAT: StringBuilder */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        private static string logNormalCommand = "::tlog";
        private static string logFallbackCommand = "::tqlog";

        internal static string putsNormalCommand = "::tputs";
        internal static string putsFallbackCommand = "::tqputs";

        private static string putsNormalChannelVarName = "::test_channel";
        private static string putsFallbackChannel = Channel.StdOut;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        internal static int hostWorkItemDelay = 10000; /* in milliseconds */
        internal static bool hostWorkItemForce = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        internal static int DefaultRepeatCount = 1;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the _Hosts.Default.BuildTestInfoList method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            if (empty || IgnoreQuietForWarning)
                localList.Add("IgnoreQuietForWarning", IgnoreQuietForWarning.ToString());

            if (empty || (NewLine != null))
                localList.Add("NewLine", FormatOps.DisplayString(NewLine));

            if (empty || (putsNormalCommand != null))
            {
                localList.Add("PutsNormalCommand",
                    FormatOps.DisplayString(putsNormalCommand));
            }

            if (empty || (putsFallbackCommand != null))
            {
                localList.Add("PutsFallbackCommand",
                    FormatOps.DisplayString(putsFallbackCommand));
            }

            if (empty || (putsNormalChannelVarName != null))
            {
                localList.Add("PutsNormalChannelVarName",
                    FormatOps.DisplayString(putsNormalChannelVarName));
            }

            if (empty || (putsFallbackChannel != null))
            {
                localList.Add("PutsFallbackChannel",
                    FormatOps.DisplayString(putsFallbackChannel));
            }

            if (empty || (hostWorkItemDelay != 0))
            {
                localList.Add("HostWorkItemDelay",
                    hostWorkItemDelay.ToString());
            }

            if (empty || hostWorkItemForce)
            {
                localList.Add("HostWorkItemForce",
                    hostWorkItemForce.ToString());
            }

            if (empty || (DefaultRepeatCount != 0))
            {
                localList.Add("DefaultRepeatCount",
                    DefaultRepeatCount.ToString());
            }

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Test Suite");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Result Matching Methods
        public static string MakeWhiteSpaceVisible(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = StringOps.NewStringBuilder(value);

            MakeWhiteSpaceVisible(builder);

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void MakeWhiteSpaceVisible(
            StringBuilder builder
            )
        {
            StringOps.FixupWhiteSpace(
                builder, Characters.Space, WhiteSpaceFlags.TestUse);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ShouldIgnoreMismatch(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            StringList patterns,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions,
            bool debug
            )
        {
            if (patterns == null)
                return false;

            foreach (string pattern in patterns)
            {
                ReturnCode code;
                bool match = false;
                Result result = null;

                code = Match(
                    interpreter, mode, text, pattern, noCase,
                    comparer, regExOptions, debug, ref match,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    //
                    // HACK: Ok, so we cannot compare the pattern to really know
                    //       if we should ignore this test error; therefore, err
                    //       on the side of caution and forbid ignoring the test
                    //       error based on this pattern.
                    //
                    TraceOps.DebugTrace(String.Format(
                        "ShouldIgnoreMismatch: interpreter = {0}, mode = {1}, " +
                        "noCase = {2}, comparer = {3}, regExOptions = {4}, " +
                        "debug = {5}, patterns = {6}, text = {7}, match = {8}, " +
                        "result = {9}{10}",
                        FormatOps.InterpreterNoThrow(interpreter), mode, noCase,
                        FormatOps.WrapOrNull(comparer), regExOptions, debug,
                        FormatOps.WrapOrNull(ArrayOps.ToHexadecimalString(patterns)),
                        FormatOps.WrapOrNull(ArrayOps.ToHexadecimalString(text)),
                        match, FormatOps.WrapOrNull(true, true, result),
                        Environment.NewLine), typeof(TestOps).Name,
                        TracePriority.TestError);

                    continue;
                }

                //
                // NOTE: If we match against any of the specified patterns, stop
                //       and indicate to our caller that the test result mismatch
                //       should be ignored.
                //
                if (match)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ShouldIgnoreFailure(
            bool fail,
            bool outputIgnore,
            bool errorIgnore,
            bool scriptIgnore
            )
        {
            if (!fail)
                return true;

            if (outputIgnore || errorIgnore || scriptIgnore)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions,
            bool debug,
            ref bool match,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Error;

            if (interpreter != null)
            {
                if (mode == MatchMode.Expression)
                {
                    if ((interpreter.EvaluateExpression(
                            pattern, ref result) == ReturnCode.Ok) &&
                        (Engine.ToBoolean(
                            result, interpreter.InternalCultureInfo, ref match,
                            ref result) == ReturnCode.Ok))
                    {
                        code = ReturnCode.Ok;
                    }
                }
                else
                {
                    if (StringOps.Match(
                            interpreter, mode, text, pattern, noCase,
                            comparer, regExOptions, ref match,
                            ref result) == ReturnCode.Ok)
                    {
                        code = ReturnCode.Ok;
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            if (debug)
            {
                TraceOps.DebugTrace(String.Format(
                    "Match: interpreter = {0}, mode = {1}, noCase = {2}, " +
                    "comparer = {3}, regExOptions = {4}, debug = {5}, " +
                    "pattern = {6}, text = {7}, code = {8}, match = {9}, " +
                    "result = {10}{11}",
                    FormatOps.InterpreterNoThrow(interpreter), mode, noCase,
                    FormatOps.WrapOrNull(comparer), regExOptions, debug,
                    FormatOps.WrapOrNull(ArrayOps.ToHexadecimalString(pattern)),
                    FormatOps.WrapOrNull(ArrayOps.ToHexadecimalString(text)),
                    code, match, FormatOps.WrapOrNull(true, true, result),
                    Environment.NewLine), typeof(TestOps).Name,
                    TracePriority.TestDebug);
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Data Support Methods
        private static string StringFromObject(
            object value
            )
        {
            return StringOps.GetStringFromObject(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void Append(
            Interpreter interpreter,
            StringBuilder testData,
            TestOutputType outputType,
            object value
            )
        {
            bool write = ShouldWriteTestData(interpreter, outputType);
            string formatted = null;

            try
            {
                formatted = StringFromObject(value); /* throw */
            }
            catch (Exception e)
            {
                DebugOps.Complain(interpreter, ReturnCode.Error, e);
            }

            if (write)
            {
                if ((formatted == null) ||
                    !TryWriteViaHost(interpreter, formatted, false))
                {
                    write = false;
                }
            }

            if (ShouldReturnTestData(interpreter, outputType, write))
            {
                //
                // WARNING: Do not remove this code, it is needed for backward
                //          compatibility with Eagle (beta).
                //
                if (testData != null) testData.Append(value);
            }
            else if (ShouldLogTestData(interpreter, outputType))
            {
                //
                // HACK: We know that output not returned does not end up in
                //       the log file, even if it does end up being written to
                //       the host; therefore, attempt to forcibly log it now.
                //
                /* IGNORED */
                TryWriteViaLog(interpreter, formatted, false);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void AppendLine(
            Interpreter interpreter,
            StringBuilder testData,
            TestOutputType outputType
            )
        {
            bool write = ShouldWriteTestData(interpreter, outputType);

            if (write && !TryWriteViaHost(interpreter, NewLine, false))
                write = false;

            if (ShouldReturnTestData(interpreter, outputType, write))
            {
                //
                // WARNING: Do not remove this code, it is needed for backward
                //          compatibility with Eagle (beta).
                //
                if (testData != null) testData.AppendLine();
            }
            else
            {
                //
                // HACK: We know that output not returned does not end up in
                //       the log file, even if it does end up being written to
                //       the host; therefore, attempt to forcibly log it now.
                //
                /* IGNORED */
                TryWriteViaLog(interpreter, NewLine, false);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void AppendLine(
            Interpreter interpreter,
            StringBuilder testData,
            TestOutputType outputType,
            string value
            )
        {
            bool write = ShouldWriteTestData(interpreter, outputType);
            string formatted = null;

            try
            {
                formatted = String.Format("{0}{1}", value, NewLine);
            }
            catch (Exception e)
            {
                DebugOps.Complain(interpreter, ReturnCode.Error, e);
            }

            if (write)
            {
                if ((formatted == null) ||
                    !TryWriteViaHost(interpreter, formatted, false))
                {
                    write = false;
                }
            }

            if (ShouldReturnTestData(interpreter, outputType, write))
            {
                //
                // WARNING: Do not remove this code, it is needed for backward
                //          compatibility with Eagle (beta).
                //
                if (testData != null) testData.AppendLine(value);
            }
            else
            {
                //
                // HACK: We know that output not returned does not end up in
                //       the log file, even if it does end up being written to
                //       the host; therefore, attempt to forcibly log it now.
                //
                /* IGNORED */
                TryWriteViaLog(interpreter, formatted, false);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void AppendFormat(
            Interpreter interpreter,
            StringBuilder testData,
            TestOutputType outputType,
            string format,
            params object[] args
            )
        {
            bool write = ShouldWriteTestData(interpreter, outputType);
            string formatted = null;

            try
            {
                formatted = String.Format(format, args); /* throw */
            }
            catch (Exception e)
            {
                DebugOps.Complain(interpreter, ReturnCode.Error, e);
            }

            if (write)
            {
                if ((formatted == null) ||
                    !TryWriteViaHost(interpreter, formatted, false))
                {
                    write = false;
                }
            }

            if (ShouldReturnTestData(interpreter, outputType, write))
            {
                //
                // WARNING: Do not remove this code, it is needed for backward
                //          compatibility with Eagle (beta).
                //
                if (testData != null) testData.AppendFormat(format, args);
            }
            else
            {
                //
                // HACK: We know that output not returned does not end up in
                //       the log file, even if it does end up being written to
                //       the host; therefore, attempt to forcibly log it now.
                //
                /* IGNORED */
                TryWriteViaLog(interpreter, formatted, false);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Statistics Support Methods
        private static bool IsRepeating(
            int repeatCount
            )
        {
            return repeatCount > 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetRepeatSuffix(
            int iterationCount,
            int repeatCount
            )
        {
            if (!IsRepeating(repeatCount))
                return String.Empty;

            return String.Format(" (iteration {0}/{1})", iterationCount, repeatCount);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the TraceOps class only.
        //
        public static string GetCurrentName(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                Interpreter localInterpreter = EntityOps.FollowTest(
                    interpreter, true);

                if (localInterpreter != null)
                {
#if !THREADING
                    bool locked = false;

                    try
                    {
                        localInterpreter.InternalSoftTryLock(
                            ref locked); /* TRANSACTIONAL */

                        if (locked)
#endif
                            return localInterpreter.TestCurrent;
#if !THREADING
                    }
                    finally
                    {
                        localInterpreter.InternalExitLock(
                            ref locked); /* TRANSACTIONAL */
                    }
#endif
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode RecordInformation(
            Interpreter interpreter,
            TestInformationType type,
            string name,
            object value,
            bool add
            )
        {
            Result error = null;

            return RecordInformation(interpreter, type, name, value, add, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode RecordInformation(
            Interpreter interpreter,
            TestInformationType type,
            string name,
            object value,
            bool add,
            ref Result error
            )
        {
            int level = 0;

            return RecordInformation(interpreter, type, name, value, add, ref level, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode RecordInformation(
            Interpreter interpreter,
            TestInformationType type,
            string name,
            object value,
            bool add,
            ref int level,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                switch (type)
                {
                    case TestInformationType.CurrentName:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (add)
                            {
                                if (name != null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestCurrent = name;
                                        return ReturnCode.Ok;
                                    }
                                }
                                else
                                {
                                    error = "invalid test name";
                                }
                            }
                            else
                            {
                                if (name == null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestCurrent = null;
                                        return ReturnCode.Ok;
                                    }
                                }
                                else
                                {
                                    error = "invalid test name";
                                }
                            }
                            break;
                        }
                    case TestInformationType.Interpreter:
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                Interpreter localInterpreter = null;

                                if (Value.GetInterpreter(
                                        interpreter, StringOps.GetStringFromObject(value),
                                        InterpreterType.Default, ref localInterpreter,
                                        ref error) == ReturnCode.Ok)
                                {
                                    interpreter.TestTargetInterpreter = localInterpreter;

                                    return ReturnCode.Ok;
                                }
                            }
                            break;
                        }
                    case TestInformationType.RepeatCount:
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                int intValue = 0;

                                if (Value.GetInteger2(
                                        StringOps.GetStringFromObject(value),
                                        ValueFlags.AnyInteger, interpreter.InternalCultureInfo,
                                        ref intValue, ref error) == ReturnCode.Ok)
                                {
                                    interpreter.TestRepeatCount = intValue;

                                    return ReturnCode.Ok;
                                }
                            }
                            break;
                        }
                    case TestInformationType.Verbose:
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                object enumValue = EnumOps.TryParseFlags(interpreter,
                                    typeof(TestOutputType), interpreter.TestVerbose.ToString(),
                                    StringOps.GetStringFromObject(value), interpreter.InternalCultureInfo,
                                    true, true, true, ref error);

                                if (enumValue is TestOutputType)
                                {
                                    interpreter.TestVerbose = (TestOutputType)enumValue;

                                    return ReturnCode.Ok;
                                }
                            }
                            break;
                        }
                    case TestInformationType.KnownBugs:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    IntDictionary testKnownBugs = interpreter.TestKnownBugs;

                                    if (testKnownBugs == null)
                                    {
                                        testKnownBugs = new IntDictionary();
                                        interpreter.TestKnownBugs = testKnownBugs;
                                    }

                                    int count;

                                    /* IGNORED */
                                    testKnownBugs.TryGetValue(name, out count);

                                    if (add)
                                        testKnownBugs[name] = ++count;
                                    else
                                        testKnownBugs[name] = --count;

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.Constraints:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (interpreter.TestConstraints == null)
                                        interpreter.TestConstraints = new StringList();

                                    if (add)
                                        interpreter.TestConstraints.Add(name);
                                    else
                                        interpreter.TestConstraints.Remove(name);

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.Counts:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    IntDictionary testCounts = interpreter.TestCounts;

                                    if (testCounts == null)
                                    {
                                        testCounts = new IntDictionary();
                                        interpreter.TestCounts = testCounts;
                                    }

                                    int count;

                                    /* IGNORED */
                                    testCounts.TryGetValue(name, out count);

                                    if (add)
                                        testCounts[name] = ++count;
                                    else
                                        testCounts[name] = --count;

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.SkippedNames:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (interpreter.TestSkipped == null)
                                        interpreter.TestSkipped = new StringListDictionary();

                                    if (add)
                                    {
                                        interpreter.TestSkipped.Merge(
                                            name, value as StringList);
                                    }
                                    else
                                    {
                                        interpreter.TestSkipped.Remove(
                                            name);
                                    }

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.FailedNames:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (interpreter.TestFailures == null)
                                        interpreter.TestFailures = new StringList();

                                    if (add)
                                        interpreter.TestFailures.Add(name);
                                    else
                                        interpreter.TestFailures.Remove(name);

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.SkipNames:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (interpreter.TestSkip == null)
                                        interpreter.TestSkip = new StringList();

                                    if (add)
                                        interpreter.TestSkip.Add(name);
                                    else
                                        interpreter.TestSkip.Remove(name);

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.MatchNames:
                        {
                            //
                            // NOTE: *WARNING* Empty test names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (interpreter.TestMatch == null)
                                        interpreter.TestMatch = new StringList();

                                    if (add)
                                        interpreter.TestMatch.Add(name);
                                    else
                                        interpreter.TestMatch.Remove(name);

                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = "invalid test name";
                            }
                            break;
                        }
                    case TestInformationType.Level:
                        {
                            if (add)
                                level = interpreter.EnterTestLevel();
                            else
                                level = interpreter.ExitTestLevel();

                            return ReturnCode.Ok;
                        }
                    case TestInformationType.Total:
                    case TestInformationType.Skipped:
                    case TestInformationType.Disabled:
                    case TestInformationType.Passed:
                    case TestInformationType.Failed:
                    case TestInformationType.SkippedBug:
                    case TestInformationType.DisabledBug:
                    case TestInformationType.PassedBug:
                    case TestInformationType.FailedBug:
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                if (interpreter.TestStatistics == null)
                                    interpreter.TestStatistics =
                                        new long[(int)TestInformationType.SizeOf];

                                if (add)
                                {
                                    Interlocked.Increment(
                                        ref interpreter.TestStatistics[(int)type]);
                                }
                                else
                                {
                                    Interlocked.Decrement(
                                        ref interpreter.TestStatistics[(int)type]);
                                }

                                return ReturnCode.Ok;
                            }
                        }
                    default:
                        {
                            error = "unsupported test information type";
                            break;
                        }
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Constraint Checking Methods
        private static ReturnCode CheckCount(
            Interpreter interpreter,
            string name,
            ref int count,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: *WARNING* Empty test names are allowed,
                //       please do not change this to "!String.IsNullOrEmpty".
                //
                if (name != null)
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        IntDictionary testCounts = interpreter.TestCounts;

                        if (testCounts != null)
                            /* IGNORED */
                            testCounts.TryGetValue(name, out count);
                        else
                            count = 0;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid test name";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode CheckNames(
            Interpreter interpreter,
            string name,
            ref bool matchName,
            ref bool skipName,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: *WARNING* Empty test names are allowed,
                //       please do not change this to "!String.IsNullOrEmpty".
                //
                if (name != null)
                {
                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                    {
                        if ((StringOps.MatchAnyOrAll(
                                interpreter, NameMatchMode, name,
                                interpreter.TestMatch, false, false,
                                ref matchName, ref error) == ReturnCode.Ok) &&
                            (StringOps.MatchAnyOrAll(
                                interpreter, NameMatchMode, name,
                                interpreter.TestSkip, false, false,
                                ref skipName, ref error) == ReturnCode.Ok))
                        {
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    error = "invalid test name";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckConstraintExpression(
            Interpreter interpreter,
            int testLevels, /* NOTE: Use this instead of member variable, no need for lock. */
            string name,
            string constraintExpression,
            bool noStatistics,
            StringBuilder testData,
            bool knownBug,
            ref bool skip,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: *WARNING* Empty test names are allowed,
            //       please do not change this to "!String.IsNullOrEmpty".
            //
            if (name == null)
            {
                error = "invalid test name";
                return ReturnCode.Error;
            }

            //
            // NOTE: If an invalid constraint expression was specified, just
            //       skip it and allow the test to run.
            //
            if (String.IsNullOrEmpty(constraintExpression))
                return ReturnCode.Ok;

            //
            // NOTE: Initially, there is no reason the test should be skipped.
            //
            StringList matchList = new StringList();

            //
            // NOTE: Evaluate the expression in the current context and try
            //       to convert the result to a boolean.
            //
            ReturnCode code;
            Result result = null;

            code = interpreter.EvaluateExpressionWithErrorInfo(
                constraintExpression, "{0}    (\"constraint\" expression)",
                ref result);

            if (code == ReturnCode.Ok)
            {
                bool value = false;

                code = Engine.ToBoolean(
                    result, interpreter.InternalCultureInfo, ref value,
                    ref error);

                if (code != ReturnCode.Ok)
                {
                    error = result;
                    return code;
                }

                if (!value)
                    matchList.Add("constraintExpression");
            }
            else
            {
                error = result;
                return code;
            }

            //
            // NOTE: Is there any reason this test should be skipped?
            //
            if (matchList.Count > 0)
            {
                if (testLevels == 1)
                {
                    if (!noStatistics)
                    {
                        ResultList errors = null;
                        Result localError = null;

                        if (RecordInformation(interpreter,
                                TestInformationType.Skipped, null, null,
                                true, ref localError) != ReturnCode.Ok)
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }
                        }

                        if (knownBug)
                        {
                            localError = null;

                            if (RecordInformation(interpreter,
                                    TestInformationType.SkippedBug, null, null,
                                    true, ref localError) != ReturnCode.Ok)
                            {
                                if (localError != null)
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(localError);
                                }
                            }
                        }

                        localError = null;

                        if (RecordInformation(interpreter,
                                TestInformationType.SkippedNames,
                                name, matchList, true,
                                ref localError) != ReturnCode.Ok)
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }
                        }

                        if (errors != null)
                        {
                            error = errors;
                            code = ReturnCode.Error;
                        }
                    }

                    AppendFormat(
                        interpreter, testData, TestOutputType.Skip,
                        "++++ {0} SKIPPED: {1}", name,
                        matchList.ToString());

                    AppendLine(
                        interpreter, testData, TestOutputType.Skip);
                }

                //
                // NOTE: Finally, we are NOT going to run this test.
                //
                skip = true;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckConstraints(
            Interpreter interpreter,
            int testLevels, /* NOTE: Use this instead of member variable, no need for lock. */
            string name,
            string constraints,
            bool once,
            bool noStatistics,
            StringBuilder testData,
            ref bool skip,
            ref bool fail,
            ref bool whatIf,
            ref bool knownBug,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: *WARNING* Empty test names are allowed,
                //       please do not change this to "!String.IsNullOrEmpty".
                //
                if (name != null)
                {
                    bool matchName = false;
                    bool skipName = false;

                    if (CheckNames(interpreter, name, ref matchName,
                            ref skipName, ref error) == ReturnCode.Ok)
                    {
                        //
                        // NOTE: How many times has this test been run before?
                        //
                        int count = 0;

                        if (!once ||
                            (CheckCount(interpreter, name,
                                ref count, ref error) == ReturnCode.Ok))
                        {
                            //
                            // NOTE: The list of constraints for this test.
                            //
                            StringList list = null;

                            //
                            // NOTE: If no constraints were supplied, skip list parsing.
                            //
                            if ((constraints == null) || ParserOps<string>.SplitList(
                                    interpreter, constraints, 0, Length.Invalid,
                                    true, ref list, ref error) == ReturnCode.Ok)
                            {
                                //
                                // HACK: Check for an proces the special "fail.false"
                                //       and "fail.true" pseudo-constraints.  They are
                                //       never added to the real list of constraints
                                //       to match.  They are only used to control the
                                //       value of the fail parameter passed in by the
                                //       caller.
                                //
                                ReturnCode code = ReturnCode.Ok;

                                string failFalseConstraint =
                                    FailConstraintPrefix + false.ToString();

                                string failTrueConstraint =
                                    FailConstraintPrefix + true.ToString();

                                if (list != null)
                                {
                                    foreach (string element in list)
                                    {
                                        if (SharedStringOps.SystemNoCaseEquals(
                                                element, failFalseConstraint))
                                        {
                                            fail = false;
                                            continue;
                                        }

                                        if (SharedStringOps.SystemNoCaseEquals(
                                                element, failTrueConstraint))
                                        {
                                            fail = true;
                                            continue;
                                        }

                                        if (SharedStringOps.SystemEquals(
                                                element, NotKnownBugConstraint))
                                        {
                                            knownBug = false;
                                            continue;
                                        }

                                        if (SharedStringOps.SystemEquals(
                                                element, KnownBugConstraint))
                                        {
                                            knownBug = true;
                                            continue;
                                        }
                                    }
                                }

                                StringList matchList = new StringList();

                                //
                                // NOTE: Is the test name explicitly set to be skipped?
                                //
                                if ((matchList.Count == 0) && skipName)
                                    matchList.Add("userSpecifiedSkip");

                                //
                                // NOTE: Is the test name explicitly set to be run?
                                //
                                if ((matchList.Count == 0) && !matchName)
                                    matchList.Add("userSpecifiedNonMatch");

                                //
                                // NOTE: Check if this test is only supposed to be run once and
                                //       then disallow it from running if it has already been
                                //       run once; however, only do this if the test name itself
                                //       has not already been disallowed.
                                //
                                if ((matchList.Count == 0) && once && (count > 0))
                                {
                                    //
                                    // HACK: Add bogus test constraint to indicate that this
                                    //       test was skipped because it has already been run
                                    //       (in addition to any other constraints that may
                                    //       not have been satisfied).
                                    //
                                    matchList.Add("once");
                                }

                                //
                                // NOTE: We do not need to bother checking constraints if the
                                //       name itself has already been disallowed.
                                //
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    if (matchList.Count == 0)
                                    {
                                        //
                                        // NOTE: Were there any constraints supplied?
                                        //
                                        if (list != null)
                                        {
                                            //
                                            // NOTE: Are there any constraints present in the
                                            //       interpreter?
                                            //
                                            StringList testConstraints = interpreter.TestConstraints;

                                            if (testConstraints != null)
                                            {
                                                foreach (string element in list)
                                                {
                                                    //
                                                    // HACK: Check for and skip over the special
                                                    //       the pseudo-constraints "fail.false",
                                                    //       and "fail.true".  They are processed
                                                    //       specially above and are not actually
                                                    //       added to the list(s) of real test
                                                    //       constraints.
                                                    //
                                                    if (SharedStringOps.SystemNoCaseEquals(
                                                            element, failFalseConstraint))
                                                    {
                                                        continue;
                                                    }

                                                    if (SharedStringOps.SystemNoCaseEquals(
                                                            element, failTrueConstraint))
                                                    {
                                                        continue;
                                                    }

                                                    //
                                                    // NOTE: All null and/or empty constraints are
                                                    //       ignored.
                                                    //
                                                    if (!String.IsNullOrEmpty(element))
                                                    {
                                                        //
                                                        // NOTE: If a constraint starts with a "!",
                                                        //       it must be false valued (i.e. not
                                                        //       present) for the test to run;
                                                        //       otherwise, it must be true valued
                                                        //       (i.e. present) for the test to run.
                                                        //
                                                        if (element[0] == Characters.ExclamationMark)
                                                        {
                                                            if (testConstraints.Contains(
                                                                    element.Substring(1)))
                                                            {
                                                                matchList.Add(element);
                                                            }
                                                        }
                                                        else if (!testConstraints.Contains(element))
                                                        {
                                                            matchList.Add(element);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    //
                                    // NOTE: Is there any reason this test should be skipped?
                                    //
                                    if (matchList.Count > 0)
                                    {
                                        if (testLevels == 1)
                                        {
                                            if (!noStatistics)
                                            {
                                                ResultList errors = null;
                                                Result localError = null;

                                                if (RecordInformation(interpreter,
                                                        TestInformationType.Skipped, null, null,
                                                        true, ref localError) != ReturnCode.Ok)
                                                {
                                                    if (localError != null)
                                                    {
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        errors.Add(localError);
                                                    }
                                                }

                                                if (knownBug)
                                                {
                                                    localError = null;

                                                    if (RecordInformation(interpreter,
                                                            TestInformationType.SkippedBug, null, null,
                                                            true, ref localError) != ReturnCode.Ok)
                                                    {
                                                        if (localError != null)
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            errors.Add(localError);
                                                        }
                                                    }
                                                }

                                                localError = null;

                                                if (RecordInformation(interpreter,
                                                        TestInformationType.SkippedNames,
                                                        name, matchList, true,
                                                        ref localError) != ReturnCode.Ok)
                                                {
                                                    if (localError != null)
                                                    {
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        errors.Add(localError);
                                                    }
                                                }

                                                if (errors != null)
                                                {
                                                    error = errors;
                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            AppendFormat(
                                                interpreter, testData, TestOutputType.Skip,
                                                "++++ {0} SKIPPED: {1}", name,
                                                matchList.ToString());

                                            AppendLine(
                                                interpreter, testData, TestOutputType.Skip);
                                        }

                                        //
                                        // NOTE: Finally, we are NOT going to run this test.
                                        //
                                        skip = true;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    if (ScriptOps.HasFlags(
                                            interpreter, InterpreterFlags.TestScriptWhatIf,
                                            true))
                                    {
                                        whatIf = true;
                                    }
                                    else
                                    {
                                        whatIf = false;
                                    }
                                }

                                return code;
                            }
                        }
                    }
                }
                else
                {
                    error = "invalid test name";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Test Suite Support Methods
        public static bool Track(
            Interpreter interpreter,
            string value,
            TestOutputType outputType
            )
        {
            if (!ShouldTrackTestData(interpreter, outputType))
                return false;

            return TryWriteViaDebug(interpreter, value, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryWriteViaDebug(
            Interpreter interpreter,
            string value,
            bool noComplain
            )
        {
            ReturnCode code = ReturnCode.Ok;
            Result result = null;

            try
            {
                int count = 0;

#if NATIVE && WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem() &&
                    NativeOps.SafeNativeMethods.IsDebuggerPresent())
                {
                    DebugOps.Output(value);
                    count++;
                }
#endif

                if (DebugOps.IsAttached())
                {
                    DebugOps.DebugWrite(value, typeof(TestOps).Name);
                    count++;
                }

                if (count == 0)
                {
                    result = "no debugger is present";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, result);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryWriteViaHost(
            Interpreter interpreter,
            string value,
            bool noComplain
            )
        {
            ReturnCode code;
            Result result;

            try
            {
                if (interpreter != null)
                {
                    IInteractiveHost interactiveHost =
                        interpreter.GetInteractiveHost();

                    if (interactiveHost != null)
                    {
                        if (interactiveHost.Write(value))
                        {
                            result = null;
                            code = ReturnCode.Ok;
                        }
                        else
                        {
                            result = "failed to write to host";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "interpreter host not available";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, result);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetPutsCommand(
            Interpreter interpreter,
            bool useFallback
            )
        {
            if (interpreter != null)
            {
                if (interpreter.DoesIExecuteExistViaResolvers(
                        putsNormalCommand) == ReturnCode.Ok)
                {
                    return putsNormalCommand;
                }

                if (useFallback)
                {
                    if (interpreter.DoesIExecuteExistViaResolvers(
                            putsFallbackCommand) == ReturnCode.Ok)
                    {
                        return putsFallbackCommand;
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ChannelOrNull(
            Interpreter interpreter,
            string name
            )
        {
            if ((interpreter != null) &&
                (interpreter.DoesChannelExist(name) == ReturnCode.Ok))
            {
                return name;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetPutsChannel(
            Interpreter interpreter,
            bool useFallback
            )
        {
            if (interpreter != null)
            {
                Result value = null;

                if (interpreter.GetVariableValue(
                        VariableFlags.None, putsNormalChannelVarName,
                        ref value) == ReturnCode.Ok)
                {
                    return ChannelOrNull(interpreter, value);
                }

                if (useFallback)
                    return ChannelOrNull(interpreter, putsFallbackChannel);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CanMaybeTryWriteViaPuts(
            Interpreter interpreter
            )
        {
            if (GetPutsCommand(interpreter, true) == null)
                return false;

            if (GetPutsChannel(interpreter, true) == null)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryWriteViaPuts( /* NOTE: Really via "::tputs" / "::tqputs"... */
            Interpreter interpreter,
            string value,
            bool ignoreQuiet,
            bool noComplain
            )
        {
            ReturnCode code;
            Result result = null;

            try
            {
                if (interpreter != null)
                {
                    if (ignoreQuiet || !interpreter.ShouldBeQuiet())
                    {
                        string commandName = GetPutsCommand(interpreter, true);

                        if (commandName == null)
                        {
                            result = "invalid test output command";
                            code = ReturnCode.Error;
                            goto done;
                        }

                        string channelName = GetPutsChannel(interpreter, true);

                        if (channelName == null)
                        {
                            result = "invalid test output channel";
                            code = ReturnCode.Error;
                            goto done;
                        }

                        code = interpreter.EvaluateScript(
                            StringList.MakeList(commandName, channelName, value),
                            ref result);
                    }
                    else
                    {
                        //
                        // NOTE: The interpreter is in "quiet" mode, make sure
                        //       and honor it.
                        //
                        code = ReturnCode.Ok;
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

        done:

            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, result);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetLogCommand(
            Interpreter interpreter,
            bool useFallback
            )
        {
            if (interpreter != null)
            {
                if (interpreter.DoesIExecuteExistViaResolvers(
                        logNormalCommand) == ReturnCode.Ok)
                {
                    return logNormalCommand;
                }

                if (useFallback)
                {
                    if (interpreter.DoesIExecuteExistViaResolvers(
                            logFallbackCommand) == ReturnCode.Ok)
                    {
                        return logFallbackCommand;
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryWriteViaLog( /* NOTE: Really via "::tlog" / "::tqlog"... */
            Interpreter interpreter,
            string value,
            bool noComplain
            )
        {
            ReturnCode code;
            Result result = null;

            try
            {
                if (interpreter != null)
                {
                    string commandName = GetLogCommand(interpreter, true);

                    if (commandName == null)
                    {
                        result = "invalid test log command";
                        code = ReturnCode.Error;
                        goto done;
                    }

                    code = interpreter.EvaluateScript(
                        StringList.MakeList(commandName, value), ref result);
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

        done:

            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, result);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsAutomaticWriteTestData(
            Interpreter interpreter, /* NOT USED */
            TestOutputType outputType
            )
        {
            //
            // HACK: Always write out the start of the test, so that
            //       we know it's running.
            //
            return outputType == TestOutputType.Start;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsAutomaticReturnTestData(
            Interpreter interpreter, /* NOT USED */
            TestOutputType outputType, /* NOT USED */
            bool wrote
            )
        {
            //
            // HACK: Always return the test data that we did not write
            //       out previously during the test.
            //
            return !wrote;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the test package only.
        //          Do not modify or remove this method.
        //
        private static bool ShouldWriteTestData(
            Interpreter interpreter,
            ReturnCode code
            )
        {
            return ShouldWriteTestData(
                interpreter, (code == ReturnCode.Ok) ?
                TestOutputType.Pass : TestOutputType.Fail);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldWriteTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {
            if ((interpreter == null) || !ScriptOps.HasFlags(
                    interpreter, InterpreterFlags.WriteTestData, true))
            {
                return false;
            }

            TestOutputType testVerbose = interpreter.TestVerbose;

            if (FlagOps.HasFlags(
                    testVerbose, TestOutputType.AutomaticWrite, true))
            {
                //
                // NOTE: In 'automatic' mode, only disallow writing test
                //       data here if that same data will later be returned.
                //
                if (!ScriptOps.HasFlags(interpreter,
                        InterpreterFlags.NoReturnTestData, true) &&
                    !IsAutomaticWriteTestData(
                        interpreter, outputType))
                {
                    return false;
                }
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldReturnTestData(
            Interpreter interpreter,
            TestOutputType outputType,
            bool wrote
            )
        {
            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterFlags.NoReturnTestData, true))
            {
                return false;
            }

            TestOutputType testVerbose = interpreter.TestVerbose;

            if (FlagOps.HasFlags(
                    testVerbose, TestOutputType.AutomaticReturn, true))
            {
                //
                // NOTE: In 'automatic' mode, only disallow returning test
                //       data here if that same data was previously written.
                //
                if (ScriptOps.HasFlags(interpreter,
                        InterpreterFlags.WriteTestData, true) &&
                    !IsAutomaticReturnTestData(
                        interpreter, outputType, wrote))
                {
                    return false;
                }
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldLogTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {
            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterFlags.NoLogTestData, true))
            {
                return false;
            }

            TestOutputType testVerbose = interpreter.TestVerbose;

            if (FlagOps.HasFlags(
                    testVerbose, TestOutputType.AutomaticLog, true))
            {
                //
                // NOTE: When 'automatic' logging of test data is enabled,
                //       all test data will be logged, ignoring the other
                //       flags, which will then only be used to impact the
                //       test data written to the host.
                //
                return true;
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldTrackTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {

            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterFlags.NoTrackTestData, true))
            {
                return false;
            }

            TestOutputType testVerbose = interpreter.TestVerbose;

            if (!FlagOps.HasFlags(
                    testVerbose, TestOutputType.Track, true))
            {
                return false;
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public static bool ShouldShowTestDetail(
            Interpreter interpreter, /* NOT USED */
            IsolationDetail passFlags,
            IsolationDetail failFlags,
            IsolationDetail hasFlags,
            bool pass
            )
        {
            //
            // NOTE: Figure out which detail level flags to use based
            //       on the pass/fail flag.
            //
            IsolationDetail flags = pass ? passFlags : failFlags;

            //
            // NOTE: Check if this specific detail level is enabled.
            //
            if (FlagOps.HasFlags(flags, hasFlags, true))
                return true;

            //
            // HACK: Higher detail levels are currently a superset.
            //
            return (flags >= hasFlags);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCodeDictionary GetReturnCodeMessages()
        {
            ReturnCodeDictionary result = new ReturnCodeDictionary();

            //
            // TODO: Localize these messages as well?
            //
            result.Add(ReturnCode.Invalid,
                "Test generated exception");

            result.Add(ReturnCode.Ok,
                "Test completed normally");

            result.Add(ReturnCode.Error,
                "Test generated error");

            result.Add(ReturnCode.Return,
                "Test generated return exception");

            result.Add(ReturnCode.Break,
                "Test generated break exception");

            result.Add(ReturnCode.Continue,
                "Test generated continue exception");

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static void GetTestScriptLocation(
            Interpreter interpreter,
            Argument argument,
            bool strict,
            out IScriptLocation location
            )
        {
            //
            // NOTE: For now, always fallback to the argument as a good
            //       default location.
            //
            location = argument;

            ReturnCode code;
            Result error = null;

            code = GetTestScriptLocation(
                interpreter, argument, strict, ref location, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTestScriptLocation(
            Interpreter interpreter,
            Argument argument,
            bool strict,
            ref IScriptLocation location,
            ref Result error
            )
        {
            ReturnCode code;
            string fileName = null;
            int currentLine = Parser.UnknownLine;

            code = ScriptOps.GetLocation(
                interpreter, true, false, ref fileName,
                ref currentLine, ref error);

            if (code == ReturnCode.Ok)
            {
                if ((fileName == null) &&
                    (currentLine == Parser.UnknownLine))
                {
                    location = argument;
                }
                else
                {
                    location = ScriptLocation.Create(
                        interpreter, fileName, currentLine,
                        false);
                }
            }
            else if (!strict)
            {
                location = argument;
            }

            return strict ? code : ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetPath(
            Interpreter interpreter,
            TestPathType pathType
            )
        {
            string result = null;

            if (interpreter != null)
                result = interpreter.TestPath;

            if (String.IsNullOrEmpty(result))
            {
                result = GlobalState.GetBasePath();

                if (!String.IsNullOrEmpty(result))
                {
                    switch (pathType)
                    {
                        case TestPathType.None:
                            {
                                //
                                // NOTE: Do nothing.
                                //
                                break;
                            }
                        case TestPathType.Library:
                            {
                                result = PathOps.CombinePath(
                                    null, result, _Path.Library,
                                    _Path.Tests);

                                break;
                            }
                        case TestPathType.Plugins:
                            {
                                result = PathOps.CombinePath(
                                    null, result, _Path.Plugins);

                                break;
                            }
                        case TestPathType.Tests:
                            {
                                result = PathOps.CombinePath(
                                    null, result, _Path.Tests);

                                break;
                            }
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        private static ReturnCode GetIsolatedExecutableFirstArguments(
            Interpreter interpreter,       /* in */
            string fileName,               /* in */
            ref StringList firstArguments, /* in, out */
            ref bool useEntryAssembly,     /* out */
            ref Result error               /* out */
            )
        {
            string location = GlobalState.GetEntryAssemblyLocation();

            if (!PathOps.IsSameFile(interpreter, location, fileName))
            {
                if (location == null)
                {
                    error = "unable to get entry assembly location";
                    return ReturnCode.Error;
                }

                //
                // HACK: When running on the .NET Core runtime insert
                //       the necessary command line argument (e.g.
                //       "exec") in the arguments list just prior to
                //       the name of the entry assembly.
                //
                if (firstArguments == null)
                    firstArguments = new StringList();

                firstArguments.Add(DotNetCoreExecutableArgument);

                //
                // NOTE: Also, make sure the fully qualified path to
                //       the managed assembly containing the entry
                //       point gets added to the final argument list.
                //
                useEntryAssembly = true;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetIsolatedExecutableName(
            Interpreter interpreter,       /* in */
            bool full,                     /* in */
            ref string fileName,           /* out */
            ref StringList firstArguments, /* in, out */
            ref bool useEntryAssembly,     /* out */
            ref Result error               /* out */
            )
        {
            string result;

            if (CommonOps.Runtime.IsMono())
            {
#if NATIVE
                result = PathOps.GetNativeExecutableName(); /* Windows */

                if (result == null)
#endif
                    result = MonoExecutableName; /* Unix */

                useEntryAssembly = true;
            }
            else
            {
                result = PathOps.GetProcessMainModuleFileName(full);

                if (CommonOps.Runtime.IsDotNetCore() &&
                    GetIsolatedExecutableFirstArguments(
                        interpreter, result, ref firstArguments,
                        ref useEntryAssembly, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            if (!String.IsNullOrEmpty(result))
            {
                fileName = result;
                return ReturnCode.Ok;
            }
            else
            {
                error = "unable to get process executable file name";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetIsolatedFileName(
            Interpreter interpreter, /* in */
            TestPathType pathType,   /* in */
            ref string fileName,     /* out */
            ref Result error         /* out */
            )
        {
            ReturnCode code = ReturnCode.Error;
            string[] fileNames = { null, null };

            try
            {
                //
                // NOTE: First, just obtain a temporary file name from the
                //       operating system.
                //
                fileNames[0] = PathOps.GetTempFileName(); /* throw */

                if (!String.IsNullOrEmpty(fileNames[0]))
                {
                    string basePath = GetPath(interpreter, pathType);

                    if (!String.IsNullOrEmpty(basePath))
                    {
                        //
                        // NOTE: Next, change the directory to the one that
                        //       contains the test files, while retaining
                        //       the temporary file name itself, including
                        //       its extension.
                        //
                        fileNames[1] = PathOps.CombinePath(
                            null, basePath, Path.GetFileName(fileNames[0]));

                        //
                        // NOTE: Finally, move the temporary file, atomically,
                        //       to the new name.
                        //
                        File.Move(fileNames[0], fileNames[1]); /* throw */

                        //
                        // NOTE: If we got this far, everything should have
                        //       succeeded.  Make sure the caller has the
                        //       script file name containing their specified
                        //       content.
                        //
                        fileName = fileNames[1];
                        code = ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid test path";
                    }
                }
                else
                {
                    error = "invalid temporary file name";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // NOTE: If we created a temporary file, always delete it
                //       prior to returning from this method.
                //
                if (code != ReturnCode.Ok)
                {
                    if (fileNames[1] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[1]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(TestOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[1] = null;
                    }

                    if (fileNames[0] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[0]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(TestOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[0] = null;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetIsolatedExecutableArguments(
            Interpreter interpreter,   /* in */
            string fileName,           /* in */
            string logFile,            /* in */
            StringList firstArguments, /* in */
            StringList otherArguments, /* in */
            StringList lastArguments,  /* in */
            bool useEntryAssembly,     /* in */
            bool safe,                 /* in */
            bool security,             /* in */
            ref string arguments,      /* out */
            ref Result error           /* out */
            )
        {
            StringList list = new StringList();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: First, if there are arguments that must preceed the
            //       rest of the command line, add them now.
            //
            if (firstArguments != null)
                list.AddRange(firstArguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When running on Mono -OR- .NET Core, we need to insert
            //       the file name for the assembly containing the managed
            //       entry point.
            //
            if (useEntryAssembly)
            {
                string location = GlobalState.GetEntryAssemblyLocation();

                if (location == null)
                {
                    error = "unable to get entry assembly location";
                    return ReturnCode.Error;
                }

                //
                // NOTE: First argument is actually to the Mono -OR- .NET
                //       Core executable itself, telling it which managed
                //       assembly to load.
                //
                list.Add(location);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: If necessary, add the command line option to force
            //       the interpreter in the child process to be "safe".
            //
            if (safe)
                list.Add(Characters.MinusSign + CommandLineOption.Safe);

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: If necessary, add the command line option to enable
            //       security for the interpreter in the child process.
            //
            if (security)
            {
                list.Add(Characters.MinusSign + CommandLineOption.Security);
                list.Add(security.ToString());
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Next, if there are (other) arguments that must occur
            //       before the test file specified on the command line,
            //       add them now.
            //
            if (otherArguments != null)
                list.AddRange(otherArguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: If the caller specified something that looks like a
            //       valid file name, add that.
            //
            if (!String.IsNullOrEmpty(fileName))
            {
                list.Add(Characters.MinusSign + CommandLineOption.File);
                list.Add(fileName);
            }

            //
            // NOTE: If the caller specified something that looks like a
            //       valid log file, add that.
            //
            if (!String.IsNullOrEmpty(logFile))
            {
                list.Add(LogFileOption);
                list.Add(logFile);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Finally, if there are last arguments that must occur
            //       after the rest of the command line, add them now.
            //
            if (lastArguments != null)
                list.AddRange(lastArguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Build the final, properly quoted command line for the
            //       caller and return it.
            //
            arguments = RuntimeOps.BuildCommandLine(list, false);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Syntax is "test name description ?options?".
        //
        public static ReturnCode GetIsolatedCommandArguments(
            Interpreter interpreter,       /* in */
            ArgumentList oldArguments,     /* in */
            ref ArgumentList newArguments, /* in, out */
            ref Result error               /* out */
            )
        {
            //
            // NOTE: Garbage in, garbage out.
            //
            if (oldArguments == null)
            {
                newArguments = null;
                return ReturnCode.Ok;
            }

            //
            // NOTE: If the new argument list has not yet been created, do it
            //       now.
            //
            if (newArguments == null)
                newArguments = new ArgumentList();

            //
            // NOTE: How many old arguments are there?
            //
            int count = oldArguments.Count;

            //
            // NOTE: If there are no old arguments, we are done.
            //
            if (count == 0)
                return ReturnCode.Ok;

            //
            // NOTE: If the old argument list has the minimum number of
            //       required arguments (or less), just copy it verbatim
            //       and return.
            //
            if (count <= MinimumArgumentCount)
            {
                newArguments.AddRange(oldArguments);
                return ReturnCode.Ok;
            }

            //
            // NOTE: If the old argument list has an odd number of option
            //       arguments (i.e. after the required ones), that is an
            //       error.
            //
            if (((count - MinimumArgumentCount) % 2) != 0)
            {
                error = "test option list unbalanced";
                return ReturnCode.Error;
            }

            //
            // NOTE: Copy all the required [test2] arguments.
            //
            for (int index = 0; index < MinimumArgumentCount; index++)
                newArguments.Add(oldArguments[index]);

            //
            // NOTE: Review all the [test2] option arguments.  For ones that
            //       are not related to isolation, copy them.
            //
            for (int index = MinimumArgumentCount; index < count; index += 2)
            {
                Argument optionName = oldArguments[index];

                //
                // NOTE: Does this option only apply to test isolation?  If
                //       so, skip adding it.
                //
                if ((optionName != null) && optionName.StartsWith(IsolationPrefix,
                        SharedStringOps.SystemComparisonType))
                {
                    continue;
                }

                Argument optionValue = oldArguments[index + 1];

                newArguments.Add(optionName);
                newArguments.Add(optionValue);
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldWarnSuiteFileName(
            Interpreter interpreter
            )
        {
            if ((interpreter != null) && (interpreter.DoesVariableExist(
                    VariableFlags.GlobalOnly, FormatOps.VariableName(
                    Vars.Core.No, fileNameWarningVarIndex)) == ReturnCode.Ok))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldWarnSuiteDirectory(
            Interpreter interpreter
            )
        {
            if ((interpreter != null) && (interpreter.DoesVariableExist(
                    VariableFlags.GlobalOnly, FormatOps.VariableName(
                    Vars.Core.No, directoryWarningVarIndex)) == ReturnCode.Ok))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDirectoryNameOnly(
            string fileName
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            return Path.GetFileName(Path.GetDirectoryName(fileName));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ShellMain(
            Interpreter interpreter,
            string pattern,
            string extraPath,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            TestPathType pathType,
            bool all,
            ref Result result,
            ref int errorLine
            )
        {
            ReturnCode code = ReturnCode.Ok;

            try
            {
                //
                // NOTE: Get the location of the test suite files.
                //
                string basePath = GetPath(interpreter, pathType);

                //
                // NOTE: If there is an extra path fragment, use it now.
                //
                if (!String.IsNullOrEmpty(extraPath))
                    basePath = PathOps.CombinePath(null, basePath, extraPath);

                //
                // NOTE: Save the original pattern provided by the caller,
                //       for later reporting.
                //
                string savedPattern = pattern;

                //
                // NOTE: The default is to run the entire test suite.
                //
                bool warning = false;

                if (pattern == null)
                    pattern = suiteFileName;
                else
                    warning = true;

                //
                // NOTE: The list of file names to [potentially] evaluate.
                //
                StringList fileNames;

                //
                // NOTE: If the pattern contains directory information OR a
                //       complex glob pattern (i.e. [string match]), we cannot
                //       use the GetFiles method to actually match against the
                //       file names.
                //
                if (StringOps.HasStringMatchWildcard(pattern) ||
                    PathOps.HasDirectory(pattern))
                {
                    //
                    // NOTE: Get all the files from the file system underneath
                    //       the base directory.  We will perform file name
                    //       matching in the file evaluation loop below.
                    //
                    fileNames = new StringList(Directory.GetFiles(basePath,
                        Characters.Asterisk.ToString() + FileExtension.Script,
                        FileOps.GetSearchOption(true)));
                }
                else
                {
                    //
                    // NOTE: Only get the files from the file system that match
                    //       the specified pattern.
                    //
                    fileNames = new StringList(Directory.GetFiles(basePath,
                        pattern, FileOps.GetSearchOption(true)));

                    //
                    // NOTE: Disable secondary file name matching in the file
                    //       evaluation loop below.
                    //
                    if ((fileNames != null) && (fileNames.Count > 0))
                        pattern = null;
                }

                //
                // NOTE: If we found it, try to evaluate it; otherwise,
                //       show an error.
                //
                if ((fileNames != null) && (fileNames.Count > 0))
                {
                    //
                    // NOTE: Make sure the file names are always evaluated
                    //       in a well-defined order.
                    //
                    IntDictionary duplicates = null;

                    fileNames.Sort(new _Comparers.StringDictionaryComparer(
                        interpreter, true, null, false, false,
                        (interpreter != null) ? interpreter.InternalCultureInfo : null,
                        ref duplicates));

                    //
                    // NOTE: Keep track of whether or not we actually manage
                    //       to evaluate any test suite file(s).
                    //
                    int count = 0;

                    //
                    // NOTE: If necessary, issue a warning about the lack of
                    //       resource leak checking when running individual
                    //       test files.
                    //
                    if (warning && ShouldWarnSuiteFileName(interpreter))
                    {
                        /* IGNORED */
                        TryWriteViaPuts(interpreter, String.Format(
                            "==== WARNING: tests are not being run via suite " +
                            "script file {0}, resource leaks will probably " +
                            "not be reported.\n",
                            FormatOps.WrapOrNull(suiteFileName)),
                            IgnoreQuietForWarning, false);
                    }

                    //
                    // NOTE: This loop will evaluate zero or more files from
                    //       the list of file names that were found above.
                    //
                    foreach (string fileName in fileNames)
                    {
                        //
                        // HACK: Skip over any obviously invalid names.
                        //
                        if (String.IsNullOrEmpty(fileName))
                            continue;

                        //
                        // NOTE: Skip over any non-script file names.
                        //
                        if (!fileName.EndsWith(
                                FileExtension.Script, PathOps.ComparisonType))
                        {
                            continue;
                        }

                        //
                        // NOTE: Grab the name of the file name without any
                        //       directory information.
                        //
                        string fileNameOnly = Path.GetFileName(fileName);

                        //
                        // NOTE: Make sure the file name is not in the list of
                        //       file names that we are purposely avoiding.
                        //
                        bool match = false;

                        if ((StringOps.MatchAnyOrAll(
                                interpreter, skipFileNameMatchMode,
                                fileNameOnly, skipFileNames, false,
                                PathOps.NoCase, ref match,
                                ref result) == ReturnCode.Ok) && !match)
                        {
                            //
                            // NOTE: Do we need to perform any secondary pattern
                            //       matching (i.e. in case we did not really
                            //       perform any when we originally queried the
                            //       file system above)?
                            //
                            if ((pattern == null) || StringOps.Match(
                                    interpreter, StringOps.DefaultMatchMode,
                                    fileName, pattern, true))
                            {
                                //
                                // NOTE: Grab the name of the parent directory
                                //       that contains the test file.  Issue a
                                //       warning if the directory name does not
                                //       case-insensitively match "Tests", which
                                //       is the directory where all test files
                                //       should be located.
                                //
                                string directoryOnly = GetDirectoryNameOnly(
                                    fileName);

                                if (!SharedStringOps.Equals(
                                        directoryOnly, Path.GetFileName(
                                            ScriptPaths.TestPackage),
                                        PathOps.ComparisonType) &&
                                    !SharedStringOps.Equals(
                                        directoryOnly, _Path.Tests,
                                        PathOps.ComparisonType) &&
                                    ShouldWarnSuiteDirectory(interpreter))
                                {
                                    /* IGNORED */
                                    TryWriteViaPuts(interpreter, String.Format(
                                        "==== WARNING: evaluating test file {0} " +
                                        "located in non-test directory {1}.\n",
                                        FormatOps.WrapOrNull(fileNameOnly),
                                        FormatOps.WrapOrNull(directoryOnly)),
                                        IgnoreQuietForWarning, false);
                                }

                                //
                                // NOTE: Set the current test file name so that it
                                //       can be displayed by the test prologue.
                                //
                                if (interpreter != null)
                                {
                                    code = interpreter.SetLibraryVariableValue(
                                        VariableFlags.None, Vars.Core.TestFile,
                                        fileName, ref result);

                                    if (code != ReturnCode.Ok)
                                        break;
                                }

                                //
                                // NOTE: Evaluate the file using the specified flags
                                //       and capture all the resulting information.
                                //
                                code = Engine.EvaluateFile(
                                    interpreter, fileName, engineFlags,
                                    substitutionFlags, eventFlags,
                                    expressionFlags, ref result, ref errorLine);

                                //
                                // NOTE: If an error was raised, bail out now.
                                //
                                if (code != ReturnCode.Ok)
                                    break;

                                //
                                // NOTE: Unset the current test file name, it is no
                                //       longer needed.
                                //
                                if (interpreter != null)
                                {
                                    code = interpreter.UnsetLibraryVariable(
                                        VariableFlags.NoComplain, Vars.Core.TestFile,
                                        ref result);

                                    if (code != ReturnCode.Ok)
                                        break;
                                }

                                //
                                // NOTE: We evaluated a[nother] file successfully.
                                //
                                count++;

                                //
                                // NOTE: If we were only supposed to evaluate one file,
                                //       bail out now.
                                //
                                if (!all)
                                    break;
                            }
                        }
                    }

                    //
                    // NOTE: If we did not evaluate any test files, return
                    //       failure to our caller.
                    //
                    if ((code == ReturnCode.Ok) && (count == 0))
                    {
                        result = String.Format(
                            "test suite file(s) matching \"{0}\" (match-glob) " +
                            "not found under path \"{1}\"",
                            !String.IsNullOrEmpty(savedPattern) ?
                                savedPattern : pattern, basePath);

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = String.Format(
                        "test suite file(s) matching \"{0}\" (file-glob) " +
                        "not found under path \"{1}\"",
                        !String.IsNullOrEmpty(savedPattern) ?
                            savedPattern : pattern, basePath);

                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Thread Start Callbacks
#if SHELL && INTERACTIVE_COMMANDS
        public static void HostCancelThreadStart(
            object obj
            )
        {
            try
            {
                IAnyPair<int, bool> anyPair = obj as IAnyPair<int, bool>;

                if (anyPair != null)
                {
                    //
                    // NOTE: Delay for a short while so that we can see the
                    //       true effect of the standard input channel being
                    //       closed from underneath a synchronous read on it.
                    //
                    /* IGNORED */
                    HostOps.ThreadSleep(anyPair.X); /* throw */

                    //
                    // NOTE: Grab the active interpreter.
                    //
                    Interpreter interpreter = Interpreter.GetAny();

                    if (interpreter != null)
                    {
                        //
                        // NOTE: Grab a copy of the reference to the interpreter
                        //       host.
                        //
                        IDebugHost debugHost = interpreter.Host;

                        //
                        // NOTE: Make sure the interpreter host is currently valid.
                        //
                        if (debugHost != null)
                        {
                            ReturnCode code;
                            Result result = null;

                            try
                            {
                                //
                                // NOTE: Mark the interpreter as exited and forcibly
                                //       close the standard input channel.
                                //
                                code = debugHost.Cancel(anyPair.Y, ref result);

                                //
                                // NOTE: Did we succeed?
                                //
                                if (code == ReturnCode.Ok)
                                    result = "host cancel complete";
                            }
                            catch (Exception e)
                            {
                                result = e;
                                code = ReturnCode.Error;
                            }

                            //
                            // NOTE: Always show the result, whether we succeeded or not.
                            //
                            debugHost.WriteResultLine(code, result);
                        }
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void HostExitThreadStart(
            object obj
            )
        {
            try
            {
                IAnyPair<int, bool> anyPair = obj as IAnyPair<int, bool>;

                if (anyPair != null)
                {
                    //
                    // NOTE: Delay for a short while so that we can see the
                    //       true effect of the standard input channel being
                    //       closed from underneath a synchronous read on it.
                    //
                    /* IGNORED */
                    HostOps.ThreadSleep(anyPair.X); /* throw */

                    //
                    // NOTE: Grab the active interpreter.
                    //
                    Interpreter interpreter = Interpreter.GetAny();

                    if (interpreter != null)
                    {
                        //
                        // NOTE: Grab a copy of the reference to the interpreter
                        //       host.
                        //
                        IDebugHost debugHost = interpreter.Host;

                        //
                        // NOTE: Make sure the interpreter host is currently valid.
                        //
                        if (debugHost != null)
                        {
                            ReturnCode code;
                            Result result = null;

                            try
                            {
                                //
                                // NOTE: Mark the interpreter as exited and forcibly
                                //       close the standard input channel.
                                //
                                code = debugHost.Exit(anyPair.Y, ref result);

                                //
                                // NOTE: Did we succeed?
                                //
                                if (code == ReturnCode.Ok)
                                    result = "host exit complete";
                            }
                            catch (Exception e)
                            {
                                result = e;
                                code = ReturnCode.Error;
                            }

                            //
                            // NOTE: Always show the result, whether we succeeded
                            //       or not.
                            //
                            if (code == ReturnCode.Ok)
                            {
                                //
                                // BUGFIX: Since we actually succeeded, the host is
                                //         now unusable.
                                //
                                TraceOps.DebugTrace(String.Format(
                                    "HostExitThreadStart: code = {0}, result = {1}",
                                    code, result), typeof(TestOps).Name,
                                    TracePriority.ThreadDebug);
                            }
                            else
                            {
                                debugHost.WriteResultLine(code, result);
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(TestOps).Name,
                    TracePriority.ThreadError);
            }
        }
#endif
        #endregion
    }
}
