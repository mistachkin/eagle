/*
 * Test1.cs --
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

#if TEST
using System.Diagnostics;
#endif

using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>test1</c> command, which defines and
    /// runs a single test case, comparing the actual result and return code of
    /// the test body against the expected ones and recording the pass, fail,
    /// skip, or ignore outcome in the interpreter test statistics.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("f20c521d-e16e-43fa-a050-ecf96c93bbdd")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("test")]
    internal sealed class Test1 : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>test1</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Test1(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>test1</c> command.  It evaluates the
        /// test body, checks any constraints, compares the actual result and
        /// return code against the expected ones, runs any registered test
        /// hooks, updates the interpreter test statistics, and produces the
        /// formatted test output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name, followed by the test name, description, an optional
        /// constraints element, the test body, and the expected result.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the complete formatted output produced
        /// by the test.  Upon failure, this contains an appropriate error
        /// message (or the result reported by a test hook when one fails).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the test ran to completion;
        /// <see cref="ReturnCode.Break" /> when the test was skipped,
        /// <see cref="ReturnCode.Continue" /> when a failure was ignored, or
        /// <see cref="ReturnCode.WhatIf" /> when the test was disabled (unless
        /// changing the raw return code is suppressed); otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, or
        /// an error occurs while running the test, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count == 5) || (arguments.Count == 6))
                    {
                        ///////////////////////////////////////////////////////////////////////////////////////////////
                        //
                        // test name description ?constraints? body result
                        //
                        ///////////////////////////////////////////////////////////////////////////////////////////////

                        string name = arguments[1];

#if DEBUGGER
                        if (DebuggerOps.CanHitBreakpoints(interpreter,
                                EngineFlags.None, BreakpointType.Test))
                        {
                            code = interpreter.CheckBreakpoints(
                                code, BreakpointType.Test, name,
                                null, null, this, null, clientData,
                                arguments, ref result);
                        }

                        if (code == ReturnCode.Ok)
#endif
                        {
                            string description = arguments[2];

                            string constraints;
                            string body;
                            IScriptLocation bodyLocation;
                            string expectedResult;

                            if (arguments.Count == 6)
                            {
                                constraints = arguments[3];
                                body = arguments[4];
                                bodyLocation = arguments[4];
                                expectedResult = arguments[5];
                            }
                            else
                            {
                                constraints = null;
                                body = arguments[3];
                                bodyLocation = arguments[3];
                                expectedResult = arguments[4];
                            }

                            ReturnCodeList returnCodes = new ReturnCodeList(new ReturnCode[] {
                                ReturnCode.Ok, ReturnCode.Return
                            });

                            MatchMode mode = StringOps.DefaultResultMatchMode;

                            bool noCase = false;

#if TEST
                            bool captureTrace = ScriptOps.HasFlags(interpreter,
                                InterpreterTestFlags.CaptureTraces, true);
#endif

                            bool noChangeReturnCode = ScriptOps.HasFlags(interpreter,
                                InterpreterTestFlags.NoChangeReturnCode, true);

                            bool stopOnHookError = ScriptOps.HasFlags(interpreter,
                                InterpreterTestFlags.StopOnHookError, true);

                            ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
                            string logFileName = null;
                            TraceListener logListener = null;
                            TraceListener[] savedListeners = null;

                            if (captureTrace)
                            {
                                logFileName = DebugOps.GetTraceLogFileName(
                                    interpreter, name, ref result);

                                if (logFileName != null)
                                {
                                    logListener = DebugOps.NewTestTraceListener(
                                        name, logFileName); /* throw */

                                    /* NO RESULT */
                                    DebugOps.PushTraceListener(
                                        false, logListener, ref savedListeners);
                                }
                                else
                                {
                                    code = ReturnCode.Error;
                                }
                            }

                            try
                            {
                                if (code == ReturnCode.Ok)
                                {
#endif
                                    int testLevels = interpreter.EnterTestLevel();

                                    try
                                    {
                                        //
                                        // NOTE: Create a place to put all the output of the this command.
                                        //
                                        StringBuilder testData = StringBuilderFactory.Create();

                                        //
                                        // NOTE: Are we going to skip this test?
                                        //
                                        bool failure = false;
                                        bool skip = false;
                                        bool fail = true;
                                        bool ignore = false;
                                        bool whatIf = false;
                                        bool knownBug = false;

                                        code = TestOps.CheckConstraints(
                                            interpreter, testLevels, name, constraints,
                                            false, false, testData, ref skip,
                                            ref fail, ref whatIf, ref knownBug, ref result);

                                        long[] statistics = interpreter.TestStatistics;
                                        TestResultType resultType = TestResultType.Pending;
                                        ArgumentList hookArguments; /* REUSED */
                                        TestHookType hookType = TestHookType.None; /* REUSED */
                                        StringList hookList; /* REUSED */
                                        ReturnCode hookCode = ReturnCode.Ok; /* REUSED */
                                        ResultList hookResults; /* REUSED */
                                        Result hookResult = null; /* REUSED */

                                        if (code == ReturnCode.Ok)
                                        {
                                            hookType = TestHookType.Before | TestHookType.AnyMatch;
                                            hookList = null;

                                            if (interpreter.HasTestHooks(
                                                    hookType, name, ref hookList) == ReturnCode.Ok)
                                            {
                                                hookArguments = null;

                                                /* NO RESULT */
                                                interpreter.PrepareTestHooksArguments(
                                                    hookType, resultType, name, description,
                                                    constraints, skip, fail, ignore, whatIf,
                                                    knownBug, null, null, ref hookArguments
                                                );

                                                hookResults = null;

                                                hookCode = interpreter.EvaluateScripts(
                                                    hookList, hookArguments, true,
                                                    stopOnHookError, ref hookResults);

                                                hookResult = hookResults;

                                                bool hookSkip = false;

                                                switch (hookCode)
                                                {
                                                    case ReturnCode.Ok:
                                                        {
                                                            //
                                                            // NOTE: Normal case, let the
                                                            //       test run normally.
                                                            //
                                                            break;
                                                        }
                                                    case ReturnCode.Error:
                                                        {
                                                            //
                                                            // NOTE: Error case, stop the
                                                            //       test from running and
                                                            //       return the error.
                                                            //
                                                            result = hookResult;
                                                            code = hookCode; /* Error */
                                                            break;
                                                        }
                                                    case ReturnCode.Break:
                                                        {
                                                            //
                                                            // NOTE: Return from the test,
                                                            //       skipping it and just
                                                            //       indicate success.
                                                            //
                                                            hookSkip = true;
                                                            break;
                                                        }
                                                    case ReturnCode.Continue:
                                                        {
                                                            //
                                                            // NOTE: Run the test; however,
                                                            //       do not allow it to be
                                                            //       counted as a failure.
                                                            //
                                                            fail = false;
                                                            break;
                                                        }
                                                    case ReturnCode.WhatIf:
                                                        {
                                                            //
                                                            // NOTE: Force "what-if" mode
                                                            //       to be enabled now.
                                                            //
                                                            whatIf = true;
                                                            break;
                                                        }
                                                    case ReturnCode.Exception:
                                                        {
                                                            //
                                                            // NOTE: Force the test to fail
                                                            //       even if it actually is
                                                            //       successful.
                                                            //
                                                            failure = true;
                                                            break;
                                                        }
                                                    default: /* NOTE: Same code as ReturnCode.Error. */
                                                        {
                                                            //
                                                            // NOTE: No idea, just error.
                                                            //
                                                            result = hookResult;
                                                            code = hookCode; /* ????? */
                                                            break;
                                                        }
                                                }

                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Hook,
                                                    "---- {0} hook ({1}) ==> {2}: {3}", name,
                                                    hookType, hookCode, FormatOps.WrapOrNull(
                                                    true, true, hookResult));

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Hook);

                                                if (hookSkip)
                                                {
                                                    skip = true;

                                                    if ((statistics != null) && (testLevels == 1))
                                                    {
                                                        Interlocked.Increment(ref statistics[
                                                            (int)TestInformationType.Skipped]);
                                                    }

                                                    TestOps.AddSkippedTestData(
                                                        interpreter, testData, name,
                                                        new StringList("userSpecifiedHook"));
                                                }

                                                if (code != ReturnCode.Ok)
                                                {
                                                    if ((statistics != null) && (testLevels == 1) && skip)
                                                    {
                                                        Interlocked.Increment(ref statistics[
                                                                (int)TestInformationType.Total]);
                                                    }
                                                }
                                            }
                                        }

                                        if (code == ReturnCode.Ok)
                                        {
                                            if ((statistics != null) && (testLevels == 1) && skip)
                                            {
                                                Interlocked.Increment(ref statistics[
                                                    (int)TestInformationType.Total]);
                                            }
                                        }

                                        if ((code == ReturnCode.Ok) && !skip)
                                        {
                                            code = TestOps.RecordInformation(
                                                interpreter, TestInformationType.Counts, name,
                                                null, true, ref result);
                                        }

                                        if ((code == ReturnCode.Ok) && !skip)
                                        {
                                            code = TestOps.RecordInformation(
                                                interpreter, TestInformationType.PreviousAndCurrentName,
                                                name, null, true, ref result);
                                        }

                                        if ((code == ReturnCode.Ok) && knownBug)
                                        {
                                            code = TestOps.RecordInformation(
                                                interpreter, TestInformationType.KnownBugs, name,
                                                null, true, ref result);
                                        }

                                        //
                                        // NOTE: Check test constraints to see if we should run the test.
                                        //
                                        if ((code == ReturnCode.Ok) && !skip)
                                        {
                                            //
                                            // NOTE: Emit tracking information for test scripts?
                                            //
                                            bool track = ScriptOps.HasFlags(
                                                interpreter, InterpreterTestFlags.TrackScripts,
                                                true);

                                            ReturnCode bodyCode = ReturnCode.Ok;
                                            Result bodyResult = null;

                                            //
                                            // NOTE: Only run the test body if the setup is successful.
                                            //
                                            if (body != null)
                                            {
                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Start,
                                                    "---- {0} start", name);

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Start);

                                                int savedPreviousLevels = interpreter.BeginNestedExecution();

                                                try
                                                {
                                                    ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                        StringList.MakeList(this.Name, "body", name),
                                                        CallFrameFlags.Test);

                                                    interpreter.PushAutomaticCallFrame(frame);

                                                    try
                                                    {
                                                        if (track)
                                                        {
                                                            TestOps.Track(interpreter, String.Format(
                                                                "ENTER TEST {0} BODY{1}", FormatOps.WrapOrNull(
                                                                name), Environment.NewLine), TestOutputType.Enter);
                                                        }

                                                        try
                                                        {
                                                            if (!whatIf)
                                                            {
                                                                bodyCode = interpreter.EvaluateScript(
                                                                    body, bodyLocation, ref bodyResult);
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            if (track)
                                                            {
                                                                TestOps.Track(interpreter, String.Format(
                                                                    "LEAVE TEST {0} BODY{1}", FormatOps.WrapOrNull(
                                                                    name), Environment.NewLine), TestOutputType.Leave);
                                                            }
                                                        }

                                                        if ((bodyResult == null) && ScriptOps.HasFlags(
                                                                interpreter, InterpreterTestFlags.NullIsEmpty,
                                                                true))
                                                        {
                                                            bodyResult = String.Empty;
                                                        }

                                                        if (bodyCode == ReturnCode.Error)
                                                        {
                                                            /* IGNORED */
                                                            interpreter.InternalCopyErrorInformation(
                                                                VariableFlags.None, false, ref bodyResult);
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
                                                catch (Exception e)
                                                {
                                                    bodyResult = e;
                                                    bodyCode = ReturnCode.Error;
                                                }
                                                finally
                                                {
                                                    interpreter.EndNestedExecution(savedPreviousLevels);
                                                }
                                            }

                                            //
                                            // NOTE: Grab the IComparer for the test interpreter.
                                            //
                                            IComparer<string> comparer = interpreter.TestComparer;

                                            //
                                            // NOTE: Did we fail to match the return code?
                                            //
                                            bool codeFailure = !returnCodes.Contains(bodyCode);

                                            //
                                            // NOTE: Does the actual result match the expected result?
                                            //
                                            bool scriptFailure = false;
                                            ReturnCode scriptCode = ReturnCode.Ok;
                                            Result scriptResult = null;

                                            if (!whatIf && (expectedResult != null))
                                            {
                                                scriptCode = TestOps.Match(
                                                    interpreter, mode, bodyResult,
                                                    expectedResult, noCase, comparer,
                                                    TestOps.RegExOptions, false,
                                                    ref scriptFailure, ref scriptResult);

                                                if (scriptCode == ReturnCode.Ok)
                                                    scriptFailure = !scriptFailure;
                                                else
                                                    scriptFailure = true;
                                            }

                                            //
                                            // NOTE: If any of the important things failed, the test fails.
                                            //
                                            if (!(failure || codeFailure || scriptFailure))
                                            {
                                                //
                                                // PASS: Test ran with no errors and the results match
                                                //       what we expected.
                                                //
                                                if ((statistics != null) && (testLevels == 1))
                                                {
                                                    if (whatIf)
                                                    {
                                                        if (knownBug)
                                                        {
                                                            Interlocked.Increment(ref statistics[
                                                                    (int)TestInformationType.DisabledBug]);
                                                        }

                                                        Interlocked.Increment(ref statistics[
                                                                (int)TestInformationType.Disabled]);
                                                    }
                                                    else
                                                    {
                                                        if (knownBug)
                                                        {
                                                            Interlocked.Increment(ref statistics[
                                                                    (int)TestInformationType.PassedBug]);
                                                        }

                                                        Interlocked.Increment(ref statistics[
                                                                (int)TestInformationType.Passed]);
                                                    }

                                                    Interlocked.Increment(ref statistics[
                                                            (int)TestInformationType.Total]);
                                                }

                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Pass,
                                                    "++++ {0} {1}", name,
                                                    whatIf ? "DISABLED" : "PASSED");

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Pass);

                                                resultType = whatIf ?
                                                    TestResultType.Disabled : TestResultType.Passed;
                                            }
                                            else
                                            {
                                                //
                                                // FAIL: Test ran with errors or the result does not match
                                                //       what we expected.
                                                //
                                                if ((statistics != null) && (testLevels == 1))
                                                {
                                                    if (fail)
                                                    {
                                                        if (knownBug)
                                                        {
                                                            Interlocked.Increment(ref statistics[
                                                                    (int)TestInformationType.FailedBug]);
                                                        }

                                                        Interlocked.Increment(ref statistics[
                                                                (int)TestInformationType.Failed]);

                                                        Interlocked.Increment(ref statistics[
                                                                (int)TestInformationType.Total]);
                                                    }
                                                }

                                                //
                                                // NOTE: Keep track of each test that fails.
                                                //
                                                if (testLevels == 1)
                                                {
                                                    TestOps.RecordInformation(
                                                        interpreter, TestInformationType.FailedNames,
                                                        name, null, true);
                                                }

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Fail);

                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Fail,
                                                    "==== {0} {1} {2}", name, description.Trim(),
                                                    fail ? "FAILED" : "IGNORED");

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Fail);

                                                resultType = fail ?
                                                    TestResultType.Failed : TestResultType.Ignored;

                                                if (!fail)
                                                    ignore = true;

                                                if (body != null)
                                                {
                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Body,
                                                        "==== Contents of test case:");

                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Body,
                                                        body);
                                                }

                                                if (scriptFailure)
                                                {
                                                    if (scriptCode == ReturnCode.Ok)
                                                    {
                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            "---- Result was:");

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            bodyResult);

                                                        TestOps.AppendFormat(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            "---- Result should have been ({0} matching):",
                                                            mode);

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Reason);

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            expectedResult);
                                                    }
                                                    else
                                                    {
                                                        TestOps.Append(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            "---- Error testing result: ");

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Reason,
                                                            scriptResult);

                                                        if ((scriptResult != null) &&
                                                            (scriptResult.ErrorInfo != null))
                                                        {
                                                            TestOps.Append(
                                                                interpreter, testData, TestOutputType.Error,
                                                                "---- errorInfo(matchResult): ");

                                                            TestOps.AppendLine(
                                                                interpreter, testData, TestOutputType.Error,
                                                                scriptResult.ErrorInfo);

                                                            TestOps.Append(
                                                                interpreter, testData, TestOutputType.Error,
                                                                "---- errorCode(matchResult): ");

                                                            TestOps.AppendLine(
                                                                interpreter, testData, TestOutputType.Error,
                                                                scriptResult.ErrorCode);
                                                        }
                                                    }
                                                }

                                                if (codeFailure)
                                                {
                                                    ReturnCodeDictionary returnCodeMessages =
                                                        interpreter.TestReturnCodeMessages;

                                                    string codeMessage;

                                                    if ((returnCodeMessages == null) ||
                                                        (!returnCodeMessages.TryGetValue(
                                                            bodyCode, out codeMessage) &&
                                                        !returnCodeMessages.TryGetValue(
                                                            ReturnCode.Invalid, out codeMessage)))
                                                    {
                                                        codeMessage = "Unknown";
                                                    }

                                                    TestOps.AppendFormat(
                                                        interpreter, testData, TestOutputType.Reason,
                                                        "---- {0}; Return code was: {1}",
                                                        codeMessage, bodyCode);

                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Reason);

                                                    TestOps.Append(
                                                        interpreter, testData, TestOutputType.Reason,
                                                        "---- Return code should have been one of: ");

                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Reason,
                                                        returnCodes.ToString());

                                                    if ((bodyResult != null) &&
                                                        (bodyResult.ErrorInfo != null) &&
                                                        !returnCodes.Contains(ReturnCode.Error))
                                                    {
                                                        TestOps.Append(
                                                            interpreter, testData, TestOutputType.Error,
                                                            "---- errorInfo(body): ");

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Error,
                                                            bodyResult.ErrorInfo);

                                                        TestOps.Append(
                                                            interpreter, testData, TestOutputType.Error,
                                                            "---- errorCode(body): ");

                                                        TestOps.AppendLine(
                                                            interpreter, testData, TestOutputType.Error,
                                                            bodyResult.ErrorCode);
                                                    }
                                                }

                                                if (failure)
                                                {
                                                    TestOps.AppendFormat(
                                                        interpreter, testData, TestOutputType.Reason,
                                                        "---- Forced failure via hooks ({0}) ==> {1}",
                                                        hookType, hookCode);

                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Reason);

                                                    TestOps.Append(
                                                        interpreter, testData, TestOutputType.Error,
                                                        "---- errors(hooks): ");

                                                    TestOps.AppendLine(
                                                        interpreter, testData, TestOutputType.Error,
                                                        hookResult);
                                                }

                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Fail,
                                                    "==== {0} {1}", name, fail ? "FAILED" : "IGNORED");

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Fail);

                                                resultType = fail ?
                                                    TestResultType.Failed : TestResultType.Ignored;

                                                if (!fail)
                                                    ignore = true;
                                            }
                                        }

                                        //
                                        // NOTE: Did the above code succeed?
                                        //
                                        if (code == ReturnCode.Ok)
                                        {
                                            //
                                            // HACK: For tests that are skipped, make sure they are
                                            //       highlighted in yellow by [host result] when it
                                            //       is called from [runTest].
                                            //
                                            // HACK: For tests where failures are ignored, make sure
                                            //       they are specially highlighted by [host result]
                                            //       when it is called from [runTest].
                                            //
                                            // HACK: If the "NoChangeReturnCode" option is enabled,
                                            //       do *NOT* change the raw return code.
                                            //
                                            if (!noChangeReturnCode)
                                            {
                                                if (skip)
                                                    code = ReturnCode.Break;
                                                else if (ignore)
                                                    code = ReturnCode.Continue;
                                                else if (whatIf)
                                                    code = ReturnCode.WhatIf;
                                            }

                                            bool keepResult = false;

                                            hookType = TestHookType.After | TestHookType.AnyMatch;
                                            hookList = null;

                                            if (interpreter.HasTestHooks(
                                                    hookType, name, ref hookList) == ReturnCode.Ok)
                                            {
                                                hookArguments = null;

                                                /* NO RESULT */
                                                interpreter.PrepareTestHooksArguments(
                                                    hookType, resultType, name, description,
                                                    constraints, skip, fail, ignore, whatIf,
                                                    knownBug, code, result, ref hookArguments
                                                );

                                                hookResults = null;

                                                hookCode = interpreter.EvaluateScripts(
                                                    hookList, hookArguments, true,
                                                    stopOnHookError, ref hookResults);

                                                hookResult = hookResults;

                                                switch (hookCode)
                                                {
                                                    case ReturnCode.Ok:
                                                        {
                                                            //
                                                            // NOTE: Normal case, let the
                                                            //       test run normally.
                                                            //
                                                            break;
                                                        }
                                                    case ReturnCode.Error:
                                                        {
                                                            //
                                                            // NOTE: Error case, stop the
                                                            //       test from passing and
                                                            //       return the error.
                                                            //
                                                            keepResult = true;
                                                            result = hookResult;
                                                            code = hookCode; /* Error */
                                                            break;
                                                        }
                                                    default: /* NOTE: Same code as ReturnCode.Error. */
                                                        {
                                                            //
                                                            // NOTE: No idea, just error.
                                                            //
                                                            keepResult = true;
                                                            result = hookResult;
                                                            code = hookCode; /* ????? */
                                                            break;
                                                        }
                                                }

                                                TestOps.AppendFormat(
                                                    interpreter, testData, TestOutputType.Hook,
                                                    "---- {0} hook ({1}) ==> {2}: {3}", name,
                                                    hookType, hookCode, FormatOps.WrapOrNull(
                                                    true, true, hookResult));

                                                TestOps.AppendLine(
                                                    interpreter, testData, TestOutputType.Hook);
                                            }

                                            //
                                            // NOTE: The result is the complete output produced by the
                                            //       entire test.
                                            //
                                            if (!keepResult)
                                            {
                                                if (testData != null)
                                                {
                                                    result = StringBuilderCache.GetStringAndRelease(
                                                        ref testData);
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Engine.SetExceptionErrorCode(interpreter, e);

                                        result = e;
                                        code = ReturnCode.Error;
                                    }
                                    finally
                                    {
                                        interpreter.ExitTestLevel();
                                    }
#if TEST
                                }
                            }
                            finally
                            {
                                if (savedListeners != null)
                                {
                                    /* NO RESULT */
                                    DebugOps.TraceFlush();

                                    /* NO RESULT */
                                    DebugOps.RestoreTraceListeners(
                                        false, ref savedListeners);
                                }

                                if (logListener != null)
                                {
                                    logListener.Flush();
                                    logListener.Close();
                                    logListener = null;
                                }

                                if (logFileName != null)
                                {
                                    /* NO RESULT */
                                    DebugOps.MaybeDeleteTraceLogFile(
                                        logFileName);
                                }
                            }
#endif
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "wrong # args: should be \"{0} name description constraints body result\"",
                            this.Name);

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
