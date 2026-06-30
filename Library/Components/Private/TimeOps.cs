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
    /// <summary>
    /// This class provides the private date and time helper routines used
    /// throughout the Eagle core, including epoch and calendar constants,
    /// elapsed-time and duration calculations, human-readable duration
    /// formatting, conversions between dates and tick/second/millisecond/
    /// microsecond counts, and support for overriding the current time during
    /// testing.
    /// </summary>
    [ObjectId("1e868a77-dae1-45ea-bfc3-279841624af5")]
    internal static class TimeOps
    {
        #region Private Constants
        /// <summary>
        /// The Unix epoch (midnight, January 1st, 1970, UTC), used for
        /// compatibility with Unix and Tcl.
        /// </summary>
        internal static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // COMPAT: Unix, Tcl.

        /// <summary>
        /// The epoch used for timestamps within PE (portable executable) files;
        /// this is the same as the Unix epoch.
        /// </summary>
        internal static readonly DateTime PeEpoch = UnixEpoch; // COMPAT: PE files.

        /// <summary>
        /// The epoch used by MSBuild-style automatic build numbering (midnight,
        /// January 1st, 2000, local time).
        /// </summary>
        internal static readonly DateTime BuildEpoch =
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local); // COMPAT: MSBuild.

        /// <summary>
        /// The divisor applied when deriving a revision number from a count of
        /// seconds, for compatibility with MSBuild.
        /// </summary>
        internal static int RevisionDivisor = 2; // COMPAT: MSBuild.

        /// <summary>
        /// The prefix prepended to a formatted, approximate human-readable
        /// duration.
        /// </summary>
        private static readonly string DurationPrefix = "approximately ";

        /// <summary>
        /// The suffix appended to a formatted human-readable duration that
        /// refers to a time in the past.
        /// </summary>
        private static readonly string DurationSuffix = " ago";

        /// <summary>
        /// The separator placed between the individual components of a
        /// formatted human-readable duration.
        /// </summary>
        private static readonly string DurationSeparator = ", ";

        /// <summary>
        /// The composite format string used to combine a duration component
        /// value with its unit name.
        /// </summary>
        private static readonly string DurationFormat = "{0} {1}";

        /// <summary>
        /// The ordinal number of the month of January.
        /// </summary>
        private static readonly long MonthOfJanuary = 1;

        /// <summary>
        /// The ordinal number of the month of February.
        /// </summary>
        private static readonly long MonthOfFebruary = 2;

        /// <summary>
        /// The ordinal number of the month of December.
        /// </summary>
        private static readonly long MonthOfDecember = 12;

        /// <summary>
        /// The number of days in February during a non-leap year.
        /// </summary>
        private static readonly long DaysInNormalFebruary = 28;

        /// <summary>
        /// The number of days in February during a leap year.
        /// </summary>
        private static readonly long DaysInLeapFebruary = 29;

        /// <summary>
        /// The maximum number of seconds of elapsed time that is still
        /// considered to be "just now".
        /// </summary>
        private static readonly long SecondsCloseToNow = 3; // TODO: Good default?

        /// <summary>
        /// The number of seconds in a normal day.
        /// </summary>
        private static readonly long SecondsInNormalDay = 86400;

        /// <summary>
        /// The number of months in a year.
        /// </summary>
        private static readonly long MonthsPerYear = 12;

        /// <summary>
        /// The number of days in a normal day (i.e. one).
        /// </summary>
        private static readonly long DaysInNormalDay = 1;

        /// <summary>
        /// The number of days in a normal week.
        /// </summary>
        private static readonly long DaysInNormalWeek = 7 * DaysInNormalDay;

        /// <summary>
        /// The number of days in a normal (approximate) month.
        /// </summary>
        private static readonly long DaysInNormalMonth = 30 * DaysInNormalDay;

        /// <summary>
        /// The number of days in a normal (non-leap) year.
        /// </summary>
        private static readonly long DaysInNormalYear = 365 * DaysInNormalDay; // NOTE: Non-leap years only.

        /// <summary>
        /// The number of days in a normal decade.
        /// </summary>
        private static readonly long DaysInNormalDecade = 10 * DaysInNormalYear;

        /// <summary>
        /// The number of days in a normal century.
        /// </summary>
        private static readonly long DaysInNormalCentury = 10 * DaysInNormalDecade;

        /// <summary>
        /// The number of days in a normal millennium.
        /// </summary>
        private static readonly long DaysInNormalMillennium = 10 * DaysInNormalCentury;

        /// <summary>
        /// The number of milliseconds in a second.
        /// </summary>
        private static readonly long MillisecondsPerSecond = 1000;

        /// <summary>
        /// The number of milliseconds in a minute.
        /// </summary>
        private static readonly long MillisecondsPerMinute = 60 * MillisecondsPerSecond;

        /// <summary>
        /// The number of milliseconds in an hour.
        /// </summary>
        private static readonly long MillisecondsPerHour = 60 * MillisecondsPerMinute;

        /// <summary>
        /// The number of milliseconds in a day.
        /// </summary>
        private static readonly long MillisecondsPerDay = 24 * MillisecondsPerHour;

        /// <summary>
        /// The number of milliseconds in a week.
        /// </summary>
        private static readonly long MillisecondsPerWeek = DaysInNormalWeek * MillisecondsPerDay;

        /// <summary>
        /// The number of milliseconds in a normal (approximate) month.
        /// </summary>
        private static readonly long MillisecondsPerMonth = DaysInNormalMonth * MillisecondsPerDay;

        /// <summary>
        /// The number of milliseconds in a normal (non-leap) year.
        /// </summary>
        private static readonly long MillisecondsPerYear = DaysInNormalYear * MillisecondsPerDay;

        /// <summary>
        /// The number of milliseconds in a normal decade.
        /// </summary>
        private static readonly long MillisecondsPerDecade = 10 * MillisecondsPerYear;

        /// <summary>
        /// The number of milliseconds in a normal century.
        /// </summary>
        private static readonly long MillisecondsPerCentury = 10 * MillisecondsPerDecade;

        /// <summary>
        /// The number of milliseconds in a normal millennium.
        /// </summary>
        private static readonly long MillisecondsPerMillennium = 10 * MillisecondsPerCentury;

        /// <summary>
        /// The number of years in a decade.
        /// </summary>
        private static readonly long YearsInDecade = 10;

        /// <summary>
        /// The number of years in a century.
        /// </summary>
        private static readonly long YearsInCentury = 100;

        /// <summary>
        /// The number of years in a millennium.
        /// </summary>
        private static readonly long YearsInMillennium = 1000;

        /// <summary>
        /// The number of decades in a century.
        /// </summary>
        private static readonly long DecadesInCentury = 10;

        /// <summary>
        /// The number of centuries in a millennium.
        /// </summary>
        private static readonly long CenturiesInMillennium = 10;

        /// <summary>
        /// An arbitrarily large number of years used to represent an effectively
        /// unbounded span of time.
        /// </summary>
