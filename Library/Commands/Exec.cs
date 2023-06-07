/*
 * Exec.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("f622148a-93e0-4fbc-9645-e2ead4e5483b")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Exec : Core
    {
        public Exec(
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
                    if (arguments.Count >= 2)
                    {
                        ObjectFlags objectFlags =
                            ObjectOps.GetDefaultObjectFlags() | ObjectFlags.NoDispose;

                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-debug", null),              // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nonormalize", null),        // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-noellipsis", null),         // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-commandline", null),        // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-dequote", null),            // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-quoteall", null),           // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-unicode", null),            // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-ignorestderr", null),       // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-overridecapture", null),    // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-killonerror", null),        // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-keepnewline", null),        // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-noexitcode", null),         // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nocapture", null),          // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nocaptureinput", null),     // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nocaptureoutput", null),    // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-shell", null),              // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nocarriagereturns", null),  // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-setall", null),             // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-trimall", null),            // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-noevents", null),           // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nosleep", null),            // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-nointerpreter", null),      // simple switch
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, "-userinterface", null),      // simple switch
                            new Option(typeof(ExitCode), OptionFlags.MustHaveEnumValue,
                                Index.Invalid, Index.Invalid, "-success", null), // success exit code
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-domainname", null), // domain name
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-username", null), // domain user name
                            new Option(null, OptionFlags.MustHaveSecureStringValue, Index.Invalid,
                                Index.Invalid, "-password", null), // domain password
                            new Option(null, OptionFlags.MustHaveListValue, Index.Invalid,
                                Index.Invalid, "-preprocessarguments", null), // command
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-directory", null), // working directory name
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-processid", null), // varName for processId
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-exitcode", null), // varName for exitCode
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-stdin", null),  // varName for StdIn input
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-stdout", null), // varName for StdOut output
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-stderr", null), // varName for StdErr output
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                                Index.Invalid, "-stdinobject", null),    // varName for StdIn object
                            new Option(null, OptionFlags.MustHaveCallbackValue, Index.Invalid,
                                Index.Invalid, "-startcallback", null),  // callback for process start
                            new Option(null, OptionFlags.MustHaveCallbackValue, Index.Invalid,
                                Index.Invalid, "-stdoutcallback", null), // callback for StdOut output
                            new Option(null, OptionFlags.MustHaveCallbackValue, Index.Invalid,
                                Index.Invalid, "-stderrcallback", null), // callback for StdErr output
                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid,
                                Index.Invalid, "-timeout", null), // exit wait milliseconds
                            new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                                Index.Invalid, Index.Invalid, "-objectflags",
                                new Variant(objectFlags)), // for FixupReturnValue, etc.
                            new Option(typeof(EventFlags), OptionFlags.MustHaveEnumValue,
                                Index.Invalid, Index.Invalid, "-eventflags",
                                new Variant(interpreter.EngineEventFlags)), // for [after], etc.
                            new Option(typeof(ProcessWindowStyle), OptionFlags.MustHaveEnumValue,
                                Index.Invalid, Index.Invalid, "-windowstyle",
                                new Variant(ProcessWindowStyle.Normal)), // minimized, etc.
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1,
                            Index.Invalid, true, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if (argumentIndex != Index.Invalid)
                            {
                                bool noInterpreter = false;

                                if (options.IsPresent("-nointerpreter"))
                                    noInterpreter = true;

                                bool noEvents = false;

                                if (options.IsPresent("-noevents"))
                                    noEvents = true;

                                bool noSleep = false;

                                if (options.IsPresent("-nosleep"))
                                    noSleep = true;

                                bool userInterface = false;

                                if (options.IsPresent("-userinterface"))
                                    userInterface = true;

                                bool debug = false;

                                if (options.IsPresent("-debug"))
                                    debug = true;

                                bool normalize = true;

                                if (options.IsPresent("-nonormalize"))
                                    normalize = false;

                                bool ellipsis = true;

                                if (options.IsPresent("-noellipsis"))
                                    ellipsis = false;

                                TracePriority priority = TracePriority.CommandDebug;

                                if (debug)
                                    TraceOps.ExternalAdjustTracePriority(ref priority, 1);

                                bool commandLine = false;

                                if (options.IsPresent("-commandline"))
                                    commandLine = true;

                                bool dequote = false;

                                if (options.IsPresent("-dequote"))
                                    dequote = true;

                                bool quoteAll = false;

                                if (options.IsPresent("-quoteall"))
                                    quoteAll = true;

                                bool captureExitCode = true;

                                if (options.IsPresent("-noexitcode"))
                                    captureExitCode = false;

                                bool captureInput = true;
                                bool captureOutput = true;

                                if (options.IsPresent("-nocapture"))
                                {
                                    captureInput = false;
                                    captureOutput = false;
                                }

                                if (options.IsPresent("-nocaptureinput"))
                                    captureInput = false;

                                if (options.IsPresent("-nocaptureoutput"))
                                    captureOutput = false;

                                bool useUnicode = false;

                                if (options.IsPresent("-unicode"))
                                    useUnicode = true;

                                bool ignoreStdErr = false;

                                if (options.IsPresent("-ignorestderr"))
                                    ignoreStdErr = true;

                                bool overrideCapture = false;

                                if (options.IsPresent("-overridecapture"))
                                    overrideCapture = true;

                                bool killOnError = false;

                                if (options.IsPresent("-killonerror"))
                                    killOnError = true;

                                bool keepNewLine = false;

                                if (options.IsPresent("-keepnewline"))
                                    keepNewLine = true;

                                bool carriageReturns = true;

                                if (options.IsPresent("-nocarriagereturns"))
                                    carriageReturns = false;

                                bool setAll = false;

                                if (options.IsPresent("-setall"))
                                    setAll = true;

                                bool trimAll = false;

                                if (options.IsPresent("-trimall"))
                                    trimAll = true;

                                bool useShellExecute = false;

                                if (options.IsPresent("-shell"))
                                    useShellExecute = true;

                                Variant value = null;

                                if (options.IsPresent("-objectflags", ref value))
                                    objectFlags = (ObjectFlags)value.Value;

                                ExitCode? successExitCode = null;

                                if (options.IsPresent("-success", ref value))
                                    successExitCode = (ExitCode)value.Value;

                                string domainName = null;

                                if (options.IsPresent("-domainname", ref value))
                                    domainName = value.ToString();

                                string userName = null;

                                if (options.IsPresent("-username", ref value))
                                    userName = value.ToString();

                                SecureString password = null;

                                if (options.IsPresent("-password", ref value))
                                    password = (SecureString)value.Value;

                                string directory = null;

                                if (options.IsPresent("-directory", ref value))
                                    directory = value.ToString();

                                string processIdVarName = null;

                                if (options.IsPresent("-processid", ref value))
                                    processIdVarName = value.ToString();

                                string exitCodeVarName = null;

                                if (options.IsPresent("-exitcode", ref value))
                                    exitCodeVarName = value.ToString();

                                string stdInVarName = null;

                                if (options.IsPresent("-stdin", ref value))
                                    stdInVarName = value.ToString();

                                string stdOutVarName = null;

                                if (options.IsPresent("-stdout", ref value))
                                    stdOutVarName = value.ToString();

                                string stdErrVarName = null;

                                if (options.IsPresent("-stderr", ref value))
                                    stdErrVarName = value.ToString();

                                string stdInObjectVarName = null;

                                if (options.IsPresent("-stdinobject", ref value))
                                    stdInObjectVarName = value.ToString();

                                ICallback startCallback = null;

                                if (options.IsPresent("-startcallback", ref value))
                                    startCallback = (ICallback)value.Value;

                                ICallback stdOutCallback = null;

                                if (options.IsPresent("-stdoutcallback", ref value))
                                    stdOutCallback = (ICallback)value.Value;

                                ICallback stdErrCallback = null;

                                if (options.IsPresent("-stderrcallback", ref value))
                                    stdErrCallback = (ICallback)value.Value;

                                EventFlags eventFlags = interpreter.EngineEventFlags;

                                if (options.IsPresent("-eventflags", ref value))
                                    eventFlags = (EventFlags)value.Value;

                                ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal;

                                if (options.IsPresent("-windowstyle", ref value))
                                    windowStyle = (ProcessWindowStyle)value.Value;

                                int? timeout = null;

                                if (options.IsPresent("-timeout", ref value))
                                    timeout = (int)value.Value;

                                StringList list = null;

                                if (options.IsPresent("-preprocessarguments", ref value))
                                    list = (StringList)value.Value;

                                int argumentStopIndex = arguments.Count - 1;
                                bool background = false;

                                if (SharedStringOps.SystemEquals(
                                        arguments[arguments.Count - 1],
                                        Characters.Ampersand.ToString()))
                                {
                                    argumentStopIndex--;
                                    background = true;
                                }

                                string execFileName = arguments[argumentIndex];

                                if (!PathOps.IsRemoteUri(execFileName))
                                    execFileName = PathOps.GetNativePath(execFileName);

                                string execArguments = null;

                                if ((argumentIndex + 1) < arguments.Count)
                                {
                                    if (commandLine)
                                    {
                                        execArguments = RuntimeOps.BuildCommandLine(
                                            ArgumentList.GetRangeAsStringList(arguments,
                                                argumentIndex + 1, argumentStopIndex,
                                                dequote),
                                            quoteAll);
                                    }
                                    else
                                    {
                                        execArguments = ListOps.Concat(arguments,
                                            argumentIndex + 1, argumentStopIndex);
                                    }
                                }

                                Result input = null;
                                IObject inputObject = null;
                                DataReceivedEventHandler outputHandler = null;
                                DataReceivedEventHandler errorHandler = null;
                                EventHandler startHandler = null;

                                code = ProcessOps.HandleCaptureOptions(
                                    interpreter, options, startCallback, stdOutCallback,
                                    stdErrCallback, stdInVarName, stdInObjectVarName,
                                    objectFlags, captureInput, captureOutput, ref input,
                                    ref inputObject, ref outputHandler, ref errorHandler,
                                    ref startHandler, ref result);

                                if (debug)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "Execute: interpreter = {0}, domainName = {1}, userName = {2}, " +
                                        "password = {3}, execFileName = {4}, execArguments = {5}, " +
                                        "directory = {6}, input = {7}, inputObject = {8}, windowStyle = {9}, " +
                                        "eventFlags = {10}, timeout = {11}, noInterpreter = {12}, debug = {13}, " +
                                        "commandLine = {14}, dequote = {15}, quoteAll = {16}, " +
                                        "useShellExecute = {17}, captureExitCode = {18}, captureInput = {19}, " +
                                        "captureOutput = {20}, useUnicode = {21}, ignoreStdErr = {22}, " +
                                        "overrideCapture = {23}, userInterface = {24}, noSleep = {25}, " +
                                        "killOnError = {26}, keepNewLine = {27}, carriageReturns = {28}, " +
                                        "trimAll = {29}, background = {30}, noEvents = {31}, successExitCode = {32}, " +
                                        "processIdVarName = {33}, exitCodeVarName = {34}, stdInVarName = {35}, " +
                                        "stdInObjectVarName = {36}, stdOutVarName = {37}, stdErrVarName = {38}, " +
                                        "startCallback = {39}, stdOutCallback = {40}, stdErrCallback = {41}, " +
                                        "startHandler = {42}, outputHandler = {43}, errorHandler = {44}",
                                        FormatOps.InterpreterNoThrow(interpreter), FormatOps.WrapOrNull(domainName),
                                        FormatOps.WrapOrNull(userName), FormatOps.WrapOrNull(password),
                                        FormatOps.WrapOrNull(execFileName), FormatOps.WrapOrNull(execArguments),
                                        FormatOps.WrapOrNull(directory), FormatOps.WrapOrNull(input),
                                        FormatOps.WrapOrNull(inputObject), FormatOps.WrapOrNull(windowStyle),
                                        FormatOps.WrapOrNull(eventFlags), FormatOps.WrapOrNull(timeout),
                                        noInterpreter, debug, commandLine, dequote, quoteAll, useShellExecute,
                                        captureExitCode, captureInput, captureOutput, useUnicode, ignoreStdErr,
                                        overrideCapture, userInterface, noSleep, killOnError, keepNewLine,
                                        carriageReturns, trimAll, background, noEvents,
                                        FormatOps.WrapOrNull(successExitCode), FormatOps.WrapOrNull(processIdVarName),
                                        FormatOps.WrapOrNull(exitCodeVarName), FormatOps.WrapOrNull(stdInVarName),
                                        FormatOps.WrapOrNull(stdInObjectVarName), FormatOps.WrapOrNull(stdOutVarName),
                                        FormatOps.WrapOrNull(stdErrVarName), FormatOps.WrapOrNull(startCallback),
                                        FormatOps.WrapOrNull(stdOutCallback), FormatOps.WrapOrNull(stdErrCallback),
                                        FormatOps.WrapOrNull(startHandler), FormatOps.WrapOrNull(outputHandler),
                                        FormatOps.WrapOrNull(errorHandler)), typeof(Exec).Name, priority);
                                }

                                long processId = 0;
                                bool attempted = false;
                                ExitCode exitCode = ResultOps.SuccessExitCode();
                                Result error = null;

                                if (code == ReturnCode.Ok)
                                {
                                    bool done = false;

                                    code = ProcessOps.PreProcessArguments(
                                        interpreter, list, execFileName, directory,
                                        ref execArguments, ref done, ref result);

                                    if (done)
                                        goto done;

                                    if (code == ReturnCode.Ok)
                                    {
                                        result = null; /* FAIL-SAFE */

                                        code = ProcessOps.ExecuteProcess(
                                            noInterpreter ? null : interpreter, domainName, userName,
                                            password, execFileName, execArguments, directory, input,
                                            inputObject, startHandler, outputHandler, errorHandler,
                                            windowStyle, eventFlags, timeout, useShellExecute,
                                            captureExitCode, captureOutput, useUnicode, ignoreStdErr,
                                            overrideCapture, userInterface, noSleep, killOnError,
                                            keepNewLine, background, !noEvents && !background,
                                            ref processId, ref exitCode, ref result, ref error);

                                        attempted = true; /* probably? */
                                    }
                                }

                            done:

                                if (debug)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "Execute: interpreter = {0}, domainName = {1}, userName = {2}, " +
                                        "password = {3}, execFileName = {4}, execArguments = {5}, " +
                                        "directory = {6}, input = {7}, inputObject = {8}, windowStyle = {9}, " +
                                        "eventFlags = {10}, timeout = {11}, noInterpreter = {12}, debug = {13}, " +
                                        "commandLine = {14}, dequote = {15}, quoteAll = {16}, " +
                                        "useShellExecute = {17}, captureExitCode = {18}, captureInput = {19}, " +
                                        "captureOutput = {20}, useUnicode = {21}, ignoreStdErr = {22}, " +
                                        "overrideCapture = {23}, userInterface = {24}, noSleep = {25}, " +
                                        "killOnError = {26}, keepNewLine = {27}, carriageReturns = {28}, " +
                                        "trimAll = {29}, background = {30}, noEvents = {31}, successExitCode = {32}, " +
                                        "processIdVarName = {33}, exitCodeVarName = {34}, stdInVarName = {35}, " +
                                        "stdInObjectVarName = {36}, stdOutVarName = {37}, stdErrVarName = {38}, " +
                                        "startCallback = {39}, stdOutCallback = {40}, stdErrCallback = {41}, " +
                                        "startHandler = {42}, outputHandler = {43}, errorHandler = {44}, " +
                                        "processId = {45}, exitCode = {46}, result = {47}, error = {48}",
                                        FormatOps.InterpreterNoThrow(interpreter), FormatOps.WrapOrNull(domainName),
                                        FormatOps.WrapOrNull(userName), FormatOps.WrapOrNull(password),
                                        FormatOps.WrapOrNull(execFileName), FormatOps.WrapOrNull(execArguments),
                                        FormatOps.WrapOrNull(directory), FormatOps.WrapOrNull(input),
                                        FormatOps.WrapOrNull(inputObject), FormatOps.WrapOrNull(windowStyle),
                                        FormatOps.WrapOrNull(eventFlags), FormatOps.WrapOrNull(timeout),
                                        noInterpreter, debug, commandLine, dequote, quoteAll, useShellExecute,
                                        captureExitCode, captureInput, captureOutput, useUnicode, ignoreStdErr,
                                        overrideCapture, userInterface, noSleep, killOnError, keepNewLine,
                                        carriageReturns, trimAll, background, noEvents,
                                        FormatOps.WrapOrNull(successExitCode), FormatOps.WrapOrNull(processIdVarName),
                                        FormatOps.WrapOrNull(exitCodeVarName), FormatOps.WrapOrNull(stdInVarName),
                                        FormatOps.WrapOrNull(stdInObjectVarName), FormatOps.WrapOrNull(stdOutVarName),
                                        FormatOps.WrapOrNull(stdErrVarName), FormatOps.WrapOrNull(startCallback),
                                        FormatOps.WrapOrNull(stdOutCallback), FormatOps.WrapOrNull(stdErrCallback),
                                        FormatOps.WrapOrNull(startHandler), FormatOps.WrapOrNull(outputHandler),
                                        FormatOps.WrapOrNull(errorHandler), processId, exitCode,
                                        FormatOps.WrapOrNull(normalize, ellipsis, result),
                                        FormatOps.WrapOrNull(normalize, ellipsis, error)),
                                        typeof(Exec).Name, priority);
                                }

                                ResultList setErrors = null;

                                /* NO RESULT */
                                ProcessOps.HandleCaptureResults(
                                    interpreter, processIdVarName, exitCodeVarName,
                                    stdOutVarName, stdErrVarName, processId, exitCode,
                                    successExitCode, attempted, useShellExecute,
                                    background, captureExitCode, captureOutput, setAll,
                                    trimAll, carriageReturns, ref code, ref result,
                                    ref error, ref setErrors);

                                if (setErrors != null)
                                {
                                    //
                                    // HACK: There should not be any errors during [set];
                                    //       complain louder.
                                    //
                                    TraceOps.ExternalAdjustTracePriority(ref priority, 1);

                                    TraceOps.DebugTrace(String.Format(
                                        "Execute: interpreter = {0}, setErrors = {1}",
                                        FormatOps.InterpreterNoThrow(interpreter),
                                        FormatOps.WrapOrNull(setErrors)),
                                        typeof(Exec).Name, priority);
                                }
                            }
                            else
                            {
                                result = "wrong # args: should be \"exec ?options? arg ?arg ...?\"";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"exec ?options? arg ?arg ...?\"";
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
