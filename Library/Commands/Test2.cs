/*
 * Test2.cs --
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

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("a382feba-f2ff-4596-b4c7-81dfa628733b")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard | CommandFlags.Diagnostic
#if NATIVE && WINDOWS
        //
        // NOTE: Uses native code indirectly for profiling with the "-time"
        //       option (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        )]
    [ObjectGroup("test")]
    internal sealed class Test2 : Core
    {
        public Test2(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 3)
                    {
                        //
                        // COMPAT: Tcl, tcltest requires all test options to have values.
                        //
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {

                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            //
                            // NOTE: These are the options that work like those in "tcltest".
                            //
                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-constraints", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-setup", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-body", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-cleanup", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-result", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-output", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-errorOutput", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveReturnCodeListValue, Index.Invalid, Index.Invalid, "-returnCodes", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveReturnCodeListValue, Index.Invalid, Index.Invalid, "-execReturnCodes", null),
                            new Option(typeof(ExitCode), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-exitCode", null),
                            new Option(typeof(ExitCode), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-execExitCode", null),
                            new Option(null, OptionFlags.MustHaveMatchModeValue, Index.Invalid, Index.Invalid, "-match",
                                new Variant(StringOps.DefaultResultMatchMode)),

                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            //
                            // NOTE: These are the Eagle specific options.
                            //
                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-debug", null),
                            new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trace", null),
#if TEST
                            new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-captureTrace", null),
#else
                            new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-captureTrace", null),
#endif
                            new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-time", null),
                            new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-once", null),
                            new Option(null, OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-text", null),
                            new Option(null, OptionFlags.MustHaveListValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-argv", null),

                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            //
                            // NOTE: These are the "NoCase" options.
                            //
                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveRuleSetValue, Index.Invalid, Index.Invalid, "-ruleSet", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-noCase", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-visibleSpace", null),
                            new Option(typeof(RegexOptions), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-regExOptions", new Variant(TestOps.RegExOptions)),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-constraintExpression", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveIntegerValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-repeatCount", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noCancel", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-globalCancel", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noData", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-noEvent", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-noExit", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noHalt", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noProcessId", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noStatistics", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noSecurity", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-noTrack", null),
                            new Option(null, OptionFlags.MustHaveMatchModeValue, Index.Invalid, Index.Invalid, "-ignoreMatch",
                                new Variant(StringOps.DefaultResultMatchMode)),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-ignorePatterns", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-libraryPath", null),
#if ISOLATED_INTERPRETERS
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-packagePath", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-baseDirectory", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-useBasePath", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-verifyCoreAssembly", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-useEntryAssembly", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-optionalEntryAssembly", null),
#else
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-packagePath", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-baseDirectory", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-useBasePath", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-verifyCoreAssembly", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-useEntryAssembly", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe | OptionFlags.Unsupported,
                                Index.Invalid, Index.Invalid, "-optionalEntryAssembly", null),
#endif
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveListValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-autoPath", null),
                            new Option(typeof(IsolationLevel), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-isolationLevel", new Variant(IsolationLevel.Default)),
                            new Option(typeof(IsolationDetail), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-isolationPassDetail", new Variant(IsolationDetail.Default)),
                            new Option(typeof(IsolationDetail), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-isolationFailDetail", new Variant(IsolationDetail.Default)),
                            new Option(typeof(TestPathType), OptionFlags.NoCase | OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-isolationPathType", new Variant(TestPathType.Default)),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationUnicode", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationTemplate", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveListValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationOtherArguments", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveListValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationLastArguments", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationFileName", null),
                            new Option(null, OptionFlags.NoCase | OptionFlags.MustHaveValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                                "-isolationLogFile", null),
                            new Option(null, OptionFlags.MustHaveBooleanValue | OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noChangeReturnCode", null),

                            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 3)
                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Support the new syntax only.
                            //
                            if (argumentIndex == Index.Invalid)
                            {
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

                                    Variant value = null;
                                    IRuleSet ruleSet = null;

                                    if (options.IsPresent("-ruleSet", true, ref value))
                                        ruleSet = (IRuleSet)value.Value;

                                    int repeatCount = interpreter.TestRepeatCount;

                                    if (options.IsPresent("-repeatCount", true, ref value))
                                        repeatCount = (int)value.Value;

                                    int valueIndex = Index.Invalid;
                                    string constraints = null;

                                    if (options.IsPresent("-constraints", ref value))
                                        constraints = value.ToString();

                                    string constraintExpression = null;

                                    if (options.IsPresent("-constraintExpression", true, ref value))
                                        constraintExpression = value.ToString();

                                    string setup = null;
                                    IScriptLocation setupLocation = null;

                                    if (options.IsPresent("-setup", ref value, ref valueIndex))
                                    {
                                        setup = value.ToString();
                                        setupLocation = arguments[valueIndex + 1];
                                    }

                                    string body = null;
                                    IScriptLocation bodyLocation = null;

                                    if (options.IsPresent("-body", ref value, ref valueIndex))
                                    {
                                        body = value.ToString();
                                        bodyLocation = arguments[valueIndex + 1];
                                    }

                                    string cleanup = null;
                                    IScriptLocation cleanupLocation = null;

                                    if (options.IsPresent("-cleanup", ref value, ref valueIndex))
                                    {
                                        cleanup = value.ToString();
                                        cleanupLocation = arguments[valueIndex + 1];
                                    }

                                    Result expectedResult = null;

                                    if (options.IsPresent("-result", ref value))
                                        expectedResult = value.ToString();

                                    string expectedOutput = null;

                                    if (options.IsPresent("-output", ref value))
                                        expectedOutput = value.ToString();

                                    string expectedError = null;

                                    if (options.IsPresent("-errorOutput", true, ref value))
                                        expectedError = value.ToString();

                                    ReturnCodeList returnCodes = new ReturnCodeList(new ReturnCode[] {
                                        ReturnCode.Ok, ReturnCode.Return
                                    });

                                    if (options.IsPresent("-returnCodes", true, ref value))
                                        returnCodes = (ReturnCodeList)value.Value;

                                    ReturnCodeList execReturnCodes = new ReturnCodeList(new ReturnCode[] {
                                        ReturnCode.Ok, ReturnCode.Return
                                    });

                                    if (options.IsPresent("-execReturnCodes", true, ref value))
                                        execReturnCodes = (ReturnCodeList)value.Value;

                                    ExitCode? exitCode = null;

                                    if (options.IsPresent("-exitCode", true, ref value))
                                        exitCode = (ExitCode)value.Value;

                                    ExitCode? execExitCode = null;

                                    if (options.IsPresent("-execExitCode", true, ref value))
                                        execExitCode = (ExitCode)value.Value;

                                    MatchMode mode = StringOps.DefaultResultMatchMode;

                                    if (options.IsPresent("-match", ref value))
                                        mode = (MatchMode)value.Value;

                                    bool noCase = false;

                                    if (options.IsPresent("-noCase", true, ref value))
                                        noCase = (bool)value.Value;

                                    RegexOptions regExOptions = TestOps.RegExOptions;

                                    if (options.IsPresent("-regExOptions", true, ref value))
                                        regExOptions = (RegexOptions)value.Value;

                                    bool debug = false;

                                    if (options.IsPresent("-debug", ref value))
                                        debug = (bool)value.Value;

                                    bool visibleSpace = false;

                                    if (options.IsPresent("-visibleSpace", true, ref value))
                                        visibleSpace = (bool)value.Value;

                                    bool trace = false;

                                    if (options.IsPresent("-trace", ref value))
                                        trace = (bool)value.Value;

#if TEST
                                    bool captureTrace = ScriptOps.HasFlags(interpreter,
                                        InterpreterFlags.CaptureTestTraces, true);

                                    if (options.IsPresent("-captureTrace", ref value))
                                        captureTrace = (bool)value.Value;
#endif

                                    bool time = false;

                                    if (options.IsPresent("-time", ref value))
                                        time = (bool)value.Value;

                                    bool noCancel = false;

                                    if (options.IsPresent("-noCancel", true, ref value))
                                        noCancel = (bool)value.Value;

                                    bool globalCancel = false;

                                    if (options.IsPresent("-globalCancel", true, ref value))
                                        globalCancel = (bool)value.Value;

                                    bool noData = false;

                                    if (options.IsPresent("-noData", true, ref value))
                                        noData = (bool)value.Value;

                                    bool noEvent = false;

                                    if (options.IsPresent("-noEvent", true, ref value))
                                        noEvent = (bool)value.Value;

                                    bool noExit = false;

                                    if (options.IsPresent("-noExit", true, ref value))
                                        noExit = (bool)value.Value;

                                    bool noHalt = false;

                                    if (options.IsPresent("-noHalt", true, ref value))
                                        noHalt = (bool)value.Value;

                                    bool noProcessId = false;

                                    if (options.IsPresent("-noProcessId", true, ref value))
                                        noProcessId = (bool)value.Value;

                                    bool noStatistics = false;

                                    if (options.IsPresent("-noStatistics", true, ref value))
                                        noStatistics = (bool)value.Value;

                                    bool noSecurity = false;

                                    if (options.IsPresent("-noSecurity", true, ref value))
                                        noSecurity = (bool)value.Value;

                                    bool noTrack = false;

                                    if (options.IsPresent("-noTrack", true, ref value))
                                        noTrack = (bool)value.Value;

                                    bool noChangeReturnCode = ScriptOps.HasFlags(interpreter,
                                        InterpreterFlags.NoChangeTestReturnCode, true);

                                    if (options.IsPresent("-noChangeReturnCode", true, ref value))
                                        noChangeReturnCode = (bool)value.Value;

                                    IsolationLevel isolationLevel = IsolationLevel.Default;

                                    if (options.IsPresent("-isolationLevel", true, ref value))
                                        isolationLevel = (IsolationLevel)value.Value;

                                    TestPathType isolationPathType = TestPathType.Default;

                                    if (options.IsPresent("-isolationPathType", true, ref value))
                                        isolationPathType = (TestPathType)value.Value;

                                    IsolationDetail isolationPassDetail = IsolationDetail.Default;

                                    if (options.IsPresent("-isolationPassDetail", true, ref value))
                                        isolationPassDetail = (IsolationDetail)value.Value;

                                    IsolationDetail isolationFailDetail = IsolationDetail.Default;

                                    if (options.IsPresent("-isolationFailDetail", true, ref value))
                                        isolationFailDetail = (IsolationDetail)value.Value;

                                    MatchMode ignoreMode = StringOps.DefaultResultMatchMode;

                                    if (options.IsPresent("-ignoreMatch", ref value))
                                        ignoreMode = (MatchMode)value.Value;

                                    StringList ignorePatterns = null;

                                    if (options.IsPresent("-ignorePatterns", true, ref value))
                                        ignorePatterns = (StringList)value.Value;

                                    string isolationTemplate = null;

                                    if (options.IsPresent("-isolationTemplate", true, ref value))
                                        isolationTemplate = value.ToString();

                                    StringList isolationOtherArguments = null;

                                    if (options.IsPresent("-isolationOtherArguments", true, ref value))
                                        isolationOtherArguments = (StringList)value.Value;

                                    StringList isolationLastArguments = null;

                                    if (options.IsPresent("-isolationLastArguments", true, ref value))
                                        isolationLastArguments = (StringList)value.Value;

                                    string isolationFileName = null;

                                    if (options.IsPresent("-isolationFileName", true, ref value))
                                        isolationFileName = value.ToString();

                                    string isolationLogFile = null;

                                    if (options.IsPresent("-isolationLogFile", true, ref value))
                                        isolationLogFile = value.ToString();

                                    bool isolationUnicode = false;

                                    if (options.IsPresent("-isolationUnicode", true, ref value))
                                        isolationUnicode = (bool)value.Value;

                                    bool once = false;

                                    if (options.IsPresent("-once", ref value))
                                        once = (bool)value.Value;

                                    StringList argv = null;

                                    if (options.IsPresent("-argv", ref value))
                                        argv = (StringList)value.Value;

                                    string text = null;

                                    if (options.IsPresent("-text", ref value))
                                        text = value.ToString();

                                    string libraryPath = null;

                                    if (options.IsPresent("-libraryPath", true, ref value))
                                        libraryPath = value.ToString();

#if ISOLATED_INTERPRETERS
                                    string packagePath = null;

                                    if (options.IsPresent("-packagePath", true, ref value))
                                        packagePath = value.ToString();

                                    string baseDirectory = null;

                                    if (options.IsPresent("-baseDirectory", true, ref value))
                                        baseDirectory = value.ToString();

                                    bool useBasePath = true; // TODO: Good default?

                                    if (options.IsPresent("-useBasePath", true, ref value))
                                        useBasePath = (bool)value.Value;

                                    bool verifyCoreAssembly = true; // TODO: Good default?

                                    if (options.IsPresent("-verifyCoreAssembly", true, ref value))
                                        verifyCoreAssembly = (bool)value.Value;

                                    bool useEntryAssembly = true; // TODO: Good default?

                                    if (options.IsPresent("-useEntryAssembly", true, ref value))
                                        useEntryAssembly = (bool)value.Value;

                                    bool optionalEntryAssembly = true; // TODO: Good default?

                                    if (options.IsPresent("-optionalEntryAssembly", true, ref value))
                                        optionalEntryAssembly = (bool)value.Value;
#endif

                                    StringList autoPathList = null;

                                    if (options.IsPresent("-autoPath", true, ref value))
                                        autoPathList = (StringList)value.Value;

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
                                            try
                                            {
                                                int testLevels = interpreter.EnterTestLevel();

                                                try
                                                {
                                                    //
                                                    // NOTE: Create a place to put all the output of the this
                                                    //       command.
                                                    //
                                                    StringBuilder testData = null;

                                                    if (!noData)
                                                        testData = StringBuilderFactory.Create();

                                                    //
                                                    // NOTE: Check the specified test constraints, if any.
                                                    //
                                                    bool skip = false;
                                                    bool fail = true;
                                                    bool ignore = false;
                                                    bool whatIf = false;
                                                    bool knownBug = false;

                                                    code = TestOps.CheckConstraints(
                                                        interpreter, testLevels, name, constraints,
                                                        once, noStatistics || skip, testData, ref skip,
                                                        ref fail, ref whatIf, ref knownBug, ref result);

                                                    //
                                                    // NOTE: Evaluate the constraint expression, if any.
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = TestOps.CheckConstraintExpression(
                                                            interpreter, testLevels, name,
                                                            constraintExpression, noStatistics || skip,
                                                            testData, knownBug, ref skip, ref result);
                                                    }

                                                    //
                                                    // NOTE: If this test is going to be skipped, add to the overall
                                                    //       total now.
                                                    //
                                                    long[] testStatistics = null;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        testStatistics = interpreter.TestStatistics;

                                                        if (!noStatistics &&
                                                            (testStatistics != null) && (testLevels == 1) && skip)
                                                        {
                                                            Interlocked.Increment(ref testStatistics[
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
                                                            interpreter, TestInformationType.CurrentName, name,
                                                            null, true, ref result);
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
                                                        // NOTE: Is security enabled in the current interpreter?  If
                                                        //       so (and the -noSecurity option is not present), make
                                                        //       sure the new interpreter, if any, also ends up with
                                                        //       it enabled.
                                                        //
                                                        bool security = noSecurity ? false : interpreter.HasSecurity();

                                                        //
                                                        // NOTE: Skip running test and reporting results?  This is
                                                        //       currently only used when full process isolation is
                                                        //       enabled.  In that case, the actual test is handled
                                                        //       primarily by the child process.
                                                        //
                                                        bool wasHandled = false;
#if ISOLATED_INTERPRETERS
                                                        AppDomain testAppDomain = null;
                                                        InterpreterHelper testInterpreterHelper = null;
#endif
                                                        Interpreter testInterpreter = null;
                                                        Result isolationResult = null;
                                                        IProfilerState profiler; /* REUSED */
                                                        bool dispose; /* REUSED */

                                                        try
                                                        {
                                                            switch (isolationLevel)
                                                            {
                                                                case IsolationLevel.None:
                                                                    {
                                                                        testInterpreter = interpreter;
                                                                        break;
                                                                    }
                                                                case IsolationLevel.Interpreter:
                                                                    {
                                                                        CreateFlags testCreateFlags = CreateFlags.TestUse |
                                                                            interpreter.FilterCreateFlags(true, true, true);

                                                                        HostCreateFlags testHostCreateFlags = HostCreateFlags.TestUse |
                                                                            interpreter.FilterHostCreateFlags(true);

                                                                        //
                                                                        // HACK: For now, just clone the initialize flags of
                                                                        //       the "parent" interpreter.
                                                                        //
                                                                        InitializeFlags testInitializeFlags = interpreter.InitializeFlags;

                                                                        //
                                                                        // HACK: For now, just clone the script flags of the
                                                                        //       "parent" interpreter.
                                                                        //
                                                                        ScriptFlags testScriptFlags = interpreter.ScriptFlags;

                                                                        //
                                                                        // HACK: For now, just clone the flags of the "parent"
                                                                        //       interpreter.
                                                                        //
                                                                        InterpreterFlags testInterpreterFlags = interpreter.InterpreterFlags;
                                                                        PluginFlags testPluginFlags = interpreter.PluginFlags;

#if NATIVE && TCL
                                                                        FindFlags testFindFlags = interpreter.TclFindFlags;
                                                                        LoadFlags testLoadFlags = interpreter.TclLoadFlags;
#endif

                                                                        testInterpreter = Interpreter.Create(ruleSet,
                                                                            argv, testCreateFlags, testHostCreateFlags,
                                                                            testInitializeFlags, testScriptFlags,
                                                                            testInterpreterFlags, testPluginFlags,
#if NATIVE && TCL
                                                                            testFindFlags, testLoadFlags,
#endif
                                                                            text, libraryPath, autoPathList,
                                                                            ref isolationResult);

                                                                        if ((testInterpreter != null) && security)
                                                                        {
                                                                            code = ScriptOps.EnableOrDisableSecurity(
                                                                                testInterpreter, true, FlagOps.HasFlags(
                                                                                testCreateFlags, CreateFlags.NoVariablesMask,
                                                                                false), ref isolationResult);
                                                                        }
                                                                        break;
                                                                    }
                                                                case IsolationLevel.AppDomain:
                                                                case IsolationLevel.AppDomainOrInterpreter:
                                                                    {
#if ISOLATED_INTERPRETERS
                                                                        if (AppDomainOps.CreateForTest(
                                                                                interpreter, name, baseDirectory,
                                                                                packagePath, clientData, useBasePath,
                                                                                verifyCoreAssembly, useEntryAssembly,
                                                                                optionalEntryAssembly, ref testAppDomain,
                                                                                ref isolationResult) == ReturnCode.Ok)
                                                                        {
                                                                            CreateFlags testCreateFlags = CreateFlags.TestUse |
                                                                                interpreter.FilterCreateFlags(true, true, true);

                                                                            HostCreateFlags testHostCreateFlags = HostCreateFlags.TestUse |
                                                                                interpreter.FilterHostCreateFlags(true);

                                                                            //
                                                                            // HACK: For now, just clone the initialize flags of
                                                                            //       the "parent" interpreter.
                                                                            //
                                                                            InitializeFlags testInitializeFlags = interpreter.InitializeFlags;

                                                                            //
                                                                            // HACK: For now, just clone the script flags of the
                                                                            //       "parent" interpreter.
                                                                            //
                                                                            ScriptFlags testScriptFlags = interpreter.ScriptFlags;

                                                                            //
                                                                            // HACK: For now, just clone the flags of the "parent"
                                                                            //       interpreter.
                                                                            //
                                                                            InterpreterFlags testInterpreterFlags = interpreter.InterpreterFlags;
                                                                            PluginFlags testPluginFlags = interpreter.PluginFlags;

#if NATIVE && TCL
                                                                            FindFlags testFindFlags = interpreter.TclFindFlags;
                                                                            LoadFlags testLoadFlags = interpreter.TclLoadFlags;
#endif

                                                                            testInterpreterHelper = InterpreterHelper.Create(
                                                                                testAppDomain, ruleSet, argv, testCreateFlags,
                                                                                testHostCreateFlags, testInitializeFlags,
                                                                                testScriptFlags, testInterpreterFlags,
                                                                                testPluginFlags,
#if NATIVE && TCL
                                                                                testFindFlags, testLoadFlags,
#endif
                                                                                text, libraryPath,
                                                                                autoPathList, ref isolationResult);

                                                                            if (testInterpreterHelper != null)
                                                                            {
                                                                                testInterpreter = testInterpreterHelper.Interpreter;

                                                                                if (testInterpreter != null)
                                                                                {
                                                                                    if (security)
                                                                                    {
                                                                                        code = ScriptOps.EnableOrDisableSecurity(
                                                                                            testInterpreter, true, FlagOps.HasFlags(
                                                                                            testCreateFlags, CreateFlags.NoVariablesMask,
                                                                                            false), ref isolationResult);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    isolationResult = "interpreter helper has no interpreter";
                                                                                }
                                                                            }
                                                                        }
#else
                                                                        if (isolationLevel == IsolationLevel.AppDomainOrInterpreter)
                                                                        {
                                                                            CreateFlags testCreateFlags = CreateFlags.TestUse |
                                                                                interpreter.FilterCreateFlags(true, true, true);

                                                                            HostCreateFlags testHostCreateFlags = HostCreateFlags.TestUse |
                                                                                interpreter.FilterHostCreateFlags(true);

                                                                            //
                                                                            // HACK: For now, just clone the initialize flags of
                                                                            //       the "parent" interpreter.
                                                                            //
                                                                            InitializeFlags testInitializeFlags = interpreter.InitializeFlags;

                                                                            //
                                                                            // HACK: For now, just clone the script flags of the
                                                                            //       "parent" interpreter.
                                                                            //
                                                                            ScriptFlags testScriptFlags = interpreter.ScriptFlags;

                                                                            //
                                                                            // HACK: For now, just clone the flags of the "parent"
                                                                            //       interpreter.
                                                                            //
                                                                            InterpreterFlags testInterpreterFlags = interpreter.InterpreterFlags;
                                                                            PluginFlags testPluginFlags = interpreter.PluginFlags;

#if NATIVE && TCL
                                                                            FindFlags testFindFlags = interpreter.TclFindFlags;
                                                                            LoadFlags testLoadFlags = interpreter.TclLoadFlags;
#endif

                                                                            testInterpreter = Interpreter.Create(ruleSet,
                                                                                argv, testCreateFlags, testHostCreateFlags,
                                                                                testInitializeFlags, testScriptFlags,
                                                                                testInterpreterFlags, testPluginFlags,
#if NATIVE && TCL
                                                                                testFindFlags, testLoadFlags,
#endif
                                                                                text, libraryPath, autoPathList,
                                                                                ref isolationResult);

                                                                            if ((testInterpreter != null) && security)
                                                                            {
                                                                                code = ScriptOps.EnableOrDisableSecurity(
                                                                                    testInterpreter, true, FlagOps.HasFlags(
                                                                                    testCreateFlags, CreateFlags.NoVariablesMask,
                                                                                    false), ref isolationResult);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            isolationResult = "not implemented";
                                                                        }
#endif
                                                                        break;
                                                                    }
                                                                case IsolationLevel.Process:
                                                                    {
#if SHELL
                                                                        testInterpreter = interpreter;
                                                                        wasHandled = true; /* NOTE: Locally. */

                                                                        if (isolationTemplate != null)
                                                                        {
                                                                            string isolationExecutableName = null;
                                                                            StringList isolationFirstArguments = null;
                                                                            bool isolationUseEntryAssembly = false;

                                                                            if (TestOps.GetIsolatedExecutableName(
                                                                                    testInterpreter, true,
                                                                                    ref isolationExecutableName,
                                                                                    ref isolationFirstArguments,
                                                                                    ref isolationUseEntryAssembly,
                                                                                    ref isolationResult) == ReturnCode.Ok)
                                                                            {
                                                                                if ((isolationFileName != null) ||
                                                                                    TestOps.GetIsolatedFileName(
                                                                                        testInterpreter, isolationPathType,
                                                                                        ref isolationFileName,
                                                                                        ref isolationResult) == ReturnCode.Ok)
                                                                                {
                                                                                    string isolationExecutableArguments = null;

                                                                                    if (TestOps.GetIsolatedExecutableArguments(
                                                                                            testInterpreter, isolationFileName,
                                                                                            isolationLogFile,
                                                                                            isolationFirstArguments,
                                                                                            isolationOtherArguments,
                                                                                            isolationLastArguments,
                                                                                            isolationUseEntryAssembly,
                                                                                            testInterpreter.InternalIsSafe(),
                                                                                            security,
                                                                                            ref isolationExecutableArguments,
                                                                                            ref isolationResult) == ReturnCode.Ok)
                                                                                    {
                                                                                        ArgumentList isolationCommandArguments = null;

                                                                                        if (TestOps.GetIsolatedCommandArguments(
                                                                                                testInterpreter, arguments,
                                                                                                ref isolationCommandArguments,
                                                                                                ref isolationResult) == ReturnCode.Ok)
                                                                                        {
                                                                                            TestOps.AppendFormat(
                                                                                                interpreter, testData, TestOutputType.Start,
                                                                                                "---- {0} start", name);

                                                                                            TestOps.AppendLine(
                                                                                                interpreter, testData, TestOutputType.Start);

                                                                                            File.WriteAllText(
                                                                                                isolationFileName, StringOps.StrMap(
                                                                                                    testInterpreter, MatchMode.Exact,
                                                                                                    isolationTemplate, 0,
                                                                                                    new StringPairList(
                                                                                                        new StringPair(TestOps.TestToken,
                                                                                                            isolationCommandArguments.ToString())),
                                                                                                    SharedStringOps.SystemComparisonType,
                                                                                                    RegexOptions.None, Count.Invalid, false)); /* throw */

                                                                                            //
                                                                                            // NOTE: Used to hold the result of the -body script being
                                                                                            //       executed in the child process.
                                                                                            //
                                                                                            ReturnCode bodyExecCode;
                                                                                            long bodyExecProcessId = 0;
                                                                                            ExitCode bodyExecExitCode = ResultOps.SuccessExitCode();
                                                                                            Result bodyExecResult = null;
                                                                                            Result bodyExecError = null;

                                                                                            profiler = null;
                                                                                            dispose = true;

                                                                                            try
                                                                                            {
                                                                                                if (time)
                                                                                                {
                                                                                                    profiler = ProfilerState.Create(
                                                                                                        interpreter, ref dispose);
                                                                                                }

                                                                                                if (profiler != null)
                                                                                                    profiler.Start();

                                                                                                if (whatIf)
                                                                                                {
                                                                                                    bodyExecCode = ReturnCode.Ok;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    bodyExecCode = ProcessOps.ExecuteProcess(
                                                                                                        testInterpreter, isolationExecutableName,
                                                                                                        isolationExecutableArguments, null,
                                                                                                        EventFlags.All, isolationUnicode,
                                                                                                        ref bodyExecProcessId, ref bodyExecExitCode,
                                                                                                        ref bodyExecResult, ref bodyExecError);
                                                                                                }

                                                                                                if (noProcessId)
                                                                                                    testInterpreter.ResetPreviousProcessId();

                                                                                                if (profiler != null)
                                                                                                {
                                                                                                    profiler.Stop(interpreter.InternalIsSafe());

                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.Time,
                                                                                                        "---- {0} process {1} completed in {2}",
                                                                                                        name, bodyExecProcessId, FormatOps.MaybeNull(
                                                                                                        profiler));

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.Time);
                                                                                                }
                                                                                            }
                                                                                            finally
                                                                                            {
                                                                                                if (profiler != null)
                                                                                                {
                                                                                                    if (dispose)
                                                                                                    {
                                                                                                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                                                                                                            interpreter, ref profiler);
                                                                                                    }

                                                                                                    profiler = null;
                                                                                                }
                                                                                            }

                                                                                            //
                                                                                            // NOTE: Did the child process fail to execute?
                                                                                            //
                                                                                            bool execCodeFailure = !execReturnCodes.Contains(
                                                                                                bodyExecCode);

                                                                                            //
                                                                                            // NOTE: Did the child process return a non-success
                                                                                            //       exit code?
                                                                                            //
                                                                                            bool execExitFailure = (execExitCode != null) &&
                                                                                                (bodyExecExitCode != execExitCode);

                                                                                            //
                                                                                            // NOTE: Was the test a failure in any way?
                                                                                            //
                                                                                            bool execFailure = execCodeFailure || execExitFailure;

                                                                                            //
                                                                                            // NOTE: Did the isolated test appear to pass in the
                                                                                            //       child process?
                                                                                            //
                                                                                            if (!execFailure)
                                                                                            {
                                                                                                //
                                                                                                // PASS: Test ran with no errors (i.e. the isolated
                                                                                                //       process exit code was "success").
                                                                                                //
                                                                                                if (!noStatistics &&
                                                                                                    (testStatistics != null) && (testLevels == 1))
                                                                                                {
                                                                                                    if (whatIf)
                                                                                                    {
                                                                                                        if (knownBug)
                                                                                                        {
                                                                                                            Interlocked.Increment(ref testStatistics[
                                                                                                                    (int)TestInformationType.DisabledBug]);
                                                                                                        }

                                                                                                        Interlocked.Increment(ref testStatistics[
                                                                                                                (int)TestInformationType.Disabled]);
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        if (knownBug)
                                                                                                        {
                                                                                                            Interlocked.Increment(ref testStatistics[
                                                                                                                    (int)TestInformationType.PassedBug]);
                                                                                                        }

                                                                                                        Interlocked.Increment(ref testStatistics[
                                                                                                                (int)TestInformationType.Passed]);
                                                                                                    }

                                                                                                    Interlocked.Increment(ref testStatistics[
                                                                                                            (int)TestInformationType.Total]);
                                                                                                }

                                                                                                TestOps.AppendFormat(
                                                                                                    interpreter, testData, TestOutputType.Pass,
                                                                                                    "++++ {0} {1}", name,
                                                                                                    whatIf ? "DISABLED" : "PASSED");

                                                                                                TestOps.AppendLine(
                                                                                                    interpreter, testData, TestOutputType.Pass);
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                //
                                                                                                // FAIL: Test ran with errors (i.e. the isolated
                                                                                                //       process exit code was not "success").
                                                                                                //
                                                                                                if (!noStatistics &&
                                                                                                    (testStatistics != null) && (testLevels == 1))
                                                                                                {
                                                                                                    if (fail)
                                                                                                    {
                                                                                                        if (knownBug)
                                                                                                        {
                                                                                                            Interlocked.Increment(ref testStatistics[
                                                                                                                    (int)TestInformationType.FailedBug]);
                                                                                                        }

                                                                                                        Interlocked.Increment(ref testStatistics[
                                                                                                                (int)TestInformationType.Failed]);

                                                                                                        Interlocked.Increment(ref testStatistics[
                                                                                                                (int)TestInformationType.Total]);
                                                                                                    }
                                                                                                }

                                                                                                //
                                                                                                // NOTE: Keep track of each test that fails.
                                                                                                //
                                                                                                if (!noStatistics && (testLevels == 1))
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

                                                                                                if (!fail)
                                                                                                    ignore = true;

                                                                                                TestOps.AppendLine(
                                                                                                    interpreter, testData, TestOutputType.Fail);

                                                                                                if (execCodeFailure &&
                                                                                                    TestOps.ShouldShowTestDetail(interpreter,
                                                                                                        isolationPassDetail, isolationFailDetail,
                                                                                                        IsolationDetail.Medium, !execFailure))
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        "---- Test process {0} failed:",
                                                                                                        bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        bodyExecError);

                                                                                                    if ((bodyExecError != null) &&
                                                                                                        (bodyExecError.ErrorInfo != null))
                                                                                                    {
                                                                                                        TestOps.Append(
                                                                                                            interpreter, testData, TestOutputType.Error,
                                                                                                            "---- errorInfo(process): ");

                                                                                                        TestOps.AppendLine(
                                                                                                            interpreter, testData, TestOutputType.Error,
                                                                                                            bodyExecError.ErrorInfo);

                                                                                                        TestOps.Append(
                                                                                                            interpreter, testData, TestOutputType.Error,
                                                                                                            "---- errorCode(process): ");

                                                                                                        TestOps.AppendLine(
                                                                                                            interpreter, testData, TestOutputType.Error,
                                                                                                            bodyExecError.ErrorCode);
                                                                                                    }
                                                                                                }

                                                                                                if (execExitFailure &&
                                                                                                    TestOps.ShouldShowTestDetail(interpreter,
                                                                                                        isolationPassDetail, isolationFailDetail,
                                                                                                        IsolationDetail.Low, !execFailure))
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.Exit,
                                                                                                        "---- Test process {0} exit code was: {1}",
                                                                                                        bodyExecProcessId, bodyExecExitCode);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.Exit);

                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.Exit,
                                                                                                        "---- Test process {0} exit code should have been: ",
                                                                                                        bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.Exit,
                                                                                                        ResultOps.SuccessExitCode().ToString());
                                                                                                }
                                                                                            }

                                                                                            if (TestOps.ShouldShowTestDetail(interpreter,
                                                                                                    isolationPassDetail, isolationFailDetail,
                                                                                                    IsolationDetail.Highest, !execFailure))
                                                                                            {
                                                                                                if (execCodeFailure)
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        "---- {0} no process {1} standard output",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);
                                                                                                }
                                                                                                else if (bodyExecResult != null)
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        "---- {0} begin process {1} standard output",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);

                                                                                                    TestOps.Append(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        bodyExecResult);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);

                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        "---- {0} end process {1} standard output",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdOut,
                                                                                                        "---- {0} null process {1} standard output",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdOut);
                                                                                                }
                                                                                            }

                                                                                            if (TestOps.ShouldShowTestDetail(testInterpreter,
                                                                                                    isolationPassDetail, isolationFailDetail,
                                                                                                    IsolationDetail.High, !execFailure))
                                                                                            {
                                                                                                if (execCodeFailure)
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdErr,
                                                                                                        "---- {0} no process {1} standard error",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdErr);
                                                                                                }
                                                                                                else if (bodyExecError != null)
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdErr,
                                                                                                        "---- {0} begin process {1} standard error",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdErr);

                                                                                                    TestOps.Append(
                                                                                                        interpreter, testData, TestOutputType.StdErr,
                                                                                                        bodyExecError);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdErr);

                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdErr,
                                                                                                        "---- {0} end process {1} standard error",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdErr);
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    TestOps.AppendFormat(
                                                                                                        interpreter, testData, TestOutputType.StdErr,
                                                                                                        "---- {0} null process {1} standard error",
                                                                                                        name, bodyExecProcessId);

                                                                                                    TestOps.AppendLine(
                                                                                                        interpreter, testData, TestOutputType.StdErr);
                                                                                                }
                                                                                            }

                                                                                            if (execFailure)
                                                                                            {
                                                                                                TestOps.AppendFormat(
                                                                                                    interpreter, testData, TestOutputType.Fail,
                                                                                                    "==== {0} {1}", name, fail ? "FAILED" : "IGNORED");

                                                                                                if (!fail)
                                                                                                    ignore = true;

                                                                                                TestOps.AppendLine(
                                                                                                    interpreter, testData, TestOutputType.Fail);
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            isolationResult = "missing isolation template";
                                                                            code = ReturnCode.Error;
                                                                        }