#pragma warning disable 414
        private static readonly long YearsInForever = 10000;
#pragma warning restore 414

        /// <summary>
        /// The number of days in each month of a non-leap year, indexed from
        /// January through December.
        /// </summary>
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

        /// <summary>
        /// The number of ticks in a single microsecond.
        /// </summary>
        private static readonly int TicksPerMicrosecond =
            (int)TimeSpan.TicksPerMillisecond / 1000;

        /// <summary>
        /// The base year used by the stardate calculation.
        /// </summary>
        private const int Roddenberry = 1946; // Another epoch (Hi, Jeff!)
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The mapping from a duration unit (expressed in milliseconds) to the
        /// singular and plural names of that unit.
        /// </summary>
        private static readonly DurationDictionary DurationNames = new DurationDictionary();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-null, this value is returned in place of the actual local
        /// current date and time, primarily to support deterministic testing.
        /// </summary>
        private static DateTime? fakeNow = null;

        /// <summary>
        /// When non-null, this value is returned in place of the actual UTC
        /// current date and time, primarily to support deterministic testing.
        /// </summary>
        private static DateTime? fakeUtcNow = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method examines a "clock scan" format string and determines
        /// which date components (year, month, and day) it explicitly
        /// specifies, so that any unspecified components can later be taken from
        /// a base clock value.  Only the date specifiers are considered; the
        /// time-of-day specifiers are ignored.
        /// </summary>
        /// <param name="format">
        /// The "clock scan" format string to examine.
        /// </param>
        /// <param name="hasYear">
        /// Upon return, this is set to true if the format string specifies a
        /// year component; otherwise, it is left unchanged.
        /// </param>
        /// <param name="hasMonth">
        /// Upon return, this is set to true if the format string specifies a
        /// month component; otherwise, it is left unchanged.
        /// </param>
        /// <param name="hasDay">
        /// Upon return, this is set to true if the format string specifies a
        /// day component; otherwise, it is left unchanged.
        /// </param>
        private static void GetFormatDateComponents(
            string format,     /* in */
            ref bool hasYear,  /* in, out */
            ref bool hasMonth, /* in, out */
            ref bool hasDay    /* in, out */
            )
        {
            //
            // NOTE: Determine which date components "clock scan" format string
            //       actually specifies, so the others can be taken from a base
            //       clock value.  Only the date specifiers are considered (the
            //       time-of-day is never taken from the base).
            //
            int length = format.Length;
            int index = 0;

            while (index < length)
            {
                if (format[index] != Characters.PercentSign)
                {
                    index++;
                    continue;
                }

                index++; /* consume the percent sign. */

                if (index >= length)
                    break;

                char specifier = format[index];
                index++;

                switch (specifier)
                {
                    case Characters.Y: /* year (with century). */
                    case Characters.y: /* year (without century). */
                    case Characters.C: /* century. */
                    case Characters.G: /* ISO 8601 year. */
                    case Characters.g: /* ISO 8601 year (without century). */
                        {
                            hasYear = true;
                            break;
                        }
                    case Characters.m: /* month number. */
                    case Characters.B: /* full month name. */
                    case Characters.b: /* abbreviated month name. */
                    case Characters.h: /* abbreviated month name. */
                    case Characters.N: /* month number (no padding). */
                        {
                            hasMonth = true;
                            break;
                        }
                    case Characters.d: /* day of month. */
                    case Characters.e: /* day of month (no padding). */
                        {
                            hasDay = true;
                            break;
                        }
                    case Characters.j: /* day of year (implies month + day). */
                        {
                            hasMonth = true;
                            hasDay = true;
                            break;
                        }
                    case Characters.D: /* %m/%d/%y. */
                    case Characters.F: /* %Y-%m-%d. */
                    case Characters.x: /* locale date. */
                    case Characters.c: /* locale date and time. */
                    case Characters.s: /* seconds since the epoch. */
                        {
                            hasYear = true;
                            hasMonth = true;
                            hasDay = true;
                            break;
                        }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method overlays a base date onto a parsed date and time,
        /// supplying any date components that were absent from the parsed value
        /// from the base clock value.  The time-of-day is always taken from the
        /// parsed value, never from the base.
        /// </summary>
        /// <param name="dateTime">
        /// The parsed date and time value.
        /// </param>
        /// <param name="baseDateTime">
        /// The base clock value from which any absent date components are
        /// supplied.
        /// </param>
        /// <param name="kind">
        /// The date and time kind for the resulting value.
        /// </param>
        /// <param name="format">
        /// The "clock scan" format string used to parse the value, or null if
        /// the free-form parser was used.
        /// </param>
        /// <returns>
        /// The resulting date and time value, with absent date components
        /// supplied from the base clock value.
        /// </returns>
        public static DateTime ApplyBaseDate(
            DateTime dateTime,     /* in */
            DateTime baseDateTime, /* in */
            DateTimeKind kind,     /* in */
            string format          /* in */
            )
        {
            //
            // NOTE: Decide which date components came from the input and which
            //       must be supplied by the base clock value.  The time-of-day
            //       is always taken from the parsed value (an absent time
            //       defaults to midnight, matching Tcl), never from the base.
            //
            bool hasYear;
            bool hasMonth;
            bool hasDay;

            if (format != null)
            {
                //
                // NOTE: With an explicit format, the specified date components
                //       are known exactly from the conversion specifiers.
                //
                hasYear = false;
                hasMonth = false;
                hasDay = false;

                GetFormatDateComponents(
                    format, ref hasYear, ref hasMonth, ref hasDay);
            }
            else
            {
                //
                // NOTE: For the free-form parser only the wholly-absent-date
                //       (i.e. time-only) case is reliably detectable; it was
                //       parsed with "NoCurrentDateDefault", so an absent date
                //       is at the minimum value.
                //
                bool dateAbsent =
                    (dateTime.Year == DateTime.MinValue.Year) &&
                    (dateTime.Month == DateTime.MinValue.Month) &&
                    (dateTime.Day == DateTime.MinValue.Day);

                hasYear = !dateAbsent;
                hasMonth = !dateAbsent;
                hasDay = !dateAbsent;
            }

            //
            // NOTE: A date component is taken from the input only when it and
            //       every lower-order date component are present (Tcl overlays
            //       the date contiguously from the day upward): the day needs
            //       the day; the month needs the month and day; the year needs
            //       the year, month, and day.  Otherwise that component comes
            //       from the base.  (So a lone year, lone month, or year+month
            //       with no day is ignored in favor of the base date.)
            //
            bool useDay = hasDay;
            bool useMonth = useDay && hasMonth;
            bool useYear = useMonth && hasYear;

            int year = useYear ? dateTime.Year : baseDateTime.Year;
            int month = useMonth ? dateTime.Month : baseDateTime.Month;
            int day = useDay ? dateTime.Day : baseDateTime.Day;

            return new DateTime(
                year, month, day, dateTime.Hour, dateTime.Minute,
                dateTime.Second, dateTime.Millisecond, kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally truncates a date and time value to a
        /// granularity expressed in seconds.  When the granularity evenly
        /// divides a day, the value is truncated to the start of the day;
        /// otherwise, it is truncated down to the nearest multiple of the
        /// granularity within the day.
        /// </summary>
        /// <param name="value">
        /// The date and time value to truncate.  If this value is null, the
        /// current UTC date and time is used instead.
        /// </param>
        /// <param name="seconds">
        /// The granularity, in seconds, to truncate to.  If this value is zero
        /// or negative, no truncation is performed.
        /// </param>
        /// <returns>
        /// The truncated date and time value.
        /// </returns>
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

        /// <summary>
        /// This method counts the number of leap years that have elapsed up to
        /// the specified date, adjusting for whether the date falls before or
        /// after the end of February.
        /// </summary>
        /// <param name="value">
        /// The date and time value up to which leap years are counted.
        /// </param>
        /// <returns>
        /// The number of leap years that have elapsed up to the specified date.
        /// </returns>
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
        /// <summary>
        /// This method calculates the three components of a stardate from the
        /// specified date and time value.  The algorithm was adapted from the
        /// [clock] command implementation in Tcl 8.5.
        /// </summary>
        /// <param name="value">
        /// The date and time value to convert to a stardate.
        /// </param>
        /// <param name="part1">
        /// Upon return, this receives the year component of the stardate,
        /// relative to the base year.
        /// </param>
        /// <param name="part2">
        /// Upon return, this receives the fractional day-of-year component of
        /// the stardate.
        /// </param>
        /// <param name="part3">
        /// Upon return, this receives the fractional time-of-day component of
        /// the stardate.
        /// </param>
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

        /// <summary>
        /// This method populates the mapping from duration units to their
        /// singular and plural names, if it has not already been populated.
        /// This method is thread-safe.
        /// </summary>
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

        /// <summary>
        /// This method looks up the name of a duration unit, returning either
        /// the singular or plural form depending on the associated value.  This
        /// method is thread-safe.
        /// </summary>
        /// <param name="key">
        /// The duration unit to look up, expressed in milliseconds (or a
        /// special sentinel value).
        /// </param>
        /// <param name="value">
        /// The quantity of the duration unit, used to decide between the
        /// singular and plural forms of the name.
        /// </param>
        /// <param name="pluralOnly">
        /// When true, the plural form of the name is always returned,
        /// regardless of the value.
        /// </param>
        /// <returns>
        /// The singular or plural name of the duration unit, or null if the
        /// unit has no associated name.
        /// </returns>
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

        /// <summary>
        /// This method formats a set of pre-computed duration components into a
        /// human-readable list of strings, honoring the various formatting
        /// options.
        /// </summary>
        /// <param name="iterations">
        /// The number of iterations performed while computing the duration.
        /// </param>
        /// <param name="millennia">
        /// The whole-millennia component of the duration.
        /// </param>
        /// <param name="centuries">
        /// The whole-centuries component of the duration.
        /// </param>
        /// <param name="decades">
        /// The whole-decades component of the duration.
        /// </param>
        /// <param name="years">
        /// The whole-years component of the duration.
        /// </param>
        /// <param name="months">
        /// The whole-months component of the duration.
        /// </param>
        /// <param name="weeks">
        /// The whole-weeks component of the duration.
        /// </param>
        /// <param name="days">
        /// The whole-days component of the duration.
        /// </param>
        /// <param name="hours">
        /// The whole-hours component of the duration.
        /// </param>
        /// <param name="minutes">
        /// The whole-minutes component of the duration.
        /// </param>
        /// <param name="seconds">
        /// The whole-seconds component of the duration.
        /// </param>
        /// <param name="milliseconds">
        /// The whole-milliseconds component of the duration.
        /// </param>
        /// <param name="ago">
        /// When true, the duration refers to a time in the past, and a suffix
        /// indicating this may be appended.
        /// </param>
        /// <param name="nonZero">
        /// When true, only the non-zero duration components are included.
        /// </param>
        /// <param name="asList">
        /// When true, the result is formatted as a structured list of values
        /// (and optionally names); otherwise, it is formatted as a sequence of
        /// formatted component strings.
        /// </param>
        /// <param name="includeIterations">
        /// When true, the iteration count is included in the result.
        /// </param>
        /// <param name="includeMilliseconds">
        /// When true, the milliseconds component is included in the result.
        /// </param>
        /// <param name="withNames">
        /// When true, the name of each duration unit is included in the result.
        /// </param>
        /// <param name="pluralOnly">
        /// When true, the plural form of each unit name is always used.
        /// </param>
        /// <param name="noPrefix">
        /// When true, the leading approximate-duration prefix is omitted.
        /// </param>
        /// <param name="noSuffix">
        /// When true, the trailing time-in-the-past suffix is omitted.
        /// </param>
        /// <returns>
        /// The list of strings comprising the human-readable duration.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of days in the specified month of the
        /// specified year, accounting for leap years when the month is February.
        /// </summary>
        /// <param name="month">
        /// The ordinal number of the month, from one (January) through twelve
        /// (December).
        /// </param>
        /// <param name="year">
        /// The year, used to determine whether February has an extra day.
        /// </param>
        /// <returns>
        /// The number of days in the specified month, zero if the month is out
        /// of range, or negative one if the resulting index is out of bounds.
        /// </returns>
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

        /// <summary>
        /// This method precisely calculates the duration between two date and
        /// time values, decomposing it into whole millennia, centuries,
        /// decades, years, months, weeks, days, hours, minutes, seconds, and
        /// milliseconds by iterating day-by-day over the interval.
        /// </summary>
        /// <param name="start">
        /// The starting date and time value of the interval.
        /// </param>
        /// <param name="end">
        /// The ending date and time value of the interval.
        /// </param>
        /// <param name="millennia">
        /// Upon return, this receives the whole-millennia component of the
        /// duration.
        /// </param>
        /// <param name="centuries">
        /// Upon return, this receives the whole-centuries component of the
        /// duration.
        /// </param>
        /// <param name="decades">
        /// Upon return, this receives the whole-decades component of the
        /// duration.
        /// </param>
        /// <param name="years">
        /// Upon return, this receives the whole-years component of the duration.
        /// </param>
        /// <param name="months">
        /// Upon return, this receives the whole-months component of the
        /// duration.
        /// </param>
        /// <param name="weeks">
        /// Upon return, this receives the whole-weeks component of the duration.
        /// </param>
        /// <param name="days">
        /// Upon return, this receives the whole-days component of the duration.
        /// </param>
        /// <param name="hours">
        /// Upon return, this receives the whole-hours component of the duration.
        /// </param>
        /// <param name="minutes">
        /// Upon return, this receives the whole-minutes component of the
        /// duration.
        /// </param>
        /// <param name="seconds">
        /// Upon return, this receives the whole-seconds component of the
        /// duration.
        /// </param>
        /// <param name="milliseconds">
        /// Upon return, this receives the whole-milliseconds component of the
        /// duration.
        /// </param>
        /// <param name="ago">
        /// Upon return, this is set to true if the ending value precedes the
        /// starting value (i.e. the duration refers to a time in the past).
        /// </param>
        /// <param name="iterations">
        /// Upon return, this receives the number of iterations performed while
        /// computing the duration.
        /// </param>
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

        /// <summary>
        /// This method computes and formats a human-readable representation of
        /// the duration between two date and time values, honoring the supplied
        /// duration flags.  Depending on the flags, the calculation may be
        /// precise or approximate, and the result may be returned as a single
        /// formatted string or as a structured list.
        /// </summary>
        /// <param name="start">
        /// The starting date and time value of the interval.
        /// </param>
        /// <param name="end">
        /// The ending date and time value of the interval.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the duration is calculated and formatted.
        /// </param>
        /// <returns>
        /// The list of strings comprising the human-readable duration, or null
        /// if an exception is encountered.
        /// </returns>
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

        /// <summary>
        /// This method returns the current local date and time, or the fake
        /// local date and time if one has been set.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The current local date and time.
        /// </returns>
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

        /// <summary>
        /// This method returns the current UTC date and time, or the fake UTC
        /// date and time if one has been set.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The current UTC date and time.
        /// </returns>
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

        /// <summary>
        /// This method sets or clears the fake local date and time that is
        /// returned in place of the actual local date and time.  This method is
        /// thread-safe.
        /// </summary>
        /// <param name="now">
        /// The fake local date and time to return, or null to clear it and
        /// resume returning the actual local date and time.
        /// </param>
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

        /// <summary>
        /// This method sets or clears the fake UTC date and time that is
        /// returned in place of the actual UTC date and time.  This method is
        /// thread-safe.
        /// </summary>
        /// <param name="now">
        /// The fake UTC date and time to return, or null to clear it and resume
        /// returning the actual UTC date and time.
        /// </param>
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

        /// <summary>
        /// This method returns the number of ticks corresponding to the current
        /// UTC date and time (or the fake UTC date and time, if one has been
        /// set).
        /// </summary>
        /// <returns>
        /// The number of ticks corresponding to the current UTC date and time.
        /// </returns>
        public static long GetUtcNowTicks()
        {
            DateTime now = GetUtcNow();

            return now.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the Thursday of the same ISO 8601 week as the
        /// specified date.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time value whose week is used.
        /// </param>
        /// <returns>
        /// The Thursday of the same ISO 8601 week as the specified date.
        /// </returns>
        public static DateTime ThisThursday(
            DateTime dateTime
            )
        {
            return dateTime.AddDays(-(((int)dateTime.DayOfWeek + 6) % 7)).AddDays(3);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns midnight on the first day of the year of the
        /// specified date, preserving its date and time kind.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time value whose year is used.
        /// </param>
        /// <returns>
        /// Midnight on the first day of the year of the specified date.
        /// </returns>
        public static DateTime StartOfYear(
            DateTime dateTime
            )
        {
            return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the date and time representing the start of the
        /// day (midnight) containing the specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time whose containing day is used.
        /// </param>
        /// <returns>
        /// The date and time at the start of the specified day.
        /// </returns>
        private static DateTime StartOfDay(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, dateTime.Day,
                0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the date and time representing the end of the
        /// day (the last whole second) containing the specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time whose containing day is used.
        /// </param>
        /// <returns>
        /// The date and time at the end of the specified day.
        /// </returns>
        private static DateTime EndOfDay(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, dateTime.Day,
                23, 59, 59, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the date and time representing the start of the
        /// month (midnight on the first day) containing the specified date and
        /// time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time whose containing month is used.
        /// </param>
        /// <returns>
        /// The date and time at the start of the specified month.
        /// </returns>
        private static DateTime StartOfMonth(
            DateTime dateTime
            )
        {
            return new DateTime(
                dateTime.Year, dateTime.Month, 1,
                0, 0, 0, dateTime.Kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the date and time representing the end of the
        /// month (the last whole second of the last day) containing the
        /// specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time whose containing month is used.
        /// </param>
        /// <returns>
        /// The date and time at the end of the specified month.
        /// </returns>
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

        /// <summary>
        /// This method returns the date and time representing the end of the
        /// year (the last whole second of December 31st) containing the
        /// specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time whose containing year is used.
        /// </param>
        /// <returns>
        /// The date and time at the end of the specified year.
        /// </returns>
        private static DateTime EndOfYear(
            DateTime dateTime
            )
        {
            return new DateTime(dateTime.Year, 12, 31, 23, 59, 59, dateTime.Kind);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of seconds that have elapsed
        /// between the specified epoch and the current UTC date and time.
        /// </summary>
        /// <param name="seconds">
        /// Upon success, this receives the number of seconds that have elapsed
        /// between the epoch and the current UTC date and time.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which elapsed time is measured.
        /// </param>
        /// <returns>
        /// True if the elapsed time was calculated successfully; otherwise,
        /// false.
        /// </returns>
        public static bool ElapsedSeconds(
            ref double seconds,
            DateTime epoch
            )
        {
            return ElapsedSeconds(ref seconds, GetUtcNow(), epoch);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of whole seconds that have elapsed
        /// between the specified epoch and the specified date and time.
        /// </summary>
        /// <param name="seconds">
        /// Upon success, this receives the number of seconds that have elapsed
        /// between the epoch and the specified date and time.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value up to which elapsed time is measured.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which elapsed time is measured.
        /// </param>
        /// <returns>
        /// True if the elapsed time was calculated successfully; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method calculates the number of whole days that have elapsed
        /// between the specified epoch and the current date and time.
        /// </summary>
        /// <param name="days">
        /// Upon success, this receives the number of days that have elapsed
        /// between the epoch and the current date and time.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which elapsed time is measured.
        /// </param>
        /// <returns>
        /// True if the elapsed time was calculated successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of whole days that have elapsed
        /// between the specified epoch and the specified date and time.
        /// </summary>
        /// <param name="days">
        /// Upon success, this receives the number of days that have elapsed
        /// between the epoch and the specified date and time.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value up to which elapsed time is measured.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which elapsed time is measured.
        /// </param>
        /// <returns>
        /// True if the elapsed time was calculated successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of seconds that have elapsed
        /// between midnight on the specified date and the specified date and
        /// time itself.
        /// </summary>
        /// <param name="seconds">
        /// Upon success, this receives the number of seconds since midnight on
        /// the specified date.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value whose seconds-since-midnight are calculated.
        /// </param>
        /// <returns>
        /// True if the elapsed time was calculated successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of whole seconds represented by the
        /// ticks of the specified date and time value.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time value whose whole seconds are returned.
        /// </param>
        /// <returns>
        /// The number of whole seconds represented by the ticks of the
        /// specified date and time value.
        /// </returns>
        private static long WholeSeconds(
            DateTime dateTime
            )
        {
            return (dateTime.Ticks / TimeSpan.TicksPerSecond);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of days in the specified year,
        /// accounting for leap years.
        /// </summary>
        /// <param name="year">
        /// The year whose number of days is returned.
        /// </param>
        /// <returns>
        /// The number of days in the specified year.
        /// </returns>
        private static long GetDaysInYear(
            int year
            )
        {
            return DaysInNormalYear + ConversionOps.ToLong(
                DateTime.IsLeapYear(year));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of microseconds between the
        /// specified epoch and the specified date and time.
        /// </summary>
        /// <param name="microseconds">
        /// Upon success, this receives the number of microseconds between the
        /// epoch and the specified date and time.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value up to which the microseconds are measured.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which the microseconds are measured.
        /// </param>
        /// <returns>
        /// True if the calculation succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of milliseconds between the
        /// specified epoch and the specified date and time.
        /// </summary>
        /// <param name="milliseconds">
        /// Upon success, this receives the number of milliseconds between the
        /// epoch and the specified date and time.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value up to which the milliseconds are measured.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which the milliseconds are measured.
        /// </param>
        /// <returns>
        /// True if the calculation succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of seconds between the specified
        /// epoch and the specified date and time.
        /// </summary>
        /// <param name="seconds">
        /// Upon success, this receives the number of seconds between the epoch
        /// and the specified date and time.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value up to which the seconds are measured.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which the seconds are measured.
        /// </param>
        /// <returns>
        /// True if the calculation succeeded; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method converts a value expressed in either milliseconds or
        /// seconds (relative to the Unix epoch) into a date and time value,
        /// automatically detecting which unit is in use.
        /// </summary>
        /// <param name="milliseconds">
        /// The value to convert, expressed in milliseconds or, if it is an
        /// exact multiple of one thousand, interpreted as seconds.
        /// </param>
        /// <param name="dateTime">
        /// Upon return, this receives the resulting date and time value.
        /// </param>
        /// <param name="value">
        /// Upon return, this receives the numeric value in the detected units.
        /// </param>
        /// <param name="units">
        /// Upon return, this receives the name of the detected units (either
        /// "seconds" or "milliseconds").
        /// </param>
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

        /// <summary>
        /// This method converts a value expressed in either milliseconds or
        /// seconds (relative to the specified epoch) into a date and time value,
        /// automatically detecting which unit is in use.
        /// </summary>
        /// <param name="milliseconds">
        /// The value to convert, expressed in milliseconds or, if it is an
        /// exact multiple of one thousand, interpreted as seconds.
        /// </param>
        /// <param name="dateTime">
        /// Upon return, this receives the resulting date and time value.
        /// </param>
        /// <param name="value">
        /// Upon return, this receives the numeric value in the detected units.
        /// </param>
        /// <param name="units">
        /// Upon return, this receives the name of the detected units (either
        /// "seconds" or "milliseconds").
        /// </param>
        /// <param name="epoch">
        /// The epoch relative to which the value is interpreted.
        /// </param>
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
        /// <summary>
        /// This method converts a number of milliseconds elapsed since the
        /// specified epoch into the corresponding date and time.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds elapsed since the epoch.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, this receives the date and time corresponding to the
        /// specified number of milliseconds elapsed since the epoch.
        /// </param>
        /// <param name="epoch">
        /// The epoch from which the elapsed milliseconds are measured.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method converts a number of ticks into a date and time value of
        /// the specified kind.
        /// </summary>
        /// <param name="ticks">
        /// The number of ticks to convert.
        /// </param>
        /// <param name="kind">
        /// The date and time kind for the resulting value.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, this receives the resulting date and time value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method converts a number of seconds relative to the specified
        /// epoch into a date and time value.
        /// </summary>
        /// <param name="seconds">
        /// The number of seconds, relative to the epoch, to convert.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, this receives the resulting date and time value.
        /// </param>
        /// <param name="epoch">
        /// The epoch relative to which the seconds are interpreted.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method converts a number of seconds relative to the Unix epoch
        /// into a date and time value.
        /// </summary>
        /// <param name="seconds">
        /// The number of seconds, relative to the Unix epoch, to convert.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, this receives the resulting date and time value.
        /// </param>
        /// <returns>
        /// True if the conversion succeeded; otherwise, false.
        /// </returns>
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
