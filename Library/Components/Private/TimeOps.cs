/*
 * TimeOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

using DurationDictionary = System.Collections.Generic.Dictionary<
    long, Eagle._Components.Public.StringPair>;

namespace Eagle._Components.Private
{
    [ObjectId("1e868a77-dae1-45ea-bfc3-279841624af5")]
    internal static class TimeOps
    {
        #region Private Constants
        internal static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // COMPAT: Unix, Tcl.

        internal static readonly DateTime PeEpoch = UnixEpoch; // COMPAT: PE files.

        internal static readonly DateTime BuildEpoch =
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local); // COMPAT: MSBuild.

        internal static int RevisionDivisor = 2; // COMPAT: MSBuild.

        private static readonly string DurationPrefix = "approximately ";
        private static readonly string DurationSuffix = " ago";

        private static readonly string DurationSeparator = ", ";
        private static readonly string DurationFormat = "{0} {1}";

        private static readonly long MonthOfJanuary = 1;
        private static readonly long MonthOfFebruary = 2;
        private static readonly long MonthOfDecember = 12;

        private static readonly long DaysInNormalFebruary = 28;
        private static readonly long DaysInLeapFebruary = 29;

        private static readonly long SecondsCloseToNow = 3; // TODO: Good default?
        private static readonly long SecondsInNormalDay = 86400;

        private static readonly long MonthsPerYear = 12;

        private static readonly long DaysInNormalDay = 1;
        private static readonly long DaysInNormalWeek = 7 * DaysInNormalDay;
        private static readonly long DaysInNormalMonth = 30 * DaysInNormalDay;
        private static readonly long DaysInNormalYear = 365 * DaysInNormalDay; // NOTE: Non-leap years only.

        private static readonly long DaysInNormalDecade = 10 * DaysInNormalYear;
        private static readonly long DaysInNormalCentury = 10 * DaysInNormalDecade;
        private static readonly long DaysInNormalMillennium = 10 * DaysInNormalCentury;

        private static readonly long MillisecondsPerSecond = 1000;
        private static readonly long MillisecondsPerMinute = 60 * MillisecondsPerSecond;
        private static readonly long MillisecondsPerHour = 60 * MillisecondsPerMinute;
        private static readonly long MillisecondsPerDay = 24 * MillisecondsPerHour;
        private static readonly long MillisecondsPerWeek = DaysInNormalWeek * MillisecondsPerDay;
        private static readonly long MillisecondsPerMonth = DaysInNormalMonth * MillisecondsPerDay;
        private static readonly long MillisecondsPerYear = DaysInNormalYear * MillisecondsPerDay;
        private static readonly long MillisecondsPerDecade = 10 * MillisecondsPerYear;
        private static readonly long MillisecondsPerCentury = 10 * MillisecondsPerDecade;
        private static readonly long MillisecondsPerMillennium = 10 * MillisecondsPerCentury;

        private static readonly long YearsInDecade = 10;
        private static readonly long YearsInCentury = 100;
        private static readonly long YearsInMillennium = 1000;

        private static readonly long DecadesInCentury = 10;
        private static readonly long CenturiesInMillennium = 10;

#pragma warning disable 414
        private static readonly long YearsInForever = 10000;
#pragma warning restore 414

        private static readonly long[] DaysInMonth = {
            31, /* January */
            28, /* February */
            31, /* March */
            30, /* April */
            31, /* May */
            30, /* June */
            31, /* July */
            31, /* August */
            30, /* September */
            31, /* October */
            30, /* November */
            31  /* December */
        };

        private static readonly int TicksPerMicrosecond =
            (int)TimeSpan.TicksPerMillisecond / 1000;

        private const int Roddenberry = 1946; // Another epoch (Hi, Jeff!)
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly DurationDictionary DurationNames = new DurationDictionary();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static DateTime? fakeNow = null;
        private static DateTime? fakeUtcNow = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime MaybeTruncate(
            DateTime? value, /* in: OPTIONAL */
            long seconds     /* in */
            )
        {
            DateTime localValue = (value != null) ?
                (DateTime)value : GetUtcNow();

            if (seconds > 0)
            {
                DateTime date = localValue.Date;
                long granularity = seconds % (long)SecondsInNormalDay;

                if (granularity == 0)
                    return date;

                TimeSpan time = localValue.TimeOfDay;
                long totalSeconds = (long)time.TotalSeconds;

                seconds = totalSeconds % granularity;
                seconds = totalSeconds - seconds;

                localValue = date.AddSeconds(seconds);
            }

            return localValue;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long CountLeapYears(
            DateTime value /* in */
            )
        {
            int year = value.Year;

            if (value.Month <= 2)
                year--;

            return (year / 4) - (year / 100) + (year / 400);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was shamelessly stolen
        //       from Kevin B. Kenny's [clock] command
        //       implementation in Tcl 8.5.
        //
        public static void CalculateStardate(
            DateTime value, /* in */
            out long part1, /* out */
            out long part2, /* out */
            out long part3  /* out */
            ) // COMPAT: Tcl
        {
            int year = value.Year;
            long dayOfYear = value.DayOfYear;

            part1 = year - Roddenberry;
            part2 = ((dayOfYear - 1) * 1000) / GetDaysInYear(year);

            part3 = (WholeSeconds(value) % SecondsInNormalDay) /
                    (SecondsInNormalDay / 10);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeDurationNames()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DurationNames == null)
                    return;

                if (DurationNames.Count == 0)
                {
                    DurationNames[-1] =
                        new StringPair("iteration", "iterations");

                    DurationNames[0] =
                        new StringPair("none", "just now");

                    DurationNames[1] =
                        new StringPair("millisecond", "milliseconds");

                    DurationNames[MillisecondsPerSecond] =
                        new StringPair("second", "seconds");

                    DurationNames[MillisecondsPerMinute] =
                        new StringPair("minute", "minutes");

                    DurationNames[MillisecondsPerHour] =
                        new StringPair("hour", "hours");

                    DurationNames[MillisecondsPerDay] =
                        new StringPair("day", "days");

                    DurationNames[MillisecondsPerWeek] =
                        new StringPair("week", "weeks");

                    DurationNames[MillisecondsPerMonth] =
                        new StringPair("month", "months");

                    DurationNames[MillisecondsPerYear] =
                        new StringPair("year", "years");

                    DurationNames[MillisecondsPerDecade] =
                        new StringPair("decade", "decades");

                    DurationNames[MillisecondsPerCentury] =
                        new StringPair("century", "centuries");

                    DurationNames[MillisecondsPerMillennium] =
                        new StringPair("millennium", "millennia");
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetDurationName(
            long key,       /* in */
            long value,     /* in */
            bool pluralOnly /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DurationNames == null)
                    return null;

                StringPair names;

                if (!DurationNames.TryGetValue(key, out names) ||
                    (names == null))
                {
                    return null;
                }

                return (pluralOnly || (value != 1)) ? names.Y : names.X;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringList GetHumanDuration(
            long iterations,          /* in */
            long millennia,           /* in */
            long centuries,           /* in */
            long decades,             /* in */
            long years,               /* in */
            long months,              /* in */
            long weeks,               /* in */
            long days,                /* in */
            long hours,               /* in */
            long minutes,             /* in */
            long seconds,             /* in */
            long milliseconds,        /* in */
            bool ago,                 /* in */
            bool nonZero,             /* in */
            bool asList,              /* in */
            bool includeIterations,   /* in */
            bool includeMilliseconds, /* in */
            bool withNames,           /* in */
            bool pluralOnly,          /* in */
            bool noPrefix,            /* in */
            bool noSuffix             /* in */
            )
        {
            InitializeDurationNames();

            StringList list = new StringList();
            string name; /* REUSED */

            if (includeIterations &&
                (!nonZero || (iterations != 0)))
            {
                name = withNames ? GetDurationName(
                    -1, iterations, pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, iterations, name));
                    }
                    else
                    {
                        list.Add(iterations.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(iterations.ToString());
                }
            }

            if (!nonZero || (millennia != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerMillennium, millennia,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, millennia, name));
                    }
                    else
                    {
                        list.Add(millennia.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(millennia.ToString());
                }
            }

            if (!nonZero || (centuries != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerCentury, centuries,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, centuries, name));
                    }
                    else
                    {
                        list.Add(centuries.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(centuries.ToString());
                }
            }

            if (!nonZero || (decades != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerDecade, decades,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, decades, name));
                    }
                    else
                    {
                        list.Add(decades.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(decades.ToString());
                }
            }

            if (!nonZero || (years != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerYear, years,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, years, name));
                    }
                    else
                    {
                        list.Add(years.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(years.ToString());
                }
            }

            if (!nonZero || (months != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerMonth, months,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, months, name));
                    }
                    else
                    {
                        list.Add(months.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(months.ToString());
                }
            }

            if (!nonZero || (weeks != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerWeek, weeks,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, weeks, name));
                    }
                    else
                    {
                        list.Add(weeks.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(weeks.ToString());
                }
            }

            if (!nonZero || (days != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerDay, days,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, days, name));
                    }
                    else
                    {
                        list.Add(days.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(days.ToString());
                }
            }

            if (!nonZero || (hours != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerHour, hours,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, hours, name));
                    }
                    else
                    {
                        list.Add(hours.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(hours.ToString());
                }
            }

            if (!nonZero || (minutes != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerMinute, minutes,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, minutes, name));
                    }
                    else
                    {
                        list.Add(minutes.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(minutes.ToString());
                }
            }

            if (!nonZero || (seconds != 0))
            {
                name = withNames ? GetDurationName(
                    MillisecondsPerSecond, seconds,
                    pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, seconds, name));
                    }
                    else
                    {
                        list.Add(seconds.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(seconds.ToString());
                }
            }

            if (includeMilliseconds &&
                (!nonZero || (milliseconds != 0)))
            {
                name = withNames ? GetDurationName(
                    1, milliseconds, pluralOnly) : null;

                if (!asList)
                {
                    if (withNames)
                    {
                        list.Add(String.Format(
                            DurationFormat, milliseconds, name));
                    }
                    else
                    {
                        list.Add(milliseconds.ToString());
                    }
                }
                else
                {
                    if (withNames)
                        list.Add(name);

                    list.Add(milliseconds.ToString());
                }
            }

            if (list.Count > 0)
            {
                if (!noPrefix)
                {
                    list.Insert(0, 1.ToString());
                    list.Insert(0, DurationPrefix.TrimEnd());
                }

                if (ago && !noSuffix)
                {
                    list.Add(DurationSuffix.TrimStart());
                    list.Add(1.ToString());
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetDaysInMonth(
            long month, /* in */
            long year   /* in */
            )
        {
            if ((month < MonthOfJanuary) ||
                (month > MonthOfDecember))
            {
                return 0;
            }

            int index = (int)month - 1;
            int length = DaysInMonth.Length;

            if ((index < 0) || (index >= length))
                return -1;

            long days = DaysInMonth[index];

            if ((month == MonthOfFebruary) &&
                (days == DaysInNormalFebruary) &&
                DateTime.IsLeapYear((int)year))
            {
                days = DaysInLeapFebruary;
            }

            return days;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CalculateDuration(
            DateTime start,        /* in */
            DateTime end,          /* in */
            out long millennia,    /* out */
            out long centuries,    /* out */
            out long decades,      /* out */
            out long years,        /* out */
            out long months,       /* out */
            out long weeks,        /* out */
            out long days,         /* out */
            out long hours,        /* out */
            out long minutes,      /* out */
            out long seconds,      /* out */
            out long milliseconds, /* out */
            out bool ago,          /* out */
            out long iterations    /* out */
            )
        {
            millennia = 0;
            centuries = 0;
            decades = 0;
            years = 0;
            months = 0;
            weeks = 0;
            days = 0;
            hours = 0;
            minutes = 0;
            seconds = 0;
            milliseconds = 0;
            ago = false;
            iterations = 0;

            TimeSpan difference;

        retry:

            if (end > start)
            {
                DateTime midnight; /* REUSED */
                DateTime previousMidnight; /* REUSED */
                DateTime endMidnight = end.Date; /* CONSTANT */

                midnight = start.Date;

                while (midnight < endMidnight)
                {
                    iterations++;
                    midnight = midnight.AddDays(DaysInNormalDay);
                    days++;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                midnight = start.Date;
                previousMidnight = DateTime.MinValue;

                while (midnight < endMidnight)
                {
                    long daysInYear = GetDaysInYear(midnight.Year);

                    iterations++;
                    midnight = midnight.AddDays(DaysInNormalDay);

                    if (previousMidnight > DateTime.MinValue)
                    {
                        if (midnight.Year > previousMidnight.Year)
                        {
                            if (days >= daysInYear)
                            {
                                years++;
                                weeks = 0;
                                months = 0;
                                days -= daysInYear;
                            }

                            previousMidnight = midnight;
                            continue;
                        }
                    }

                    previousMidnight = midnight;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                midnight = start.Date;
                previousMidnight = DateTime.MinValue;

                while (midnight < endMidnight)
                {
                    long daysInMonth = GetDaysInMonth(
                        midnight.Month, midnight.Year);

                    iterations++;
                    midnight = midnight.AddDays(DaysInNormalDay);

                    if (previousMidnight > DateTime.MinValue)
                    {
                        if (midnight.Month > previousMidnight.Month)
                        {
                            if (days >= daysInMonth)
                            {
                                months++;
                                weeks = 0;
                                days -= daysInMonth;
                            }

                            previousMidnight = midnight;
                            continue;
                        }
                    }

                    previousMidnight = midnight;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                while (days >= DaysInNormalWeek)
                {
                    iterations++;
                    weeks++;
                    days -= DaysInNormalWeek;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                while (years >= YearsInMillennium)
                {
                    iterations++;
                    millennia++;
                    years -= YearsInMillennium;
                }

                while (years >= YearsInCentury)
                {
                    iterations++;
                    centuries++;
                    years -= YearsInCentury;
                }

                while (years >= YearsInDecade)
                {
                    iterations++;
                    decades++;
                    years -= YearsInDecade;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                while (decades >= DecadesInCentury)
                {
                    iterations++;
                    centuries++;
                    decades -= DecadesInCentury;
                }

                while (centuries >= CenturiesInMillennium)
                {
                    iterations++;
                    millennia++;
                    centuries -= CenturiesInMillennium;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                difference = end.Subtract(start);
            }
            else
            {
                ago = true;

                DateTime swap = start;

                start = end;
                end = swap;

                goto retry;
            }

            hours = difference.Hours;
            minutes = difference.Minutes;
            seconds = difference.Seconds;
            milliseconds = difference.Milliseconds;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList GetHumanDuration( /* v2.0 */
            DateTime start,     /* in */
            DateTime end,       /* in */
            DurationFlags flags /* in */
            )
        {
            try
            {
                bool includeMonths = FlagOps.HasFlags(
                    flags, DurationFlags.IncludeMonths, true);

                bool includeMilliseconds = FlagOps.HasFlags(
                    flags, DurationFlags.IncludeMilliseconds, true);

                bool approximateMonths = FlagOps.HasFlags(
                    flags, DurationFlags.ApproximateMonths, true);

                bool approximateYears = FlagOps.HasFlags(
                    flags, DurationFlags.ApproximateYears, true);

                bool asList = FlagOps.HasFlags(
                    flags, DurationFlags.AsList, true);

                bool withNames = FlagOps.HasFlags(
                    flags, DurationFlags.WithNames, true);

                bool pluralOnly = FlagOps.HasFlags(
                    flags, DurationFlags.PluralOnly, true);

                bool noPrefix = FlagOps.HasFlags(
                    flags, DurationFlags.NoPrefix, true);

                bool noSuffix = FlagOps.HasFlags(
                    flags, DurationFlags.NoSuffix, true);

                bool precise = FlagOps.HasFlags(
                    flags, DurationFlags.Precise, true);

                bool includeIterations = FlagOps.HasFlags(
                    flags, DurationFlags.IncludeIterations, true);

                if (start == end)
                {
                    if (!asList)
                    {
                        DateTime now = GetUtcNow();

                        TimeSpan elapsed = (now > start) ?
                            now.Subtract(start) : start.Subtract(now);

                        bool closeToNow;

                        if (elapsed.TotalSeconds < SecondsCloseToNow)
                            closeToNow = true;
                        else
                            closeToNow = false;

                        return new StringList(GetDurationName(
                            0, closeToNow ? 0 : 1, pluralOnly));
                    }
                    else
                    {
                        return GetHumanDuration(
                            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                            false, false, asList, includeIterations,
                            includeMilliseconds, withNames, pluralOnly,
                            noPrefix, noSuffix);
                    }
                }

                long iterations = 0;
                long millennia = 0;
                long centuries = 0;
                long decades = 0;
                long years = 0;
                long months = 0;
                long weeks = 0;
                long days = 0;
                long hours = 0;
                long minutes = 0;
                long seconds = 0;
                long milliseconds = 0;
                bool ago;
                StringBuilder builder; /* REUSED */
                StringList list; /* REUSED */
                int count; /* REUSED */

                if (precise)
                {
                    CalculateDuration(
                        start, end, out millennia,
                        out centuries, out decades,
                        out years, out months,
                        out weeks, out days,
                        out hours, out minutes,
                        out seconds, out milliseconds,
                        out ago, out iterations);

                    builder = StringBuilderFactory.Create();

                    try
                    {
                        list = GetHumanDuration(
                            iterations, millennia, centuries, decades,
                            years, months, weeks, days, hours, minutes,
                            seconds, milliseconds, ago, false, asList,
                            includeIterations, includeMilliseconds,
                            withNames, pluralOnly, true, true);

                        count = list.Count;

                        if ((count > 0) && !FlagOps.HasFlags(
                                flags, DurationFlags.NoJoin, true))
                        {
#if NET_40
                            builder.Append(String.Join(
                                DurationSeparator, list));
#else
                            builder.Append(String.Join(
                                DurationSeparator, list.ToArray()));
#endif

                            if (ago && !noSuffix)
                                builder.Append(DurationSuffix);

                            return new StringList(builder.ToString());
                        }
                        else
                        {
                            return list;
                        }
                    }
                    finally
                    {
                        StringBuilderCache.Release(ref builder);
                    }
                }

                TimeSpan difference;

                if (end > start)
                {
                    difference = end.Subtract(start);
                    ago = false;
                }
                else
                {
                    difference = start.Subtract(end);
                    ago = true;
                }

                days = difference.Days;

                if (includeMonths && !approximateMonths)
                {
                    if (ago)
                    {
                        months = ((start.Year - end.Year) * MonthsPerYear) +
                                 (start.Month - end.Month);

                        if ((months > 0) && (start.Day < end.Day))
                            months--;

                        days = (start - end.AddMonths((int)months)).Days;
                    }
                    else
                    {
                        months = ((end.Year - start.Year) * MonthsPerYear) +
                                 (end.Month - start.Month);

                        if ((months > 0) && (end.Day < start.Day))
                            months--;

                        days = (end - start.AddMonths((int)months)).Days;
                    }
                }

                millennia = days / DaysInNormalMillennium;

                days -= (millennia * DaysInNormalMillennium);

                centuries = days / DaysInNormalCentury;

                days -= (centuries * DaysInNormalCentury);

                decades = days / DaysInNormalDecade;

                days -= (decades * DaysInNormalDecade);

                long leapDays;

                if (FlagOps.HasFlags(
                        flags, DurationFlags.CountLeapDays, true))
                {
                    if (ago)
                    {
                        leapDays = CountLeapYears(start) -
                                   CountLeapYears(end);
                    }
                    else
                    {
                        leapDays = CountLeapYears(end) -
                                   CountLeapYears(start);
                    }

                    if (days >= leapDays)
                        days -= leapDays;
                    else
                        leapDays = 0;
                }
                else
                {
                    leapDays = 0;
                }

                if (includeMonths &&
                    !approximateMonths && approximateYears)
                {
                    years = months / MonthsPerYear;
                    months -= (years * MonthsPerYear);
                }
                else
                {
                    years = days / DaysInNormalYear;
                    days -= (years * DaysInNormalYear);
                }

                if (leapDays > 0)
                    days += leapDays;

                if (includeMonths && approximateMonths)
                {
                    months = days / DaysInNormalMonth;
                    days -= (months * DaysInNormalMonth);
                }

                if (FlagOps.HasFlags(
                        flags, DurationFlags.IncludeWeeks, true))
                {
                    weeks = days / DaysInNormalWeek;
                    days -= (weeks * DaysInNormalWeek);
                }

                hours = difference.Hours;
                minutes = difference.Minutes;
                seconds = difference.Seconds;
                milliseconds = difference.Milliseconds;

                if (!asList)
                {
                    builder = StringBuilderFactory.Create();

                    try
                    {
                        list = GetHumanDuration(
                            iterations, millennia, centuries, decades,
                            years, months, weeks, days, hours, minutes,
                            seconds, milliseconds, ago, true, false,
                            includeIterations, includeMilliseconds,
                            withNames, pluralOnly, true, true);

                        count = list.Count;

                        if ((count > 0) && !FlagOps.HasFlags(
                                flags, DurationFlags.NoJoin, true))
                        {
#if NET_40
                            builder.Append(String.Join(
                                DurationSeparator, list));
#else
                            builder.Append(String.Join(
                                DurationSeparator, list.ToArray()));
#endif

                            if (!noPrefix)
                                builder.Insert(0, DurationPrefix);

                            if (ago && !noSuffix)
                                builder.Append(DurationSuffix);

                            return new StringList(builder.ToString());
                        }
                        else
                        {
                            return list;
                        }
                    }
                    finally
                    {
                        StringBuilderCache.Release(ref builder);
                    }
                }
                else
                {
                    return GetHumanDuration(
                        iterations, millennia, centuries, decades,
                        years, months, weeks, days, hours, minutes,
                        seconds, milliseconds, ago, true, asList,
                        includeIterations, includeMilliseconds,
                        withNames, pluralOnly, noPrefix, noSuffix);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(TimeOps).Name,
                    TracePriority.TimeError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime GetNow()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (fakeNow != null)
                    return (DateTime)fakeNow;
            }

            return DateTime.Now;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime GetUtcNow()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (fakeUtcNow != null)
                    return (DateTime)fakeUtcNow;
            }

            return DateTime.UtcNow;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetFakeNow(
            DateTime? now /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                fakeNow = now;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetFakeUtcNow(
            DateTime? now /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                fakeUtcNow = now;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetUtcNowTicks()
        {
            DateTime now = GetUtcNow();

            return now.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ThisThursday(
            DateTime dateTime
            )
        {
            return dateTime.AddDays(-(((int)dateTime.DayOfWeek + 6) % 7)).AddDays(3);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime StartOfYear(
            DateTime dateTime
            )
        {
            return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static DateTime StartOfDay(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, dateTime.Day,
                0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime EndOfDay(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, dateTime.Day,
                23, 59, 59, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime StartOfMonth(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, 1,
                0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime EndOfMonth(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month,
                DateTime.DaysInMonth(dateTime.Year, dateTime.Month),
                23, 59, 59, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime EndOfYear(
            DateTime dateTime
            )
        {
            return new DateTime(dateTime.Year, 12, 31, 23, 59, 59, dateTime.Kind);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ElapsedSeconds(
            ref double seconds,
            DateTime epoch
            )
        {
            return ElapsedSeconds(ref seconds, GetUtcNow(), epoch);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ElapsedSeconds(
            ref double seconds,
            DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                //
                // NOTE: Calculate the number of whole seconds between
                //       the supplied epoch and the supplied date.
                //
                seconds = dateTime.Subtract(epoch).TotalSeconds;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool ElapsedDays(
            ref double days,
            DateTime epoch
            )
        {
            return ElapsedDays(ref days, GetUtcNow(), epoch);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ElapsedDays(
            ref double days,
            DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                //
                // NOTE: Calculate the number of whole days between the
                //       supplied epoch and the supplied date.
                //
                days = dateTime.Subtract(epoch).TotalDays;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SecondsSinceStartOfDay(
            ref double seconds,
            DateTime dateTime
            )
        {
            try
            {
                //
                // NOTE: Calculate the number of seconds between midnight
                //       on the supplied date until the supplied date
                //       itself.
                //
                seconds = dateTime.Subtract(dateTime.Date).TotalSeconds;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long WholeSeconds(
            DateTime dateTime
            )
        {
            return (dateTime.Ticks / TimeSpan.TicksPerSecond);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long GetDaysInYear(
            int year
            )
        {
            return DaysInNormalYear + ConversionOps.ToLong(
                DateTime.IsLeapYear(year));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool DateTimeToMicroseconds(
            ref long microseconds,
            DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                TimeSpan timeSpan = dateTime.Subtract(epoch);
                microseconds = (timeSpan.Ticks / TicksPerMicrosecond);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool DateTimeToMilliseconds(
            ref long milliseconds,
            DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                TimeSpan timeSpan = dateTime.Subtract(epoch);
                milliseconds = (timeSpan.Ticks / TimeSpan.TicksPerMillisecond);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool DateTimeToSeconds(
            ref long seconds,
            DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                TimeSpan timeSpan = dateTime.Subtract(epoch);
                seconds = (timeSpan.Ticks / TimeSpan.TicksPerSecond);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        public static void UnixMillisecondsOrSecondsToDateTime(
            double milliseconds,
            ref DateTime dateTime,
            ref double value,
            ref string units
            )
        {
            MillisecondsOrSecondsToDateTime(
                milliseconds, ref dateTime, ref value, ref units,
                UnixEpoch);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MillisecondsOrSecondsToDateTime(
            double milliseconds,
            ref DateTime dateTime,
            ref double value,
            ref string units,
            DateTime epoch
            )
        {
            if (Math.IEEERemainder(
                    milliseconds, MillisecondsPerSecond) == 0.0)
            {
                value = milliseconds / MillisecondsPerSecond;
                dateTime = epoch.AddSeconds(value);
                units = "seconds";
            }
            else
            {
                value = milliseconds;
                dateTime = epoch.AddMilliseconds(value);
                units = "milliseconds";
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool MillisecondsToDateTime(
            long milliseconds,
            ref DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                dateTime = epoch.AddMilliseconds(milliseconds);

                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TicksToDateTime(
            long ticks,
            DateTimeKind kind,
            ref DateTime dateTime
            )
        {
            try
            {
                dateTime = new DateTime(ticks, kind);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SecondsToDateTime(
            long seconds,
            ref DateTime dateTime,
            DateTime epoch
            )
        {
            try
            {
                dateTime = epoch.AddSeconds(seconds);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool UnixSecondsToDateTime(
            long seconds,
            ref DateTime dateTime
            )
        {
            return SecondsToDateTime(
                seconds, ref dateTime, UnixEpoch);
        }
    }
}
