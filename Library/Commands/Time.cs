/*
 * Time.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("3921a1d3-c345-42e5-a567-b9ca2ef6b366")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard
#if NATIVE && WINDOWS
        //
        // NOTE: Uses native code indirectly for profiling (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        )]
    [ObjectGroup("time")]
    internal sealed class Time : Core
    {
        public Time(
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
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveIntegerValue,
                                Index.Invalid, Index.Invalid, "-timeout", null),
                            new Option(null, OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-statistics", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-breakOk", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-errorOk", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-noCancel", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-globalCancel", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-noHalt", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-noEvent", null),
                            new Option(null, OptionFlags.Unsafe | OptionFlags.NoCase | OptionFlags.MustHaveBooleanValue,
                                Index.Invalid, Index.Invalid, "-noExit", null),
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 3)
                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                        {
                            if (argumentIndex == Index.Invalid)
                            {
                                long requestedIterations = 1; /* DEFAULT: One iteration. */

                                if (arguments.Count >= 3)
                                {
                                    if (Value.GetWideInteger2(
                                            (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                            interpreter.InternalCultureInfo, ref requestedIterations,
                                            ref result) != ReturnCode.Ok)
                                    {
                                        return ReturnCode.Error;
                                    }
                                }

                                Variant value = null;
                                int timeout = _Timeout.Infinite; /* milliseconds */

                                if (options.IsPresent("-timeout", ref value))
                                    timeout = (int)value.Value;

                                bool statistics = false;

                                if (options.IsPresent("-statistics", ref value))
                                    statistics = (bool)value.Value;

                                bool breakOk = false;

                                if (options.IsPresent("-breakOk", true, ref value))
                                    breakOk = (bool)value.Value;

                                bool errorOk = false;

                                if (options.IsPresent("-errorOk", true, ref value))
                                    errorOk = (bool)value.Value;

                                bool noCancel = false;

                                if (options.IsPresent("-noCancel", true, ref value))
                                    noCancel = (bool)value.Value;

                                bool globalCancel = false;

                                if (options.IsPresent("-globalCancel", true, ref value))
                                    globalCancel = (bool)value.Value;

                                bool noHalt = false;

                                if (options.IsPresent("-noHalt", true, ref value))
                                    noHalt = (bool)value.Value;

                                bool noExit = false;

                                if (options.IsPresent("-noExit", true, ref value))
                                    noExit = (bool)value.Value;

                                bool noEvent = false;

                                if (options.IsPresent("-noEvent", true, ref value))
                                    noEvent = (bool)value.Value;

                                //
                                // NOTE: By default, reset only local cancel flags.
                                //
                                CancelFlags cancelFlags = CancelFlags.Time;

                                if (globalCancel)
                                    cancelFlags |= CancelFlags.Global;

                                //
                                // NOTE: These variables are used to keep track of
                                //       the minimum and maximum performance counts
                                //       for a given iteration (i.e. only when the
                                //       statistics option is enabled).
                                //
                                long? minimumIterationCount = null;
                                long? maximumIterationCount = null;

                                //
                                // NOTE: Always start with the number of iterations
                                //       requested by the caller.  Also, record the
                                //       overall starting performance count now.
                                //
                                long actualIterations = 0;
                                long remainingIterations = requestedIterations;
                                bool haveTimeout = (timeout >= 0);
                                long startCount = PerformanceOps.GetCount();

                                //
                                // NOTE: If the requested number of iterations is
                                //       exactly negative one, we will keep going
                                //       forever (i.e. until canceled).
                                //
                                try
                                {
                                    while (true)
                                    {
                                        if (remainingIterations == 0)
                                            break;

                                        long iterationStartCount = statistics ?
                                            PerformanceOps.GetCount() : 0;

                                        code = interpreter.EvaluateScript(arguments[1],
                                            ref result);

                                        if (statistics)
                                        {
                                            long iterationStopCount =
                                                PerformanceOps.GetCount();

                                            long iterationCount =
                                                iterationStopCount - iterationStartCount;

                                            if ((minimumIterationCount == null) ||
                                                (iterationCount < minimumIterationCount))
                                            {
                                                minimumIterationCount = iterationCount;
                                            }

                                            if ((maximumIterationCount == null) ||
                                                (iterationCount > maximumIterationCount))
                                            {
                                                maximumIterationCount = iterationCount;
                                            }
                                        }

                                        actualIterations++;

                                        if (haveTimeout)
                                        {
                                            long stopCount = PerformanceOps.GetCount();

                                            if (PerformanceOps.GetMillisecondsFromCount(
                                                    startCount, stopCount, 1) >= timeout)
                                            {
                                                break;
                                            }
                                        }

                                        if (code == ReturnCode.Continue)
                                            continue;

                                        if (code != ReturnCode.Ok)
                                            break;

                                        if (remainingIterations == Count.Invalid)
                                            continue;

                                        if (--remainingIterations <= 0)
                                            break;
                                    }
                                }
                                finally
                                {
                                    if (noCancel)
                                    {
                                        /* IGNORED */
                                        Engine.ResetCancel(interpreter, cancelFlags);
                                    }

                                    if (noHalt)
                                    {
                                        /* IGNORED */
                                        Engine.ResetHalt(interpreter, cancelFlags);
                                    }

                                    if (noEvent)
                                        /* IGNORED */
                                        interpreter.ClearEvents();

                                    //
                                    // NOTE: If requested, prevent the interactive loop from
                                    //       actually exiting and reset the exit code to be
                                    //       "success".
                                    //
                                    if (noExit && interpreter.ExitNoThrow)
                                    {
                                        interpreter.ExitCodeNoThrow = ResultOps.SuccessExitCode();
                                        interpreter.ExitNoThrow = false;
                                    }
                                }

                                //
                                // NOTE: Make sure the return code indicates "success".
                                //
                                if ((code == ReturnCode.Ok) || (code == ReturnCode.Break) ||
                                    (errorOk && (code == ReturnCode.Error)))
                                {
                                    //
                                    // NOTE: Limit the precision to avoid leaking too much
                                    //       timing information to "safe" interpreters.
                                    //
                                    bool obfuscate = interpreter.InternalIsSafeWithFlags(
                                        InterpreterFlags.SafeTiming, true);

                                    //
                                    // NOTE: Record the overall ending performance count
                                    //       now.
                                    //
                                    long stopCount = PerformanceOps.GetCount();

                                    //
                                    // NOTE: Calculate the average number of microseconds
                                    //       per iteration based on the starting and ending
                                    //       performance counts and the effective number of
                                    //       iterations.
                                    //
                                    long resultIterations = PerformanceOps.GetIterations(
                                        requestedIterations, actualIterations, code, breakOk);

                                    string withStatistics = FormatOps.PerformanceWithStatistics(
                                        requestedIterations, actualIterations, resultIterations,
                                        code, (code == ReturnCode.Error) ? result : null,
                                        startCount, stopCount, minimumIterationCount,
                                        maximumIterationCount, obfuscate);

                                    TraceOps.DebugTrace(string.Format(
                                        "Execute: {0} ==> {1}", FormatOps.WrapOrNull(
                                            true, false, arguments[1]), withStatistics),
                                        typeof(Time).Name, TracePriority.PerformanceDebug);

                                    if (statistics)
                                    {
                                        //
                                        // HACK: *SECURITY* These calls to GetMicroseconds* are
                                        //       EXEMPT from timing side-channel mitigation
                                        //       because the PerformanceWithStatistics method
                                        //       does its own timing side-channel mitigation.
                                        //
                                        result = withStatistics;
                                    }
                                    else
                                    {
                                        double averageMicroseconds = (resultIterations != 0) ?
                                            PerformanceOps.GetMicrosecondsFromCount(startCount,
                                                stopCount, resultIterations, obfuscate) : 0;

                                        result = FormatOps.PerformanceMicroseconds(averageMicroseconds);
                                    }

                                    //
                                    // NOTE: If the "errorOk" option was used, make sure an
                                    //       "Error" return code gets translated to "Ok".
                                    //
                                    if (errorOk && (code == ReturnCode.Error))
                                    {
                                        Engine.ResetCancel(interpreter, cancelFlags);

                                        code = ReturnCode.Ok;
                                    }
                                }
                            }
                            else
                            {
                                result = "wrong # args: should be \"time script ?count? ?options?\"";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"time script ?count? ?options?\"";
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