#else
                                                                        isolationResult = "not implemented";
                                                                        code = ReturnCode.Error;
#endif
                                                                        break;
                                                                    }
                                                                case IsolationLevel.Session:
                                                                case IsolationLevel.Machine:
                                                                    {
                                                                        isolationResult = "not implemented";
                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }
                                                                default:
                                                                    {
                                                                        isolationResult = String.Format(
                                                                            "unsupported isolation level \"{0}\"",
                                                                            isolationLevel);

                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }
                                                            }

                                                            //
                                                            // NOTE: Did we succeed at creating the test interpreter,
                                                            //       if necessary -AND- do we still need to actually
                                                            //       run the test (i.e. it was not run via an isolated
                                                            //       process, etc)?
                                                            //
                                                            if ((code == ReturnCode.Ok) &&
                                                                (testInterpreter != null) && !wasHandled)
                                                            {
                                                                //
                                                                // NOTE: Emit tracking information for test scripts?
                                                                //
                                                                bool track = ScriptOps.HasFlags(
                                                                    testInterpreter, InterpreterFlags.TrackTestScripts,
                                                                    true);

                                                                //
                                                                // NOTE: Setup the [temporary] testing-only link between the
                                                                //       test interpreter and the current interpreter.
                                                                //
                                                                if (!Object.ReferenceEquals(testInterpreter, interpreter))
                                                                    testInterpreter.TestTargetInterpreter = interpreter;

                                                                //
                                                                // NOTE: Less than zero means use the default number of test
                                                                //       iterations.
                                                                //
                                                                if (repeatCount < 0)
                                                                    repeatCount = TestOps.DefaultRepeatCount;

                                                                int iterationCount = 0;

                                                                CancelFlags cancelFlags = CancelFlags.Test2;

                                                                if (globalCancel)
                                                                    cancelFlags |= CancelFlags.Global;

                                                                while (iterationCount < repeatCount)
                                                                {
                                                                    //
                                                                    // BUGFIX: Make sure the test interpreter is *still* ready, e.g. if
                                                                    //         we are canceled by an interactive user, etc, make sure to
                                                                    //         actually stop doing the test iterations.
                                                                    //
                                                                    Result readyError = null;

                                                                    code = Interpreter.EngineReady(
                                                                        testInterpreter, ReadyFlags.ViaTest, ref readyError);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        result = readyError;
                                                                        break;
                                                                    }

                                                                    //
                                                                    // BUGFIX: Mark the test as starting now, before the setup script is
                                                                    //         evaluated.
                                                                    //
                                                                    TestOps.AppendFormat(
                                                                        interpreter, testData, TestOutputType.Start,
                                                                        "---- {0} start{1}", name,
                                                                        TestOps.GetRepeatSuffix(
                                                                            iterationCount + 1, repeatCount));

                                                                    TestOps.AppendLine(
                                                                        interpreter, testData, TestOutputType.Start);

                                                                    //
                                                                    // NOTE: Used to hold the result of the -setup, -body, and
                                                                    //       -cleanup scripts, if any.
                                                                    //
                                                                    ReturnCode setupCode;
                                                                    Result setupResult = null;
                                                                    ReturnCode bodyCode = ReturnCode.Ok;
                                                                    Result bodyResult = null;
                                                                    ExitCode bodyExitCode = ResultOps.SuccessExitCode();
                                                                    ReturnCode cleanupCode;
                                                                    Result cleanupResult = null;

                                                                    profiler = null;
                                                                    dispose = true;

                                                                    try
                                                                    {
                                                                        if (time)
                                                                        {
                                                                            profiler = ProfilerState.Create(
                                                                                interpreter, ref dispose);
                                                                        }

                                                                        //
                                                                        // NOTE: Run the setup script, if any.
                                                                        //
                                                                        if (setup != null)
                                                                        {
                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: starting test {0} setup...",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.CommandDebug);
                                                                            }

                                                                            if (profiler != null)
                                                                                profiler.Start();

                                                                            int? savedPreviousLevels;

                                                                            interpreter.MaybeBeginNestedExecution(
                                                                                testInterpreter, out savedPreviousLevels);

                                                                            try
                                                                            {
                                                                                ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                                                    StringList.MakeList(this.Name, "setup", name),
                                                                                    CallFrameFlags.Test);

                                                                                interpreter.PushAutomaticCallFrame(frame);

                                                                                try
                                                                                {
                                                                                    if (!noTrack && track)
                                                                                    {
                                                                                        TestOps.Track(testInterpreter, String.Format(
                                                                                            "ENTER TEST {0} SETUP{1}", FormatOps.WrapOrNull(
                                                                                            name), Environment.NewLine), TestOutputType.Enter);
                                                                                    }

                                                                                    try
                                                                                    {
                                                                                        if (whatIf)
                                                                                        {
                                                                                            setupCode = ReturnCode.Ok;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            setupCode = testInterpreter.EvaluateScript(
                                                                                                setup, setupLocation, ref setupResult);
                                                                                        }
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        if (!noTrack && track)
                                                                                        {
                                                                                            TestOps.Track(testInterpreter, String.Format(
                                                                                                "LEAVE TEST {0} SETUP{1}", FormatOps.WrapOrNull(
                                                                                                name), Environment.NewLine), TestOutputType.Leave);
                                                                                        }
                                                                                    }

                                                                                    if (setupCode == ReturnCode.Error)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.InternalCopyErrorInformation(
                                                                                            VariableFlags.None, false, ref setupResult);
                                                                                    }
                                                                                }
                                                                                finally
                                                                                {
                                                                                    if (noCancel)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetCancel(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noHalt)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetHalt(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noEvent)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.ClearEvents();
                                                                                    }

                                                                                    //
                                                                                    // NOTE: If requested, prevent the interactive loop from
                                                                                    //       actually exiting and reset the exit code to be
                                                                                    //       "success".
                                                                                    //
                                                                                    if (noExit && testInterpreter.ExitNoThrow)
                                                                                    {
                                                                                        testInterpreter.ExitCodeNoThrow = ResultOps.SuccessExitCode();
                                                                                        testInterpreter.ExitNoThrow = false;
                                                                                    }

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
                                                                                setupResult = e;
                                                                                setupCode = ReturnCode.Error;
                                                                            }
                                                                            finally
                                                                            {
                                                                                interpreter.MaybeEndNestedExecution(savedPreviousLevels);

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop(interpreter.InternalIsSafe());

                                                                                    TestOps.AppendFormat(
                                                                                        interpreter, testData, TestOutputType.Time,
                                                                                        "---- {0} setup completed in {1}", name,
                                                                                        FormatOps.MaybeNull(profiler));

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Time);
                                                                                }
                                                                            }

                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: test {0} setup complete",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.CommandDebug);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            setupCode = ReturnCode.Ok;
                                                                        }

                                                                        //
                                                                        // NOTE: Only run the test body if the setup is successful.
                                                                        //
                                                                        if ((setupCode == ReturnCode.Ok) && (body != null))
                                                                        {
                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: starting test {0} body...",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.Command);
                                                                            }

                                                                            if (profiler != null)
                                                                                profiler.Start();

                                                                            int? savedPreviousLevels;

                                                                            interpreter.MaybeBeginNestedExecution(
                                                                                testInterpreter, out savedPreviousLevels);

                                                                            try
                                                                            {
                                                                                ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                                                    StringList.MakeList(this.Name, "body", name),
                                                                                    CallFrameFlags.Test);

                                                                                interpreter.PushAutomaticCallFrame(frame);

                                                                                try
                                                                                {
                                                                                    if (!noTrack && track)
                                                                                    {
                                                                                        TestOps.Track(testInterpreter, String.Format(
                                                                                            "ENTER TEST {0} BODY{1}", FormatOps.WrapOrNull(
                                                                                            name), Environment.NewLine), TestOutputType.Enter);
                                                                                    }

                                                                                    try
                                                                                    {
                                                                                        if (!whatIf)
                                                                                        {
                                                                                            bodyCode = testInterpreter.EvaluateScript(
                                                                                                body, bodyLocation, ref bodyResult);
                                                                                        }
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        if (!noTrack && track)
                                                                                        {
                                                                                            TestOps.Track(testInterpreter, String.Format(
                                                                                                "LEAVE TEST {0} BODY{1}", FormatOps.WrapOrNull(
                                                                                                name), Environment.NewLine), TestOutputType.Leave);
                                                                                        }
                                                                                    }

                                                                                    if ((bodyResult == null) && ScriptOps.HasFlags(
                                                                                            interpreter, InterpreterFlags.TestNullIsEmpty,
                                                                                            true))
                                                                                    {
                                                                                        bodyResult = String.Empty;
                                                                                    }

                                                                                    bodyExitCode = testInterpreter.ExitCodeNoThrow;

                                                                                    if (bodyCode == ReturnCode.Error)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.InternalCopyErrorInformation(
                                                                                            VariableFlags.None, false, ref bodyResult);
                                                                                    }
                                                                                }
                                                                                finally
                                                                                {
                                                                                    if (noCancel)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetCancel(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noHalt)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetHalt(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noEvent)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.ClearEvents();
                                                                                    }

                                                                                    //
                                                                                    // NOTE: If requested, prevent the interactive loop from
                                                                                    //       actually exiting and reset the exit code to be
                                                                                    //       "success".
                                                                                    //
                                                                                    if (noExit && testInterpreter.ExitNoThrow)
                                                                                    {
                                                                                        testInterpreter.ExitCodeNoThrow = ResultOps.SuccessExitCode();
                                                                                        testInterpreter.ExitNoThrow = false;
                                                                                    }

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
                                                                                interpreter.MaybeEndNestedExecution(savedPreviousLevels);

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop(interpreter.InternalIsSafe());

                                                                                    TestOps.AppendFormat(
                                                                                        interpreter, testData, TestOutputType.Time,
                                                                                        "---- {0} body completed in {1}", name,
                                                                                        FormatOps.MaybeNull(profiler));

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Time);
                                                                                }
                                                                            }

                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: test {0} body complete",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.Command);
                                                                            }
                                                                        }

                                                                        //
                                                                        // NOTE: Always run the cleanup script, if any.
                                                                        //
                                                                        if (cleanup != null)
                                                                        {
                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: starting test {0} cleanup...",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.CommandDebug);
                                                                            }

                                                                            if (profiler != null)
                                                                                profiler.Start();

                                                                            int? savedPreviousLevels;

                                                                            interpreter.MaybeBeginNestedExecution(
                                                                                testInterpreter, out savedPreviousLevels);

                                                                            try
                                                                            {
                                                                                ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                                                    StringList.MakeList(this.Name, "cleanup", name),
                                                                                    CallFrameFlags.Test);

                                                                                interpreter.PushAutomaticCallFrame(frame);

                                                                                try
                                                                                {
                                                                                    if (!noTrack && track)
                                                                                    {
                                                                                        TestOps.Track(testInterpreter, String.Format(
                                                                                            "ENTER TEST {0} CLEANUP{1}", FormatOps.WrapOrNull(
                                                                                            name), Environment.NewLine), TestOutputType.Enter);
                                                                                    }

                                                                                    try
                                                                                    {
                                                                                        if (whatIf)
                                                                                        {
                                                                                            cleanupCode = ReturnCode.Ok;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            cleanupCode = testInterpreter.EvaluateScript(
                                                                                                cleanup, cleanupLocation, ref cleanupResult);
                                                                                        }
                                                                                    }
                                                                                    finally
                                                                                    {
                                                                                        if (!noTrack && track)
                                                                                        {
                                                                                            TestOps.Track(testInterpreter, String.Format(
                                                                                                "LEAVE TEST {0} CLEANUP{1}", FormatOps.WrapOrNull(
                                                                                                name), Environment.NewLine), TestOutputType.Leave);
                                                                                        }
                                                                                    }

                                                                                    if (cleanupCode == ReturnCode.Error)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.InternalCopyErrorInformation(
                                                                                            VariableFlags.None, false, ref cleanupResult);
                                                                                    }
                                                                                }
                                                                                finally
                                                                                {
                                                                                    if (noCancel)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetCancel(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noHalt)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        Engine.ResetHalt(testInterpreter, cancelFlags);
                                                                                    }

                                                                                    if (noEvent)
                                                                                    {
                                                                                        /* IGNORED */
                                                                                        testInterpreter.ClearEvents();
                                                                                    }

                                                                                    //
                                                                                    // NOTE: If requested, prevent the interactive loop from
                                                                                    //       actually exiting and reset the exit code to be
                                                                                    //       "success".
                                                                                    //
                                                                                    if (noExit && testInterpreter.ExitNoThrow)
                                                                                    {
                                                                                        testInterpreter.ExitCodeNoThrow = ResultOps.SuccessExitCode();
                                                                                        testInterpreter.ExitNoThrow = false;
                                                                                    }

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
                                                                                cleanupResult = e;
                                                                                cleanupCode = ReturnCode.Error;
                                                                            }
                                                                            finally
                                                                            {
                                                                                interpreter.MaybeEndNestedExecution(savedPreviousLevels);

                                                                                if (profiler != null)
                                                                                {
                                                                                    profiler.Stop(interpreter.InternalIsSafe());

                                                                                    TestOps.AppendFormat(
                                                                                        interpreter, testData, TestOutputType.Time,
                                                                                        "---- {0} cleanup completed in {1}", name,
                                                                                        FormatOps.MaybeNull(profiler));

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Time);
                                                                                }
                                                                            }

                                                                            if (trace)
                                                                            {
                                                                                TraceOps.DebugTrace(String.Format(
                                                                                    "Execute: test {0} cleanup complete",
                                                                                    FormatOps.WrapOrNull(name)),
                                                                                    typeof(Test2).Name, TracePriority.CommandDebug);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            cleanupCode = ReturnCode.Ok;
                                                                        }
                                                                    }
                                                                    finally
                                                                    {
                                                                        if (profiler != null)
                                                                        {
                                                                            if (dispose)
                                                                            {
                                                                                ObjectOps.TryDisposeOrComplain<IProfilerState>(
                                                                                    interpreter, ref profiler);
                                                                            }

                                                                            profiler = null;
                                                                        }
                                                                    }

                                                                    //
                                                                    // NOTE: Grab the IComparer for the test interpreter.
                                                                    //
                                                                    IComparer<string> comparer = testInterpreter.TestComparer;

                                                                    //
                                                                    // NOTE: Did the setup script fail?
                                                                    //
                                                                    bool setupFailure = (setupCode != ReturnCode.Ok);

                                                                    //
                                                                    // NOTE: Did the cleanup script fail?
                                                                    //
                                                                    bool cleanupFailure = (cleanupCode != ReturnCode.Ok);

                                                                    //
                                                                    // NOTE: Did we fail to match the return code?
                                                                    //
                                                                    bool codeFailure = !setupFailure && !returnCodes.Contains(bodyCode);

                                                                    //
                                                                    // NOTE: Did we match exit code, if provided?
                                                                    //
                                                                    bool exitFailure = (exitCode != null) && (bodyExitCode != exitCode);

                                                                    //
                                                                    // NOTE: The virtual output from the StdOut channel.
                                                                    //
                                                                    StringBuilder outputData = null;

                                                                    //
                                                                    // NOTE: The virtual output from the StdErr channel.
                                                                    //
                                                                    StringBuilder errorData = null;

                                                                    //
                                                                    // NOTE: Did we match the StdOut output, if provided?  If not,
                                                                    //       was there some sort of error?
                                                                    //
                                                                    bool outputIgnore = false;
                                                                    bool outputFailure = false;
                                                                    ReturnCode outputCode = ReturnCode.Ok;
                                                                    Result outputResult = null;

                                                                    //
                                                                    // NOTE: Did we match the StdErr output, if provided?  If not,
                                                                    //       was there some sort of error?
                                                                    //
                                                                    bool errorIgnore = false;
                                                                    bool errorFailure = false;
                                                                    ReturnCode errorCode = ReturnCode.Ok;
                                                                    Result errorResult = null;

                                                                    if (!whatIf && (expectedOutput != null))
                                                                    {
                                                                        outputData = testInterpreter.GetChannelVirtualOutput(
                                                                            Channel.StdOut);

                                                                        if (outputData != null)
                                                                        {
                                                                            //
                                                                            // NOTE: Did we match the StdOut output, if provided?
                                                                            //
                                                                            string outputString = outputData.ToString();

                                                                            outputCode = TestOps.Match(
                                                                                testInterpreter, mode, outputString,
                                                                                expectedOutput, noCase, comparer, regExOptions,
                                                                                debug, ref outputFailure, ref outputResult);

                                                                            if (outputCode == ReturnCode.Ok)
                                                                            {
                                                                                outputFailure = !outputFailure;

                                                                                if (TestOps.ShouldIgnoreMismatch(
                                                                                        testInterpreter, ignoreMode,
                                                                                        outputString, ignorePatterns,
                                                                                        noCase, null, regExOptions,
                                                                                        debug))
                                                                                {
                                                                                    outputIgnore = true;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                outputFailure = true;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: There was no StdOut output and we expected some.
                                                                            //       This is a failure.
                                                                            //
                                                                            outputFailure = true;
                                                                        }
                                                                    }

                                                                    if (!whatIf && (expectedError != null))
                                                                    {
                                                                        errorData = testInterpreter.GetChannelVirtualOutput(
                                                                            Channel.StdErr);

                                                                        if (errorData != null)
                                                                        {
                                                                            //
                                                                            // NOTE: Did we match the StdErr output, if provided?
                                                                            //
                                                                            string errorString = errorData.ToString();

                                                                            errorCode = TestOps.Match(
                                                                                testInterpreter, mode, errorString,
                                                                                expectedError, noCase, comparer, regExOptions,
                                                                                debug, ref errorFailure, ref errorResult);

                                                                            if (errorCode == ReturnCode.Ok)
                                                                            {
                                                                                errorFailure = !errorFailure;

                                                                                if (TestOps.ShouldIgnoreMismatch(
                                                                                        testInterpreter, ignoreMode,
                                                                                        errorString, ignorePatterns,
                                                                                        noCase, null, regExOptions,
                                                                                        debug))
                                                                                {
                                                                                    errorIgnore = true;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                errorFailure = true;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: There was no StdErr output and we expected some.
                                                                            //       This is a failure.
                                                                            //
                                                                            errorFailure = true;
                                                                        }
                                                                    }

                                                                    //
                                                                    // NOTE: Does the actual result match the expected result?
                                                                    //
                                                                    bool scriptIgnore = false;
                                                                    bool scriptFailure = false;
                                                                    ReturnCode scriptCode = ReturnCode.Ok;
                                                                    Result scriptResult = null;

                                                                    if (!whatIf && (expectedResult != null))
                                                                    {
                                                                        scriptCode = TestOps.Match(
                                                                            testInterpreter, mode, bodyResult,
                                                                            expectedResult, noCase, comparer, regExOptions,
                                                                            debug, ref scriptFailure, ref scriptResult);

                                                                        if (scriptCode == ReturnCode.Ok)
                                                                        {
                                                                            scriptFailure = !scriptFailure;

                                                                            if (TestOps.ShouldIgnoreMismatch(
                                                                                    testInterpreter, ignoreMode,
                                                                                    bodyResult, ignorePatterns,
                                                                                    noCase, null, regExOptions,
                                                                                    debug))
                                                                            {
                                                                                scriptIgnore = true;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            scriptFailure = true;
                                                                        }
                                                                    }

                                                                    //
                                                                    // NOTE: If any of the important things failed, the test fails.
                                                                    //
                                                                    if (!(setupFailure || cleanupFailure || outputFailure ||
                                                                            errorFailure || codeFailure || scriptFailure ||
                                                                            exitFailure))
                                                                    {
                                                                        //
                                                                        // PASS: Test ran with no errors and the results match
                                                                        //       what we expected.
                                                                        //
                                                                        if (!noStatistics &&
                                                                            (testStatistics != null) && (testLevels == 1))
                                                                        {
                                                                            if (whatIf)
                                                                            {
                                                                                if (knownBug)
                                                                                {
                                                                                    Interlocked.Increment(ref testStatistics[
                                                                                            (int)TestInformationType.DisabledBug]);
                                                                                }

                                                                                Interlocked.Increment(ref testStatistics[
                                                                                        (int)TestInformationType.Disabled]);
                                                                            }
                                                                            else
                                                                            {
                                                                                if (knownBug)
                                                                                {
                                                                                    Interlocked.Increment(ref testStatistics[
                                                                                            (int)TestInformationType.PassedBug]);
                                                                                }

                                                                                Interlocked.Increment(ref testStatistics[
                                                                                        (int)TestInformationType.Passed]);
                                                                            }

                                                                            Interlocked.Increment(ref testStatistics[
                                                                                    (int)TestInformationType.Total]);
                                                                        }

                                                                        TestOps.AppendFormat(
                                                                            interpreter, testData, TestOutputType.Pass,
                                                                            "++++ {0} {1}{2}", name,
                                                                            whatIf ? "DISABLED" : "PASSED",
                                                                            TestOps.GetRepeatSuffix(
                                                                                iterationCount + 1, repeatCount));

                                                                        TestOps.AppendLine(
                                                                            interpreter, testData, TestOutputType.Pass);
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // FAIL: Test ran with errors or the result does not match
                                                                        //       what we expected.
                                                                        //
                                                                        if (!noStatistics &&
                                                                            (testStatistics != null) && (testLevels == 1))
                                                                        {
                                                                            if (!TestOps.ShouldIgnoreFailure(
                                                                                    fail, outputIgnore, errorIgnore,
                                                                                    scriptIgnore))
                                                                            {
                                                                                if (knownBug)
                                                                                {
                                                                                    Interlocked.Increment(ref testStatistics[
                                                                                            (int)TestInformationType.FailedBug]);
                                                                                }

                                                                                Interlocked.Increment(ref testStatistics[
                                                                                        (int)TestInformationType.Failed]);

                                                                                Interlocked.Increment(ref testStatistics[
                                                                                        (int)TestInformationType.Total]);
                                                                            }
                                                                        }

                                                                        //
                                                                        // NOTE: Keep track of each test that fails.
                                                                        //
                                                                        if (!noStatistics && (testLevels == 1))
                                                                        {
                                                                            TestOps.RecordInformation(
                                                                                interpreter, TestInformationType.FailedNames,
                                                                                name, null, true);
                                                                        }

                                                                        TestOps.AppendLine(
                                                                            interpreter, testData, TestOutputType.Fail);

                                                                        TestOps.AppendFormat(
                                                                            interpreter, testData, TestOutputType.Fail,
                                                                            "==== {0} {1} {2}{3}", name, description.Trim(),
                                                                            !TestOps.ShouldIgnoreFailure(
                                                                                fail, outputIgnore, errorIgnore,
                                                                                scriptIgnore) ? "FAILED" : "IGNORED",
                                                                            TestOps.GetRepeatSuffix(
                                                                                iterationCount + 1, repeatCount));

                                                                        if (TestOps.ShouldIgnoreFailure(
                                                                                fail, outputIgnore, errorIgnore,
                                                                                scriptIgnore))
                                                                        {
                                                                            ignore = true;
                                                                        }

                                                                        TestOps.AppendLine(
                                                                            interpreter, testData, TestOutputType.Fail);

                                                                        if (body != null)
                                                                        {
                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Body,
                                                                                "==== Contents of test case:");

                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Body,
                                                                                body);
                                                                        }

                                                                        if (setupFailure)
                                                                        {
                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Reason,
                                                                                "---- Test setup failed:");

                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Reason,
                                                                                setupResult);

                                                                            if ((setupResult != null) &&
                                                                                (setupResult.ErrorInfo != null))
                                                                            {
                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    "---- errorInfo(setup): ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    setupResult.ErrorInfo);

                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    "---- errorCode(setup): ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    setupResult.ErrorCode);
                                                                            }
                                                                        }

                                                                        if (scriptFailure)
                                                                        {
                                                                            if (scriptCode == ReturnCode.Ok)
                                                                            {
                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Result was:");

                                                                                if (visibleSpace)
                                                                                {
                                                                                    bodyResult = TestOps.MakeWhiteSpaceVisible(
                                                                                        bodyResult);
                                                                                }

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    bodyResult);

                                                                                TestOps.AppendFormat(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Result should have been ({0} matching):",
                                                                                    mode);

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason);

                                                                                if (visibleSpace)
                                                                                {
                                                                                    expectedResult = TestOps.MakeWhiteSpaceVisible(
                                                                                        expectedResult);
                                                                                }

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

                                                                        if (outputFailure)
                                                                        {
                                                                            if (outputCode == ReturnCode.Ok)
                                                                            {
                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Output was:");

                                                                                if (visibleSpace)
                                                                                    TestOps.MakeWhiteSpaceVisible(outputData);

                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    outputData);

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason);

                                                                                TestOps.AppendFormat(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Output should have been ({0} matching):",
                                                                                    mode);

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason);

                                                                                if (visibleSpace)
                                                                                {
                                                                                    expectedOutput = TestOps.MakeWhiteSpaceVisible(
                                                                                        expectedOutput);
                                                                                }

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    expectedOutput);
                                                                            }
                                                                            else
                                                                            {
                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Error testing output: ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    outputResult);

                                                                                if ((outputResult != null) &&
                                                                                    (outputResult.ErrorInfo != null))
                                                                                {
                                                                                    TestOps.Append(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        "---- errorInfo(matchOutput): ");

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        outputResult.ErrorInfo);

                                                                                    TestOps.Append(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        "---- errorCode(matchOutput): ");

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        outputResult.ErrorCode);
                                                                                }
                                                                            }
                                                                        }

                                                                        if (errorFailure)
                                                                        {
                                                                            if (errorCode == ReturnCode.Ok)
                                                                            {
                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Error output was:");

                                                                                if (visibleSpace)
                                                                                    TestOps.MakeWhiteSpaceVisible(errorData);

                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    errorData);

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason);

                                                                                TestOps.AppendFormat(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Error output should have been ({0} matching):",
                                                                                    mode);

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason);

                                                                                if (visibleSpace)
                                                                                {
                                                                                    expectedError = TestOps.MakeWhiteSpaceVisible(
                                                                                        expectedError);
                                                                                }

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    expectedError);
                                                                            }
                                                                            else
                                                                            {
                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Error testing errorOutput: ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    errorResult);

                                                                                if ((errorResult != null) &&
                                                                                    (errorResult.ErrorInfo != null))
                                                                                {
                                                                                    TestOps.Append(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        "---- errorInfo(matchError): ");

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        errorResult.ErrorInfo);

                                                                                    TestOps.Append(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        "---- errorCode(matchError): ");

                                                                                    TestOps.AppendLine(
                                                                                        interpreter, testData, TestOutputType.Error,
                                                                                        errorResult.ErrorCode);
                                                                                }
                                                                            }
                                                                        }

                                                                        if (cleanupFailure)
                                                                        {
                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Reason,
                                                                                "---- Test cleanup failed:");

                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Reason,
                                                                                cleanupResult);

                                                                            if ((cleanupResult != null) &&
                                                                                (cleanupResult.ErrorInfo != null))
                                                                            {
                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    "---- errorInfo(cleanup): ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    cleanupResult.ErrorInfo);

                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    "---- errorCode(cleanup): ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Error,
                                                                                    cleanupResult.ErrorCode);
                                                                            }
                                                                        }

                                                                        if (exitFailure)
                                                                        {
                                                                            TestOps.AppendFormat(
                                                                                interpreter, testData, TestOutputType.Reason,
                                                                                "---- Exit code was: {1}", bodyExitCode);

                                                                            TestOps.AppendLine(
                                                                                interpreter, testData, TestOutputType.Reason);

                                                                            if (exitCode != null)
                                                                            {
                                                                                TestOps.Append(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    "---- Exit code should have been: ");

                                                                                TestOps.AppendLine(
                                                                                    interpreter, testData, TestOutputType.Reason,
                                                                                    exitCode.ToString());
                                                                            }
                                                                        }

                                                                        TestOps.AppendFormat(
                                                                            interpreter, testData, TestOutputType.Fail,
                                                                            "==== {0} {1}{2}", name,
                                                                            !TestOps.ShouldIgnoreFailure(
                                                                                fail, outputIgnore, errorIgnore,
                                                                                scriptIgnore) ? "FAILED" : "IGNORED",
                                                                            TestOps.GetRepeatSuffix(
                                                                                iterationCount + 1, repeatCount));

                                                                        if (TestOps.ShouldIgnoreFailure(
                                                                                fail, outputIgnore, errorIgnore,
                                                                                scriptIgnore))
                                                                        {
                                                                            ignore = true;
                                                                        }

                                                                        TestOps.AppendLine(
                                                                            interpreter, testData, TestOutputType.Fail);
                                                                    }

                                                                    iterationCount++;
                                                                }
                                                            }
                                                            else if (!wasHandled)
                                                            {
                                                                //
                                                                // FAIL: Test did not run because we failed to setup the
                                                                //       isolated test interpreter.
                                                                //
                                                                if (!noStatistics &&
                                                                    (testStatistics != null) && (testLevels == 1))
                                                                {
                                                                    if (fail)
                                                                    {
                                                                        if (knownBug)
                                                                        {
                                                                            Interlocked.Increment(ref testStatistics[
                                                                                    (int)TestInformationType.FailedBug]);
                                                                        }

                                                                        Interlocked.Increment(ref testStatistics[
                                                                                (int)TestInformationType.Failed]);

                                                                        Interlocked.Increment(ref testStatistics[
                                                                                (int)TestInformationType.Total]);
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: Keep track of each test that fails.
                                                                //
                                                                if (!noStatistics && (testLevels == 1))
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

                                                                if (!fail)
                                                                    ignore = true;

                                                                TestOps.AppendLine(
                                                                    interpreter, testData, TestOutputType.Fail);

                                                                TestOps.AppendFormat(
                                                                    interpreter, testData, TestOutputType.Reason,
                                                                    "---- Test isolation failed ({0} level):",
                                                                    isolationLevel);

                                                                TestOps.AppendLine(
                                                                    interpreter, testData, TestOutputType.Reason);

                                                                TestOps.AppendLine(
                                                                    interpreter, testData, TestOutputType.Reason,
                                                                    isolationResult);

                                                                if ((isolationResult != null) &&
                                                                    (isolationResult.ErrorInfo != null))
                                                                {
                                                                    TestOps.Append(
                                                                        interpreter, testData, TestOutputType.Error,
                                                                        "---- errorInfo(isolation): ");

                                                                    TestOps.AppendLine(
                                                                        interpreter, testData, TestOutputType.Error,
                                                                        isolationResult.ErrorInfo);

                                                                    TestOps.Append(
                                                                        interpreter, testData, TestOutputType.Error,
                                                                        "---- errorCode(isolation): ");

                                                                    TestOps.AppendLine(
                                                                        interpreter, testData, TestOutputType.Error,
                                                                        isolationResult.ErrorCode);
                                                                }
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            //
                                                            // NOTE: If a new interpreter was used, dispose it now.
                                                            //
                                                            if ((testInterpreter != null) &&
                                                                !Object.ReferenceEquals(testInterpreter, interpreter))
                                                            {
                                                                ObjectOps.TryDisposeOrComplain<Interpreter>(
                                                                    interpreter, ref testInterpreter);

                                                                testInterpreter = null;
                                                            }

#if ISOLATED_INTERPRETERS
                                                            if (testInterpreterHelper != null)
                                                            {
                                                                ObjectOps.TryDisposeOrComplain<InterpreterHelper>(
                                                                    interpreter, ref testInterpreterHelper);
                                                            }

                                                            if (testAppDomain != null)
                                                            {
                                                                AppDomainOps.UnloadOrComplain(
                                                                    interpreter, name, testAppDomain,
                                                                    clientData);
                                                            }
#endif
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

                                                        //
                                                        // NOTE: The result is the complete output produced by the
                                                        //       entire test.
                                                        //
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
                                                finally
                                                {
                                                    interpreter.ExitTestLevel();
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
                                                try
                                                {
                                                    //
                                                    // NOTE: If we created a temporary file, always delete it
                                                    //       prior to returning from this method.
                                                    //
                                                    if (isolationFileName != null)
                                                        File.Delete(isolationFileName); /* throw */
                                                }
                                                catch (Exception e)
                                                {
                                                    TraceOps.DebugTrace(
                                                        e, typeof(Test2).Name, TracePriority.CommandError);
                                                }
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
                                    "wrong # args: should be \"{0} name description ?options?\"",
                                    this.Name);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "wrong # args: should be \"{0} name description ?options?\"",
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
