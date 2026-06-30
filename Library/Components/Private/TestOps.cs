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
using System.Globalization;
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
    /// <summary>
    /// This class provides the private helper methods and shared state that
    /// implement Eagle's test harness, including test result matching, test
    /// data output (to the host, log, and debugger), test statistics and
    /// constraint tracking, the test suite runner, and the support for
    /// running individual tests in an isolated child process.  It is used by
    /// the test-related commands and by the interactive "#test" command.
    /// </summary>
    [ObjectId("c2335b96-f944-44c5-97dc-abdb6dd08525")]
    internal static class TestOps
    {
        #region Private Constants
        /// <summary>
        /// The name of the test constraint indicating that a test exercises a
        /// known bug.
        /// </summary>
        private static readonly string KnownBugConstraint = "knownBug";

        /// <summary>
        /// The name of the test constraint indicating that a test does not
        /// exercise a known bug.
        /// </summary>
        private static readonly string NotKnownBugConstraint = "!knownBug";

        /// <summary>
        /// The prefix used by the special "fail.false" and "fail.true"
        /// pseudo-constraints that control the value of the failure flag.
        /// </summary>
        private static readonly string FailConstraintPrefix = "fail.";

        /// <summary>
        /// The string matching mode used when comparing test names against the
        /// match and skip name patterns.
        /// </summary>
        private static readonly MatchMode NameMatchMode = StringOps.DefaultMatchMode;

        /// <summary>
        /// The regular expression options used when matching test results
        /// using regular expressions.
        /// </summary>
        internal static readonly RegexOptions RegExOptions = StringOps.DefaultRegExTestOptions;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && !ENTERPRISE_LOCKDOWN
        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// The name of the command used to assign a variable when building the
        /// command line for an isolated test process.
        /// </summary>
        private static string SetCommandName = "::set";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// The placeholder token used to represent the current test within
        /// isolated test command lines.
        /// </summary>
        internal static string TestToken = "%test%";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        /// <summary>
        /// The command line option prefix used to identify test options that
        /// apply only to test isolation.
        /// </summary>
        private static string IsolationPrefix = "-isolation";

        /// <summary>
        /// The minimum number of required arguments for an isolated test
        /// command (i.e. the test name, description, and body).
        /// </summary>
        private static int MinimumArgumentCount = 3;

        /// <summary>
        /// The file name of the Mono runtime executable used to launch an
        /// isolated test process under Mono.
        /// </summary>
        private static string MonoExecutableName = "mono";

        /// <summary>
        /// The command line option used to specify the log file name for the
        /// "test.eagle" package within an isolated test process.
        /// </summary>
        private static string LogFileOption = "-logFile"; /* NOTE: For "test.eagle" package. */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        /// <summary>
        /// The .NET Core executable argument used to execute a managed
        /// assembly when launching an isolated test process.
        /// </summary>
        private static string DotNetCoreExecutableArgument = "exec"; // TODO: Not official?

        /// <summary>
        /// The .NET Core command line option used to enable roll-forward
        /// behavior when launching an isolated test process.
        /// </summary>
        private static string DotNetCoreRollForwardMajor = "--roll-forward"; // TODO: Not official?

        /// <summary>
        /// The .NET Core roll-forward policy value used when launching an
        /// isolated test process.
        /// </summary>
        private static string DotNetCoreMajor = "Major"; // TODO: Not official?
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These file names are skipped by the interactive "#test" command
        //       prior to any other pattern matching.
        //
        /// <summary>
        /// The string matching mode used when comparing test file names
        /// against the list of file names to skip.
        /// </summary>
        private static readonly MatchMode skipFileNameMatchMode = MatchMode.Exact;

        /// <summary>
        /// The file names that are always skipped by the interactive "#test"
        /// command prior to any other pattern matching.
        /// </summary>
        private static readonly StringList skipFileNames = new StringList(new string[] {
            "epilogue.eagle", "prologue.eagle"
        });

        /// <summary>
        /// The variable name index used to detect whether the warning about
        /// running individual test files (instead of the full suite) has been
        /// suppressed.
        /// </summary>
        private const string fileNameWarningVarIndex = "warningForAllEagle";

        /// <summary>
        /// The variable name index used to detect whether the warning about
        /// running test files from a non-test directory has been suppressed.
        /// </summary>
        private const string directoryWarningVarIndex = "warningForTestsPath";

        /// <summary>
        /// The default file name of the master test suite script.
        /// </summary>
        private const string suiteFileName = "all.eagle";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// When non-zero, test suite warnings are emitted even when the
        /// interpreter is in "quiet" mode.
        /// </summary>
        private static bool IgnoreQuietForWarning = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// The line terminator string appended when emitting test data.
        /// </summary>
        private static string NewLine = Environment.NewLine; /* COMPAT: StringBuilder */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        /// <summary>
        /// The name of the normal command used to write test data to the log
        /// file.
        /// </summary>
        private static string logNormalCommand = "::tlog";

        /// <summary>
        /// The name of the fallback command used to write test data to the log
        /// file when the normal command is not available.
        /// </summary>
        private static string logFallbackCommand = "::tqlog";

        /// <summary>
        /// The name of the normal command used to write test data to the test
        /// output channel.
        /// </summary>
        internal static string putsNormalCommand = "::tputs";

        /// <summary>
        /// The name of the fallback command used to write test data to the
        /// test output channel when the normal command is not available.
        /// </summary>
        internal static string putsFallbackCommand = "::tqputs";

        /// <summary>
        /// The name of the variable that holds the name of the channel used
        /// for normal test output.
        /// </summary>
        private static string putsNormalChannelVarName = "::test_channel";

        /// <summary>
        /// The name of the fallback channel used for test output when the
        /// normal test output channel is not available.
        /// </summary>
        private static string putsFallbackChannel = Channel.StdOut;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* These are purposely not marked as read-only.
        //
        /// <summary>
        /// The delay, in milliseconds, used when queuing test-related host
        /// work items.
        /// </summary>
        internal static int hostWorkItemDelay = 10000; /* in milliseconds */

        /// <summary>
        /// When non-zero, test-related host work items are forced to run.
        /// </summary>
        internal static bool hostWorkItemForce = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        /// <summary>
        /// The default number of times an individual test is repeated.
        /// </summary>
        internal static int DefaultRepeatCount = 1;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the _Hosts.Default.BuildTestInfoList method.
        //
        /// <summary>
        /// This method adds the test suite tuning settings to the specified
        /// list of name/value pairs, for diagnostic display purposes.
        /// </summary>
        /// <param name="list">
        /// The list of name/value pairs to add the settings to.  If this
        /// parameter is null, nothing is done.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control how much detail is included in the output.
        /// </param>
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
        /// <summary>
        /// This method returns a copy of the specified string with its white
        /// space characters replaced by visible representations, for use when
        /// displaying test results.
        /// </summary>
        /// <param name="value">
        /// The string to process.  If this parameter is null or empty, it is
        /// returned unchanged.
        /// </param>
        /// <returns>
        /// The string with its white space made visible.
        /// </returns>
        public static string MakeWhiteSpaceVisible(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = StringBuilderFactory.Create(value);

            MakeWhiteSpaceVisible(builder);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method modifies the specified string builder in place,
        /// replacing its white space characters with visible representations,
        /// for use when displaying test results.
        /// </summary>
        /// <param name="builder">
        /// The string builder whose contents will be modified.
        /// </param>
        public static void MakeWhiteSpaceVisible(
            StringBuilder builder
            )
        {
            StringOps.FixupWhiteSpace(
                builder, Characters.Space, WhiteSpaceFlags.TestUse);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a test result mismatch should be
        /// ignored, based on whether the result text matches any of the
        /// specified patterns.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when matching, or null if none is
        /// available.
        /// </param>
        /// <param name="mode">
        /// The string matching mode to use.
        /// </param>
        /// <param name="text">
        /// The test result text to be matched against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The list of patterns to match against.  If this parameter is null,
        /// the result is false.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="comparer">
        /// The comparer to use for matching, or null to use the default.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching using regular
        /// expressions.
        /// </param>
        /// <param name="debug">
        /// Non-zero to emit diagnostic trace output during matching.
        /// </param>
        /// <returns>
        /// True if any of the patterns matched and the mismatch should be
        /// ignored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a test failure should be ignored,
        /// based on the failure flag and the various per-aspect ignore flags.
        /// </summary>
        /// <param name="fail">
        /// Non-zero if the test is expected to be able to fail; if zero, the
        /// failure is always ignored.
        /// </param>
        /// <param name="outputIgnore">
        /// Non-zero if a mismatch in the test output should be ignored.
        /// </param>
        /// <param name="errorIgnore">
        /// Non-zero if a mismatch in the test error result should be ignored.
        /// </param>
        /// <param name="scriptIgnore">
        /// Non-zero if a mismatch produced by the test result script should be
        /// ignored.
        /// </param>
        /// <returns>
        /// True if the failure should be ignored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method matches the specified text against a single pattern,
        /// optionally evaluating the pattern as an expression, for use when
        /// comparing test results.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when matching.  If this parameter is
        /// null, an error is returned.
        /// </param>
        /// <param name="mode">
        /// The string matching mode to use.  When this is
        /// <see cref="MatchMode.Expression" />, the pattern is evaluated as an
        /// expression and its boolean result is used.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against, or the expression to evaluate.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="comparer">
        /// The comparer to use for matching, or null to use the default.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching using regular
        /// expressions.
        /// </param>
        /// <param name="debug">
        /// Non-zero to emit diagnostic trace output during matching.
        /// </param>
        /// <param name="match">
        /// Upon success, set to non-zero if the text matched the pattern;
        /// otherwise, set to zero.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an appropriate error message; may also
        /// receive the expression evaluation result.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                CultureInfo cultureInfo = interpreter.InternalCultureInfo;

                if (mode == MatchMode.Expression)
                {
                    if ((interpreter.EvaluateExpression(
                            pattern, ref result) == ReturnCode.Ok) &&
                        (Engine.ToBoolean(
                            result, cultureInfo, ref match,
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
        /// <summary>
        /// This method returns the string representation of the specified
        /// object value, using the standard object-to-string conversion.
        /// </summary>
        /// <param name="value">
        /// The object value to convert to a string.
        /// </param>
        /// <returns>
        /// The string representation of the value.
        /// </returns>
        private static string StringFromObject(
            object value
            )
        {
            return StringOps.GetStringFromObject(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the string representation of the specified
        /// object value to the test data, writing it to the host and/or log as
        /// appropriate based on the current test output settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when emitting the test data.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being emitted.
        /// </param>
        /// <param name="value">
        /// The object value to append.
        /// </param>
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

        /// <summary>
        /// This method appends a line terminator to the test data, writing it
        /// to the host and/or log as appropriate based on the current test
        /// output settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when emitting the test data.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being emitted.
        /// </param>
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

        /// <summary>
        /// This method appends the specified string value followed by a line
        /// terminator to the test data, writing it to the host and/or log as
        /// appropriate based on the current test output settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when emitting the test data.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being emitted.
        /// </param>
        /// <param name="value">
        /// The string value to append.
        /// </param>
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

        /// <summary>
        /// This method formats the specified format string and arguments and
        /// appends the result to the test data, writing it to the host and/or
        /// log as appropriate based on the current test output settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when emitting the test data.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being emitted.
        /// </param>
        /// <param name="format">
        /// The composite format string.
        /// </param>
        /// <param name="args">
        /// The array of objects to format using the format string.
        /// </param>
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
        /// <summary>
        /// This method determines whether the specified repeat count indicates
        /// that a test is being repeated more than once.
        /// </summary>
        /// <param name="repeatCount">
        /// The number of times the test is to be run.
        /// </param>
        /// <returns>
        /// True if the repeat count is greater than one; otherwise, false.
        /// </returns>
        private static bool IsRepeating(
            int repeatCount
            )
        {
            return repeatCount > 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a display suffix indicating the current
        /// iteration of a repeated test, or an empty string when the test is
        /// not being repeated.
        /// </summary>
        /// <param name="iterationCount">
        /// The number of the current iteration.
        /// </param>
        /// <param name="repeatCount">
        /// The total number of times the test is to be run.
        /// </param>
        /// <returns>
        /// The iteration suffix string, or an empty string when not repeating.
        /// </returns>
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
        /// <summary>
        /// This method returns the name of the test currently being run by the
        /// specified interpreter (or by the test it is following), if any.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  If this parameter is null, null is
        /// returned.
        /// </param>
        /// <returns>
        /// The name of the current test, or null if there is none.
        /// </returns>
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

        /// <summary>
        /// This method records the specified piece of test information into the
        /// interpreter, discarding any error message that may be produced.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the information will be recorded.
        /// </param>
        /// <param name="type">
        /// The category of test information being recorded.
        /// </param>
        /// <param name="name">
        /// The test name (or other key) associated with the information, when
        /// applicable.  Empty names are permitted.
        /// </param>
        /// <param name="value">
        /// The value to record, when applicable.
        /// </param>
        /// <param name="add">
        /// Non-zero to add (or increment) the information; zero to remove (or
        /// decrement) it.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method records the specified piece of test information into the
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the information will be recorded.
        /// </param>
        /// <param name="type">
        /// The category of test information being recorded.
        /// </param>
        /// <param name="name">
        /// The test name (or other key) associated with the information, when
        /// applicable.  Empty names are permitted.
        /// </param>
        /// <param name="value">
        /// The value to record, when applicable.
        /// </param>
        /// <param name="add">
        /// Non-zero to add (or increment) the information; zero to remove (or
        /// decrement) it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method records the specified piece of test information into the
        /// interpreter, dispatching to the appropriate handling based on the
        /// information type.  This is the most general overload; the other
        /// overloads delegate to it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the information will be recorded.  If
        /// this parameter is null, an error is returned.
        /// </param>
        /// <param name="type">
        /// The category of test information being recorded.
        /// </param>
        /// <param name="name">
        /// The test name (or other key) associated with the information, when
        /// applicable.  Empty names are permitted.
        /// </param>
        /// <param name="value">
        /// The value to record, when applicable.
        /// </param>
        /// <param name="add">
        /// Non-zero to add (or increment) the information; zero to remove (or
        /// decrement) it.
        /// </param>
        /// <param name="level">
        /// When recording the test level, receives the resulting test nesting
        /// level after entering or exiting a level.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                CultureInfo cultureInfo = interpreter.InternalCultureInfo;

                switch (type)
                {
                    case TestInformationType.PreviousAndCurrentName:
                        {
                            if (add)
                            {
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
                                if (name != null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestPrevious = interpreter.TestCurrent;
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
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
                                if (name == null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestPrevious = interpreter.TestCurrent;
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
                    case TestInformationType.PreviousName:
                        {
                            if (add)
                            {
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
                                if (name != null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestPrevious = name;
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
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
                                if (name == null)
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        interpreter.TestPrevious = null;
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
                    case TestInformationType.CurrentName:
                        {
                            if (add)
                            {
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
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
                                //
                                // NOTE: *WARNING* Empty test names are allowed,
                                //       please do not change this to "!String.IsNullOrEmpty".
                                //
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
                                        ValueFlags.AnyInteger, cultureInfo,
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
                                object enumValue = EnumOps.TryParseFlags(
                                    interpreter, typeof(TestOutputType),
                                    interpreter.TestVerbose.ToString(),
                                    StringOps.GetStringFromObject(value),
                                    cultureInfo, true, true, true, ref error);

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
        /// <summary>
        /// This method retrieves the number of times the named test has been
        /// run so far, as tracked by the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  If this parameter is null, an error is
        /// returned.
        /// </param>
        /// <param name="name">
        /// The name of the test.  Empty names are permitted; a null name is an
        /// error.
        /// </param>
        /// <param name="count">
        /// Upon success, receives the number of times the test has been run.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method checks the specified test name against the interpreter's
        /// configured match and skip name patterns.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  If this parameter is null, an error is
        /// returned.
        /// </param>
        /// <param name="name">
        /// The name of the test.  Empty names are permitted; a null name is an
        /// error.
        /// </param>
        /// <param name="matchName">
        /// Upon success, set to non-zero if the test name matches the
        /// configured match patterns.
        /// </param>
        /// <param name="skipName">
        /// Upon success, set to non-zero if the test name matches the
        /// configured skip patterns.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method appends a formatted "SKIPPED" notice for the named test
        /// to the test data, listing the reasons the test was skipped.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when emitting the test data.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="name">
        /// The name of the test that was skipped.
        /// </param>
        /// <param name="list">
        /// The list of reasons (e.g. unsatisfied constraints) the test was
        /// skipped.
        /// </param>
        public static void AddSkippedTestData(
            Interpreter interpreter,
            StringBuilder testData,
            string name,
            StringList list
            )
        {
            AppendFormat(
                interpreter, testData, TestOutputType.Skip,
                "++++ {0} SKIPPED: {1}", name,
                list.ToString());

            AppendLine(
                interpreter, testData, TestOutputType.Skip);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates a constraint expression for the named test
        /// and, if the expression is not satisfied, records the test as skipped
        /// and indicates that it should not be run.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, an error
        /// is returned.
        /// </param>
        /// <param name="testLevels">
        /// The current test nesting level; statistics are only recorded at the
        /// outermost level (one).
        /// </param>
        /// <param name="name">
        /// The name of the test.  Empty names are permitted; a null name is an
        /// error.
        /// </param>
        /// <param name="constraintExpression">
        /// The constraint expression to evaluate.  If this is null or empty,
        /// the test is allowed to run.
        /// </param>
        /// <param name="noStatistics">
        /// Non-zero to suppress recording skip statistics in the interpreter.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="knownBug">
        /// Non-zero if the test exercises a known bug, in which case the
        /// "skipped bug" statistic is also recorded.
        /// </param>
        /// <param name="skip">
        /// Set to non-zero if the test should be skipped because the constraint
        /// expression was not satisfied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

            code = interpreter.InternalEvaluateExpressionWithErrorInfo(
                constraintExpression, "{0}    (\"constraint\" expression)",
                ref result);

            if (code == ReturnCode.Ok)
            {
                CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                bool value = false;

                code = Engine.ToBoolean(
                    result, cultureInfo, ref value, ref error);

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


                    AddSkippedTestData(
                        interpreter, testData, name, matchList);
                }

                //
                // NOTE: Finally, we are NOT going to run this test.
                //
                skip = true;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the named test against the interpreter's match
        /// and skip name patterns, its run-once policy, and its list of
        /// constraints, and determines whether the test should be skipped.  It
        /// also processes the special "fail.false", "fail.true", "knownBug",
        /// and "!knownBug" pseudo-constraints.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, an error
        /// is returned.
        /// </param>
        /// <param name="testLevels">
        /// The current test nesting level; statistics are only recorded at the
        /// outermost level (one).
        /// </param>
        /// <param name="name">
        /// The name of the test.  Empty names are permitted; a null name is an
        /// error.
        /// </param>
        /// <param name="constraints">
        /// The list of constraints (as a string list) required for the test to
        /// run, or null if there are none.
        /// </param>
        /// <param name="once">
        /// Non-zero if the test should only be run once; if it has already been
        /// run, it will be skipped.
        /// </param>
        /// <param name="noStatistics">
        /// Non-zero to suppress recording skip statistics in the interpreter.
        /// </param>
        /// <param name="testData">
        /// The string builder accumulating the returned test data, or null if
        /// none.
        /// </param>
        /// <param name="skip">
        /// Set to non-zero if the test should be skipped.
        /// </param>
        /// <param name="fail">
        /// Set based on the "fail.false" and "fail.true" pseudo-constraints,
        /// controlling whether the test is permitted to fail.
        /// </param>
        /// <param name="whatIf">
        /// Set to non-zero if the interpreter is in script "what-if" mode.
        /// </param>
        /// <param name="knownBug">
        /// Set based on the "knownBug" and "!knownBug" pseudo-constraints,
        /// indicating whether the test exercises a known bug.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

                                            AddSkippedTestData(
                                                interpreter, testData, name, matchList);
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
                                            interpreter, InterpreterTestFlags.ScriptWhatIf,
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
        /// <summary>
        /// This method writes the specified value to the attached debugger as
        /// test tracking output, when test data tracking is enabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being tracked.
        /// </param>
        /// <returns>
        /// True if the value was written to the debugger; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to write the specified value to any attached
        /// debugger (and, on Windows, to the native debug output).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when reporting a complaint on failure.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting a complaint when the write fails.
        /// </param>
        /// <returns>
        /// True if the value was successfully written to a debugger; otherwise,
        /// false.
        /// </returns>
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
                    DebugOps.Output(value, DebugPriority.FromTest);
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

        /// <summary>
        /// This method attempts to write the specified value to the
        /// interpreter's interactive host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive host will be written to.  If this
        /// parameter is null, the write fails.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting a complaint when the write fails.
        /// </param>
        /// <returns>
        /// True if the value was successfully written to the host; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method returns the name of the available test output command,
        /// preferring the normal command and optionally falling back to the
        /// fallback command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the command.  If this parameter is
        /// null, null is returned.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to allow returning the fallback command when the normal
        /// command does not exist.
        /// </param>
        /// <returns>
        /// The name of an available test output command, or null if none is
        /// available.
        /// </returns>
        private static string GetPutsCommand(
            Interpreter interpreter,
            bool useFallback
            )
        {
            if (interpreter != null)
            {
                if (interpreter.InternalDoesIExecuteExistViaResolvers(
                        putsNormalCommand) == ReturnCode.Ok)
                {
                    return putsNormalCommand;
                }

                if (useFallback)
                {
                    if (interpreter.InternalDoesIExecuteExistViaResolvers(
                            putsFallbackCommand) == ReturnCode.Ok)
                    {
                        return putsFallbackCommand;
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the specified channel name if a channel with
        /// that name exists in the interpreter; otherwise, it returns null.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the channel.  If this parameter is
        /// null, null is returned.
        /// </param>
        /// <param name="name">
        /// The name of the channel to check for.
        /// </param>
        /// <returns>
        /// The channel name if the channel exists; otherwise, null.
        /// </returns>
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

        /// <summary>
        /// This method returns the name of the channel to use for test output,
        /// preferring the channel named by the configured variable and
        /// optionally falling back to the fallback channel.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.  If this parameter is null, null is
        /// returned.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to allow returning the fallback channel when the normal
        /// channel is not available.
        /// </param>
        /// <returns>
        /// The name of an available test output channel, or null if none is
        /// available.
        /// </returns>
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

        /// <summary>
        /// This method determines whether it may be possible to write test
        /// output via the test output command and channel, i.e. whether both
        /// are available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.
        /// </param>
        /// <returns>
        /// True if both a test output command and channel are available;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to write the specified value to the test
        /// output channel by evaluating the test output command (i.e.
        /// "::tputs" or "::tqputs"), honoring the interpreter's "quiet" mode
        /// unless instructed otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// write fails.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="ignoreQuiet">
        /// Non-zero to write the value even when the interpreter is in "quiet"
        /// mode.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting a complaint when the write fails.
        /// </param>
        /// <returns>
        /// True if the value was successfully written (or suppressed due to
        /// quiet mode); otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the name of the available test log command,
        /// preferring the normal command and optionally falling back to the
        /// fallback command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the command.  If this parameter is
        /// null, null is returned.
        /// </param>
        /// <param name="useFallback">
        /// Non-zero to allow returning the fallback command when the normal
        /// command does not exist.
        /// </param>
        /// <returns>
        /// The name of an available test log command, or null if none is
        /// available.
        /// </returns>
        private static string GetLogCommand(
            Interpreter interpreter,
            bool useFallback
            )
        {
            if (interpreter != null)
            {
                if (interpreter.InternalDoesIExecuteExistViaResolvers(
                        logNormalCommand) == ReturnCode.Ok)
                {
                    return logNormalCommand;
                }

                if (useFallback)
                {
                    if (interpreter.InternalDoesIExecuteExistViaResolvers(
                            logFallbackCommand) == ReturnCode.Ok)
                    {
                        return logFallbackCommand;
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to write the specified value to the test log
        /// file by evaluating the test log command (i.e. "::tlog" or
        /// "::tqlog").
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// write fails.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress reporting a complaint when the write fails.
        /// </param>
        /// <returns>
        /// True if the value was successfully written to the log; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified test output type should
        /// be written automatically when test output is in "automatic" mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context (currently unused).
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered.
        /// </param>
        /// <returns>
        /// True if the output should be written automatically (i.e. it marks
        /// the start of a test); otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified test output should be
        /// returned automatically when test output is in "automatic" mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context (currently unused).
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered (currently unused).
        /// </param>
        /// <param name="wrote">
        /// Non-zero if the data was already written out during the test.
        /// </param>
        /// <returns>
        /// True if the output should be returned automatically (i.e. it was not
        /// already written); otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether test data should be written to the
        /// host, based on the specified return code (mapped to a pass or fail
        /// output type).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="code">
        /// The return code of the test;
        /// <see cref="ReturnCode.Ok" /> maps to pass output and any other value
        /// maps to fail output.
        /// </param>
        /// <returns>
        /// True if the test data should be written to the host; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether test data of the specified output
        /// type should be written to the host, based on the interpreter's test
        /// flags and verbosity settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// result is false.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered.
        /// </param>
        /// <returns>
        /// True if the test data should be written to the host; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldWriteTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {
            if ((interpreter == null) || !ScriptOps.HasFlags(
                    interpreter, InterpreterTestFlags.WriteData, true))
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
                        InterpreterTestFlags.NoReturnData, true) &&
                    !IsAutomaticWriteTestData(
                        interpreter, outputType))
                {
                    return false;
                }
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether test data of the specified output
        /// type should be returned to the caller, based on the interpreter's
        /// test flags and verbosity settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// result is false.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered.
        /// </param>
        /// <param name="wrote">
        /// Non-zero if the data was already written out during the test.
        /// </param>
        /// <returns>
        /// True if the test data should be returned; otherwise, false.
        /// </returns>
        private static bool ShouldReturnTestData(
            Interpreter interpreter,
            TestOutputType outputType,
            bool wrote
            )
        {
            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterTestFlags.NoReturnData, true))
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
                        InterpreterTestFlags.WriteData, true) &&
                    !IsAutomaticReturnTestData(
                        interpreter, outputType, wrote))
                {
                    return false;
                }
            }

            return FlagOps.HasFlags(testVerbose, outputType, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether test data of the specified output
        /// type should be written to the log file, based on the interpreter's
        /// test flags and verbosity settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// result is false.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered.
        /// </param>
        /// <returns>
        /// True if the test data should be logged; otherwise, false.
        /// </returns>
        private static bool ShouldLogTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {
            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterTestFlags.NoLogData, true))
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

        /// <summary>
        /// This method determines whether test data of the specified output
        /// type should be tracked (i.e. written to the debugger), based on the
        /// interpreter's test flags and verbosity settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  If this parameter is null, the
        /// result is false.
        /// </param>
        /// <param name="outputType">
        /// The category of test output being considered.
        /// </param>
        /// <returns>
        /// True if the test data should be tracked; otherwise, false.
        /// </returns>
        private static bool ShouldTrackTestData(
            Interpreter interpreter,
            TestOutputType outputType
            )
        {

            if ((interpreter == null) || ScriptOps.HasFlags(
                    interpreter, InterpreterTestFlags.NoTrackData, true))
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
        /// <summary>
        /// This method determines whether a particular detail level should be
        /// shown for an isolated test based on its pass or fail status.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context (currently unused).
        /// </param>
        /// <param name="passFlags">
        /// The detail level flags to use when the test passed.
        /// </param>
        /// <param name="failFlags">
        /// The detail level flags to use when the test failed.
        /// </param>
        /// <param name="hasFlags">
        /// The specific detail level flags being checked for.
        /// </param>
        /// <param name="pass">
        /// Non-zero if the test passed; zero if it failed.
        /// </param>
        /// <returns>
        /// True if the requested detail level should be shown; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method returns a dictionary mapping the relevant return codes
        /// to their human-readable descriptions, for use when reporting test
        /// outcomes.
        /// </summary>
        /// <returns>
        /// A dictionary of return code to message mappings.
        /// </returns>
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
        /// <summary>
        /// This method determines the script location associated with the
        /// specified argument, complaining if it cannot be determined.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when determining the script location.
        /// </param>
        /// <param name="argument">
        /// The argument used as the source of location information and as the
        /// fallback location.
        /// </param>
        /// <param name="strict">
        /// When non-zero, the location must be determined precisely rather than
        /// falling back to the argument.
        /// </param>
        /// <param name="location">
        /// Upon return, receives the determined script location.
        /// </param>
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

        /// <summary>
        /// This method determines the script location associated with the
        /// specified argument.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when determining the script location.
        /// </param>
        /// <param name="argument">
        /// The argument used as the source of location information and as the
        /// fallback location.
        /// </param>
        /// <param name="strict">
        /// When non-zero, the location must be determined precisely rather than
        /// falling back to the argument.
        /// </param>
        /// <param name="location">
        /// Upon success, receives the determined script location.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method returns the base path where the test suite files are
        /// located, preferring the interpreter's configured test path and
        /// otherwise deriving it from the global base path and the requested
        /// path type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for its configured test path, or null if
        /// none.
        /// </param>
        /// <param name="pathType">
        /// The category of test path to derive when no configured test path is
        /// available.
        /// </param>
        /// <returns>
        /// The test suite path, or null if it cannot be determined.
        /// </returns>
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

#if SHELL && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// This method, when running on the .NET Core runtime, prepends the
        /// command line arguments needed to execute the entry assembly via the
        /// host executable when launching an isolated test process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when comparing file names.
        /// </param>
        /// <param name="fileName">
        /// The file name of the executable that will be launched.
        /// </param>
        /// <param name="firstArguments">
        /// The list of arguments that must precede the rest of the command
        /// line; this list is created if needed and appended to.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Set to non-zero if the fully qualified path to the entry assembly
        /// must be added to the final argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                firstArguments.Add(DotNetCoreRollForwardMajor);
                firstArguments.Add(DotNetCoreMajor);

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

        /// <summary>
        /// This method determines the file name of the executable to launch in
        /// order to run an isolated test process, accounting for the Mono and
        /// .NET Core runtimes, and any required leading arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="full">
        /// Non-zero to obtain the fully qualified executable file name.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name of the executable to launch.
        /// </param>
        /// <param name="firstArguments">
        /// The list of arguments that must precede the rest of the command
        /// line; this list may be created and appended to.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Set to non-zero if the entry assembly path must be added to the
        /// final argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method creates a uniquely named temporary script file located
        /// within the test suite directory, for use as the script file of an
        /// isolated test process.  The temporary file is cleaned up on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="pathType">
        /// The category of test path under which the file should be created.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the resulting script file name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                fileNames[0] = PathOps.GetTempFileName( /* throw */
                    interpreter, "eitf_"); /* Eagle Isolated Test File */

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
                        // BUGFIX: Do this only if the file exists.  If not,
                        //         that is fine and the final file will be
                        //         created later by our caller.
                        //
                        if (File.Exists(fileNames[0]))
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

        /// <summary>
        /// This method adds the command line arguments needed to pre-initialize
        /// a test-related variable, with the value of the specified test
        /// information, in an isolated test process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the test information.  If this
        /// parameter is null, an error is returned.
        /// </param>
        /// <param name="type">
        /// The category of test information to retrieve and assign.
        /// </param>
        /// <param name="list">
        /// The argument list to which the assignment arguments will be added.
        /// If this parameter is null, an error is returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode AddIsolatedVariableAssignment(
            Interpreter interpreter,  /* in */
            TestInformationType type, /* in */
            StringList list,          /* in */
            ref Result error          /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (list == null)
            {
                error = "invalid list";
                return ReturnCode.Error;
            }

            Result result = null;

            if (interpreter.GetTestInformation(type, ref result) != ReturnCode.Ok)
            {
                error = result;
                return ReturnCode.Error;
            }

            list.Add(Characters.MinusSign + CommandLineOption.PreInitialize);

            list.Add(new StringList(SetCommandName, FormatOps.VariableName(
                Vars.Core.Tests, type.ToString()), result).ToString());

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the complete, properly quoted command line used
        /// to launch an isolated test process, including the entry assembly,
        /// the safe and security options, the previous and current test name
        /// assignments, the optional pre-initialize script, the test and log
        /// file options, and any caller-supplied leading, other, and trailing
        /// arguments.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="fileName">
        /// The test script file name to pass on the command line, or null if
        /// none.
        /// </param>
        /// <param name="logFile">
        /// The log file name to pass on the command line, or null if none.
        /// </param>
        /// <param name="firstArguments">
        /// The arguments that must precede the rest of the command line, or
        /// null if none.
        /// </param>
        /// <param name="otherArguments">
        /// The arguments that must occur before the test file on the command
        /// line, or null if none.
        /// </param>
        /// <param name="lastArguments">
        /// The arguments that must occur after the rest of the command line, or
        /// null if none.
        /// </param>
        /// <param name="text">
        /// The pre-initialize script to pass on the command line, or null if
        /// none.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero if the entry assembly file name must be inserted into the
        /// command line.
        /// </param>
        /// <param name="safe">
        /// Non-zero to force the interpreter in the child process to be "safe".
        /// </param>
        /// <param name="security">
        /// Non-zero to enable security for the interpreter in the child
        /// process.
        /// </param>
        /// <param name="arguments">
        /// Upon success, receives the built command line string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetIsolatedExecutableArguments(
            Interpreter interpreter,   /* in */
            string fileName,           /* in */
            string logFile,            /* in */
            StringList firstArguments, /* in */
            StringList otherArguments, /* in */
            StringList lastArguments,  /* in */
            string text,               /* in */
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
            // NOTE: First, if there are arguments that must precede the
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
            // HACK: Make sure that the isolated process has access to the
            //       previous test name.
            //
            if (AddIsolatedVariableAssignment(
                    interpreter, TestInformationType.PreviousName, list,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // HACK: Make sure that the isolated process has access to the
            //       current test name.
            //
            if (AddIsolatedVariableAssignment(
                    interpreter, TestInformationType.CurrentName, list,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Next, if there was a pre-initialize script specified
            //       by the caller, add that now.
            //
            if (text != null)
            {
                list.Add(Characters.MinusSign + CommandLineOption.StartupPreInitialize);
                list.Add(text);
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
            bool done = false;

            arguments = RuntimeOps.BuildCommandLine(
                interpreter, list, null, false, false, false, ref done,
                ref error);

            if (done)
                return ReturnCode.Ok;

            return (arguments != null) ? ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Syntax is "test name description ?options?".
        //
        /// <summary>
        /// This method copies the arguments of a test command, omitting the
        /// options that apply only to test isolation, so that the resulting
        /// argument list can be run normally within the isolated process.  The
        /// command syntax is "test name description ?options?".
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context (currently unused for matching).
        /// </param>
        /// <param name="oldArguments">
        /// The original test command argument list.  If this parameter is null,
        /// the new argument list is set to null.
        /// </param>
        /// <param name="newArguments">
        /// The argument list to receive the copied, filtered arguments; it is
        /// created if needed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a warning should be issued about
        /// running individual test files (instead of the full test suite),
        /// based on whether the corresponding warning suppression variable has
        /// been set.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the suppression variable.
        /// </param>
        /// <returns>
        /// True if the warning should be issued; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a warning should be issued about
        /// running test files located in a non-test directory, based on whether
        /// the corresponding warning suppression variable has been set.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query for the suppression variable.
        /// </param>
        /// <returns>
        /// True if the warning should be issued; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the name of the immediate parent directory of
        /// the specified file, without any further directory information.
        /// </summary>
        /// <param name="fileName">
        /// The file name whose parent directory name is desired.  If this
        /// parameter is null or empty, null is returned.
        /// </param>
        /// <returns>
        /// The name of the parent directory, or null if it cannot be
        /// determined.
        /// </returns>
        private static string GetDirectoryNameOnly(
            string fileName
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            return Path.GetFileName(Path.GetDirectoryName(fileName));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates and evaluates the test suite script file(s)
        /// matching the specified pattern beneath the test suite path,
        /// optionally evaluating all matching files, while skipping the
        /// well-known prologue and epilogue files and issuing warnings about
        /// running individual files or files located in non-test directories.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the test files will be evaluated.
        /// </param>
        /// <param name="pattern">
        /// The file name or glob pattern of the test files to run, or null to
        /// run the master test suite file.
        /// </param>
        /// <param name="extraPath">
        /// An optional additional path fragment appended to the base test
        /// path, or null if none.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the test files.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the test files.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use when evaluating the test files.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the test files.
        /// </param>
        /// <param name="pathType">
        /// The category of test path to search beneath.
        /// </param>
        /// <param name="all">
        /// Non-zero to evaluate all matching test files; zero to evaluate only
        /// the first one.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the last test file evaluated;
        /// upon failure, receives an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure during evaluation, receives the line number where the
        /// error occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                    CultureInfo cultureInfo = null;

                    if (interpreter != null)
                        cultureInfo = interpreter.InternalCultureInfo;

                    //
                    // NOTE: Make sure the file names are always evaluated
                    //       in a well-defined order.
                    //
                    IntDictionary duplicates = null;

                    fileNames.Sort(new _Comparers.StringDictionaryComparer(
                        interpreter, true, null, false, false, cultureInfo,
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
        /// <summary>
        /// This method is a thread start callback that, after a short delay,
        /// cancels the active interpreter's host (closing the standard input
        /// channel), for use in testing host cancellation behavior.
        /// </summary>
        /// <param name="obj">
        /// The thread start argument, expected to be a pair whose first value
        /// is the delay in milliseconds and whose second value indicates
        /// whether to force the cancel.
        /// </param>
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
                        IDebugHost debugHost = interpreter.InternalHost;

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

        /// <summary>
        /// This method is a thread start callback that, after a short delay,
        /// exits the active interpreter's host (closing the standard input
        /// channel), for use in testing host exit behavior.
        /// </summary>
        /// <param name="obj">
        /// The thread start argument, expected to be a pair whose first value
        /// is the delay in milliseconds and whose second value indicates
        /// whether to force the exit.
        /// </param>
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
                        IDebugHost debugHost = interpreter.InternalHost;

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
