/*
 * FormatOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;

#if NETWORK
using System.Net;
#endif

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

#if CAS_POLICY
using System.Security.Permissions;
using System.Security.Policy;
#endif

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#if !NET_STANDARD_20
using Microsoft.Win32;
#endif

using Eagle._Attributes;
using Eagle._Components.Private.Delegates;

#if NATIVE && TCL
using Eagle._Components.Private.Tcl;
#endif

using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _Result = Eagle._Components.Public.Result;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("62feeba0-3df8-4395-b850-c4d307d021a7")]
    internal static class FormatOps
    {
        #region Private Constants
        private static readonly string TracePriorityFormat = "X2";

        private const string RuntimeSeparator = " - ";
        private const string ConfigurationSeparator = " - ";

        private static readonly string UnknownTypeName = "unknown";

        private static readonly string DisplayNoType = "<noType>";
        private static readonly string DisplayNoAssembly = "<noAssembly>";

        internal static readonly string DisplayNoResult = "<noResult>";
        internal static readonly string DisplayNone = "<none>";
        internal static readonly string DisplayNull = "<null>";
        private static readonly string DisplayNullKey = "<nullKey>";
        internal static readonly string DisplayProxy = "<proxy>";

#if DEBUGGER || SHELL
        private static readonly string DisplayTypeMismatch = "<typeMismatch>";
#endif

#if POLICY_TRACE
        internal static readonly string DisplayObject = "<object>";
#endif

        private static readonly string DisplayNullString = "<nullString>";
        private static readonly string DisplayEmptyString = "<emptyString>";
        internal static readonly string DisplayEmpty = "<empty>";
        private static readonly string DisplaySpace = "<space>";
        internal static readonly string DisplayDisposed = "<disposed>";
        internal static readonly string DisplayBusy = "<busy>";
        private static readonly string DisplayError = "<error>";
        private static readonly string DisplayError0 = "<error:{0}>";
        internal static readonly string DisplayUnknown = "<unknown>";
        private static readonly string DisplayObfuscated = "<obfuscated>";
        internal static readonly string DisplayPresent = "<present>";
        internal static readonly string DisplayFormat = "<{0}>";
        private static readonly string DisplayUnavailable = "<unavailable>";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string WrapPrefix = Characters.QuotationMark.ToString();
        private static string WrapSuffix = Characters.QuotationMark.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string AltWrapPrefix = Characters.OpenBrace.ToString();
        private static string AltWrapSuffix = Characters.CloseBrace.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool AlwaysShowSignatures = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Package Title Formatting
        //
        // HACK: This is purposely not read-only.
        //
        private static bool IncludeBuildSecondsForPackageDateTime = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Ellipsis Limits
        //
        // HACK: These are purposely not read-only.
        //
        private static int ResultEllipsisLimit = 78;

#if HISTORY
        private static int HistoryEllipsisLimit = 78;
#endif

        private static int DefaultEllipsisLimit = 60;
        private static int WrapEllipsisLimit = 200;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string ResultEllipsis = " ...";

#if HISTORY
        private const string HistoryEllipsis = " ...";
#endif

        private const string DefaultEllipsis = "...";

        private const string ByteOutputFormat = "x2";
        private const string ULongOutputFormat = "x16";

        internal const string HexadecimalPrefix = "0x";
        private const string HexadecimalFormat = "{0}{1:X}";

        // private const string HexavigesimalAlphabet = "0123456789ABCDEFGHIJKLMNOP";
        private const string HexavigesimalAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Regex releaseShortNameRegEx = RegExOps.Create(
            "(?:Pre-|Post-)?(?:Alpha|Beta|RC|Final|Release) \\d+(?:\\.\\d+)?",
            RegexOptions.IgnoreCase);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Clock Constants
        private const string GmtTimeZoneName = "GMT";
        private const string UtcTimeZoneName = "UTC";

        private const int Roddenberry = 1946; // Another epoch (Hi, Jeff!)

        private const string DefaultFullDateTimeFormat = "dddd, dd MMMM yyyy HH:mm:ss";

        private const string DayOfYearFormat = "000"; // COMPAT: Tcl
        private const string WeekOfYearFormat = "00"; // COMPAT: Tcl
        private const string Iso8601YearFormat = "00"; // COMPAT: Tcl

#if SHELL
        private const string UpdateDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
#endif

        private const string Iso8601FullDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        private const string TraceDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";
        private const string TraceInteractiveDateTimeFormat = "MM/dd/yyyy HH:mm:ss";

        private const string Iso8601UpdateDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        private const string Iso8601DateTimeOutputFormat = "yyyy.MM.ddTHH:mm:ss.fff";
        private const string PackageDateTimeOutputFormat = "yyyy.MM.dd";

        private const string StardateInputFormat = "%Q"; // COMPAT: Tcl
        private const string StardateOutputFormat = "Stardate {0:D2}{1:D3}{2}{3:D1}";

#if UNIX
        internal static readonly string StringInputFormat = "%s";
#endif

        private static readonly StringPairList tclClockFormats = new StringPairList(
            new StringPair(GmtTimeZoneName, "\\G\\M\\T"),
            new StringPair("%a", "ddd"), new StringPair("%A", "dddd"),
            new StringPair("%b", "MMM"), new StringPair("%B", "MMMM"),
            new StringPair("%c", GetFullDateTimeFormat()), new StringPair("%C", null),
            new StringPair("%d", "dd"), new StringPair("%D", "MM/dd/yy"),
            new StringPair("%e", "%d"), new StringPair("%g", null),
            new StringPair("%G", null), new StringPair("%h", "MMM"),
            new StringPair("%H", "HH"), new StringPair("%i", Iso8601DateTimeOutputFormat),
            new StringPair("%I", "hh"), new StringPair("%j", null),
            new StringPair("%k", "%H"), new StringPair("%l", "%h"),
            new StringPair("%m", "MM"), new StringPair("%M", "mm"),
            new StringPair("%n", Characters.NewLine.ToString()), new StringPair("%p", "tt"),
            new StringPair("%Q", null), new StringPair("%r", "hh:mm:ss tt"),
            new StringPair("%R", "HH:mm"), new StringPair("%s", null),
            new StringPair("%S", "ss"), new StringPair("%t", Characters.HorizontalTab.ToString()),
            new StringPair("%T", "HH:mm:ss"), new StringPair("%u", null),
            new StringPair("%U", null), new StringPair("%V", null),
            new StringPair("%w", null), new StringPair("%W", null),
            new StringPair("%x", "M/d/yyyy"), new StringPair("%X", "h:mm:ss tt"),
            new StringPair("%y", "yy"), new StringPair("%Y", "yyyy"),
            new StringPair("%Z", null), new StringPair("%%", "\\%"));

        private static readonly DelegateDictionary tclClockDelegates = new DelegateDictionary(
            new ObjectPair("%C", new ClockTransformCallback(TclClockDelegates.GetCentury)),
            new ObjectPair("%g", new ClockTransformCallback(TclClockDelegates.GetTwoDigitYearIso8601)),
            new ObjectPair("%G", new ClockTransformCallback(TclClockDelegates.GetFourDigitYearIso8601)),
            new ObjectPair("%j", new ClockTransformCallback(TclClockDelegates.GetDayOfYear)),
            new ObjectPair("%Q", new ClockTransformCallback(TclClockDelegates.GetStardate)),
            new ObjectPair("%s", new ClockTransformCallback(TclClockDelegates.GetSecondsSinceEpoch)),
            new ObjectPair("%u", new ClockTransformCallback(TclClockDelegates.GetWeekdayNumberOneToSeven)),
            new ObjectPair("%U", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearSundayIsFirstDay)),
            new ObjectPair("%V", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearIso8601)),
            new ObjectPair("%w", new ClockTransformCallback(TclClockDelegates.GetWeekdayNumberZeroToSix)),
            new ObjectPair("%W", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearMondayIsFirstDay)),
            new ObjectPair("%Z", new ClockTransformCallback(TclClockDelegates.GetTimeZoneName)));
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string StackTraceStart = "<stackTrace>";
        private static string StackTraceEnd = "</stackTrace>";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the next unique event "serial number" within this
        //       application domain.  It is only ever accessed by this class
        //       (in one place) using an interlocked increment operation in
        //       order to assist in constructing event names that are unique
        //       within the entire application domain (i.e. there are other
        //       aspects of the final event name that ensure it is unique on
        //       this system).
        //
        private static long nextEventId;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the list of method names to skip when figuring out
        //       the "correct" method name to use for trace output.
        //
        private static StringList skipNames = new StringList(
            "DebugTrace", "DebugWrite", "DebugWriteTo", "DebugWriteOrTrace");
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Clock Delegates
        [ObjectId("706d94c0-f87f-4562-abbd-a1917ed99e8c")]
        private static class TclClockDelegates
        {
            public static string GetCentury(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.Year / 100) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetTwoDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (TimeOps.ThisThursday(
                        clockData.DateTime).Year % 100).ToString(Iso8601YearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetFourDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, TimeOps.ThisThursday(clockData.DateTime).Year) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetDayOfYear(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.DayOfYear.ToString(DayOfYearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetStardate(
                IClockData clockData
                )
            {
                return (clockData != null) ? WrapOrNull(true, Stardate(clockData.DateTime)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetSecondsSinceEpoch(
                IClockData clockData
                )
            {
                long seconds = 0;

                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime.ToUniversalTime();
                    DateTime epoch = clockData.Epoch;

                    if (TimeOps.DateTimeToSeconds(ref seconds, dateTime, epoch))
                        return WrapOrNull(true, seconds);
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekdayNumberOneToSeven(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;
                    DayOfWeek dayOfWeek = dateTime.DayOfWeek;

                    //
                    // HACK: Make Sunday have the value of seven (Saturday + 1).
                    //
                    if (dayOfWeek == DayOfWeek.Sunday)
                        dayOfWeek = DayOfWeek.Saturday + 1;

                    return WrapOrNull(true, (int)dayOfWeek);
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearSundayIsFirstDay(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;

                    return WrapOrNull(true, ((dateTime.DayOfYear + 7 -
                        (int)dateTime.DayOfWeek) / 7).ToString(WeekOfYearFormat));
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearIso8601(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    CultureInfo cultureInfo = clockData.CultureInfo;

                    if (cultureInfo != null)
                    {
                        Calendar calendar = cultureInfo.Calendar;

                        if (calendar != null)
                        {
                            DateTime dateTime = clockData.DateTime;

                            return WrapOrNull(true, calendar.GetWeekOfYear(
                                dateTime, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday).ToString(WeekOfYearFormat));
                        }
                    }
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekdayNumberZeroToSix(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (int)clockData.DateTime.DayOfWeek) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearMondayIsFirstDay(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;

                    return WrapOrNull(true, ((dateTime.DayOfYear + 7 -
                        ((dateTime.DayOfWeek != DayOfWeek.Sunday) ?
                            (int)dateTime.DayOfWeek : 6)) / 7).ToString(WeekOfYearFormat));
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetTimeZoneName(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    TimeZone timeZone = clockData.TimeZone;
                    DateTime dateTime = clockData.DateTime;

                    if (timeZone != null)
                    {
                        return WrapOrNull(true, dateTime.IsDaylightSavingTime() ?
                            timeZone.DaylightName : timeZone.StandardName);
                    }
                    else if (dateTime.Kind == DateTimeKind.Utc)
                    {
                        return UtcTimeZoneName;
                    }
                }

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string DisplayTclBuild(
            TclBuild build
            )
        {
            if (build == null)
                return DisplayNull;

            return build.ToString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        public static string RegistrySubKey(
            RegistryKey key,
            string subKeyName
            )
        {
            if (key != null)
            {
                if (subKeyName != null)
                {
                    bool wrap = true;
                    string prefix = WrapPrefix;
                    string suffix = WrapSuffix;
                    string stringValue;

                    MaybeChangeWrapPrefixAndSuffix(
                        wrap, String.Format(
                            "{0}\\{1}", key, subKeyName),
                        ref prefix, ref suffix,
                        out stringValue);

                    return WrapOrNull(
                        wrap, true, false, true, prefix,
                        stringValue, suffix);
                }
                else
                {
                    return WrapOrNull(key);
                }
            }
            else
            {
                return WrapOrNull(subKeyName);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetFullDateTimeFormat()
        {
            DateTimeFormatInfo dateTimeFormatInfo =
                Value.GetDateTimeFormatProvider() as DateTimeFormatInfo;

            if (dateTimeFormatInfo != null)
            {
                string format = dateTimeFormatInfo.FullDateTimePattern;

                if (format != null)
                    return format;
            }

            return DefaultFullDateTimeFormat;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static StringList FlagsEnum(
            Enum enumValue,
            bool noCase,
            bool skipNameless,
            bool skipBadName,
            bool skipBadValue,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            if (enumValue == null)
            {
                error = "invalid value";
                return null;
            }

            Type enumType = enumValue.GetType();

            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    TypeName(enumType));

                return null;
            }

            string[] names = Enum.GetNames(enumType);

            if (names == null)
            {
                error = "invalid enumeration names";
                return null;
            }

            ulong currentUlongValue;

            try
            {
                //
                // NOTE: Get the underlying unsigned long integer
                //       value for the overall enumerated value.
                //       This may throw an exception.
                //
                currentUlongValue = EnumOps.ToUIntOrULong(
                    enumValue); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }

            StringList list = new StringList();

            //
            // NOTE: If the enumerated value is zero, just
            //       return the (empty) result list now.
            //
            if (currentUlongValue == 0)
                return list;

            ulong previousUlongValue = 0;

            foreach (string name in names)
            {
                if (String.IsNullOrEmpty(name))
                {
                    //
                    // TODO: This block should never be hit?
                    //
                    if (!skipNameless)
                    {
                        error = "invalid enumeration name";
                        return null;
                    }
                    else
                    {
                        //
                        // NOTE: No point in calling TryParse
                        //       on something we *know* is not
                        //       valid.
                        //
                        continue;
                    }
                }

                object localEnumValue;
                Result localError = null;

                localEnumValue = EnumOps.TryParse(
                    enumType, name, false, noCase, ref localError);

                if (localEnumValue == null)
                {
                    if (!skipBadName)
                    {
                        error = localError;
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }

                try
                {
                    //
                    // NOTE: Get the underlying unsigned long integer
                    //       value for the current enumerated value.
                    //       This may throw an exception.
                    //
                    ulong localUlongValue = EnumOps.ToUIntOrULong(
                        (Enum)localEnumValue); /* throw */

                    //
                    // NOTE: If the value for the current enumerated
                    //       value is zero, skip it.  The associated
                    //       names will never be added to the result
                    //       unless the "keepZeros" flag is set.
                    //
                    if (localUlongValue == 0)
                    {
                        if (keepZeros)
                            list.Add(name);
                        else
                            continue;
                    }

                    //
                    // NOTE: Check if the overall enumerated value
                    //       has all the bits set from the current
                    //       enumerated value.
                    //
                    if (FlagOps.HasFlags(
                            currentUlongValue, localUlongValue, true) ||
                        (!uniqueValues && FlagOps.HasFlags(
                            previousUlongValue, localUlongValue, true)))
                    {
                        //
                        // NOTE: The current enumerated value has
                        //       now been handled; remove it from
                        //       the overall enumerated value and
                        //       add the name to the result list.
                        //
                        currentUlongValue &= ~localUlongValue;
                        previousUlongValue |= localUlongValue;

                        list.Add(name);

                        //
                        // NOTE: If the value is now zero, then we
                        //       are done.
                        //
                        if (currentUlongValue == 0)
                            break;
                    }
                }
                catch (Exception e)
                {
                    if (!skipBadValue)
                    {
                        error = e;
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            //
            // NOTE: If there are any residual bit values within the
            //       overall enumerated value, add them verbatim to
            //       the result list.
            //
            if (currentUlongValue != 0)
                list.Add(currentUlongValue.ToString());

            return list;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList FlagsEnumV2(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipEnumType,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            StringList localEnumNames = (enumNames != null) ?
                new StringList(enumNames) : null;

            UlongList localEnumValues = (enumValues != null) ?
                new UlongList(enumValues) : null;

            if (!skipEnumType)
            {
                if (enumValue == null)
                {
                    error = "invalid value";
                    return null;
                }

                Type enumType = enumValue.GetType();

                if (enumType == null)
                {
                    error = "invalid type";
                    return null;
                }

                if (!enumType.IsEnum)
                {
                    error = String.Format(
                        "type {0} is not an enumeration",
                        TypeName(enumType));

                    return null;
                }

                if (EnumOps.GetNamesAndValues(
                        enumType, ref localEnumNames, ref localEnumValues,
                        ref error) != ReturnCode.Ok)
                {
                    return null;
                }
            }

            return FlagsEnumCore(
                enumValue, localEnumNames, localEnumValues, skipNameless,
                keepZeros, uniqueValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringList FlagsEnumCore(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            if (enumValue == null)
            {
                error = "invalid value";
                return null;
            }

            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return null;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return null;
            }

            if (enumNames.Count != enumValues.Count)
            {
                error = "mismatched names and values counts";
                return null;
            }

            ulong currentUlongValue;

            try
            {
                //
                // NOTE: Get the underlying unsigned long integer
                //       value for the overall enumerated value.
                //       This may throw an exception.
                //
                currentUlongValue = EnumOps.ToUIntOrULong(
                    enumValue); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }

            StringList list = new StringList();

            //
            // NOTE: If the enumerated value is zero, just return
            //       the (empty) result list now.
            //
            if (currentUlongValue == 0)
                return list;

            int count = enumNames.Count;
            ulong previousUlongValue = 0;

            for (int index = 0; index < count; index++)
            {
                string localEnumName = enumNames[index];

                if (String.IsNullOrEmpty(localEnumName))
                {
                    //
                    // TODO: This block should never be hit?
                    //
                    if (!skipNameless)
                    {
                        error = "invalid enumeration name";
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }

                ulong localEnumValue = enumValues[index];

                //
                // NOTE: If the value for the current enumerated
                //       value is zero, skip it.  The associated
                //       names will never be added to the result
                //       unless the "keepZeros" flag is set.
                //
                if (localEnumValue == 0)
                {
                    if (keepZeros)
                        list.Add(localEnumName);
                    else
                        continue;
                }

                //
                // NOTE: Check if the overall enumerated value
                //       has all the bits set from the current
                //       enumerated value.
                //
                if (FlagOps.HasFlags(
                        currentUlongValue, localEnumValue, true) ||
                    (!uniqueValues && FlagOps.HasFlags(
                        previousUlongValue, localEnumValue, true)))
                {
                    //
                    // NOTE: The current enumerated value has
                    //       now been handled; remove it from
                    //       the overall enumerated value and
                    //       add the name to the result list.
                    //
                    currentUlongValue &= ~localEnumValue;
                    previousUlongValue |= localEnumValue;

                    list.Add(localEnumName);

                    //
                    // NOTE: If the value is now zero, then we
                    //       are done.
                    //
                    if (currentUlongValue == 0)
                        break;
                }
            }

            //
            // NOTE: If there are any residual bit values within the
            //       overall enumerated value, add them verbatim to
            //       the result list.
            //
            if (currentUlongValue != 0)
                list.Add(currentUlongValue.ToString());

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayRegExMatch(
            Match match
            )
        {
            if (match == null)
                return DisplayNull;

            if (!match.Success)
                return "<notSuccess>";

            GroupCollection groups = match.Groups;

            if (groups == null)
                return "<nullGroups>";

            if (groups.Count == 0)
                return "<groupZeroMissing>";

            Group group = groups[0];

            if (group == null)
                return "<groupZeroNull>";

            return String.Format(
                "index {0}, length {1}, value {2}", group.Index,
                group.Length, WrapOrNull(group.Value));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string TclBuildFileName(
            TclBuild build
            )
        {
            if (build == null)
                return DisplayNull;

            return WrapOrNull(build.FileName);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayCallFrame(
            ICallFrame frame
            )
        {
            if (frame == null)
                return DisplayNull;

            return String.Format(
                "{0} ({1})", frame.Name, frame.FrameId).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayColor(
            ConsoleColor color
            )
        {
            return (color != _ConsoleColor.None) ? color.ToString() : "None";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static StringList ConsoleKeyInfo(
            ConsoleKeyInfo consoleKeyInfo
            )
        {
            return new StringList(
                "Modifiers", consoleKeyInfo.Modifiers.ToString(),
                "Key", consoleKeyInfo.Key.ToString(),
                "KeyChar", consoleKeyInfo.KeyChar.ToString());
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static string DisplayWaitHandle(
            WaitHandle waitHandle
            )
        {
            if (waitHandle != null)
                return String.Format("{0}", waitHandle.Handle);

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayWaitHandles(
            WaitHandle[] waitHandles
            )
        {
            if (waitHandles != null)
            {
                StringBuilder result = StringOps.NewStringBuilder();

                for (int index = 0; index < waitHandles.Length; index++)
                {
                    WaitHandle waitHandle = waitHandles[index];

                    if (waitHandle != null)
                    {
                        if (result.Length > 0)
                            result.Append(Characters.Space);

                        result.Append(DisplayWaitHandle(waitHandle));
                    }
                }

                return result.ToString();
            }

            return DisplayNull;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_ARGUMENTS
        public static string WrapTraceOrNull(
            bool normalize,
            bool ellipsis,
            bool quote,
            bool display,
            object value
            )
        {
            string result = StringOps.GetStringFromObject(value);

            if (result != null)
            {
                if (result.Length > 0)
                {
                    try
                    {
                        if (normalize)
                        {
                            result = StringOps.NormalizeWhiteSpace(
                                result, Characters.Space,
                                WhiteSpaceFlags.FormattedUse);
                        }

                        if (ellipsis)
                        {
                            result = Ellipsis(result, GetEllipsisLimit(
                                WrapEllipsisLimit), false);
                        }

                        return quote ? StringList.MakeList(result) : result;
                    }
                    catch (Exception e)
                    {
                        Type type = (e != null) ? e.GetType() : null;

                        return String.Format(DisplayError0,
                            (type != null) ? type.Name : UnknownTypeName);
                    }
                }

                return display ? DisplayEmpty : String.Empty;
            }

            return display ? DisplayNull : null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ToString(
            IBinder binder,
            CultureInfo cultureInfo,
            object value,
            string @default
            )
        {
            IScriptBinder scriptBinder = binder as IScriptBinder;

            if (scriptBinder == null)
                goto fallback;

            Type type = AppDomainOps.MaybeGetTypeOrObject(value);

            if (!scriptBinder.HasToStringCallback(type, false))
                goto fallback;

            IChangeTypeData changeTypeData = new ChangeTypeData(
                "FormatOps.ToString", type, value, null, cultureInfo, null,
                MarshalFlags.None);

            ReturnCode code;
            Result error = null;

            code = scriptBinder.ToString(changeTypeData, ref error);

            if (code == ReturnCode.Ok)
            {
                string stringValue = changeTypeData.NewValue as string;

                if (stringValue == null)
                    goto fallback;

                return stringValue;
            }
            else
            {
                DebugOps.Complain(code, error);
            }

        fallback:

            return StringOps.GetStringFromObject(value, @default, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodArguments(
            IBinder binder,
            CultureInfo cultureInfo,
            IEnumerable<object> args,
            bool display
            )
        {
            string @default = display ? DisplayNull : null;

            if (args == null)
                return @default;

            StringList list = new StringList();
            int index = 0;

            foreach (object arg in args)
            {
                //
                // TODO: Review this usage of the TypeName() method.
                //
                string typeName;

                if (arg == null)
                {
                    typeName = @default;
                }
                else if (AppDomainOps.MaybeGetTypeName(arg, out typeName))
                {
                    if (typeName == null)
                        typeName = @default;
                }
                else
                {
                    typeName = TypeName(arg.GetType(), @default, false);
                }

                list.Add(StringList.MakeList(index, typeName,
                    ToString(binder, cultureInfo, arg, @default)));

                index++;
            }

            if (list.Count == 0)
                return display ? DisplayEmpty : null;

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapArgumentsOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> args
            )
        {
            if (args == null)
                return DisplayNull;

            return WrapOrNull(normalize, ellipsis, new StringList(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptForLog(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            if (value == null)
                return DisplayNull;

            string text = value.ToString();

            if (text == null) /* NOTE: Impossible? */
                return DisplayNullString;

            if (text.Length == 0)
                return DisplayEmpty;

            if (text.Trim().Length == 0)
                return DisplaySpace;

            if (normalize)
            {
                text = StringOps.NormalizeWhiteSpace(
                    text, Characters.Space,
                    WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
            {
                text = Ellipsis(text, GetEllipsisLimit(WrapEllipsisLimit),
                    false);
            }

            return StringList.MakeList(text);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeConvertToStringList(
            ref IEnumerable<string> value /* in, out */
            )
        {
            if ((value != null) && !(value is IStringList))
                value = new StringList(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeChangeWrapPrefixAndSuffix(
            bool wrap,         /* in */
            object value,      /* in */
            ref string prefix, /* in, out */
            ref string suffix  /* in, out */
            )
        {
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeChangeWrapPrefixAndSuffix(
            bool wrap,             /* in */
            object value,          /* in */
            ref string prefix,     /* in, out */
            ref string suffix,     /* in, out */
            out string stringValue /* out */
            )
        {
            if (wrap)
            {
                stringValue = StringOps.GetStringFromObject(value);

                if (stringValue != null)
                {
                    if (((prefix != null) &&
                            (stringValue.IndexOf(prefix) != Index.Invalid)) ||
                        ((suffix != null) &&
                            (stringValue.IndexOf(suffix) != Index.Invalid)))
                    {
                        if (prefix != null)
                            prefix = AltWrapPrefix;

                        if (suffix != null)
                            suffix = AltWrapSuffix;
                    }
                }
            }
            else
            {
                //
                // HACK: This is a placeholder value and WILL NOT actually
                //       be used.
                //
                stringValue = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            ReturnCode code,
            Result result
            )
        {
            object value = ResultOps.Format(code, result);

            bool wrap = true;
            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            return WrapOrNull(
                wrap, true, false, true, prefix,
                stringValue, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> value
            )
        {
            return WrapOrNull(normalize, ellipsis, false, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return WrapOrNull(normalize, ellipsis, false, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            IEnumerable<string> value
            )
        {
            MaybeConvertToStringList(ref value);

            bool wrap = (value != null);
            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            return WrapOrNull(
                wrap, normalize, ellipsis, display,
                prefix, stringValue, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            object value
            )
        {
            bool wrap = (value != null);
            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            return WrapOrNull(
                wrap, normalize, ellipsis, display,
                prefix, stringValue, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string StripOuter(
            string value,
            char character
            )
        {
            return StripOuter(value, character, character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string StripOuter(
            string value,
            char prefix,
            char suffix
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            int length = value.Length;

            if (length < 2) /* i.e. prefix + suffix */
                return value;

            int prefixIndex = value.IndexOf(prefix);

            if (prefixIndex != 0)
                return value;

            int suffixIndex = value.LastIndexOf(suffix);

            if (suffixIndex != (length - 1))
                return value;

            return value.Substring(prefixIndex + 1, length - 2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object MaybeNull(
            object value
            )
        {
            if (value == null)
                return DisplayNull;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ReleaseAttribute(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            //
            // NOTE: Skip special handling if the regular expression
            //       pattern that we need is not available.
            //
            if (releaseShortNameRegEx != null)
            {
                //
                // NOTE: The convention used here is that the release
                //       attribute contains a string of the format:
                //
                //       "<Short_Description>(\n|.|,) <Type> XY"
                //
                //       Where "Short_Description" is something like
                //       "Namespaces Edition", "Type" is one of
                //       ["Alpha", "Beta", "Final", "Release"] and
                //       "XY" is a number.  Together, the "Type" and
                //       "XY" portion are considered to really be the
                //       "Short_Name".
                //
                int index = value.LastIndexOf(Characters.LineFeed);

                if (index == Index.Invalid)
                    index = value.LastIndexOf(Characters.Comma);

                if (index == Index.Invalid)
                    index = value.LastIndexOf(Characters.Period);

                //
                // NOTE: Extract the "Short_Name" portion of the value.
                //
                if (index != Index.Invalid)
                {
                    string partOne = value.Substring(0, index).Trim();
                    string partTwo = value.Substring(index + 1).Trim();

                    if (releaseShortNameRegEx.IsMatch(partTwo))
                        return partTwo;
                    else if (releaseShortNameRegEx.IsMatch(partOne))
                        return partOne;
                }
            }

            //
            // NOTE: Return the whole original string, with extra
            //       spaces removed, possibly wrapped in quotes.
            //
            return WrapOrNull(value.Trim());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static string Exists(
            bool exists
            )
        {
            return exists ?
                "already exists" : "does not exist";
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeAndWrapOrNull(
            object value
            )
        {
            bool wrap = (value != null);
            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            Type type = wrap ?
                AppDomainOps.MaybeGetType(value) :
                typeof(object); /* null value */

            return String.Format(
                "{0}object {1} of type {2} with value {3}",
                AppDomainOps.IsTransparentProxy(value) ?
                    "proxy " : String.Empty, String.Format(
                "0x{0:X}", RuntimeOps.GetHashCode(value)),
                TypeName(type, wrap), WrapOrNull(wrap, true,
                false, true, prefix, stringValue, suffix));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        public static string WrapOrNull(
            byte[] bytes
            )
        {
            if (bytes == null)
                return DisplayNull;

            return Parser.Quote(StringList.MakeList(
                "Length", bytes.Length, "Base64", (bytes.Length > 0) ?
                Convert.ToBase64String(bytes) : DisplayEmpty));
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            IEnumerable<string> value
            )
        {
            return WrapOrNull((value != null), value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            object value
            )
        {
            return WrapOrNull((value != null), value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WrapOrNull(
            bool wrap,
            IEnumerable<string> value
            )
        {
            MaybeConvertToStringList(ref value);

            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            return WrapOrNull(
                wrap, false, false, true, prefix,
                stringValue, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WrapOrNull(
            bool wrap,
            object value
            )
        {
            string prefix = WrapPrefix;
            string suffix = WrapSuffix;
            string stringValue;

            MaybeChangeWrapPrefixAndSuffix(
                wrap, value, ref prefix, ref suffix,
                out stringValue);

            return WrapOrNull(
                wrap, false, false, true, prefix,
                stringValue, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WrapOrNull(
            bool wrap,
            bool normalize,
            bool ellipsis,
            bool display,
            string prefix,
            string value,
            string suffix
            )
        {
            if (wrap)
            {
                try
                {
                    string result = value;

                    if (normalize)
                    {
                        result = StringOps.NormalizeWhiteSpace(
                            result, Characters.Space,
                            WhiteSpaceFlags.FormattedUse);
                    }

                    if (ellipsis)
                    {
                        result = Ellipsis(result, GetEllipsisLimit(
                            WrapEllipsisLimit), false);
                    }

                    if (display)
                    {
                        if (result == null)
                            return DisplayNull;

                        if (result.Length == 0)
                            return DisplayEmpty;
                    }

                    return String.Format(
                        "{0}{1}{2}", prefix, result, suffix);
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public static bool HaveCacheCounts(
            long[] counts
            )
        {
            if (counts == null)
                return false;

            if (counts.Length < (int)CacheCountType.SizeOf)
                return false;

            for (int index = 0; index < (int)CacheCountType.SizeOf; index++)
                if (counts[index] > 0) return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CacheCounts(
            long[] counts,
            bool empty
            )
        {
            if ((counts != null) &&
                (counts.Length >= (int)CacheCountType.SizeOf))
            {
                long hit = counts[(int)CacheCountType.Hit];
                long miss = counts[(int)CacheCountType.Miss];
                long skip = counts[(int)CacheCountType.Skip];
                long collide = counts[(int)CacheCountType.Collide];
                long found = counts[(int)CacheCountType.Found];
                long notFound = counts[(int)CacheCountType.NotFound];
                long add = counts[(int)CacheCountType.Add];
                long change = counts[(int)CacheCountType.Change];
                long remove = counts[(int)CacheCountType.Remove];
                long clear = counts[(int)CacheCountType.Clear];
                long trim = counts[(int)CacheCountType.Trim];
                long total = hit + miss;

                double percent = (total != 0) ?
                    ((double)hit / (double)total) * 100 : 0;

                StringList list = new StringList();

                if (empty || (percent > 0))
                    list.Add("hit%", String.Format("{0:0.####}%", percent));

                if (empty || (hit > 0))
                    list.Add("hit", hit.ToString());

                if (empty || (miss > 0))
                    list.Add("miss", miss.ToString());

                if (empty || (skip > 0))
                    list.Add("skip", skip.ToString());

                if (empty || (collide > 0))
                    list.Add("collide", collide.ToString());

                if (empty || (found > 0))
                    list.Add("found", found.ToString());

                if (empty || (notFound > 0))
                    list.Add("notFound", notFound.ToString());

                if (empty || (add > 0))
                    list.Add("add", add.ToString());

                if (empty || (change > 0))
                    list.Add("change", change.ToString());

                if (empty || (remove > 0))
                    list.Add("remove", remove.ToString());

                if (empty || (clear > 0))
                    list.Add("clear", clear.ToString());

                if (empty || (trim > 0))
                    list.Add("trim", trim.ToString());

                return list.ToString();
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CountDictionary(
            IEnumerable<KeyValuePair<string, int>> collection
            )
        {
            if (collection == null)
                return DisplayNull;

            int count = 0;

            foreach (KeyValuePair<string, int> pair in collection)
                count += pair.Value;

            return count.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DumpDictionary(
            IEnumerable<KeyValuePair<string, int>> collection,
            string hashAlgorithmName,
            bool raw
            )
        {
            if (collection == null)
                return DisplayNull;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (KeyValuePair<string, int> pair in collection)
            {
                if (builder.Length > 0)
                    builder.Append(Characters.NewLine);

                string key = pair.Key;

                if (key == null)
                {
                    builder.AppendFormat(DisplayNullKey);
                }
                else if (raw)
                {
                    builder.AppendFormat("{0}{1}{2}",
                        Characters.OpenBrace, key, Characters.CloseBrace);
                }
                else
                {
                    builder.Append(ArrayOps.ToHexadecimalString(
                        HashOps.HashString(hashAlgorithmName, (string)null,
                        key)));
                }

                builder.AppendFormat(
                    "{0}{1}", Characters.HorizontalTab, pair.Value);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string KeysAndValues(
            _Containers.Public.StringDictionary dictionary,
            bool display,
            bool normalize,
            bool ellipsis
            )
        {
            string result = (dictionary != null) ?
                dictionary.KeysAndValuesToString(null, false) : null;

            return display ? WrapOrNull(normalize, ellipsis, result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NameValueCollection(
            NameValueCollection collection,
            bool display
            )
        {
            if (collection == null)
                return display ? DisplayNull : null;

            StringList list = null;
            int count = collection.Count;

            if (count > 0)
            {
                list = new StringList();

                for (int index = 0; index < count; index++)
                {
                    list.Add(collection.GetKey(index));
                    list.Add(collection.Get(index));
                }
            }

            if (list == null)
                return display ? DisplayEmpty : null;

            return WrapOrNull(list);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Result ComplaintResult(
            Result result
            )
        {
            if (result != null)
            {
                string resultString = result;

                if (resultString != null)
                {
                    if (resultString.Length > 0)
                    {
                        if (!StringOps.IsLogicallyEmpty(
                                resultString))
                        {
                            return result;
                        }
                        else
                        {
                            return _Result.Copy(
                                result, DisplaySpace,
                                ResultFlags.Complaint);
                        }
                    }
                    else
                    {
                        return _Result.Copy(
                            result, DisplayEmptyString,
                            ResultFlags.Complaint);
                    }
                }
                else
                {
                    return _Result.Copy(
                        result, DisplayNullString,
                        ResultFlags.Complaint);
                }
            }
            else
            {
                return DisplayNull;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Complaint(
            long id,
            ReturnCode code,
            Result result,
            string stackTrace
            )
        {
            Result localResult = ComplaintResult(result);

            string resultStackTrace = (localResult != null) ?
                localResult.StackTrace : null;

            return ThreadMessage(
                GlobalState.GetCurrentSystemThreadId(),
                id, ResultOps.Format(code, localResult),
                resultStackTrace, stackTrace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ThreadIdOrNull(
            Thread thread
            )
        {
            if (thread == null)
                return DisplayNull;

            return thread.ManagedThreadId.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ThreadIdNoThrow(
            Thread thread
            )
        {
            try
            {
                return ThreadIdOrNull(thread);
            }
            catch (Exception e)
            {
                Type type = (e != null) ? e.GetType() : null;

                return String.Format(DisplayError0,
                    (type != null) ? type.Name : UnknownTypeName);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayThread(
            Thread thread
            )
        {
            if (thread != null)
            {
                try
                {
                    StringBuilder result = StringOps.NewStringBuilder();

                    result.AppendFormat(
                        "{0}: {1}, ", "Name", WrapOrNull(thread.Name));

                    result.AppendFormat(
                        "{0}: {1}, ", "ManagedThreadId", thread.ManagedThreadId);

                    result.AppendFormat(
                        "{0}: {1}, ", "Priority", thread.Priority);

                    result.AppendFormat(
                        "{0}: {1}, ", "ApartmentState", thread.ApartmentState);

                    result.AppendFormat(
                        "{0}: {1}, ", "ThreadState", thread.ThreadState);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsAlive", thread.IsAlive);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsBackground", thread.IsBackground);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsThreadPoolThread", thread.IsThreadPoolThread);

                    result.AppendFormat(
                        "{0}: {1}, ", "CurrentCulture", WrapOrNull(thread.CurrentCulture));

                    result.AppendFormat(
                        "{0}: {1}", "CurrentUICulture", WrapOrNull(thread.CurrentUICulture));

                    return result.ToString();
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ProcedureBody(
            string body,
            int startLine,
            bool showLines
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(body))
            {
                if (showLines)
                {
                    int line = (startLine != Parser.NoLine)
                        ? startLine : Parser.StartLine;

                    int count = Parser.CountLines(body);

                    string format = "{0," + (MathOps.Log10(line +
                        count) + ((count >= 10) ? 1 : 0)).ToString() + "}: ";

                    result.AppendFormat(format, line++);

                    for (int index = 0; index < body.Length; index++)
                    {
                        char character = body[index];

                        result.Append(character);

                        if (Parser.IsLineTerminator(character))
                            result.AppendFormat(format, line++);
                    }
                }
                else
                {
                    result.Append(body);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ThreadMessage(
            long threadId,
            long id,
            string message,
            string resultStackTrace,
            string complainStackTrace
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder(
                String.Format("{0} ({1}): {2}", threadId, id,
                DisplayString(message)));

            if (!String.IsNullOrEmpty(resultStackTrace))
            {
                builder.AppendFormat("{0}{0}RESULT STACK{0}{1}",
                    Environment.NewLine, resultStackTrace);
            }

            if (!String.IsNullOrEmpty(complainStackTrace))
            {
                builder.AppendFormat("{0}{0}COMPLAIN STACK{0}{1}",
                    Environment.NewLine, complainStackTrace);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            byte value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(ByteOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            ulong value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(ULongOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            ValueType value,
            bool prefix
            )
        {
            return String.Format(
                HexadecimalFormat,
                prefix ? HexadecimalPrefix : String.Empty, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexavigesimal(
            ulong value,
            byte width
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (value > 0)
            {
                do
                {
                    //
                    // NOTE: Get the current digit.
                    //
                    ulong digit = value % (ulong)HexavigesimalAlphabet.Length;

                    //
                    // NOTE: Append it to the result.
                    //
                    result.Append(HexavigesimalAlphabet[(int)digit]);

                    //
                    // NOTE: Advance to the next digit.
                    //
                    value /= (ulong)HexavigesimalAlphabet.Length;

                    //
                    // NOTE: Continue until we no longer need more digits.
                    //
                } while (value > 0);

                //
                // NOTE: Finally, reverse the string to put the digits in
                //       the correct order.
                //
                result = StringOps.NewStringBuilder(
                    StringOps.StrReverse(result.ToString()));
            }
            else
            {
                //
                // NOTE: The value is exactly zero.
                //
                result.Append(HexavigesimalAlphabet[0]);
            }

            //
            // NOTE: If requested, 'zero' pad to the requested width.
            //
            if (width > result.Length)
                result.Insert(0, StringOps.StrRepeat(width - result.Length, HexavigesimalAlphabet[0]));

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK && WEB
        public static string DisplayByteLength(
            byte[] bytes
            )
        {
            if (bytes == null)
                return DisplayNull;

            int length = bytes.Length;

            if (length == 0)
                return DisplayEmpty;

            return String.Format("{0} {1}", length,
                (length == 1) ? "byte" : "bytes");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayStringLength(
            string text
            )
        {
            if (text == null)
                return DisplayNull;

            int length = text.Length;

            if (length == 0)
                return DisplayEmpty;

            return String.Format("{0} {1}", length,
                (length == 1) ? "character" : "characters");
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageDateTime(
            DateTime value
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder(
                value.ToString(PackageDateTimeOutputFormat));

            if (IncludeBuildSecondsForPackageDateTime)
            {
                double seconds = 0.0;

                if (TimeOps.SecondsSinceStartOfDay(ref seconds, value))
                {
                    builder.AppendFormat(
                        ".{0}", Math.Truncate(Math.Truncate(seconds) /
                        TimeOps.RevisionDivisor));
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TranslateDateTimeFormats(
            CultureInfo cultureInfo,
            TimeZone timeZone,
            string format,
            DateTime dateTime,
            DateTime epoch,
            bool useFormats,
            bool useDelegates
            )
        {
            if (!String.IsNullOrEmpty(format))
            {
                if (useFormats && (tclClockFormats != null))
                {
                    foreach (IPair<string> element in tclClockFormats)
                    {
                        if ((element != null) &&
                            !String.IsNullOrEmpty(element.X))
                        {
                            if (element.Y != null)
                                format = format.Replace(element.X, element.Y);
                        }
                    }
                }

                if (useDelegates && (tclClockDelegates != null))
                {
                    IClockData clockData = new ClockData(null, cultureInfo, timeZone,
                        format, dateTime, epoch, ClientData.Empty);

                    foreach (KeyValuePair<string, Delegate> pair in tclClockDelegates)
                    {
                        if ((pair.Value != null) && (format.IndexOf(pair.Key,
                                SharedStringOps.SystemComparisonType) != Index.Invalid))
                        {
                            string newValue = pair.Value.DynamicInvoke(clockData) as string;

                            if (newValue != null)
                                format = format.Replace(pair.Key, newValue);
                        }
                    }
                }
            }

            return format;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TclClockDateTime(
            CultureInfo cultureInfo,
            TimeZone timeZone,
            string format,
            DateTime dateTime,
            DateTime epoch
            )
        {
            if (!String.IsNullOrEmpty(format))
            {
                format = TranslateDateTimeFormats(
                    cultureInfo, timeZone, format, dateTime, epoch, true, true);

                if (format.Trim().Length > 0)
                {
                    return (cultureInfo != null) ?
                        dateTime.ToString(format, cultureInfo) :
                        dateTime.ToString(format);
                }
            }

            return format;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The reason to retain this wrapper method is to keep
        //       its intent clear as "mainly for cosmetic purposes"
        //       (i.e. it is only used when displaying strings).
        //
        public static string NormalizeNewLines(
            string value
            )
        {
            return StringOps.NormalizeLineEndings(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ReplaceNewLines(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = StringOps.NewStringBuilder(value);

            StringOps.FixupDisplayLineEndings(builder, false);

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayEngineResult(
            Result value
            )
        {
            if (value != null)
            {
                StringBuilder result = StringOps.NewStringBuilder();

                result.AppendFormat(
                    "{0}: {1}, ", "ReturnCode", value.ReturnCode);

                result.AppendFormat(
                    "{0}: {1}, ", "Result",
                    WrapOrNull(true, true, value));

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorLine", value.ErrorLine);

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorCode", value.ErrorCode);

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorInfo",
                    WrapOrNull(true, true, value.ErrorInfo));

                result.AppendFormat(
                    "{0}: {1}, ", "PreviousReturnCode", value.PreviousReturnCode);

                result.AppendFormat(
                    "{0}: {1}", "Exception",
                    WrapOrNull(true, true, value.Exception));

                return result.ToString();
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayResult(
            string value,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            if (value != null)
            {
                if (value.Length > 0)
                    return Result(value, ellipsis, replaceNewLines);
                else
                    return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayValue(
            string value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value.Length == 0)
                return DisplayEmpty;

            string trimmed = value.Trim();

            if (trimmed.Length == 0)
                return DisplaySpace;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Result(
            string value,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            string result = value;

            if (ellipsis)
            {
                result = Ellipsis(
                    result, 0, (result != null) ? result.Length : 0,
                    GetEllipsisLimit(ResultEllipsisLimit), false,
                    ResultEllipsis);
            }

            if (replaceNewLines)
                result = ReplaceNewLines(NormalizeNewLines(result));

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            IClientData clientData
            )
        {
            PerformanceClientData performanceClientData =
                clientData as PerformanceClientData;

            return (performanceClientData != null) ?
                Performance(performanceClientData.Microseconds) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            double microseconds
            )
        {
            return Performance(microseconds, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            double microseconds,
            string suffix
            )
        {
            return String.Format(
                "{0:0.####} {1}microseconds per iteration",
                Interpreter.FixIntermediatePrecision(microseconds), suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PerformanceWithStatistics(
            long requestedIterations,
            long actualIterations,
            long resultIterations,
            ReturnCode code,
            Result result,
            long startCount,
            long stopCount,
            long? minimumIterationCount,
            long? maximumIterationCount,
            bool obfuscate
            )
        {
            StringList localResult = new StringList();

            if (!obfuscate)
            {
                localResult.Add(String.Format(
                    "{0} requested iterations", requestedIterations));

                localResult.Add(String.Format(
                    "{0} actual iterations", actualIterations));

                localResult.Add(String.Format(
                    "{0} result iterations", resultIterations));
            }

            localResult.Add(
                new StringPair("code", code.ToString()).ToString());

            if (result != null)
            {
                localResult.Add(
                    new StringPair("result", result).ToString());
            }

            if (!obfuscate)
            {
                localResult.Add(String.Format(
                    "{0} raw start count", startCount));

                localResult.Add(String.Format(
                    "{0} raw stop count", stopCount));

                localResult.Add(String.Format("{0} count per second",
                    PerformanceOps.GetCountsPerSecond()));
            }

            double averageMicroseconds = (resultIterations != 0) ?
                PerformanceOps.GetMicroseconds(startCount,
                    stopCount, resultIterations, false) : 0;

            double minimumMicroseconds = PerformanceOps.GetMicroseconds(
                (minimumIterationCount != null) ?
                    (long)minimumIterationCount : 0, 1, false);

            double maximumMicroseconds = PerformanceOps.GetMicroseconds(
                (maximumIterationCount != null) ?
                    (long)maximumIterationCount : 0, 1, false);

            if (obfuscate)
            {
                averageMicroseconds = PerformanceOps.ObfuscateMicroseconds(
                    averageMicroseconds);

                minimumMicroseconds = PerformanceOps.ObfuscateMicroseconds(
                    minimumMicroseconds);

                maximumMicroseconds = PerformanceOps.ObfuscateMicroseconds(
                    maximumMicroseconds);
            }

            localResult.Add(Performance(averageMicroseconds, "average "));
            localResult.Add(Performance(minimumMicroseconds, "minimum "));
            localResult.Add(Performance(maximumMicroseconds, "maximum "));

            return localResult.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
        public static StringPair HistoryItem(
            int count,
            IClientData clientData,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            HistoryClientData historyClientData = clientData as HistoryClientData;

            if (historyClientData != null)
            {
                ArgumentList arguments = historyClientData.Arguments;

                string value = StringOps.GetStringFromObject(
                    arguments, DisplayNull, true);

                if (ellipsis)
                {
                    value = Ellipsis(
                        value, 0, (value != null) ? value.Length : 0,
                        GetEllipsisLimit(HistoryEllipsisLimit), false,
                        HistoryEllipsis);
                }

                if (replaceNewLines)
                    value = ReplaceNewLines(NormalizeNewLines(value));

                return new StringPair(String.Format(
                    "#{0}, Level {1}, {2}", count, historyClientData.Levels,
                    historyClientData.Flags), value);
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int GetEllipsisLimit(
            int @default
            )
        {
            string value = CommonOps.Environment.GetVariable(
                EnvVars.EllipsisLimit);

            if (!String.IsNullOrEmpty(value))
            {
                int intValue = 0;

                if (Value.GetInteger2(
                        value, ValueFlags.AnyInteger, null,
                        ref intValue) == ReturnCode.Ok)
                {
                    return intValue;
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value
            )
        {
            return Ellipsis(
                value, 0, (value != null) ? value.Length : 0,
                GetEllipsisLimit(DefaultEllipsisLimit), false,
                DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value,
            int limit,
            bool strict
            )
        {
            return Ellipsis(value, 0, (value != null) ?
                value.Length : 0, limit, strict, DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value,
            int startIndex,
            int length,
            int limit,
            bool strict
            )
        {
            return Ellipsis(value, startIndex, length, limit, strict, DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Ellipsis(
            string value,
            int startIndex,
            int length,
            int limit,
            bool strict,
            string ellipsis
            )
        {
            string result = value;

            if (!String.IsNullOrEmpty(result) && (limit >= 0))
            {
                if ((startIndex >= 0) && (startIndex < result.Length))
                {
                    //
                    // NOTE: Are we going to actually truncate anything?
                    //
                    if (length > limit)
                    {
                        //
                        // NOTE: Prevent going past the end of the string.
                        //
                        if ((startIndex + limit) > result.Length)
                            limit = result.Length - startIndex;

                        //
                        // NOTE: Was a valid ellipsis string provided and will
                        //       it fit within the limit?
                        //
                        if (!String.IsNullOrEmpty(ellipsis) &&
                            (limit >= ellipsis.Length))
                        {
                            int newLimit = limit;

                            if (strict)
                                newLimit -= ellipsis.Length;

                            result = String.Format("{0}{1}",
                                result.Substring(startIndex, newLimit), ellipsis);
                        }
                        else
                        {
                            //
                            // BUGFIX: If the ellipsis is invalid or the limit is
                            //         less than the length of the it, just use
                            //         the initial substring of the value.
                            //
                            result = result.Substring(startIndex, limit);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Prevent going past the end of the string.
                        //
                        if ((startIndex + length) > result.Length)
                            length = result.Length - startIndex;

                        result = result.Substring(startIndex, length);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        public static string Hash(
            Hash hash
            )
        {
            return (hash != null) ?
                StringList.MakeList(
                    "md5", Hash(hash.MD5),
                    "sha1", Hash(hash.SHA1)) : DisplayNull;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SourceId(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceId(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SourceTimeStamp(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceTimeStamp(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string UpdateUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyUpdateBaseUri(assembly);
            return StringOps.GetStringFromObject(uri, @default, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DownloadUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyDownloadBaseUri(assembly);
            return StringOps.GetStringFromObject(uri, @default, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PublicKeyToken(
            AssemblyName assemblyName,
            string @default
            )
        {
            string result = AssemblyOps.GetPublicKeyToken(assemblyName);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PublicKeyToken(
            byte[] publicKeyToken
            )
        {
            if (publicKeyToken == null)
                return DisplayNull;

            if (publicKeyToken.Length == 0)
                return DisplayEmpty;

            return ArrayOps.ToHexadecimalString(publicKeyToken);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetQualifiedTypeFullName(
            string namespaceName,
            string typeName,
            Assembly assembly
            )
        {
            //
            // NOTE: Garbage in, garbage out.
            //
            if (String.IsNullOrEmpty(typeName))
                return typeName;

            //
            // HACK: The namespace name for the hash algorithms can be
            //       obtained based on the "HashAlgorithm" type here;
            //       however, the actual hash algorithm implementations
            //       reside in an entirely different assembly.  This
            //       code assumes all hash algorithm implementations
            //       reside in the *SAME* assembly as that is the only
            //       reasonable way to make this lookup work.
            //
            if ((namespaceName != null) && !typeName.StartsWith(
                    namespaceName, SharedStringOps.SystemComparisonType))
            {
                if ((assembly != null) &&
                    !MarshalOps.IsAssemblyQualifiedTypeName(typeName))
                {
                    return String.Format(
                        "{0}{1}{2}, {3}", namespaceName, Type.Delimiter,
                        typeName, assembly);
                }
                else
                {
                    return String.Format(
                        "{0}{1}{2}", namespaceName, Type.Delimiter,
                        typeName);
                }
            }
            else if ((assembly != null) &&
                !MarshalOps.IsAssemblyQualifiedTypeName(typeName))
            {
                return String.Format(
                    "{0}, {1}", typeName, assembly);
            }
            else
            {
                return typeName;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string StrongName(
            Assembly assembly,
#if CAS_POLICY
            StrongName strongName,
#endif
            bool verified
            )
        {
            if ((assembly != null)
#if CAS_POLICY
                    && (strongName != null)
#endif
                )
            {
                AssemblyName assemblyName = assembly.GetName();

                if (assemblyName != null)
                {
                    byte[] assemblyNamePublicKey = assemblyName.GetPublicKey();

                    try
                    {
#if CAS_POLICY
                        bool isMono = CommonOps.Runtime.IsMono();

                        //
                        // HACK: Is there no other way to get the public key byte array
                        //       from a StrongName object?
                        //
                        byte[] strongNamePublicKey =
                            (byte[])typeof(StrongNamePublicKeyBlob).InvokeMember(
                                isMono ? "pubkey" : "PublicKey",
                                ObjectOps.GetBindingFlags(
                                    MetaBindingFlags.PrivateInstanceGetField,
                                true), null, strongName.PublicKey,null);

                        //
                        // NOTE: Make sure the caller gave us a "matching set" of objects.
                        //
                        if (ArrayOps.Equals(assemblyNamePublicKey, strongNamePublicKey))
#endif
                        {
#if CAS_POLICY
                            string strongNameName = strongName.Name;
                            Version strongNameVersion = strongName.Version;
#else
                            string assemblyNameName = assemblyName.Name;
                            Version assemblyNameVersion = assemblyName.Version;
#endif

                            string strongNameTag = SharedAttributeOps.GetAssemblyStrongNameTag(assembly);
                            byte[] assemblyNamePublicKeyToken = assemblyName.GetPublicKeyToken();

                            StringList list = new StringList();

#if CAS_POLICY
                            if (strongNameName != null)
                                list.Add("name", strongNameName);

                            if (strongNameVersion != null)
                                list.Add("version", strongNameVersion.ToString());
#else
                            if (assemblyName != null)
                                list.Add("name", assemblyNameName);

                            if (assemblyNameVersion != null)
                                list.Add("version", assemblyNameVersion.ToString());
#endif

                            if (assemblyNamePublicKeyToken != null)
                                list.Add("publicKeyToken", ArrayOps.ToHexadecimalString(
                                    assemblyNamePublicKeyToken));

                            list.Add("verified",
                                (verified && RuntimeOps.IsStrongNameVerified(
                                    assembly.Location, true)).ToString());

                            if (strongNameTag != null)
                                list.Add("tag", strongNameTag);

                            return list.ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        //
                        // NOTE: Nothing we can do here except log the failure.
                        //       The method name reported in the trace output
                        //       here may be wrong due to skipping of built-in
                        //       classes by the DebugOps class.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(FormatOps).Name,
                            TracePriority.SecurityError);
                    }
                }
            }

            return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            Assembly assembly,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            return Certificate(
                (assembly != null) ? assembly.Location : null,
                certificate, trusted, verbose, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            X509Certificate certificate,
            bool verbose,
            bool wrap
            )
        {
            StringList list = RuntimeOps.CertificateToList(
                certificate, verbose);

            string result = StringOps.GetStringFromObject(list);

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            string fileName,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            try
            {
                X509Certificate certificate = new X509Certificate(
                    fileName);

                StringList list = RuntimeOps.CertificateToList(
                    certificate, verbose);

                string result = null;

                if (list != null)
                {
                    if (fileName != null)
                    {
                        list.Add("trusted", trusted ?
                            RuntimeOps.IsFileTrusted(
                                fileName, IntPtr.Zero).ToString() :
                            false.ToString());
                    }

                    result = list.ToString();
                }

                return wrap ? WrapOrNull(result) : result;
            }
            catch
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            string fileName,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            StringList list = RuntimeOps.CertificateToList(
                certificate, verbose);

            string result = null;

            if (list != null)
            {
                if (fileName != null)
                {
                    list.Add("trusted", trusted ?
                        RuntimeOps.IsFileTrusted(
                            fileName, IntPtr.Zero).ToString() :
                        false.ToString());
                }

                result = list.ToString();
            }

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedScriptExtension(
            string value,
            ref string extension
            )
        {
            //
            // NOTE: If the [path] value is null or empty, there would be
            //       no need to add a file extension.
            //
            if (String.IsNullOrEmpty(value))
                return false;

            //
            // NOTE: Grab the script file extension.  This should normally
            //       be ".eagle".
            //
            string scriptExtension = FileExtension.Script;

            //
            // NOTE: If the script file extension is null (or empty), there
            //       is no point in ever appending it [to anything].
            //
            if (String.IsNullOrEmpty(scriptExtension))
                return false;

            //
            // NOTE: If the file name already ends with the script file
            //       extension, there is no point in appending it.
            //
            if (value.EndsWith(scriptExtension, PathOps.ComparisonType))
                return false;

            //
            // NOTE: If the file name already ends with any "well-known"
            //       file extension, skip appending an extension.
            //
            if (PathOps.HasKnownExtension(value))
                return false;

            extension = scriptExtension;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptTypeToFileName(
            string scriptType,
            PackageType packageType,
            bool fileNameOnly,
            bool strict
            )
        {
            string result = scriptType;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: If the "script type" (which might really be a file
                //       name) specified by the caller already has the file
                //       extension, skip appending it; otherwise, make sure
                //       that it ends with the file extension now.
                //
                string extension = null;

                if (NeedScriptExtension(result, ref extension))
                {
                    //
                    // NOTE: Append the script file extension to the base
                    //       name (i.e. "script type").
                    //
                    result = String.Format("{0}{1}", result, extension);
                }

                //
                // NOTE: If the result already has some kind of directory,
                //       skip adding the library path fragment; otherwise,
                //       make sure it has the library path fragment as a
                //       prefix.
                //
                if (!fileNameOnly && !PathOps.HasDirectory(result))
                {
                    //
                    // HACK: In the [missing] default case here, we simply
                    //       do nothing.
                    //
                    switch (packageType)
                    {
                        case PackageType.Library:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.LibraryPackage,
                                    result));

                                break;
                            }
                        case PackageType.Test:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.TestPackage,
                                    result));

                                break;
                            }
                    }
                }

                return result;
            }
            else if (!strict)
            {
                return result; /* NOTE: Either "null" or "String.Empty". */
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CultureName(
            CultureInfo cultureInfo,
            bool display
            )
        {
            if (cultureInfo != null)
            {
                //
                // NOTE: For some reason, the invariant culture has an empty
                //       string as the result of its ToString() method.  In
                //       that case, use the string "invariant" if the caller
                //       has not requested the display name.
                //
                string result = cultureInfo.ToString();

                if ((result != null) && (result.Length == 0))
                {
                    result = display ?
                        cultureInfo.DisplayName : "invariant";
                }
                else if (display && (result == null))
                {
                    result = cultureInfo.DisplayName;
                }

                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BreakOrFail(
            string methodName,
            params string[] strings
            )
        {
            return String.Format("{0}: {1}", methodName,
                (strings != null) ? StringList.MakeList(strings) : DisplayNull);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NumericTimeZone(
            long totalSeconds
            )
        {
            //
            // NOTE: This code was cut and pasted from
            //       ::tcl::clock::FormatNumericTimeZone
            //       (Tcl 8.5+) and translated from Tcl
            //       to C#.
            //
            StringBuilder result = StringOps.NewStringBuilder();

            if (totalSeconds < 0)
            {
                result.Append(Characters.MinusSign);
                totalSeconds = -totalSeconds; /* normalize */
            }
            else
            {
                result.Append(Characters.PlusSign);
            }

            result.AppendFormat("{0:00}", totalSeconds / 3600);
            totalSeconds = totalSeconds % 3600;

            result.AppendFormat("{0:00}", totalSeconds / 60);
            totalSeconds = totalSeconds % 60;

            if (totalSeconds != 0)
                result.AppendFormat("{0:00}", totalSeconds);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AlwaysOrNever(
            long value
            )
        {
            if (value < 0)
                return "never";
            else if (value == 0)
                return "always";
            else
                return "sometimes";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601UpdateDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601UpdateDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceDateTime(
            DateTime value,
            bool interactive
            )
        {
            return value.ToString(interactive ?
                TraceInteractiveDateTimeFormat :
                TraceDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601FullDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601FullDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE || NATIVE
        public static string Iso8601DateTime(
            DateTime? value
            )
        {
            return Iso8601DateTime(value, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601DateTime(
            DateTime? value,
            bool timeZone
            )
        {
            DateTime dateTime;

            if (value != null)
                dateTime = (DateTime)value;
            else
                dateTime = DateTime.MinValue;

            string offset = null;

            if (timeZone)
            {
                if ((dateTime.Kind == DateTimeKind.Utc) ||
                    (dateTime.Kind == DateTimeKind.Local))
                {
                    TimeSpan span = TimeZone.CurrentTimeZone.GetUtcOffset(
                        dateTime);

                    offset = NumericTimeZone((long)span.TotalSeconds);
                }
            }

            return String.Format(
                "{0} {1}", dateTime.ToString(Iso8601DateTimeOutputFormat),
                offset).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SettingKey(
            IIdentifierName identifierName,
            ElementDictionary arrayValue,
            string varIndex
            ) /* CANNOT RETURN NULL */
        {
            if (identifierName != null)
            {
                string varName = identifierName.Name;

                if (varName != null)
                {
                    if (varIndex != null)
                    {
                        return String.Format(
                            "{0}{1}{2}{3}", varName,
                            Characters.OpenParenthesis, varIndex,
                            Characters.CloseParenthesis);
                    }
                    else
                    {
                        return varName;
                    }
                }
            }

            return varIndex;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorVariableName(
            IVariable variable,
            string linkIndex,
            string varName,
            string varIndex
            )
        {
            return ErrorVariableName(varName, varIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorVariableName(
            string varName
            )
        {
            return ErrorVariableName(varName, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return SomeKindOfPrefixAndSuffix(
                VariableName(varName, varIndex));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string SomeKindOfPrefixAndSuffix(
            string value
            ) /* CANNOT RETURN NULL */
        {
            if (value != null)
            {
                string prefix = WrapPrefix;
                string suffix = WrapSuffix;
                string stringValue;

                MaybeChangeWrapPrefixAndSuffix(
                    true, value, ref prefix, ref suffix,
                    out stringValue);

                if (stringValue != null)
                {
                    return String.Format(
                        "{0}{1}{2}", prefix, stringValue,
                        suffix);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SomeKindOfPrefixAndSuffix(
            StringBuilder builder
            ) /* CANNOT RETURN NULL */
        {
            if (builder != null)
            {
                string prefix = WrapPrefix;
                string suffix = WrapSuffix;

                MaybeChangeWrapPrefixAndSuffix(
                    true, builder, ref prefix, ref suffix);

                builder.Insert(0, prefix);
                builder.Append(suffix);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorElementName(
            BreakpointType breakpointType,
            string varName,
            string varIndex
            )
        {
            return String.Format(
                "can't {0} {1}: no such element in array",
                Breakpoint(breakpointType, DisplayUnknown),
                ErrorVariableName(varName, varIndex));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MissingElementName(
            BreakpointType breakpointType,
            string varName,
            bool isArray
            )
        {
            return String.Format(
                "can't {0} {1}: variable {2} array",
                Breakpoint(breakpointType, DisplayUnknown),
                ErrorVariableName(varName), isArray ?
                    "is" : "isn't");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MissingVariableName(
            BreakpointType breakpointType,
            string varName,
            string suffix
            )
        {
            return String.Format(
                "can't {0} {1}: no such variable{2}",
                Breakpoint(breakpointType, DisplayUnknown),
                ErrorVariableName(varName), suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MissingValuesName(
            BreakpointType breakpointType,
            string varName,
            string suffix
            )
        {
            return String.Format(
                "can't {0} {1}: variable values unavailable{2}",
                Breakpoint(breakpointType, DisplayUnknown),
                ErrorVariableName(varName));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string VariableName(
            string varName,
            string varIndex
            )
        {
            if (varIndex == null)
                return varName;

            if (varName == null)
                return null;

            return String.Format(
                "{0}{1}{2}{3}", varName, Characters.OpenParenthesis,
                varIndex, Characters.CloseParenthesis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Breakpoint(
            BreakpointType breakpointType
            )
        {
            return Breakpoint(breakpointType, String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Breakpoint(
            BreakpointType breakpointType,
            string @default
            )
        {
            string result = @default;

            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableExist:
                    result = "verify";
                    break;
                case BreakpointType.BeforeVariableGet:
                    result = "read";
                    break;
                case BreakpointType.BeforeVariableSet:
                case BreakpointType.BeforeVariableAdd:
                    result = "set";
                    break;
                case BreakpointType.BeforeVariableUnset:
                    result = "unset";
                    break;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string FunctionTypeName(
            string name,
            bool wrap
            )
        {
            string result = String.Format(
                "{0}{1}{2}", typeof(_Functions.Default).Namespace,
                Type.Delimiter, name);

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorTypeName(
            string name,
            bool wrap
            )
        {
            string result = String.Format(
                "{0}{1}{2}", typeof(_Operators.Default).Namespace,
                Type.Delimiter, name);

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayByteArray(
            byte[] bytes
            )
        {
            if (bytes == null)
                return DisplayNull;

            if (bytes.Length == 0)
                return DisplayEmpty;

            return BitConverter.ToString(bytes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string HashBytes(
            byte[] bytes
            )
        {
            Result error = null; /* NOT USED */

            byte[] hashValue = HashOps.HashBytes(null, bytes, ref error);

            return (hashValue != null) ? Hash(hashValue) : DisplayError0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hash(
            byte[] bytes
            )
        {
            return BitConverter.ToString(bytes).Replace(
                Characters.MinusSign.ToString(), String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Level(
            bool absolute,
            int level
            )
        {
            return String.Format(
                "{0}{1}",
                absolute ? Characters.NumberSign.ToString() : String.Empty,
                level);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
#if CACHE_DICTIONARY
        public static string MaybeEnableOrDisable(
            Dictionary<CacheFlags, int> dictionary,
            bool display
            )
        {
            IStringList list = GenericOps<CacheFlags, int>.KeysAndValues(
                dictionary, false, true, true, MatchMode.None, null, null,
                null, null, null, false);

            if (list == null)
                return display ? DisplayNull : null;

            if (display && (list.Count == 0))
                return DisplayEmpty;

            return list.ToString();
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayNamespace(
            INamespace @namespace
            )
        {
            if (@namespace == null)
                return DisplayNull;

            return DisplayValue(@namespace.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayScriptLocationList(
            ScriptLocationList scriptLocations
            )
        {
            if (scriptLocations == null)
                return DisplayUnavailable;

            if (scriptLocations.IsEmpty)
                return DisplayEmpty;

            IScriptLocation scriptLocation = scriptLocations.Peek();

            if (scriptLocation == null)
                return DisplayNull;

            return scriptLocation.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SCRIPT_ARGUMENTS
        public static string DisplayScriptArgumentsQueue(
            ArgumentListStack scriptArguments
            )
        {
            if (scriptArguments == null)
                return DisplayUnavailable;

            if (scriptArguments.Count == 0)
                return DisplayEmpty;

            return scriptArguments.ToString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayString(
            string value
            )
        {
            return DisplayString(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayString(
            string value,
            bool wrap
            )
        {
            if (value != null)
            {
                if (value.Length > 0)
                    return wrap ? WrapOrNull(value) : value;

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayChars(
            char[] value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value.Length == 0)
                return DisplayEmpty;

            StringBuilder builder = StringOps.NewStringBuilder();

            builder.Append(value);

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayWidthAndHeight(
            int width,
            int height
            )
        {
            return String.Format(
                "Width={0}, Height={1}", width, height);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayException(
            Exception exception,
            bool innermost
            )
        {
            if (exception != null)
            {
                if (innermost)
                    exception = exception.GetBaseException();

                return String.Format(DisplayFormat, exception.GetType());
            }

            return String.Format(DisplayFormat, typeof(Exception).Name.ToLower());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayKeys(
            IDictionary dictionary
            )
        {
            if (dictionary != null)
            {
                if (dictionary.Count > 0)
                    return new StringList(dictionary.Keys).ToString();

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayList(
            IList list
            )
        {
            if (list != null)
            {
                if (list.Count > 0)
                    return list.ToString();

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NameAndVersion(
            string name,
            Version version,
            string build,
            string extra
            )
        {
            return NameAndVersion(
                name, (version != null) ? version.ToString() : null, build, extra);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NameAndVersion(
            string name,
            string version,
            string build,
            string extra
            )
        {
            if (!String.IsNullOrEmpty(build))
            {
                return String.Format(
                    "{0} {1} [{2}] {3}", name, version, build, extra).Trim();
            }
            else
            {
                return String.Format(
                    "{0} {1} {2}", name, version, extra).Trim();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string VMajorMinorOrNull(
            Version version
            )
        {
            if (version == null)
                return DisplayNull;

            return MajorMinor(version, "v", null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MajorMinor(
            Version version
            )
        {
            return MajorMinor(version, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MajorMinor(
            Version version,
            string prefix,
            string suffix
            )
        {
            return (version != null) ? String.Format("{0}{1}{2}", prefix,
                version.ToString(2), suffix) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ShortRuntimeName()
        {
            if (CommonOps.Runtime.IsDotNetCore())
            {
                if (CommonOps.Runtime.IsDotNetCore5xOr6x())
                    return "NET"; // .NET 5.0, etc.
                else
                    return "Core"; // CoreCLR 2.x, 3.x
            }

            if (CommonOps.Runtime.IsMono())
                return "Mono"; // Mono 2.x+

            if (CommonOps.Runtime.IsFramework20() || // .NET Framework 2.0, 3.5
                CommonOps.Runtime.IsFramework40())   // .NET Framework 4.x
            {
                return "CLRv"; /* COMPAT: Eagle beta. */
            }

            return "Unknown";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ShortRuntimeVersion( /* e.g. "CLRv4", "Core3", "NET5", etc. */
            Version value
            )
        {
            if (value == null)
                return null;

            return String.Format(
                "{0}{1}", ShortRuntimeName(), value.Major);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ShortImageRuntimeVersion( /* e.g. "CLRv2" or "CLRv4" */
            string value
            )
        {
            if (value == null)
                return null;

            int length = value.Length;

            if (length < 2)
                return null;

            if (value[0] != Characters.v)
                return null;

            if (!Char.IsDigit(value[1]))
                return null;

            return String.Format("CLR{0}", value.Substring(0, 2));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyTextAndConfiguration(
            string text,
            string runtimeVersion,
            string configuration,
            string prefix,
            string suffix
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(text))
                builder.Append(text);

            if (!String.IsNullOrEmpty(runtimeVersion))
            {
                if (builder.Length > 0)
                    builder.Append(RuntimeSeparator);

                builder.Append(runtimeVersion);
            }

            if (!String.IsNullOrEmpty(configuration))
            {
                if (builder.Length > 0)
                    builder.Append(ConfigurationSeparator);

                builder.Append(configuration);
            }

            if (builder.Length == 0)
                return String.Empty;

            return String.Format("{0}{1}{2}", prefix, builder, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorWithException(
            Result error,
            Exception exception
            )
        {
            string result;

            if (error != null)
            {
                if (exception != null)
                    result = String.Format("{0}{1}{2}{3}", error, Environment.NewLine,
                        Environment.NewLine, exception);
                else
                    result = error;
            }
            else
            {
                if (exception != null)
                    result = exception.ToString();
                else
                    result = String.Empty;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NestedArrayName(
            string name,
            string index
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(name))
            {
                result.Append(name);

                if (!String.IsNullOrEmpty(index))
                {
                    result.Append(Characters.Underscore);
                    result.Append(index.Replace(Characters.Comma, Characters.Underscore));
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InvokeRawTypeName(
            Type type
            )
        {
            if (type == null)
                return DisplayNull;

            return InvokeRawTypeName(type, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string InvokeRawTypeName(
            Type type,
            bool full
            )
        {
            if (type == null)
                return DisplayNull;

            return WrapOrNull(QualifiedAndOrFullName(type, full,
                !IsSystemAssembly(type) && !IsSameAssembly(type),
                true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string RawTypeName(
            Type type
            )
        {
            return TypeName(type, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string RawTypeName(
            object @object
            )
        {
            return TypeName(@object, null, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static string RawTypeNameOrFullName(
            Type type
            )
        {
            return TypeNameOrFullName(type, null, null, false);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string RawTypeNameOrFullName(
            object @object
            )
        {
            return TypeNameOrFullName(@object, null, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            Type type
            )
        {
            return TypeName(type, DisplayNull, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeNameOrFullName(
            Type type
            )
        {
            return TypeNameOrFullName(
                type, DisplayNull, !IsSameAssembly(type), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static string TypeName(
            Type type,
            bool wrap
            )
        {
            return TypeName(type, DisplayNull, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TypeNameOrFullName(
            Type type,
            bool wrap
            )
        {
            return TypeNameOrFullName(type, DisplayNull, true, wrap);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TypeName(
            Type type,
            string @default,
            bool wrap
            )
        {
            if (type == null)
                return @default;

            return wrap ? WrapOrNull(type.FullName) : type.FullName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeNameOrFullName(
            Type type,
            string @default,
            bool full,
            bool wrap
            )
        {
            if (type == null)
                return @default;

            string typeName = full ? type.FullName : type.Name;

            return wrap ? WrapOrNull(typeName) : typeName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            object @object
            )
        {
            return TypeName(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeNameOrFullName(
            object @object
            )
        {
            return TypeNameOrFullName(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            object @object,
            bool wrap
            )
        {
            return TypeName(
                @object, DisplayNull, DisplayProxy, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TypeNameOrFullName(
            object @object,
            bool wrap
            )
        {
            return TypeNameOrFullName(
                @object, DisplayNull, DisplayProxy, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            object @object,
            string nullTypeName,
            string proxyTypeName,
            bool wrap
            )
        {
            string typeName;

            if (AppDomainOps.MaybeGetTypeName(@object, out typeName))
            {
                if (typeName == null)
                    return proxyTypeName;

                return wrap ? WrapOrNull(typeName) : typeName;
            }

            Type type = (@object != null) ? @object.GetType() : null;

            return TypeName(type, nullTypeName, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TypeNameOrFullName(
            object @object,
            string nullTypeName,
            string proxyTypeName,
            bool wrap
            )
        {
            string typeName;

            if (AppDomainOps.MaybeGetTypeName(@object, out typeName))
            {
                if (typeName == null)
                    return proxyTypeName;

                return wrap ? WrapOrNull(typeName) : typeName;
            }

            Type type = (@object != null) ? @object.GetType() : null;

            return TypeNameOrFullName(type, nullTypeName, true, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        public static string IpAddressAndPort(
            IPAddress address,
            int port
            )
        {
            if (port != Port.Invalid)
            {
                return String.Format("{0}:{1}",
                    (address != null) ? address : IPAddress.Any,
                    port);
            }
            else
            {
                return String.Format("{0}",
                    (address != null) ? address : IPAddress.Any);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NetworkHostAndPort(
            string hostNameOrAddress,
            string portNameOrNumber
            )
        {
            if (portNameOrNumber != null)
            {
                return String.Format("{0}:{1}",
                    (hostNameOrAddress != null) ?
                        hostNameOrAddress : DisplayNull,
                    portNameOrNumber);
            }
            else
            {
                return String.Format("{0}",
                    (hostNameOrAddress != null) ?
                        hostNameOrAddress : DisplayNull);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MemberName(
            MemberInfo memberInfo
            )
        {
            if (memberInfo == null)
                return DisplayNull;

            return WrapOrNull(memberInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string TclBridgeName(
            string interpName,
            string commandName
            )
        {
            return StringList.MakeList(interpName, commandName);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageName(
            string name,
            Version version
            )
        {
            return WrapOrNull((version != null) ?
                String.Format("{0} {1}",
                    (name != null) ? name : DisplayNull, version) :
                name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageDirectory(
            string name,
            Version version,
            bool full
            )
        {
            return String.Format("{0}{1}{2}", full ?
                TclVars.Path.Lib + PathOps.NativeDirectorySeparatorChar : String.Empty,
                name, version);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ProcessName(
            Process process,
            bool display
            )
        {
            if (process != null)
            {
                int id = 0;

                try
                {
                    id = process.Id; /* throw */
                }
                catch
                {
                    // do nothing.
                }

                string fileName = PathOps.GetProcessMainModuleFileName(
                    process, false);

                if (!String.IsNullOrEmpty(fileName))
                    return StringList.MakeList(id, fileName);
                else
                    return id.ToString();
            }

            return display ? DisplayUnknown : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BetweenOrExact(
            int lowerBound,
            int upperBound
            )
        {
            return String.Format(
                (lowerBound != upperBound) ?
                    "between {0} and {1}" : "{0}",
                lowerBound, upperBound);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            IIdentifierName identifierName,
            Interpreter interpreter,
            ArgumentList arguments
            )
        {
            string commandName = (identifierName != null) ?
                identifierName.Name : null;

            if ((interpreter != null) &&
                (arguments != null) && (arguments.Count >= 2))
            {
                IEnsemble ensemble = identifierName as IEnsemble;

                if (ensemble != null)
                {
                    string subCommandName = arguments[1];

                    if (subCommandName != null)
                    {
                        if (ScriptOps.SubCommandFromEnsemble(
                                interpreter, ensemble, null, null, true,
                                false, ref subCommandName) == ReturnCode.Ok)
                        {
                            return DisplayName(StringList.MakeList(
                                commandName, subCommandName));
                        }
                    }
                }
            }

            return DisplayName(commandName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            IIdentifierName identifierName
            )
        {
            return (identifierName != null) ?
                DisplayName(identifierName.Name) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            string name
            )
        {
            if (name == null)
                return DisplayNull;

            if (name.Length == 0)
                return DisplayEmpty;

            if (name.IndexOf(Characters.QuotationMark) != Index.Invalid)
            {
                return Parser.Quote(
                    name, ListElementFlags.DontQuoteHash);
            }
            else
            {
                return String.Format(
                    "{0}{1}{0}", Characters.QuotationMark, /* EXEMPT */
                    name);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayPath(
            string path
            )
        {
            if (path == null)
                return DisplayNull;

            return WrapOrNull(PathOps.MaybeTrimEnd(path));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAssemblyName(
            Assembly assembly
            )
        {
            if (assembly == null)
                return DisplayNull;

            string location = null;

            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                // do nothing.
            }

            if (location == null)
                location = DisplayNull;

            string codeBase = assembly.CodeBase;

            if (codeBase == null)
                codeBase = DisplayNull;

            return String.Format(
                "[{0}, {1}, {2}, {3}]", assembly.FullName,
                AssemblyOps.GetModuleVersionId(assembly),
                location, codeBase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyName(
            AssemblyName assemblyName,
            long id,
            bool paths,
            bool wrap
            )
        {
            StringList list = null;

            if (assemblyName != null)
            {
                list = new StringList();
                list.Add(assemblyName.FullName);

                if (id != 0)
                    list.Add(id.ToString());

                if (paths)
                    list.Add(assemblyName.CodeBase);
            }

            if (wrap)
                return WrapOrNull(list);
            else if (list != null)
                return list.ToString();
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyName(
            Assembly assembly,
            long id,
            bool paths,
            bool wrap
            )
        {
            StringList list = null;

            if (assembly != null)
            {
                list = new StringList();
                list.Add(assembly.FullName);

                list.Add(AssemblyOps.GetModuleVersionId(
                    assembly).ToString());

                if (id != 0)
                    list.Add(id.ToString());

                if (paths)
                {
                    try
                    {
                        list.Add(assembly.Location);
                    }
                    catch (NotSupportedException)
                    {
                        list.Add((string)null);
                    }

                    list.Add(assembly.CodeBase);
                }
            }

            if (wrap)
                return WrapOrNull(list);
            else if (list != null)
                return list.ToString();
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string EventName(
            Interpreter interpreter,
            string prefix,
            string name,
            long id
            )
        {
            //
            // BUGFIX: We need to make 100% sure that event names are unique
            //         throughout the entire system.  Therefore, format them
            //         with some information that uniquely identifies this
            //         process, thread, application domain, and an ever
            //         increasing value (i.e. "event serial number") that is
            //         unique within this application domain (i.e. regardless
            //         of how many interpreters exist).
            //
            return Id(
                prefix, name, ProcessOps.GetId().ToString(),
                GlobalState.GetCurrentSystemThreadId().ToString(),
                AppDomainOps.GetCurrentId().ToString(),
                (interpreter != null) ?
                    interpreter.IdNoThrow.ToString() : null,
                Interlocked.Increment(ref nextEventId).ToString(),
                (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeTreatAsFatalError(
            TracePriority? priority,
            Interpreter interpreter
            )
        {
            if ((interpreter == null) || (priority == null))
                return;

            _Hosts.Default defaultHost =
                interpreter.InternalHost as _Hosts.Default;

            if (defaultHost == null)
                return;

            try
            {
                defaultHost.SetTreatAsFatalError(FlagOps.HasFlags(
                    (TracePriority)priority, TracePriority.Fatal, true));
            }
            catch
            {
                // do nothing.
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeRemoveMethodName(
            string methodName, /* in */
            bool method,       /* in */
            ref string message /* in, out */
            )
        {
            //
            // HACK: Remove the duplicate "MethodName: " prefix from the
            //       message if using the real class and method names are
            //       enabled.  It will only be removed if it is at the
            //       start of the message.
            //
            if (!String.IsNullOrEmpty(message) &&
                method && !String.IsNullOrEmpty(methodName))
            {
                string[] parts = methodName.Split(Type.Delimiter);

                if (parts != null)
                {
                    int length = parts.Length;

                    if (length >= 1)
                    {
                        string part = parts[length - 1];

                        if (!String.IsNullOrEmpty(part))
                        {
                            //
                            // HACK: This takes advantage of the consistent
                            //       formatting of trace messages throughout
                            //       the core library and may not work with
                            //       external code.
                            //
                            if (message.StartsWith(part + ": ",
                                    SharedStringOps.SystemComparisonType))
                            {
                                message = message.Substring(part.Length + 2);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Great care must be taken in this method because it is directly
        //       called by DebugTrace, which is used everywhere.  Accessing the
        //       interpreter requires a lock and a try/catch block.
        //
        public static string TraceInterpreter(
            Interpreter interpreter
            )
        {
            //
            // NOTE: If there is no interpreter, just return a value suitable
            //       for displaying "null".
            //
            if (interpreter == null)
                return DisplayNull;

            //
            // NOTE: The interpreter may have been disposed and we do not want
            //       to throw an exception; therefore, wrap all the interpreter
            //       property access in a try block.
            //
            bool locked = false;

            try
            {
                interpreter.InternalSoftTryLock(
                    ref locked); /* TRANSACTIONAL */

                if (locked) /* TRANSACTIONAL */
                {
                    if (interpreter.Disposed)
                        return DisplayDisposed;

                    return interpreter.Id.ToString(); /* EXEMPT */
                }
                else
                {
                    return DisplayBusy;
                }
            }
            catch (Exception e)
            {
                Type type = (e != null) ? e.GetType() : null;

                return String.Format(DisplayError0,
                    (type != null) ? type.Name : UnknownTypeName);
            }
            finally
            {
                interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceWrite(
            string message,
            string category
            )
        {
            if (category != null)
                return String.Format("{0}: {1}", category, message);
            else
                return message;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TraceOutput(
            string format,
            string prefix,
            DateTime? dateTime,
            TracePriority? priority,
#if WEB && !NET_STANDARD_20
            string serverName,
#endif
            string testName,
            AppDomain appDomain,
            Interpreter interpreter,
            long? threadId,
            string message,
            bool method,
            bool stack,
            int skipFrames
            )
        {
            string category = null;
            string methodName = null;

            return TraceOutput(
                format, prefix, dateTime,
                priority,
#if WEB && !NET_STANDARD_20
                serverName,
#endif
                testName, appDomain, interpreter,
                threadId, message, method,
                stack, skipFrames + 1,
                ref category, ref methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TraceOutput(
            string format,
            string prefix,
            DateTime? dateTime,
            TracePriority? priority,
#if WEB && !NET_STANDARD_20
            string serverName,
#endif
            string testName,
            AppDomain appDomain,
            Interpreter interpreter,
            long? threadId,
            string message,
            bool method,
            bool stack,
            int skipFrames,
            ref string category,
            ref string methodName
            )
        {
            string displayMethodName;

            if (method)
            {
                bool thisAssembly;
                string typeName;

                DebugOps.GetMethodName(
                    0, skipNames, true, false, null,
                    out thisAssembly, out typeName,
                    out methodName);

                //
                // HACK: Maybe change trace category for
                //       any core library types that are
                //       within namespaces that may have
                //       ambiguous names like "Default",
                //       etc.
                //
                if (thisAssembly && !StringOps.Match(
                        null, StringOps.DefaultMatchMode,
                        typeName, "Eagle._Components.*",
                        false))
                {
                    category = typeName;
                }

                if (methodName != null)
                {
                    if (TraceOps.CanDisplayMethodName(methodName))
                    {
                        displayMethodName = methodName;
                    }
                    else
                    {
                        displayMethodName = DisplayObfuscated;
                    }
                }
                else
                {
                    displayMethodName = DisplayUnknown;
                }
            }
            else
            {
                displayMethodName = DisplayNull;
            }

            string displayStackTrace;

            if (stack)
            {
                displayStackTrace = String.Format(
                    "{1}{2}{1}{0}{1}{3}", DebugOps.GetStackTraceString(
                    skipFrames + 1, DisplayUnavailable), Environment.NewLine,
                    StackTraceStart, StackTraceEnd);
            }
            else
            {
                displayStackTrace = null;
            }

            MaybeTreatAsFatalError(priority, interpreter);
            MaybeRemoveMethodName(methodName, method, ref message);

            return String.Format(format, prefix, (dateTime != null) ?
                TraceDateTime((DateTime)dateTime, false) : DisplayNull,
                (priority != null) ? HexadecimalPrefix +
                EnumOps.ToUIntOrULong(priority.Value).ToString(
                TracePriorityFormat) : DisplayNull,
#if WEB && !NET_STANDARD_20
                (serverName != null) ? serverName : DisplayNull,
#else
                DisplayUnavailable,
#endif
                (testName != null) ? testName : DisplayNull,
                (appDomain != null) ? AppDomainOps.GetId(
                    appDomain).ToString() : DisplayNull,
                TraceInterpreter(interpreter), (threadId != null) ?
                threadId.ToString() : DisplayNull, displayMethodName,
                displayStackTrace, message, Environment.NewLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceException(
            Exception exception
            )
        {
            return String.Format("{0}", exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ExceptionMethod(
            Exception exception,
            bool display
            )
        {
            if (exception == null)
                return display ? DisplayNull : null;

            try
            {
                MethodBase methodBase = exception.TargetSite;

                if (methodBase == null)
                    return display ? DisplayNull : null;

                return String.Format("{0}{1}{2}",
                    methodBase.ReflectedType, Type.Delimiter, methodBase.Name);
            }
            catch /* NOTE: Type from different AppDomain, perhaps? */
            {
                return display ? DisplayError : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            long id
            )
        {
            return Id(prefix, name, (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            string id
            )
        {
            return Id(prefix, name, id, null, null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            string id1,
            string id2,
            string id3,
            string id4,
            string id5,
            string id6
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(prefix))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(prefix);
            }

            if (!String.IsNullOrEmpty(name))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(name);
            }

            if (!String.IsNullOrEmpty(id1))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id1);
            }

            if (!String.IsNullOrEmpty(id2))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id2);
            }

            if (!String.IsNullOrEmpty(id3))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id3);
            }

            if (!String.IsNullOrEmpty(id4))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id4);
            }

            if (!String.IsNullOrEmpty(id5))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id5);
            }

            if (!String.IsNullOrEmpty(id6))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id6);
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER || SHELL
        public static string InteractiveLoopData(
            IInteractiveLoopData loopData
            )
        {
            if (loopData == null)
                return DisplayNull;

            InteractiveLoopData localLoopData = loopData as InteractiveLoopData;

            if (localLoopData == null)
                return DisplayTypeMismatch;

            return localLoopData.ToTraceString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public static string UpdateDateTime(
            DateTime? value
            )
        {
            if (value == null)
                return String.Empty;

            return ((DateTime)value).ToString(UpdateDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ShellCallbackData(
            IShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return DisplayNull;

            ShellCallbackData localCallbackData = callbackData as ShellCallbackData;

            if (localCallbackData == null)
                return DisplayTypeMismatch;

            return localCallbackData.ToTraceString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string UpdateData(
            IUpdateData updateData
            )
        {
            if (updateData == null)
                return DisplayNull;

            UpdateData localUpdateData = updateData as UpdateData;

            if (localUpdateData == null)
                return DisplayTypeMismatch;

            return localUpdateData.ToTraceString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InterpreterNoThrow(
            Interpreter interpreter
            )
        {
            return InterpreterNoThrow(interpreter, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InterpreterNoThrow(
            Interpreter interpreter,
            bool quote
            )
        {
            if (interpreter == null)
                return DisplayNull;

            long id = interpreter.IdNoThrow;

            if (!quote)
                return id.ToString();

            return String.Format(
                "{0}{1}{0}", Characters.QuotationMark, /* EXEMPT */
                id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string EnabledAndValue(
            bool enabled,
            string value
            )
        {
            return String.Format("{0} ({1})", enabled, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
        public static string WaitResult(
            int count,
            int index
            )
        {
            string result;

            if ((index >= _Constants.WaitResult.Object0) &&
                (index <= _Constants.WaitResult.Object0 + count - 1))
            {
                int offset = index - _Constants.WaitResult.Object0;

                if ((offset >= (int)TclThreadEvent.DoneEvent) &&
                    (offset <= (int)TclThreadEvent.QueueEvent))
                {
                    return String.Format(
                        "Object({0})", (TclThreadEvent)offset);
                }

                return String.Format("Object(#{0})", offset);
            }
            else if ((index >= _Constants.WaitResult.Abandoned0) &&
                (index <= _Constants.WaitResult.Abandoned0 + count - 1))
            {
                int offset = index - _Constants.WaitResult.Abandoned0;

                if ((offset >= (int)TclThreadEvent.DoneEvent) &&
                    (offset <= (int)TclThreadEvent.QueueEvent))
                {
                    return String.Format(
                        "Abandoned({0})", (TclThreadEvent)offset);
                }

                return String.Format("Abandoned(#{0})", offset);
            }
            else if (index == _Constants.WaitResult.IoCompletion)
            {
                result = "IoCompletion";
            }
            else if (index == _Constants.WaitResult.Timeout)
            {
                result = "Timeout";
            }
            else if (index == _Constants.WaitResult.Failed)
            {
                result = "Failed";
            }
#if MONO || MONO_HACKS
            else if (index == _Constants.WaitResult.MonoFailed)
            {
                result = "MonoFailed";
            }
#endif
            else
            {
                result = String.Format("Unknown({0})", index);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorName(
            string name
            )
        {
            return DisplayString(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorName(
            string name,
            Lexeme lexeme
            )
        {
            return String.Format(
                "{0} ({1})", DisplayString(name, true), lexeme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        public static string DatabaseObjectName(
            object @object,
            string @default,
            long id
            )
        {
            if (@object != null)
            {
                Type type = @object.GetType();

                if (type != null)
                {
                    return Id(type.ToString().Replace(
                        Type.Delimiter, Characters.NumberSign), null, id);
                }
            }

            return Id(@default, null, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyLocation(
            Type type,
            bool display
            )
        {
            if (type == null)
                return display ? DisplayNoType : null;

            try
            {
                Assembly assembly = type.Assembly;

                if (assembly == null)
                    return display ? DisplayNoAssembly : null;

                string location = assembly.Location;

                return display ? WrapOrNull(location) : location;
            }
            catch
            {
                return display ? DisplayError : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSystemAssembly(Type type)
        {
            if (type == null)
                return false;

            Assembly assembly = type.Assembly;

            //
            // NOTE: Check if the type is in the assembly "mscorlib.dll".
            //
            if (Object.ReferenceEquals(assembly, typeof(object).Assembly))
                return true;

            //
            // NOTE: Check if the type is in the assembly "System.dll".
            //
            if (Object.ReferenceEquals(assembly, typeof(Uri).Assembly))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSameAssembly(Type type)
        {
            return (type != null) && GlobalState.IsAssembly(type.Assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ObjectHandleTypeName(
            Type type,
            bool full
            )
        {
            return (type != null) ? (full ? type.FullName : type.Name) : UnknownTypeName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ObjectHandle(
            string prefix,
            string name,
            long id
            )
        {
            return Id(prefix, (name != null) ?
                name.Replace(Type.Delimiter, Characters.NumberSign) : null, id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ObjectHashCode(
            string prefix,
            string name,
            object value
            )
        {
            return Id(prefix, (name != null) ?
                name.Replace(Type.Delimiter, Characters.NumberSign) : null,
                String.Format("x{0:X}", RuntimeOps.GetHashCode(value)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedAndOrFullName(
            Type type,
            bool fullName,
            bool qualified,
            bool display
            )
        {
            if (type == null)
                return display ? DisplayNull : String.Empty;

            if (fullName && qualified)
            {
                if (type.AssemblyQualifiedName != null)
                    return type.AssemblyQualifiedName;
                if (type.Assembly != null)
                    return String.Format("{0}, {1}", type.FullName, type.Assembly);
                else
                    return type.FullName;
            }
            else if (fullName)
            {
                return type.FullName;
            }
            else if (qualified && (type.Assembly != null))
            {
                return String.Format("{0}, {1}", type.Name, type.Assembly);
            }
            else
            {
                return type.Name;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            Type type
            )
        {
            if ((type != null) && (type.AssemblyQualifiedName != null))
                return type.AssemblyQualifiedName;
            if ((type != null) && (type.Assembly != null))
                return String.Format("{0}, {1}", type, type.Assembly);
            else if (type != null)
                return type.ToString();
            else
                return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            AssemblyName assemblyName,
            string typeName,
            bool full
            )
        {
            if ((assemblyName != null) && !String.IsNullOrEmpty(typeName))
                return String.Format("{0}, {1}", typeName,
                    full ? assemblyName.FullName : assemblyName.Name);
            else if (assemblyName != null)
                return full ? assemblyName.FullName : assemblyName.Name;
            else if (!String.IsNullOrEmpty(typeName))
                return typeName;
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            string parentName,
            string childName
            )
        {
            if (!String.IsNullOrEmpty(parentName) && !String.IsNullOrEmpty(childName))
                return String.Format("{0}{1}{2}", parentName, Type.Delimiter, childName);
            else if (!String.IsNullOrEmpty(parentName))
                return parentName;
            else if (!String.IsNullOrEmpty(childName))
                return childName;
            else
                return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            string typeName,
            string methodName
            )
        {
            return QualifiedName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            Delegate @delegate,
            bool assembly,
            bool display
            )
        {
            if (@delegate == null)
                return display ? DisplayNull : null;

            MethodBase methodBase = @delegate.Method;

            return DelegateMethodName(methodBase, assembly, display);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            MethodBase methodBase,
            bool assembly,
            bool display
            )
        {
            if (methodBase == null)
                return display ? DisplayNull : null;

            return DelegateMethodName(
                methodBase.DeclaringType, methodBase.Name, assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string DelegateMethodName(
            Type type,
            string methodName,
            bool assembly
            )
        {
            if ((type == null) ||
                (type == typeof(Interpreter)))
            {
                return QualifiedName((string)null, methodName);
            }
#if DATA
            else if (type == typeof(DatabaseVariable))
            {
                return QualifiedName(type.Name, methodName);
            }
#endif
#if NETWORK && WEB
            else if (type == typeof(NetworkVariable))
            {
                return QualifiedName(type.Name, methodName);
            }
#endif
#if !NET_STANDARD_20 && WINDOWS
            else if (type == typeof(RegistryVariable))
            {
                return QualifiedName(type.Name, methodName);
            }
#endif
            else if (!assembly && IsSameAssembly(type))
            {
                return QualifiedName(type.FullName, methodName);
            }
            else
            {
                return StringList.MakeList(
                    (type.Assembly != null) ? type.Assembly.FullName : null,
                    type.FullName, methodName);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ArgumentName(
            int position,
            string name
            )
        {
            return String.Format(
                "{0}{1} {2}", Characters.NumberSign,
                position, SomeKindOfPrefixAndSuffix(name));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeEmitPolicyResults(
            PolicyWrapperDictionary allPolicies,
            PolicyWrapperDictionary failedPolicies,
            MethodFlags methodFlags,
            PolicyFlags policyFlags,
            string fileName,
            ReturnCode code,
            PolicyDecision decision,
            Result result
            )
        {
            bool success = PolicyOps.IsSuccess(code, decision);

            return String.Format(
                "MaybeEmitPolicyResults: {0} --> {1}, methodFlags = {2}, " +
                "policyFlags = {3}, fileName = {4}, decision = {5}, " +
                "code = {6}, result = {7}", success ? "SUCCESS" : "FAILURE",
                success ? WrapOrNull(allPolicies) : WrapOrNull(failedPolicies),
                WrapOrNull(methodFlags), WrapOrNull(policyFlags),
                WrapOrNull(fileName), decision, code, WrapOrNull(result));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PolicyDelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            IScriptPolicy policy = @delegate.Target as IScriptPolicy;

            if (policy != null)
            {
                return String.Format(
                    "{0} {1}{1}{2} {3}", RawTypeName(policy),
                    Characters.MinusSign, Characters.GreaterThanSign,
                    RawTypeName(policy.CommandType)).Trim();
            }

            MethodBase methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                RawTypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceDelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            MethodBase methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                RawTypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            MethodInfo methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                RawTypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodQualifiedName(
            Type type,
            string methodName
            )
        {
            string typeName = null;

            if (!IsSameAssembly(type))
                typeName = (type != null) ? type.Name : null;

            return MethodName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodQualifiedFullName(
            Type type,
            string methodName
            )
        {
            string typeName = null;

            if (!IsSameAssembly(type))
                typeName = (type != null) ? type.FullName : null;

            return MethodName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodFullName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.FullName : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.Name : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodName(
            string typeOrObjectName,
            string methodName
            )
        {
            return QualifiedName(typeOrObjectName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddTypeList(
            StringBuilder builder,
            TypeList types,
            string @default
            )
        {
            if (builder == null)
                return;

            if (types == null)
            {
                if (@default != null)
                    builder.Append(@default);

                return;
            }

            int count = types.Count;

            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                Type type = types[index];

                if (type == null)
                {
                    builder.Append(DisplayNull);
                    continue;
                }

                builder.Append(type.FullName);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddSignature(
            StringBuilder builder,
            string qualifiedMethodName,
            ParameterInfo returnInfo,
            ParameterInfo[] parameterInfo
            )
        {
            if (builder == null)
                return;

            if (returnInfo != null)
            {
                TypeList returnType = null;

                if (MarshalOps.GetTypeListFromParameterInfo(
                        new ParameterInfo[] { returnInfo }, true,
                        ref returnType) == ReturnCode.Ok)
                {
                    MaybeAddTypeList(
                        builder, returnType, "[missing return type]");
                }
                else
                {
                    builder.Append("[could not get return type]");
                }
            }
            else
            {
                builder.Append("[unknown return type]");
            }

            if (builder.Length > 0)
                builder.Append(Characters.Space);

            if (qualifiedMethodName != null)
                builder.Append(qualifiedMethodName);
            else
                builder.Append("[unknown method name]");

            if (parameterInfo != null)
            {
                TypeList parameterTypes = null;

                if (MarshalOps.GetTypeListFromParameterInfo(
                        parameterInfo, false,
                        ref parameterTypes) == ReturnCode.Ok)
                {
                    builder.Append(Characters.OpenParenthesis);

                    MaybeAddTypeList(
                        builder, parameterTypes, "[missing parameter types]");

                    builder.Append(Characters.CloseParenthesis);
                }
                else
                {
                    builder.Append("[could not get parameter types]");
                }
            }
            else
            {
                builder.Append("[unknown parameter types]");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodOverload(
            int index,
            string objectName,
            string methodName,
            ParameterInfo returnInfo,
            ParameterInfo[] parameterInfo,
            MarshalFlags marshalFlags
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            string qualifiedMethodName = QualifiedName(
                objectName, methodName);

            if (AlwaysShowSignatures || FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ShowSignatures, true))
            {
                MaybeAddSignature(
                    builder, qualifiedMethodName, returnInfo,
                    parameterInfo);

                /* NO RESULT */
                SomeKindOfPrefixAndSuffix(builder);
            }
            else
            {
                builder.Append(SomeKindOfPrefixAndSuffix(
                    qualifiedMethodName));
            }

            if (index != Index.Invalid)
            {
                builder.Insert(0, String.Format("{0}{1}{2}",
                    Characters.NumberSign, index, Characters.Space));
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeHashCode(
            object value
            )
        {
            if (value == null)
                return null;

            return Hexadecimal(RuntimeOps.GetHashCode(value), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeHashCode(
            IObject @object
            )
        {
            if (@object == null)
                return null;

            return Hexadecimal(RuntimeOps.GetHashCode(@object), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapHashCode(
            object value
            )
        {
            return WrapOrNull(MaybeHashCode(value));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapHashCode(
            IObject @object
            )
        {
            return WrapOrNull(MaybeHashCode(@object));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAppDomain()
        {
            return DisplayAppDomain(AppDomainOps.GetCurrent());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAppDomain(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                try
                {
                    StringBuilder result = StringOps.NewStringBuilder();

                    result.AppendFormat(
                        "[id = {0}, default = {1}]",
                        AppDomainOps.GetId(appDomain),
                        AppDomainOps.IsDefault(appDomain));

                    return result.ToString();
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string HomeDirectoryPairs(
            IList<IAnyPair<HomeFlags, string>> value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value.Count == 0)
                return DisplayEmpty;

            StringList list = new StringList();

            foreach (IAnyPair<HomeFlags, string> anyPair in value)
            {
                if (anyPair == null)
                    continue;

                list.Add(anyPair.ToString());
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppDomainFriendlyName(
            string fileName,
            string typeName
            )
        {
            return StringList.MakeList(fileName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppDomainFriendlyName(
            AssemblyName assemblyName,
            string typeName
            )
        {
            string result;

            if (assemblyName != null)
            {
                if (typeName != null)
                {
                    result = Assembly.CreateQualifiedName(
                        assemblyName.ToString(), typeName);
                }
                else
                {
                    result = assemblyName.ToString();
                }
            }
            else
            {
                if (typeName != null)
                {
                    result = typeName;
                }
                else
                {
                    result = String.Empty;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginName(
            string assemblyName,
            string typeName
            )
        {
            // return QualifiedName(assemblyName, typeName);
            return Assembly.CreateQualifiedName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginSimpleName(
            IPluginData pluginData
            )
        {
            if (pluginData == null)
                return null;

            AssemblyName assemblyName;

#if ISOLATED_PLUGINS
            if (AppDomainOps.IsIsolated(pluginData))
            {
                assemblyName = pluginData.AssemblyName;
            }
            else
#endif
            {
                Assembly assembly = pluginData.Assembly;

                if (assembly == null)
                    return null;

                assemblyName = assembly.GetName();
            }

            if (assemblyName == null)
                return null;

            return assemblyName.Name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginCommand(
            Assembly assembly,
            string pluginName,
            Type type,
            string typeName
            )
        {
            AssemblyName assemblyName = (assembly != null) ? assembly.GetName() : null;

            return String.Format(
                "{0}{1}{2}",
                (assemblyName != null) ? assemblyName.Name : pluginName,
                Characters.Underscore,
                (type != null) ? type.Name : typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginAbout(
            IPluginData pluginData,
            bool full,
            string extra
            )
        {
            if (pluginData != null)
            {
                Type type;

                try
                {
                    type = pluginData.GetType(); /* throw */
                }
                catch
                {
                    type = null;
                }

                string appDomainId;

                try
                {
                    AppDomain appDomain = pluginData.AppDomain; /* throw */

                    if (appDomain != null)
                        appDomainId = AppDomainOps.GetId(appDomain).ToString();
                    else
                        appDomainId = DisplayNull;
                }
                catch
                {
                    appDomainId = DisplayUnknown;
                }

                string simpleName = RuntimeOps.GetPluginSimpleName(pluginData);

                if (simpleName == null)
                    simpleName = DisplayUnavailable;

                string typeName = TypeNameOrFullName(type, null, full, false);

                return String.Format(
                    "{0}{1}{2}{3} v{4} ({5}){6}", Characters.HorizontalTab,
                    RuntimeOps.PluginFlagsToPrefix(pluginData.Flags),
                    simpleName, (typeName != null) ? String.Format(
                    "{0}{1}", Type.Delimiter, typeName) : String.Empty,
                    pluginData.Version, appDomainId, extra).TrimEnd();
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was shamelessly stolen from
        //       Kevin B. Kenny's [clock] command implementation
        //       in Tcl 8.5.
        //
        private static string Stardate(
            DateTime value
            ) // COMPAT: Tcl
        {
            return String.Format(StardateOutputFormat,
                value.Year - Roddenberry,
                ((value.DayOfYear - 1) * 1000) / TimeOps.DaysInYear(value.Year),
                Characters.Period,
                (TimeOps.WholeSeconds(value) %
                    TimeOps.SecondsInNormalDay) / (TimeOps.SecondsInNormalDay / 10));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Tries(
            int tries,
            int delay,
            int limit
            )
        {
            if (tries > 0)
            {
                StringBuilder builder = StringOps.NewStringBuilder();

                builder.AppendFormat(
                    "after {0} of {1} tries", tries, (limit >= 0) ?
                    limit.ToString() : "unlimited");

                long milliseconds = (delay > 0) ? ((long)delay * tries) : 0;

                if (milliseconds > 0)
                {
                    builder.AppendFormat(
                        " or about {2} milliseconds", milliseconds);
                }

                return builder.ToString();
            }
            else
            {
                return "without trying";
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
        public static int MaybeAddSubList(
            StringPairList list,
            StringList subList,
            string item,
            bool empty
            )
        {
            int added = 0;

            if (subList != null)
            {
                int count = subList.Count;

                if (empty || (count > 0))
                {
                    if (item != null)
                    {
                        list.Add((IPair<string>)null);
                        list.Add(item);
                        list.Add((IPair<string>)null);
                        added += 3;
                    }

                    list.Add("Count", count.ToString());
                    added++;

                    if (count > 0)
                    {
                        for (int index = 0; index < count; index++)
                        {
                            list.Add(index.ToString(), subList[index]);
                            added++;
                        }
                    }
                }
            }
            else if (empty && (item != null))
            {
                list.Add((IPair<string>)null);
                list.Add(item);
                list.Add((IPair<string>)null);
                added += 3;
            }

            return added;
        }
#endif
    }
}
