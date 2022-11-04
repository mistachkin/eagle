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
using Eagle._Attributes;

namespace Eagle._Components.Private
{
    [ObjectId("1e868a77-dae1-45ea-bfc3-279841624af5")]
    internal static class TimeOps
    {
        internal static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // COMPAT: Unix, Tcl.

        internal static readonly DateTime PeEpoch = UnixEpoch; // COMPAT: PE files.

        internal static readonly DateTime BuildEpoch =
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local); // COMPAT: MSBuild.

        internal static int RevisionDivisor = 2; // COMPAT: MSBuild.

        internal static readonly int SecondsInNormalDay = 86400;

        private static readonly int DaysInNormalYear = 365; // NOTE: Non-leap years only.

#if NETWORK
        private static readonly int MillisecondsPerSecond = 1000;
#endif

        private static readonly int TicksPerMicrosecond =
            (int)TimeSpan.TicksPerMillisecond / 1000;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime GetNow()
        {
            return DateTime.Now;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetUtcNowTicks()
        {
            return DateTime.UtcNow.Ticks;
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

        public static long WholeSeconds(
            DateTime dateTime
            )
        {
            return (dateTime.Ticks / TimeSpan.TicksPerSecond);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int DaysInYear(
            int year
            )
        {
            return DaysInNormalYear + ConversionOps.ToInt(DateTime.IsLeapYear(year));
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
