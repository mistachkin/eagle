/*
 * Clock.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
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
    [ObjectId("6715457a-62f1-4865-a00f-b3dd4aeb1d9c")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard
#if NATIVE && WINDOWS
        //
        // NOTE: Uses native code indirectly for getting the current
        //       time (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        )]
    [ObjectGroup("time")]
    internal sealed class Clock : Core
    {
        public Clock(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "buildnumber", "clicks", "days", "duration", "filetime",
            "format", "isvalid", "microseconds", "milliseconds", "now",
            "scan", "seconds", "start", "stop"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary allowedSubCommands = new EnsembleDictionary(
            PolicyOps.AllowedClockSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "buildnumber":
                                case "days":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            bool build = SharedStringOps.SystemEquals(subCommand, "buildnumber");

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-format", null),
                                                new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-epoch", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-gmt", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IVariant value = null;
                                                    string format = null;

                                                    if (options.IsPresent("-format", ref value))
                                                        format = value.ToString();

                                                    DateTime epoch = build ? TimeOps.BuildEpoch : DateTime.MinValue;

                                                    if (options.IsPresent("-epoch", ref value))
                                                        epoch = (DateTime)value.Value;

                                                    bool utc = false;

                                                    if (options.IsPresent("-gmt", ref value))
                                                        utc = (bool)value.Value;

                                                    DateTime dateTime;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        dateTime = DateTime.MinValue;

                                                        code = Value.GetDateTime2(
                                                            arguments[argumentIndex], format,
                                                            ValueFlags.AnyDateTime, utc ?
                                                                DateTimeKind.Utc : DateTimeKind.Local,
                                                            interpreter.DateTimeStyles,
                                                            interpreter.InternalCultureInfo,
                                                            ref dateTime, ref result);
                                                    }
                                                    else
                                                    {
                                                        dateTime = utc ? TimeOps.GetUtcNow() : TimeOps.GetNow();
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (epoch == DateTime.MinValue)
                                                            epoch = TimeOps.StartOfYear(dateTime);

                                                        double days = 0.0;

                                                        if (TimeOps.ElapsedDays(ref days, dateTime, epoch))
                                                        {
                                                            if (build)
                                                            {
                                                                double seconds = 0.0;

                                                                if (TimeOps.SecondsSinceStartOfDay(ref seconds, dateTime))
                                                                {
                                                                    result = StringList.MakeList(
                                                                        Math.Truncate(days),
                                                                        Math.Truncate(Math.Truncate(seconds) /
                                                                            TimeOps.RevisionDivisor));
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "could not get seconds since start of day {0}",
                                                                        dateTime);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = Math.Truncate(days);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "could not get days since the epoch {0}",
                                                                epoch);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? ?dateString?\"",
                                                            this.Name, subCommand);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? ?dateString?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "clicks":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-milliseconds", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-microseconds", null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    if (options.IsPresent("-microseconds"))
                                                        //
                                                        // NOTE: Use the CPU tick count, which has microsecond resolution.
                                                        //
                                                        result = PerformanceOps.GetMicroseconds();
                                                    else if (options.IsPresent("-milliseconds"))
                                                        //
                                                        // NOTE: Use the standard system tick count, which has millisecond
                                                        //       resolution.
                                                        //
                                                        result = PerformanceOps.GetTickCount();
                                                    else
                                                        //
                                                        // NOTE: Use the highest resolution clock the system has to offer.
                                                        //
                                                        result = PerformanceOps.GetCount();
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"clock clicks ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock clicks ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "duration":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            DateTime start = DateTime.MinValue;

                                            //
                                            // NOTE: Since we are calculating the duration only, use UTC.
                                            //
                                            code = Value.GetDateTime2(
                                                arguments[2], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                interpreter.InternalCultureInfo, ref start, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                DateTime end = DateTime.MinValue;

                                                code = Value.GetDateTime2(
                                                    arguments[3], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                    interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                    interpreter.InternalCultureInfo, ref end, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = end.Subtract(start);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock duration startDateString endDateString\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "filetime":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            long fileTime = 0;

                                            code = Value.GetWideInteger2(arguments[2], ValueFlags.AnyWideInteger, interpreter.InternalCultureInfo, ref fileTime);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-format", null),
                                                    new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-epoch", null),
                                                    new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-gmt", null)
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
                                                        IVariant value = null;
                                                        string format = null;

                                                        if (options.IsPresent("-format", ref value))
                                                            format = value.ToString();

                                                        bool utc = false;

                                                        if (options.IsPresent("-gmt", ref value))
                                                            utc = (bool)value.Value;

                                                        DateTime epoch = TimeOps.UnixEpoch;

                                                        if (options.IsPresent("-epoch", ref value))
                                                            epoch = (DateTime)value.Value;

                                                        DateTime dateTime;

                                                        if (utc)
                                                            dateTime = DateTime.FromFileTimeUtc(fileTime);
                                                        else
                                                            dateTime = DateTime.FromFileTime(fileTime);

                                                        if (format != null)
                                                            result = FormatOps.TclClockDateTime(interpreter.InternalCultureInfo,
                                                                utc ? null : TimeZone.CurrentTimeZone, format, dateTime,
                                                                epoch);
                                                        else
                                                            result = dateTime;
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"clock filetime fileTimeValue ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock filetime fileTimeValue ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "format":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            long clockValue = 0;

                                            code = Value.GetWideInteger2(
                                                (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                                interpreter.InternalCultureInfo, ref clockValue, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-format", null),
                                                    new Option(typeof(DateTimeKind), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-kind", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-ticks", null),
                                                    new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-epoch", null),
                                                    new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-gmt", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-iso", null), // COMPAT: Eagle (Legacy).
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-full", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-isotimezone", null)
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
                                                        IVariant value = null;
                                                        DateTimeKind kind = ObjectOps.GetDefaultDateTimeKind();

                                                        if (options.IsPresent("-kind", ref value))
                                                            kind = (DateTimeKind)value.Value;

                                                        bool ticks = false;

                                                        if (options.IsPresent("-ticks"))
                                                            ticks = true;

                                                        string format = null;

                                                        if (options.IsPresent("-format", ref value))
                                                            format = value.ToString();

                                                        bool utc = false;

                                                        if (options.IsPresent("-gmt", ref value))
                                                            utc = (bool)value.Value;

                                                        bool iso = false;

                                                        if (options.IsPresent("-iso"))
                                                            iso = true;

                                                        bool full = false;

                                                        if (options.IsPresent("-full"))
                                                            full = true;

                                                        bool isoTimeZone = false;

                                                        if (options.IsPresent("-isotimezone"))
                                                            isoTimeZone = true;

                                                        DateTime epoch = TimeOps.UnixEpoch;

                                                        if (options.IsPresent("-epoch", ref value))
                                                            epoch = (DateTime)value.Value;

                                                        DateTime dateTime = DateTime.MinValue;

                                                        if ((ticks && TimeOps.TicksToDateTime(clockValue, kind, ref dateTime)) ||
                                                            (!ticks && TimeOps.SecondsToDateTime(clockValue, ref dateTime, epoch)))
                                                        {
                                                            if (!utc)
                                                                dateTime = dateTime.ToLocalTime();

                                                            if (iso)
                                                            {
                                                                if (full)
                                                                    result = FormatOps.Iso8601FullDateTime(dateTime);
                                                                else
                                                                    result = FormatOps.Iso8601DateTime(dateTime, isoTimeZone);
                                                            }
                                                            else if (format != null)
                                                            {
                                                                //
                                                                // BUGFIX: To maintain compatibility with Tcl, we need
                                                                //         to honor what happens when the format string
                                                                //         is either blank or consists entirely of
                                                                //         white-space.
                                                                //
                                                                if (format.Trim().Length > 0)
                                                                {
                                                                    result = FormatOps.TclClockDateTime(
                                                                        interpreter.InternalCultureInfo,
                                                                        utc ? null : TimeZone.CurrentTimeZone,
                                                                        format, dateTime, epoch);
                                                                }
                                                                else
                                                                {
                                                                    result = format;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = dateTime;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "could not get time {0} seconds since the epoch {1}",
                                                                clockValue, epoch);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"clock format clockValue ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock format clockValue ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isvalid":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            DateTime dateTime = DateTime.MinValue;

                                            code = Value.GetDateTime2(
                                                arguments[2], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                interpreter.InternalCultureInfo, ref dateTime, ref result);

                                            result = (code == ReturnCode.Ok);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock isvalid dateString\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "microseconds":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                DateTime epoch = DateTime.MinValue;

                                                //
                                                // NOTE: The epoch is always UTC.
                                                //
                                                code = Value.GetDateTime2(
                                                    arguments[2], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                    interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                    interpreter.InternalCultureInfo, ref epoch, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    long microseconds = 0;

                                                    if (TimeOps.DateTimeToMicroseconds(
                                                            ref microseconds, TimeOps.GetUtcNow(), epoch))
                                                    {
                                                        result = microseconds;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "could not get microseconds since the epoch {0}",
                                                            epoch);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                long microseconds = 0;

                                                if (TimeOps.DateTimeToMicroseconds(
                                                        ref microseconds, TimeOps.GetUtcNow(), TimeOps.UnixEpoch))
                                                {
                                                    result = microseconds;
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "could not get microseconds since the epoch {0}",
                                                        TimeOps.UnixEpoch);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock microseconds ?epoch?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "milliseconds":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                DateTime epoch = DateTime.MinValue;

                                                //
                                                // NOTE: The epoch is always UTC.
                                                //
                                                code = Value.GetDateTime2(
                                                    arguments[2], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                    interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                    interpreter.InternalCultureInfo, ref epoch, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    long milliseconds = 0;

                                                    if (TimeOps.DateTimeToMilliseconds(
                                                            ref milliseconds, TimeOps.GetUtcNow(), epoch))
                                                    {
                                                        result = milliseconds;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "could not get milliseconds since the epoch {0}",
                                                            epoch);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                long milliseconds = 0;

                                                if (TimeOps.DateTimeToMilliseconds(
                                                        ref milliseconds, TimeOps.GetUtcNow(), TimeOps.UnixEpoch))
                                                {
                                                    result = milliseconds;
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "could not get milliseconds since the epoch {0}",
                                                        TimeOps.UnixEpoch);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock milliseconds ?epoch?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "now":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 4))
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-gmt", null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IVariant value = null;
                                                    bool utc = false;

                                                    if (options.IsPresent("-gmt", ref value))
                                                        utc = (bool)value.Value;

                                                    if (utc)
                                                        result = TimeOps.GetUtcNow().Ticks;
                                                    else
                                                        result = TimeOps.GetNow().Ticks;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"clock now ?-gmt boolean?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock now ?-gmt boolean?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scan":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-format", null),
                                                new Option(null, OptionFlags.MustHaveWideIntegerValue, Index.Invalid, Index.Invalid, "-base", null),
                                                new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-epoch", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-gmt", null)
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
                                                    IVariant value = null;
                                                    string format = null;

                                                    if (options.IsPresent("-format", ref value))
                                                        format = value.ToString();

#if MONO_BUILD
#pragma warning disable 219
#endif
                                                    long clockValue = 0; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                                                    if (options.IsPresent("-base", ref value))
                                                        clockValue = (long)value.Value; /* NOT USED, COMPAT ONLY */

                                                    bool utc = false;

                                                    if (options.IsPresent("-gmt", ref value))
                                                        utc = (bool)value.Value;

                                                    DateTime epoch = TimeOps.UnixEpoch;

                                                    if (options.IsPresent("-epoch", ref value))
                                                        epoch = (DateTime)value.Value;

                                                    DateTime dateTime = DateTime.MinValue;

                                                    code = Value.GetDateTime2(
                                                        arguments[2], format,
                                                        ValueFlags.AnyDateTime, utc ?
                                                            DateTimeKind.Utc : DateTimeKind.Local,
                                                        interpreter.DateTimeStyles,
                                                        interpreter.InternalCultureInfo,
                                                        ref dateTime, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!utc)
                                                            dateTime = dateTime.ToUniversalTime();

                                                        long seconds = 0;

                                                        if (TimeOps.DateTimeToSeconds(ref seconds, dateTime, epoch))
                                                        {
                                                            result = seconds;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "could not get seconds since the epoch {0} for time {1}",
                                                                epoch, dateTime);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"clock scan dateString ?-base clockValue? ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock scan dateString ?-base clockValue? ?-format string? ?-gmt boolean? ?-epoch dateString?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "seconds":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                DateTime epoch = DateTime.MinValue;

                                                //
                                                // NOTE: The epoch is always UTC.
                                                //
                                                code = Value.GetDateTime2(
                                                    arguments[2], interpreter.DateTimeFormat, ValueFlags.AnyDateTime,
                                                    interpreter.DateTimeKind, interpreter.DateTimeStyles,
                                                    interpreter.InternalCultureInfo, ref epoch, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    long seconds = 0;

                                                    if (TimeOps.DateTimeToSeconds(
                                                            ref seconds, TimeOps.GetUtcNow(), epoch))
                                                    {
                                                        result = seconds;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "could not get seconds since epoch {0}",
                                                            epoch);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                long seconds = 0;

                                                if (TimeOps.DateTimeToSeconds(
                                                        ref seconds, TimeOps.GetUtcNow(), TimeOps.UnixEpoch))
                                                {
                                                    result = seconds;
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "could not get seconds since the epoch {0}",
                                                        TimeOps.UnixEpoch);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock seconds ?epoch?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "start":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = PerformanceOps.GetCount();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock start\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "stop":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            long startCount = 0;

                                            code = Value.GetWideInteger2(
                                                (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                                interpreter.InternalCultureInfo, ref startCount, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // HACK: *SECURITY* This call to GetMicroseconds* is
                                                //       EXEMPT from timing side-channel mitigation
                                                //       because this sub-command is disallowed in a
                                                //       "safe" interpreter.
                                                //
                                                result = PerformanceOps.GetMicrosecondsFromCount(
                                                    startCount, PerformanceOps.GetCount(),
                                                    1, false); /* EXEMPT */
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"clock stop startCount\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"clock option ?arg ...?\"";
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
