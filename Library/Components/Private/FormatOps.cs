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
using _TracePriority = Eagle._Components.Public.TracePriority;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the central collection of static helper methods
    /// used throughout the Eagle library to format values into human-readable
    /// strings for display, tracing, logging, and diagnostic output.  It
    /// handles formatting of dates and times (including Tcl-compatible clock
    /// formatting), type and method names, numbers (including hexadecimal),
    /// exceptions and stack traces, certificates, URIs, and many other kinds
    /// of values, along with the related concerns of wrapping, ellipsis
    /// limiting, and producing placeholder display strings for special cases
    /// such as null, empty, invalid, or unavailable values.
    /// </summary>
    [ObjectId("62feeba0-3df8-4395-b850-c4d307d021a7")]
    internal static class FormatOps
    {
        #region Private Constants
        /// <summary>
        /// The numeric format specifier used when formatting trace priority
        /// values (hexadecimal).
        /// </summary>
        private static readonly string TracePriorityFormat = "X";

        /// <summary>
        /// The separator placed between the runtime name and version when
        /// formatting runtime information.
        /// </summary>
        private const string RuntimeSeparator = " - ";

        /// <summary>
        /// The separator placed between configuration components when
        /// formatting configuration information.
        /// </summary>
        private const string ConfigurationSeparator = " - ";

        /// <summary>
        /// The placeholder string used when no platform name is available.
        /// </summary>
        private static readonly string NoPlatformName = "none";

        /// <summary>
        /// The placeholder string used when a type name is unknown.
        /// </summary>
        private static readonly string UnknownTypeName = "unknown";

        /// <summary>
        /// The display string used to represent an infinite value.
        /// </summary>
        internal static readonly string DisplayInfinite = "<infinite>";

        /// <summary>
        /// The display string used to represent the absence of a message.
        /// </summary>
        internal static readonly string DisplayNoMessage = "<noMessage>";

        /// <summary>
        /// The display string used to represent the absence of a category.
        /// </summary>
        internal static readonly string DisplayNoCategory = "<noCategory>";

        /// <summary>
        /// The display string used to represent the absence of a time value.
        /// </summary>
        private static readonly string DisplayNoTime = "<noTime>";

        /// <summary>
        /// The display string used to represent the absence of a type.
        /// </summary>
        private static readonly string DisplayNoType = "<noType>";

        /// <summary>
        /// The display string used to represent the absence of an assembly.
        /// </summary>
        private static readonly string DisplayNoAssembly = "<noAssembly>";

        /// <summary>
        /// The display string used to represent the absence of a result.
        /// </summary>
        internal static readonly string DisplayNoResult = "<noResult>";

        /// <summary>
        /// The display string used to represent the absence of a value (none).
        /// </summary>
        internal static readonly string DisplayNone = "<none>";

        /// <summary>
        /// The display string used to represent a value that is known to be
        /// non-null.
        /// </summary>
        private static readonly string DisplayNotNull = "<notNull>";

        /// <summary>
        /// The display string used to represent a null value.
        /// </summary>
        internal static readonly string DisplayNull = "<null>";

        /// <summary>
        /// The display string used to represent a null dictionary key.
        /// </summary>
        private static readonly string DisplayNullKey = "<nullKey>";

        /// <summary>
        /// The display string used to represent a transparent proxy object.
        /// </summary>
        internal static readonly string DisplayProxy = "<proxy>";

#if DEBUGGER || SHELL
        /// <summary>
        /// The display string used to represent a value whose type does not
        /// match what was expected.
        /// </summary>
        private static readonly string DisplayTypeMismatch = "<typeMismatch>";
#endif

        /// <summary>
        /// The display string used to represent an object.
        /// </summary>
        internal static readonly string DisplayObject = "<object>";

        /// <summary>
        /// The display string used to represent a null object reference.
        /// </summary>
        private static readonly string DisplayNullObject = "<nullObject>";

        /// <summary>
        /// The display string used to represent a null string.
        /// </summary>
        private static readonly string DisplayNullString = "<nullString>";

        /// <summary>
        /// The display string used to represent an empty string.
        /// </summary>
        private static readonly string DisplayEmptyString = "<emptyString>";

        /// <summary>
        /// The display string used when an error occurs while converting a
        /// value to its string representation.
        /// </summary>
        private static readonly string DisplayToStringError = "<toStringError>";

        /// <summary>
        /// The display string used to represent an empty value.
        /// </summary>
        internal static readonly string DisplayEmpty = "<empty>";

        /// <summary>
        /// The display string used to represent the absence of anything.
        /// </summary>
        internal static readonly string DisplayNothing = "<nothing>";

        /// <summary>
        /// The display string used to represent an invalid value.
        /// </summary>
        internal static readonly string DisplayInvalid = "<invalid>";

        /// <summary>
        /// The display string used to represent a null list.
        /// </summary>
        private static readonly string DisplayNullList = "<nullList>";

        /// <summary>
        /// The display string used to represent an empty list.
        /// </summary>
        private static readonly string DisplayEmptyList = "<emptyList>";

        /// <summary>
        /// The display string used to represent a single space character.
        /// </summary>
        private static readonly string DisplaySpace = "<space>";

        /// <summary>
        /// The display string used to represent a disposed object.
        /// </summary>
        internal static readonly string DisplayDisposed = "<disposed>";

        /// <summary>
        /// The format string used to represent a disposed object together with
        /// additional detail.
        /// </summary>
        internal static readonly string DisplayDisposedFormat = "<disposed:{0}>";

        /// <summary>
        /// The display string used to represent a busy object.
        /// </summary>
        internal static readonly string DisplayBusy = "<busy>";

        /// <summary>
        /// The format string used to represent a busy object together with
        /// additional detail.
        /// </summary>
        internal static readonly string DisplayBusyFormat = "<busy:{0}>";

        /// <summary>
        /// The display string used to represent an error.
        /// </summary>
        private static readonly string DisplayError = "<error>";

        /// <summary>
        /// The format string used to represent an error together with one
        /// additional detail value.
        /// </summary>
        private static readonly string DisplayErrorFormat0 = "<error:{0}>";

        /// <summary>
        /// The format string used to represent an error together with two
        /// additional detail values.
        /// </summary>
        private static readonly string DisplayErrorFormat1 = "<error:{0}:{1}>";

        /// <summary>
        /// The display string used to represent an unknown value.
        /// </summary>
        internal static readonly string DisplayUnknown = "<unknown>";

        /// <summary>
        /// The display string used to represent a value that has been
        /// obfuscated.
        /// </summary>
        private static readonly string DisplayObfuscated = "<obfuscated>";

        /// <summary>
        /// The display string used to represent a value that is present.
        /// </summary>
        internal static readonly string DisplayPresent = "<present>";

        /// <summary>
        /// The format string used to wrap an arbitrary value in angle brackets
        /// for display.
        /// </summary>
        internal static readonly string DisplayFormat = "<{0}>";

        /// <summary>
        /// The display string used to represent an anonymous value.
        /// </summary>
        private static readonly string DisplayAnonymous = "<anonymous>";

        /// <summary>
        /// The display string used to represent a value that is unavailable.
        /// </summary>
        internal static readonly string DisplayUnavailable = "<unavailable>";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The placeholder text used to represent the "is enabled" query
        /// portion of a formatted enabled/disabled display string.
        /// </summary>
        private static readonly string DisplayMaybeIsEnabled = "<isEnabled>";

        /// <summary>
        /// The placeholder text used to represent the "set enabled" portion of
        /// a formatted enabled/disabled display string.
        /// </summary>
        private static readonly string DisplayMaybeSetEnabled = "<setEnabled>";

        /// <summary>
        /// The placeholder text used to represent the "set disabled" portion of
        /// a formatted enabled/disabled display string.
        /// </summary>
        private static readonly string DisplayMaybeSetDisabled = "<setDisabled>";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The display text used when the present enabled state of something is
        /// unknown.
        /// </summary>
        private static readonly string DisplayIsUnknown = "is unknown";

        /// <summary>
        /// The display text used when something is presently enabled.
        /// </summary>
        private static readonly string DisplayIsEnabled = "is enabled";

        /// <summary>
        /// The display text used when something is presently disabled.
        /// </summary>
        private static readonly string DisplayIsDisabled = "is disabled";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The display text used when the former enabled state of something was
        /// unknown.
        /// </summary>
        private static readonly string DisplayWasUnknown = "was unknown";

        /// <summary>
        /// The display text used when something was formerly enabled.
        /// </summary>
        private static readonly string DisplayWasEnabled = "was enabled";

        /// <summary>
        /// The display text used when something was formerly disabled.
        /// </summary>
        private static readonly string DisplayWasDisabled = "was disabled";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The prefix string used when wrapping a value for display.
        /// </summary>
        private static string WrapPrefix = Characters.QuotationMark.ToString();

        /// <summary>
        /// The suffix string used when wrapping a value for display.
        /// </summary>
        private static string WrapSuffix = Characters.QuotationMark.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The alternate prefix string used when wrapping a value for display.
        /// </summary>
        private static string AltWrapPrefix = Characters.OpenBrace.ToString();

        /// <summary>
        /// The alternate suffix string used when wrapping a value for display.
        /// </summary>
        private static string AltWrapSuffix = Characters.CloseBrace.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, method signatures are always shown when formatting
        /// related output.
        /// </summary>
        private static bool AlwaysShowSignatures = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Package Title Formatting
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the seconds component of the build date and time is
        /// included when formatting a package date and time.
        /// </summary>
        private static bool IncludeBuildSecondsForPackageDateTime = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Ellipsis Limits
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The maximum length, in characters, of a command result before it is
        /// truncated with an ellipsis for display.
        /// </summary>
        private static int ResultEllipsisLimit = 78;

#if HISTORY
        /// <summary>
        /// The maximum length, in characters, of a command history entry before
        /// it is truncated with an ellipsis for display.
        /// </summary>
        private static int HistoryEllipsisLimit = 78;
#endif

        /// <summary>
        /// The default maximum length, in characters, of a value before it is
        /// truncated with an ellipsis for display.
        /// </summary>
        private static int DefaultEllipsisLimit = 512;

        /// <summary>
        /// The maximum length, in characters, of a wrapped value before it is
        /// truncated with an ellipsis for display.
        /// </summary>
        private static int WrapEllipsisLimit = 512;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The ellipsis string appended when a command result is truncated for
        /// display.
        /// </summary>
        private const string ResultEllipsis = " ...";

#if HISTORY
        /// <summary>
        /// The ellipsis string appended when a command history entry is
        /// truncated for display.
        /// </summary>
        private const string HistoryEllipsis = " ...";
#endif

        /// <summary>
        /// The default ellipsis string appended when a value is truncated for
        /// display.
        /// </summary>
        private const string DefaultEllipsis = "...";

        /// <summary>
        /// The numeric format specifier used to produce compact hexadecimal
        /// output.
        /// </summary>
        private const string CompactOutputFormat = "x";

        /// <summary>
        /// The numeric format specifier used to produce two-digit hexadecimal
        /// output for a byte value.
        /// </summary>
        private const string ByteOutputFormat = "x2";

        /// <summary>
        /// The numeric format specifier used to produce four-digit hexadecimal
        /// output for an unsigned short value.
        /// </summary>
        private const string UShortOutputFormat = "x4";

        /// <summary>
        /// The numeric format specifier used to produce sixteen-digit
        /// hexadecimal output for an unsigned long value.
        /// </summary>
        private const string ULongOutputFormat = "x16";

        /// <summary>
        /// The prefix used to denote a hexadecimal number.
        /// </summary>
        internal const string HexadecimalPrefix = "0x";

        /// <summary>
        /// The composite format string used to produce a prefixed hexadecimal
        /// number.
        /// </summary>
        private const string HexadecimalFormat = "{0}{1:X}";

        /// <summary>
        /// The composite format string used to produce the extra trace
        /// indicators portion of formatted trace output.
        /// </summary>
        private const string TraceIndicatorsFormat = "[f:{0}] {1}";

        // private const string HexavigesimalAlphabet = "0123456789ABCDEFGHIJKLMNOP";
        /// <summary>
        /// The set of digit characters used when formatting a value in the
        /// hexavigesimal (base twenty-six) number system.
        /// </summary>
        private const string HexavigesimalAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to match the short form of a release
        /// name (e.g. "Beta 1.0").
        /// </summary>
        private static readonly Regex releaseShortNameRegEx = RegExOps.Create(
            "(?:Pre-|Post-)?(?:Alpha|Beta|RC|Final|Release) \\d+(?:\\.\\d+)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Clock Constants
        /// <summary>
        /// The name of the Greenwich Mean Time time zone.
        /// </summary>
        private const string GmtTimeZoneName = "GMT";

        /// <summary>
        /// The name of the Coordinated Universal Time time zone.
        /// </summary>
        private const string UtcTimeZoneName = "UTC";

        /// <summary>
        /// The format string used to produce a full (long) date and time
        /// string.
        /// </summary>
        private const string DefaultFullDateTimeFormat = "dddd, dd MMMM yyyy HH:mm:ss";

        /// <summary>
        /// The format string used to produce a zero-padded day-of-year value.
        /// </summary>
        private const string DayOfYearFormat = "000"; // COMPAT: Tcl

        /// <summary>
        /// The format string used to produce a zero-padded week-of-year value.
        /// </summary>
        private const string WeekOfYearFormat = "00"; // COMPAT: Tcl

        /// <summary>
        /// The format string used to produce a zero-padded two-digit ISO 8601
        /// year value.
        /// </summary>
        private const string Iso8601YearFormat = "00"; // COMPAT: Tcl

        //
        // NOTE: The Tcl clock specifiers "%e" (day of month), "%k" (hour 0-23),
        //       and "%l" (hour 1-12) are space-padded to this width.  The .NET
        //       DateTime format language has no space-padded field, so these are
        //       handled by the clock delegates below (COMPAT: Tcl).
        //
        /// <summary>
        /// The width, in characters, to which certain space-padded clock fields
        /// are padded.
        /// </summary>
        private const int SpacePaddedFieldWidth = 2; // COMPAT: Tcl

        /// <summary>
        /// The number of hours in a half day, used when formatting a twelve-hour
        /// clock value.
        /// </summary>
        private const int HoursPerHalfDay = 12; // COMPAT: Tcl ("%l")

#if SHELL
        /// <summary>
        /// The format string used to produce the date and time portion of an
        /// update check.
        /// </summary>
        private const string UpdateDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
#endif

#if NETWORK
        /// <summary>
        /// The format string used to produce an ISO 8601 date and time with
        /// seconds precision and a time zone designator.
        /// </summary>
        private const string Iso8601DateTimeSecondsFormat = "yyyy-MM-ddTHH:mm:ssK";
#endif

        /// <summary>
        /// The format string used to produce a full-precision ISO 8601 date and
        /// time with a time zone designator.
        /// </summary>
        private const string Iso8601FullDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        /// <summary>
        /// The format string used to produce the date and time portion of
        /// formatted trace output.
        /// </summary>
        private const string TraceDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>
        /// The format string used to produce the date and time portion of
        /// formatted interactive trace output.
        /// </summary>
        private const string TraceInteractiveDateTimeFormat = "[MM-dd-yyyy hh:mm:ss tt]";

        /// <summary>
        /// The format string used to produce the date and time portion of an
        /// ISO 8601 update value.
        /// </summary>
        private const string Iso8601UpdateDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>
        /// The format string used to produce ISO 8601 date and time output.
        /// </summary>
        private const string Iso8601DateTimeOutputFormat = "yyyy.MM.ddTHH:mm:ss.fff";

        /// <summary>
        /// The format string used to produce package date and time output.
        /// </summary>
        private const string PackageDateTimeOutputFormat = "yyyy.MM.dd";

        /// <summary>
        /// The Tcl clock specifier used to parse a stardate input value.
        /// </summary>
        private const string StardateInputFormat = "%Q"; // COMPAT: Tcl

        /// <summary>
        /// The composite format string used to produce a stardate output value.
        /// </summary>
        private const string StardateOutputFormat = "Stardate {0:D2}{1:D3}{2}{3:D1}";

#if UNIX
        /// <summary>
        /// The Tcl clock specifier used to parse a string (seconds since epoch)
        /// input value.
        /// </summary>
        internal static readonly string StringInputFormat = "%s";
#endif

        /// <summary>
        /// The mapping of Tcl clock format specifiers to their equivalent .NET
        /// date and time format strings.  A null value indicates the specifier
        /// is handled by a delegate in <see cref="tclClockDelegates" /> instead.
        /// </summary>
        private static readonly StringPairList tclClockFormats = new StringPairList(
            new StringPair(GmtTimeZoneName, "\\G\\M\\T"),
            new StringPair("%a", "ddd"), new StringPair("%A", "dddd"),
            new StringPair("%b", "MMM"), new StringPair("%B", "MMMM"),
            new StringPair("%c", GetFullDateTimeFormat()), new StringPair("%C", null),
            new StringPair("%d", "dd"), new StringPair("%D", "MM/dd/yy"),
            new StringPair("%e", null), new StringPair("%g", null),
            new StringPair("%G", null), new StringPair("%h", "MMM"),
            new StringPair("%H", "HH"), new StringPair("%i", Iso8601DateTimeOutputFormat),
            new StringPair("%I", "hh"), new StringPair("%j", null),
            new StringPair("%k", null), new StringPair("%l", null),
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

        /// <summary>
        /// The mapping of Tcl clock format specifiers to the delegates that
        /// produce their formatted values, used for specifiers that have no
        /// direct .NET date and time format string equivalent.
        /// </summary>
        private static readonly DelegateDictionary tclClockDelegates = new DelegateDictionary(
            new ObjectPair("%C", new ClockTransformCallback(TclClockDelegates.GetCentury)),
            new ObjectPair("%e", new ClockTransformCallback(TclClockDelegates.GetDayOfMonthSpacePadded)),
            new ObjectPair("%g", new ClockTransformCallback(TclClockDelegates.GetTwoDigitYearIso8601)),
            new ObjectPair("%G", new ClockTransformCallback(TclClockDelegates.GetFourDigitYearIso8601)),
            new ObjectPair("%j", new ClockTransformCallback(TclClockDelegates.GetDayOfYear)),
            new ObjectPair("%k", new ClockTransformCallback(TclClockDelegates.GetHourOfDaySpacePadded)),
            new ObjectPair("%l", new ClockTransformCallback(TclClockDelegates.GetHourOfHalfDaySpacePadded)),
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
        /// <summary>
        /// When non-zero, the method name used for trace output may be obtained
        /// from anywhere in the call stack by default.
        /// </summary>
        private static bool DefaultGetMethodNameAnywhere = false;

        /// <summary>
        /// When non-zero, the full (type-qualified) method name is displayed in
        /// trace output by default.
        /// </summary>
        private static bool DefaultDisplayMethodFullName = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The marker text used to delimit the start of a stack trace within
        /// formatted output.
        /// </summary>
        private static string StackTraceStart = "<stackTrace>";

        /// <summary>
        /// The marker text used to delimit the end of a stack trace within
        /// formatted output.
        /// </summary>
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
        /// <summary>
        /// The next unique event "serial number" within this application domain,
        /// accessed only via an interlocked increment to help construct event
        /// names that are unique within the entire application domain.
        /// </summary>
        private static long nextEventId;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the list of method names to skip when figuring out
        //       the "correct" method name to use for trace output.
        //
        /// <summary>
        /// The list of method names to skip when determining the "correct"
        /// method name to use for trace output.
        /// </summary>
        private static StringList skipNames = new StringList(
            "DebugTrace", "DebugWrite", "MaybeWritePolicyTrace",
            "MaybeEmitPolicyResults");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this field is non-zero, extra trace indicators will be
        //       included in the formatted trace output.
        //
        /// <summary>
        /// When non-zero, extra trace indicators will be included in the
        /// formatted trace output.
        /// </summary>
        private static bool useTraceIndicators;

        //
        // NOTE: If this field is non-zero, extra trace indicators will be
        //       included as the full text of the associated flag names in
        //       the formatted trace output.
        //
        /// <summary>
        /// When non-zero, extra trace indicators will be included as the full
        /// text of the associated flag names in the formatted trace output.
        /// </summary>
        private static bool rawTraceIndicators;

        //
        // NOTE: If this field is non-zero, the current trace listeners will
        //       be checked and the resulting flags will be included in the
        //       extra trace indicators.
        //
        /// <summary>
        /// When non-zero, the current trace listeners will be checked and the
        /// resulting flags will be included in the extra trace indicators.
        /// </summary>
        private static bool seeTraceListeners;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this field is non-zero, extended characters are allowed to
        //       be used to replace line-endings in display strings.
        //
        /// <summary>
        /// When non-zero, extended characters are allowed to be used to replace
        /// line-endings in display strings.
        /// </summary>
        private static bool extendedLineEndings = false;

        //
        // NOTE: If this field is non-zero, Unicode characters are allowed to
        //       be used to replace line-endings in display strings.
        //
        /// <summary>
        /// When non-zero, Unicode characters are allowed to be used to replace
        /// line-endings in display strings.
        /// </summary>
        private static bool unicodeLineEndings = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Clock Delegates
        /// <summary>
        /// This class provides the delegate implementations used to produce the
        /// formatted values for the Tcl clock format specifiers that have no
        /// direct .NET date and time format string equivalent.
        /// </summary>
        [ObjectId("706d94c0-f87f-4562-abbd-a1917ed99e8c")]
        private static class TclClockDelegates
        {
            /// <summary>
            /// This method produces the century (the year divided by one
            /// hundred) for the specified clock data, corresponding to the Tcl
            /// "%C" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted century value, or null if <paramref name="clockData" />
            /// is null.
            /// </returns>
            public static string GetCentury(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.Year / 100) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the day of the month, space-padded to a
            /// width of two, for the specified clock data, corresponding to the
            /// Tcl "%e" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted day-of-month value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetDayOfMonthSpacePadded(
                IClockData clockData
                )
            {
                //
                // NOTE: Tcl "%e" is the day of the month, space-padded to a
                //       width of two (e.g. " 9", "13").
                //
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.Day.ToString().PadLeft(
                        SpacePaddedFieldWidth, Characters.Space)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the hour (0-23) on a 24-hour clock,
            /// space-padded to a width of two, for the specified clock data,
            /// corresponding to the Tcl "%k" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted hour value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetHourOfDaySpacePadded(
                IClockData clockData
                )
            {
                //
                // NOTE: Tcl "%k" is the hour (0-23) on a 24-hour clock,
                //       space-padded to a width of two (e.g. " 1", "13").
                //
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.Hour.ToString().PadLeft(
                        SpacePaddedFieldWidth, Characters.Space)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the hour (1-12) on a 12-hour clock,
            /// space-padded to a width of two, for the specified clock data,
            /// corresponding to the Tcl "%l" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted hour value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetHourOfHalfDaySpacePadded(
                IClockData clockData
                )
            {
                //
                // NOTE: Tcl "%l" is the hour (1-12) on a 12-hour clock,
                //       space-padded to a width of two (e.g. " 1", "12").
                //
                if (clockData == null)
                    return null;

                int hour = clockData.DateTime.Hour % HoursPerHalfDay;

                if (hour == 0)
                    hour = HoursPerHalfDay;

                return WrapOrNull(true, hour.ToString().PadLeft(
                    SpacePaddedFieldWidth, Characters.Space));
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the two-digit ISO 8601 week-based year for
            /// the specified clock data, corresponding to the Tcl "%g"
            /// specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted two-digit year value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetTwoDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (TimeOps.ThisThursday(
                        clockData.DateTime).Year % 100).ToString(Iso8601YearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the four-digit ISO 8601 week-based year for
            /// the specified clock data, corresponding to the Tcl "%G"
            /// specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted four-digit year value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetFourDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, TimeOps.ThisThursday(clockData.DateTime).Year) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the zero-padded day of the year for the
            /// specified clock data, corresponding to the Tcl "%j" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted day-of-year value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetDayOfYear(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.DayOfYear.ToString(DayOfYearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the stardate value for the specified clock
            /// data, corresponding to the Tcl "%Q" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted stardate value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetStardate(
                IClockData clockData
                )
            {
                return (clockData != null) ? WrapOrNull(true, Stardate(clockData.DateTime)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the number of seconds since the epoch for
            /// the specified clock data, corresponding to the Tcl "%s"
            /// specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time, as well as the
            /// epoch, to use.  This parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted seconds-since-epoch value, or null if
            /// <paramref name="clockData" /> is null or the conversion fails.
            /// </returns>
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

            /// <summary>
            /// This method produces the weekday number (1 through 7, with
            /// Sunday as 7) for the specified clock data, corresponding to the
            /// Tcl "%u" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted weekday number, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
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

            /// <summary>
            /// This method produces the week of the year, treating Sunday as
            /// the first day of the week, for the specified clock data,
            /// corresponding to the Tcl "%U" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted week-of-year value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
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

            /// <summary>
            /// This method produces the ISO 8601 week of the year for the
            /// specified clock data, corresponding to the Tcl "%V" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time, as well as the
            /// culture, to use.  This parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted week-of-year value, or null if
            /// <paramref name="clockData" /> is null or lacks the required
            /// culture information.
            /// </returns>
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

            /// <summary>
            /// This method produces the weekday number (0 through 6, with
            /// Sunday as 0) for the specified clock data, corresponding to the
            /// Tcl "%w" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted weekday number, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
            public static string GetWeekdayNumberZeroToSix(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (int)clockData.DateTime.DayOfWeek) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method produces the week of the year, treating Monday as
            /// the first day of the week, for the specified clock data,
            /// corresponding to the Tcl "%W" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time to format.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted week-of-year value, or null if
            /// <paramref name="clockData" /> is null.
            /// </returns>
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

            /// <summary>
            /// This method produces the time zone name for the specified clock
            /// data, corresponding to the Tcl "%Z" specifier.
            /// </summary>
            /// <param name="clockData">
            /// The clock data containing the date and time, as well as the time
            /// zone, to use.  This parameter may be null.
            /// </param>
            /// <returns>
            /// The formatted time zone name, or null if
            /// <paramref name="clockData" /> is null or no time zone name can be
            /// determined.
            /// </returns>
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

        /// <summary>
        /// This method produces a human-readable string for the specified time
        /// span, choosing an appropriate unit (days/hours/etc., milliseconds,
        /// or ticks) based on its magnitude.
        /// </summary>
        /// <param name="timeSpan">
        /// The time span to format.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// When non-zero and the value cannot be formatted normally, a
        /// human-readable placeholder is returned instead of null.
        /// </param>
        /// <returns>
        /// The formatted time span string, or a placeholder or null depending
        /// on <paramref name="display" />.
        /// </returns>
        public static string TimeSpan(
            TimeSpan? timeSpan,
            bool display
            )
        {
            if (timeSpan == null)
                return display ? DisplayNull : null;

            //
            // HACK: If there are a non-zero number of days, hours, etc,
            //       we can just use the ToString method here, which will
            //       produce an appropriate human readable string.
            //
            TimeSpan localTimeSpan = (TimeSpan)timeSpan;

            if (MathOps.NotZero(localTimeSpan.TotalDays) ||
                MathOps.NotZero(localTimeSpan.TotalHours) ||
                MathOps.NotZero(localTimeSpan.TotalMinutes) ||
                MathOps.NotZero(localTimeSpan.TotalSeconds))
            {
                return localTimeSpan.ToString();
            }
            else
            {
                double milliseconds = localTimeSpan.TotalMilliseconds;

                if (MathOps.NotZero(milliseconds))
                {
                    return String.Format(
                        "{0} milliseconds", milliseconds);
                }
                else
                {
                    long ticks = localTimeSpan.Ticks;

                    if (ticks != 0)
                        return String.Format("{0} ticks", ticks);
                    else
                        return display ? DisplayNoTime : null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all of the trace indicator settings to their
        /// disabled state.
        /// </summary>
        public static void ResetTraceIndicators()
        {
            useTraceIndicators = false;
            rawTraceIndicators = false;
            seeTraceListeners = false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets all of the trace indicator settings to the
        /// specified values.
        /// </summary>
        /// <param name="useIndicators">
        /// When non-zero, extra trace indicators will be included in the
        /// formatted trace output.
        /// </param>
        /// <param name="rawIndicators">
        /// When non-zero, extra trace indicators will be included as the full
        /// text of the associated flag names.
        /// </param>
        /// <param name="seeListeners">
        /// When non-zero, the current trace listeners will be checked and the
        /// resulting flags will be included in the extra trace indicators.
        /// </param>
        public static void SetTraceIndicators(
            bool useIndicators,
            bool rawIndicators,
            bool seeListeners
            )
        {
            useTraceIndicators = useIndicators;
            rawTraceIndicators = rawIndicators;
            seeTraceListeners = seeListeners;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// This method produces a display string for the specified Tcl build.
        /// </summary>
        /// <param name="build">
        /// The Tcl build to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the build, or a placeholder if
        /// <paramref name="build" /> is null.
        /// </returns>
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
        /// <summary>
        /// This method produces a display string for the specified registry
        /// subkey, optionally wrapping the combined key and subkey path.
        /// </summary>
        /// <param name="key">
        /// The parent registry key.  This parameter may be null.
        /// </param>
        /// <param name="subKeyName">
        /// The name of the subkey relative to <paramref name="key" />.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted, optionally wrapped, registry subkey string.
        /// </returns>
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

        /// <summary>
        /// This method obtains the full date and time format string to be used
        /// when formatting date and time values.
        /// </summary>
        /// <returns>
        /// The full date and time format string.  This will never be null.
        /// </returns>
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
        /// <summary>
        /// This method decomposes a flags-style enumerated value into the list
        /// of individual flag names whose bits are set within it, optionally
        /// controlling how nameless, invalid, or zero-valued flags are handled.
        /// </summary>
        /// <param name="enumValue">
        /// The flags enumeration value to decompose.  This parameter may not be
        /// null and its type must be an enumeration.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive parsing of the enumeration
        /// names; otherwise, parsing is case-sensitive.
        /// </param>
        /// <param name="skipNameless">
        /// Non-zero to silently skip any enumeration name that is null or empty;
        /// otherwise, encountering such a name is treated as an error.
        /// </param>
        /// <param name="skipBadName">
        /// Non-zero to silently skip any enumeration name that cannot be parsed
        /// into a value; otherwise, a parse failure is treated as an error.
        /// </param>
        /// <param name="skipBadValue">
        /// Non-zero to silently skip any flag whose underlying value cannot be
        /// obtained; otherwise, such a failure is treated as an error.
        /// </param>
        /// <param name="keepZeros">
        /// Non-zero to include the names of flags whose underlying value is
        /// zero in the resulting list; otherwise, such names are skipped.
        /// </param>
        /// <param name="uniqueValues">
        /// Non-zero to add a name only when its bits remain set in the overall
        /// value; otherwise, names whose bits were already accounted for by a
        /// previously matched flag may also be added.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The list of flag names whose bits are set within the value, or null
        /// if an error was encountered.
        /// </returns>
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

        /// <summary>
        /// This method builds the list of flag names that are set within the
        /// specified enumerated value, optionally deriving the candidate names
        /// and values from the type of that value.
        /// </summary>
        /// <param name="enumValue">
        /// The enumerated value to be examined.
        /// </param>
        /// <param name="enumNames">
        /// The list of candidate enumeration names to consider, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="enumValues">
        /// The list of enumeration values that correspond to the entries in
        /// <paramref name="enumNames" />, if any.  This parameter may be null.
        /// </param>
        /// <param name="skipEnumType">
        /// Non-zero to skip deriving the candidate names and values from the
        /// type of <paramref name="enumValue" />.
        /// </param>
        /// <param name="skipNameless">
        /// Non-zero to skip any enumeration value that has no associated name.
        /// </param>
        /// <param name="keepZeros">
        /// Non-zero to keep names whose underlying enumeration value is zero.
        /// </param>
        /// <param name="uniqueValues">
        /// Non-zero to require that each contributing flag value be unique.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The list of flag names that are set within the enumerated value, or
        /// null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method builds the list of flag names that are set within the
        /// specified enumerated value, using the supplied candidate names and
        /// values.
        /// </summary>
        /// <param name="enumValue">
        /// The enumerated value to be examined.
        /// </param>
        /// <param name="enumNames">
        /// The list of candidate enumeration names to consider.
        /// </param>
        /// <param name="enumValues">
        /// The list of enumeration values that correspond to the entries in
        /// <paramref name="enumNames" />.
        /// </param>
        /// <param name="skipNameless">
        /// Non-zero to skip any enumeration value that has no associated name.
        /// </param>
        /// <param name="keepZeros">
        /// Non-zero to keep names whose underlying enumeration value is zero.
        /// </param>
        /// <param name="uniqueValues">
        /// Non-zero to require that each contributing flag value be unique.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The list of flag names that are set within the enumerated value, or
        /// null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method builds the test hook pattern string for the specified
        /// test hook type and pattern.
        /// </summary>
        /// <param name="type">
        /// The test hook type to be combined with the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to be combined with the test hook type.
        /// </param>
        /// <returns>
        /// The formatted test hook pattern string.
        /// </returns>
        public static string TestHookPattern(
            TestHookType type,
            string pattern
            )
        {
            TestHookType baseType = (type & TestHookType.TypeMask);

            return String.Format("{0}_{1}", EnumOps.FixupEnumString(
                baseType.ToString()), pattern);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a regular expression match into a human-readable
        /// string for display purposes.
        /// </summary>
        /// <param name="match">
        /// The regular expression match to be formatted.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A human-readable string describing the regular expression match.
        /// </returns>
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
        /// <summary>
        /// This method formats the file name of the specified Tcl build for
        /// display purposes.
        /// </summary>
        /// <param name="build">
        /// The Tcl build whose file name is to be formatted.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The wrapped file name of the Tcl build, or a placeholder if it is
        /// null.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified call frame into a human-readable
        /// string for display purposes.
        /// </summary>
        /// <param name="frame">
        /// The call frame to be formatted.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A human-readable string describing the call frame.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified console color into a string for
        /// display purposes.
        /// </summary>
        /// <param name="color">
        /// The console color to be formatted.
        /// </param>
        /// <returns>
        /// The name of the console color, or <c>None</c> when no color is set.
        /// </returns>
        public static string DisplayColor(
            ConsoleColor color
            )
        {
            return (color != _ConsoleColor.None) ? color.ToString() : "None";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method formats the specified console key information into a list
        /// of name and value pairs for display purposes.
        /// </summary>
        /// <param name="consoleKeyInfo">
        /// The console key information to be formatted.
        /// </param>
        /// <returns>
        /// A list describing the modifiers, key, and key character of the
        /// console key information.
        /// </returns>
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
        /// <summary>
        /// This method formats the specified wait handle into a string for
        /// display purposes.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle to be formatted.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A string containing the native handle of the wait handle, or a
        /// placeholder if it is null.
        /// </returns>
        public static string DisplayWaitHandle(
            WaitHandle waitHandle
            )
        {
            if (waitHandle != null)
                return String.Format("{0}", waitHandle.Handle);

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified array of wait handles into a
        /// single string for display purposes.
        /// </summary>
        /// <param name="waitHandles">
        /// The array of wait handles to be formatted.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A string describing the wait handles, or a placeholder if it is
        /// null.
        /// </returns>
        public static string DisplayWaitHandles(
            WaitHandle[] waitHandles
            )
        {
            if (waitHandles != null)
            {
                StringBuilder result = StringBuilderFactory.Create();

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

                return StringBuilderCache.GetStringAndRelease(ref result);
            }

            return DisplayNull;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_ARGUMENTS
        /// <summary>
        /// This method formats the specified value into a string suitable for
        /// use in trace output, optionally normalizing white-space, truncating
        /// it with an ellipsis, and quoting it.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize the white-space within the string form of the
        /// value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the string form of the value with an ellipsis
        /// when it exceeds the configured limit.
        /// </param>
        /// <param name="quote">
        /// Non-zero to format the value as a single-element list, quoting it as
        /// necessary.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display placeholder when the value is null or
        /// empty.
        /// </param>
        /// <param name="value">
        /// The value to be formatted.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted string for the value, or a placeholder or null
        /// depending on the supplied flags.
        /// </returns>
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

                        return String.Format(DisplayErrorFormat0,
                            (type != null) ? type.Name : UnknownTypeName);
                    }
                }

                return display ? DisplayEmpty : String.Empty;
            }

            return display ? DisplayNull : null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to its string
        /// representation, using a custom script binder callback when one is
        /// available and falling back to the default conversion otherwise.
        /// </summary>
        /// <param name="binder">
        /// The binder used to perform any custom string conversion, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when converting the value, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to be converted to a string.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The default string to return when the value cannot be converted.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the value.
        /// </returns>
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

        /// <summary>
        /// This method formats a sequence of method arguments into a string
        /// list, recording the type name and string value of each argument.
        /// </summary>
        /// <param name="binder">
        /// The binder used when converting each argument to its string form.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when converting each argument to its string form.
        /// </param>
        /// <param name="args">
        /// The arguments to be formatted; may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to use display placeholders for null and empty values.
        /// </param>
        /// <returns>
        /// The formatted string representation of the arguments.
        /// </returns>
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

        /// <summary>
        /// This method wraps a sequence of argument strings, returning a
        /// display placeholder when the sequence is null.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the wrapped value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the wrapped value with an ellipsis when it is
        /// too long.
        /// </param>
        /// <param name="args">
        /// The argument strings to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string, or a display placeholder when the arguments are
        /// null.
        /// </returns>
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

        /// <summary>
        /// This method formats a script value for use in a log message,
        /// substituting display placeholders for null, empty, and white-space
        /// values.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the value with an ellipsis when it is too long.
        /// </param>
        /// <param name="value">
        /// The script value to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The formatted string suitable for logging.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified sequence into a string list when
        /// it is non-null and does not already implement the string list
        /// interface.
        /// </summary>
        /// <param name="value">
        /// The sequence to be converted; upon return, it may refer to a newly
        /// created string list.
        /// </param>
        private static void MaybeConvertToStringList(
            ref IEnumerable<string> value /* in, out */
            )
        {
            if ((value != null) && !(value is IStringList))
                value = new StringList(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally selects alternate wrapping prefix and
        /// suffix strings when the string form of the specified value already
        /// contains the default prefix or suffix.
        /// </summary>
        /// <param name="wrap">
        /// Non-zero if the value is to be wrapped.
        /// </param>
        /// <param name="value">
        /// The value whose string form is to be examined.
        /// </param>
        /// <param name="prefix">
        /// The wrapping prefix; upon return, it may be changed to an alternate
        /// prefix.
        /// </param>
        /// <param name="suffix">
        /// The wrapping suffix; upon return, it may be changed to an alternate
        /// suffix.
        /// </param>
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

        /// <summary>
        /// This method conditionally selects alternate wrapping prefix and
        /// suffix strings when the string form of the specified value already
        /// contains the default prefix or suffix, also returning that string
        /// form.
        /// </summary>
        /// <param name="wrap">
        /// Non-zero if the value is to be wrapped.
        /// </param>
        /// <param name="value">
        /// The value whose string form is to be examined.
        /// </param>
        /// <param name="prefix">
        /// The wrapping prefix; upon return, it may be changed to an alternate
        /// prefix.
        /// </param>
        /// <param name="suffix">
        /// The wrapping suffix; upon return, it may be changed to an alternate
        /// suffix.
        /// </param>
        /// <param name="stringValue">
        /// Upon return, contains the string form of the value, or null when
        /// the value is not being wrapped.
        /// </param>
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

        /// <summary>
        /// This method formats a return code and result, wrapping the
        /// resulting value.
        /// </summary>
        /// <param name="code">
        /// The return code to be formatted.
        /// </param>
        /// <param name="result">
        /// The result to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string representation of the return code and result.
        /// </returns>
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

        /// <summary>
        /// This method wraps the specified sequence of strings.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the wrapped value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the wrapped value with an ellipsis when it is
        /// too long.
        /// </param>
        /// <param name="value">
        /// The strings to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string, or a display placeholder when the value is
        /// null.
        /// </returns>
        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> value
            )
        {
            return WrapOrNull(normalize, ellipsis, false, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wraps the specified value.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the wrapped value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the wrapped value with an ellipsis when it is
        /// too long.
        /// </param>
        /// <param name="value">
        /// The value to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string, or a display placeholder when the value is
        /// null.
        /// </returns>
        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return WrapOrNull(normalize, ellipsis, false, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wraps the specified sequence of strings, optionally
        /// using a display placeholder for a null value.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the wrapped value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the wrapped value with an ellipsis when it is
        /// too long.
        /// </param>
        /// <param name="display">
        /// Non-zero to use a display placeholder when the value is null.
        /// </param>
        /// <param name="value">
        /// The strings to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string, or a placeholder when the value is null.
        /// </returns>
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

        /// <summary>
        /// This method wraps the specified value, optionally using a display
        /// placeholder for a null value.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to normalize white-space within the wrapped value.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the wrapped value with an ellipsis when it is
        /// too long.
        /// </param>
        /// <param name="display">
        /// Non-zero to use a display placeholder when the value is null.
        /// </param>
        /// <param name="value">
        /// The value to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string, or a placeholder when the value is null.
        /// </returns>
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

        /// <summary>
        /// This method removes a matching leading and trailing character from
        /// the specified string.
        /// </summary>
        /// <param name="value">
        /// The string to be stripped; may be null or empty.
        /// </param>
        /// <param name="character">
        /// The character to be removed from both ends of the string.
        /// </param>
        /// <returns>
        /// The stripped string, or the original string when no matching outer
        /// characters are present.
        /// </returns>
        public static string StripOuter(
            string value,
            char character
            )
        {
            return StripOuter(value, character, character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a matching leading prefix character and
        /// trailing suffix character from the specified string.
        /// </summary>
        /// <param name="value">
        /// The string to be stripped; may be null or empty.
        /// </param>
        /// <param name="prefix">
        /// The character to be removed from the start of the string.
        /// </param>
        /// <param name="suffix">
        /// The character to be removed from the end of the string.
        /// </param>
        /// <returns>
        /// The stripped string, or the original string when the matching outer
        /// characters are not present.
        /// </returns>
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

        /// <summary>
        /// This method formats a collection of defined constant names into a
        /// single space-separated string.
        /// </summary>
        /// <param name="collection">
        /// The collection of constant names to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The space-separated string of constant names, or a display
        /// placeholder when the collection is null or empty.
        /// </returns>
        public static string DefineConstants(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return DisplayNullList;

            StringBuilder builder = StringBuilderFactory.Create();

            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(Characters.Space);

                builder.Append(item);
            }

            if (builder.Length > 0)
            {
                return StringBuilderCache.GetStringAndRelease(
                    ref builder);
            }

            /* IGNORED */
            StringBuilderCache.Release(ref builder);

            return DisplayEmptyList;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a display placeholder when the specified value
        /// is null.
        /// </summary>
        /// <param name="value">
        /// The value to be checked; may be null.
        /// </param>
        /// <returns>
        /// The original value, or a display placeholder when it is null.
        /// </returns>
        public static object MaybeNull(
            object value
            )
        {
            if (value == null)
                return DisplayNull;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a display placeholder when the specified value
        /// is null or an empty string.
        /// </summary>
        /// <param name="value">
        /// The value to be checked; may be null.
        /// </param>
        /// <returns>
        /// The original value, or a display placeholder when it is null or an
        /// empty string.
        /// </returns>
        public static object MaybeNullOrEmpty(
            object value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value is string)
            {
                int length = ((string)value).Length;

                if (length == 0)
                    return DisplayEmpty;
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a nullable millisecond count as a string.
        /// </summary>
        /// <param name="value">
        /// The number of milliseconds to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The formatted millisecond string, or a display placeholder when the
        /// value is null.
        /// </returns>
        public static string MaybeMilliseconds(
            long? value
            )
        {
            if (value == null)
                return DisplayNull;

            return String.Format("{0} milliseconds", value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the short-name portion of a release attribute
        /// string, falling back to the wrapped whole value when no short name
        /// can be found.
        /// </summary>
        /// <param name="value">
        /// The release attribute string to be processed; may be null or empty.
        /// </param>
        /// <returns>
        /// The extracted short name, or the wrapped original value when no
        /// short name is present.
        /// </returns>
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
                //       "<Short_Description>(\n|.|,) <Type> XY.Z"
                //
                //       Where "Short_Description" is something like
                //       "Namespaces Edition", "Type" is one of
                //       ["Alpha", "Beta", "Final", "Release"] and
                //       "XY.Z" is a number.  Together, the "Type"
                //       and "XY.Z" portions are considered to act
                //       as sort of a "Short_Name".
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

        /// <summary>
        /// This method formats a nullable number of seconds as a string,
        /// rounded to one decimal place.
        /// </summary>
        /// <param name="seconds">
        /// The number of seconds to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The formatted seconds string, or a display placeholder when the
        /// value is null.
        /// </returns>
        public static string SecondsOrNull(
            double? seconds
            )
        {
            if (seconds == null)
                return DisplayNull;

            double localSeconds = (double)seconds;

            return String.Format(
                "{0:0.0} seconds", Math.Round(localSeconds, 1));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        /// <summary>
        /// This method returns a human-readable phrase describing whether
        /// something exists.
        /// </summary>
        /// <param name="exists">
        /// Non-zero if the subject exists.
        /// </param>
        /// <returns>
        /// A phrase indicating existence or non-existence.
        /// </returns>
        public static string Exists(
            bool exists
            )
        {
            return exists ?
                "already exists" : "does not exist";
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an array of buffer statistics into a
        /// comma-separated description string.
        /// </summary>
        /// <param name="statistics">
        /// The array of buffer statistics to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The formatted buffer statistics string, or a display placeholder
        /// when the array is null or empty.
        /// </returns>
        public static string TheBufferStats(
            int[] statistics
            )
        {
            if (statistics == null)
                return DisplayNull;

            StringBuilder builder = StringBuilderFactory.Create();
            int length = statistics.Length;

            if (length > (int)BufferStats.Length)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                builder.AppendFormat("length = {0}",
                    statistics[(int)BufferStats.Length]);
            }

            if (length > (int)BufferStats.CrCount)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                builder.AppendFormat("crCount = {0}",
                    statistics[(int)BufferStats.CrCount]);
            }

            if (length > (int)BufferStats.LfCount)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                builder.AppendFormat("lfCount = {0}",
                    statistics[(int)BufferStats.LfCount]);
            }

            if (length > (int)BufferStats.CrLfCount)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                builder.AppendFormat("crLfCount = {0}",
                    statistics[(int)BufferStats.CrLfCount]);
            }

            if (length > (int)BufferStats.LineCount)
            {
                if (builder.Length > 0)
                {
                    builder.Append(Characters.Comma);
                    builder.Append(Characters.Space);
                }

                builder.AppendFormat("lineCount = {0}",
                    statistics[(int)BufferStats.LineCount]);
            }

            if (builder.Length == 0)
            {
                StringBuilderCache.Release(ref builder);
                return DisplayEmpty;
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a display placeholder indicating whether the
        /// specified value is null.
        /// </summary>
        /// <param name="value">
        /// The value to be checked; may be null.
        /// </param>
        /// <returns>
        /// A display placeholder indicating whether the value is null.
        /// </returns>
        public static string NullOrNotNull(
            object value
            )
        {
            return value != null ? DisplayNotNull : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name of the specified identifier object.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier object whose name is to be formatted; may be null.
        /// </param>
        /// <returns>
        /// The wrapped identifier name, or a display placeholder when the
        /// object is null.
        /// </returns>
        public static string IdentifierName(
            IIdentifierName identifierName
            )
        {
            if (identifierName == null)
                return DisplayNull;

            return WrapOrNull(identifierName.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the type, hash code, and wrapped value of the
        /// specified object into a descriptive string.
        /// </summary>
        /// <param name="value">
        /// The value to be described; may be null.
        /// </param>
        /// <returns>
        /// The descriptive string for the value.
        /// </returns>
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
        /// <summary>
        /// This method wraps the specified array of bytes.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to be wrapped; may be null.
        /// </param>
        /// <returns>
        /// The wrapped string representation of the bytes, or a display
        /// placeholder when the array is null.
        /// </returns>
        public static string WrapOrNull(
            byte[] bytes
            )
        {
            return WrapOrNull(bytes, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified array of
        /// bytes, optionally including only its length.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes to format, or null.
        /// </param>
        /// <param name="lengthOnly">
        /// When non-zero, only the length of the array is included in the
        /// resulting string; otherwise, both the length and the Base64 form
        /// of the bytes are included.
        /// </param>
        /// <returns>
        /// The formatted display string, or a placeholder when the array of
        /// bytes is null.
        /// </returns>
        public static string WrapOrNull(
            byte[] bytes,
            bool lengthOnly
            )
        {
            if (bytes == null)
                return DisplayNull;

            if (lengthOnly)
            {
                return Parser.Quote(StringList.MakeList(
                    "Length", bytes.Length));
            }
            else
            {
                return Parser.Quote(StringList.MakeList(
                    "Length", bytes.Length, "Base64",
                    (bytes.Length > 0) ?
                        Convert.ToBase64String(bytes) :
                        DisplayEmpty));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified collection
        /// of strings, wrapping the value when it is not null.
        /// </summary>
        /// <param name="value">
        /// The collection of strings to format, or null.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
        public static string WrapOrNull(
            IEnumerable<string> value
            )
        {
            return WrapOrNull((value != null), value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified object,
        /// wrapping the value when it is not null.
        /// </summary>
        /// <param name="value">
        /// The object to format, or null.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
        public static string WrapOrNull(
            object value
            )
        {
            return WrapOrNull((value != null), value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified collection
        /// of strings, optionally wrapping the value.
        /// </summary>
        /// <param name="wrap">
        /// When non-zero, the value is wrapped using the configured prefix and
        /// suffix; otherwise, a placeholder is returned.
        /// </param>
        /// <param name="value">
        /// The collection of strings to format, or null.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
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

        /// <summary>
        /// This method produces a display string for the specified object,
        /// optionally wrapping the value.
        /// </summary>
        /// <param name="wrap">
        /// When non-zero, the value is wrapped using the configured prefix and
        /// suffix; otherwise, a placeholder is returned.
        /// </param>
        /// <param name="value">
        /// The object to format, or null.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
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

        /// <summary>
        /// This method produces a display string for the specified value,
        /// optionally normalizing whitespace, applying an ellipsis, and using
        /// placeholders for null or empty values.
        /// </summary>
        /// <param name="wrap">
        /// When non-zero, the value is wrapped using the supplied prefix and
        /// suffix; otherwise, a placeholder is returned.
        /// </param>
        /// <param name="normalize">
        /// When non-zero, whitespace within the value is normalized prior to
        /// formatting.
        /// </param>
        /// <param name="ellipsis">
        /// When non-zero, the value is truncated with an ellipsis when it
        /// exceeds the configured limit.
        /// </param>
        /// <param name="display">
        /// When non-zero, a placeholder is substituted for a null or empty
        /// value.
        /// </param>
        /// <param name="prefix">
        /// The prefix to prepend to the formatted value.
        /// </param>
        /// <param name="value">
        /// The value to format, or null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to the formatted value.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
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

                    return String.Format(DisplayErrorFormat0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a hexadecimal display string for the specified
        /// hash value.
        /// </summary>
        /// <param name="hashValue">
        /// The array of bytes representing the hash value, or null.
        /// </param>
        /// <returns>
        /// The hexadecimal string, prefixed with <c>0x</c>, or null when the
        /// hash value cannot be converted.
        /// </returns>
        public static string HashValue(
            byte[] hashValue
            )
        {
            string hashString = ArrayOps.ToHexadecimalString(
                hashValue);

            if (hashString == null)
                return null;

            hashString = hashString.ToLowerInvariant();
            return String.Format("0x{0}", hashString);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        /// <summary>
        /// This method determines whether the specified array of cache counts
        /// contains any non-zero values.
        /// </summary>
        /// <param name="counts">
        /// The array of cache counts to examine, or null.
        /// </param>
        /// <returns>
        /// True if the array is present and contains at least one non-zero
        /// count; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method produces a display string of the cache counts contained
        /// in the specified array.
        /// </summary>
        /// <param name="counts">
        /// The array of cache counts to format, or null.
        /// </param>
        /// <param name="empty">
        /// When non-zero, all counts are included even when their value is
        /// zero; otherwise, only non-zero counts are included.
        /// </param>
        /// <returns>
        /// The formatted display string, or null when the array of counts is
        /// missing or too small.
        /// </returns>
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
                long noRemove = counts[(int)CacheCountType.NoRemove];
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

                if (empty || (noRemove > 0))
                    list.Add("noRemove", noRemove.ToString());

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

        /// <summary>
        /// This method produces a display string containing the sum of the
        /// values in the specified collection of key/value pairs.
        /// </summary>
        /// <param name="collection">
        /// The collection of key/value pairs whose values are summed, or null.
        /// </param>
        /// <returns>
        /// The string form of the summed value, or a placeholder when the
        /// collection is null.
        /// </returns>
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

        /// <summary>
        /// This method appends a textual dump of the specified collection of
        /// key/value pairs to the supplied string builder.
        /// </summary>
        /// <param name="collection">
        /// The collection of key/value pairs to dump, or null.
        /// </param>
        /// <param name="builder">
        /// The string builder to which the dump is appended, or null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used to hash each key when raw
        /// output is not requested.
        /// </param>
        /// <param name="raw">
        /// When non-zero, each key is emitted verbatim; otherwise, each key is
        /// emitted as a hexadecimal hash.
        /// </param>
        public static void DumpDictionary(
            IEnumerable<KeyValuePair<string, int>> collection,
            StringBuilder builder,
            string hashAlgorithmName,
            bool raw
            )
        {
            if ((collection == null) || (builder == null))
                return;

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
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string of the keys and values
        /// contained in the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys and values are formatted, or null.
        /// </param>
        /// <param name="display">
        /// When non-zero, the resulting string is wrapped for display
        /// purposes.
        /// </param>
        /// <param name="normalize">
        /// When non-zero, whitespace within the value is normalized prior to
        /// formatting.
        /// </param>
        /// <param name="ellipsis">
        /// When non-zero, the value is truncated with an ellipsis when it
        /// exceeds the configured limit.
        /// </param>
        /// <returns>
        /// The formatted display string.
        /// </returns>
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

        /// <summary>
        /// This method produces a display string of the keys and values
        /// contained in the specified name/value collection.
        /// </summary>
        /// <param name="collection">
        /// The name/value collection to format, or null.
        /// </param>
        /// <param name="display">
        /// When non-zero, a placeholder is substituted for a null or empty
        /// collection; otherwise, null is returned in those cases.
        /// </param>
        /// <returns>
        /// The formatted display string, or null when the collection is null
        /// or empty and display formatting was not requested.
        /// </returns>
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

        /// <summary>
        /// This method normalizes the specified result for use in a complaint,
        /// substituting placeholders for null, empty, or logically empty
        /// values.
        /// </summary>
        /// <param name="result">
        /// The result to normalize, or null.
        /// </param>
        /// <returns>
        /// The normalized result suitable for use in a complaint.
        /// </returns>
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

        /// <summary>
        /// This method produces a formatted complaint message from the
        /// specified identifier, return code, result, and stack trace.
        /// </summary>
        /// <param name="id">
        /// The identifier associated with the complaint.
        /// </param>
        /// <param name="code">
        /// The return code associated with the complaint.
        /// </param>
        /// <param name="result">
        /// The result associated with the complaint, or null.
        /// </param>
        /// <param name="stackTrace">
        /// The stack trace associated with the complaint, or null.
        /// </param>
        /// <returns>
        /// The formatted complaint message.
        /// </returns>
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

        /// <summary>
        /// This method produces a display string for the name of the specified
        /// plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose name is formatted, or null.
        /// </param>
        /// <param name="wrap">
        /// When non-zero, the resulting name is wrapped for display purposes.
        /// </param>
        /// <returns>
        /// The formatted plugin name, or a placeholder when the plugin data is
        /// null or the name is unavailable.
        /// </returns>
        public static string PluginName(
            IPluginData pluginData,
            bool wrap
            )
        {
            if (pluginData == null)
                return DisplayUnavailable;

            string name = pluginData.Name;

            if (name == null)
                name = pluginData.ToString();

            if (name != null)
            {
                //
                // HACK: Extract simple plugin name, if any.
                //
                int index = name.IndexOf(Characters.Comma);

                if (index != Index.Invalid)
                    name = name.Substring(0, index);

                if (wrap)
                    name = WrapOrNull(name);
            }
            else
            {
                name = DisplayAnonymous;
            }

            return name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string identifying the specified
        /// client data, based on its hash code.
        /// </summary>
        /// <param name="clientData">
        /// The client data to identify, or null.
        /// </param>
        /// <returns>
        /// The formatted identifier string, or a placeholder when the client
        /// data is null.
        /// </returns>
        private static string ClientDataName(
            IClientData clientData
            )
        {
            if (clientData == null)
                return DisplayNull;

            return String.Format(
                "0x{0:X}", RuntimeOps.GetHashCode(clientData));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a display string for the specified object,
        /// quoting it when necessary and substituting placeholders for null or
        /// empty values.
        /// </summary>
        /// <param name="value">
        /// The object to format, or null.
        /// </param>
        /// <returns>
        /// The formatted, optionally quoted, display string.
        /// </returns>
        private static string MaybeQuote(
            object value
            )
        {
            if (value == null)
                return DisplayNullObject;

            string stringValue = value.ToString();

            if (stringValue == null)
                return DisplayNullString;

            if (stringValue.Length == 0)
                return DisplayEmptyString;

            if (!Parser.NeedsQuoting(stringValue))
                return stringValue;

            if (stringValue.IndexOf(
                    Characters.QuotationMark) != Index.Invalid)
            {
                return Parser.Quote(stringValue,
                    ListElementFlags.DontQuoteHash);
            }
            else
            {
                return String.Format("{0}{1}{0}",
                    Characters.QuotationMark, /* EXEMPT */
                    stringValue);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a formatted command log entry describing a
        /// command invocation and, optionally, its result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the command invocation, or null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data associated with the command invocation, or null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command invocation, or null.
        /// </param>
        /// <param name="arguments">
        /// The arguments of the command invocation, or null.
        /// </param>
        /// <param name="returnCode">
        /// The return code produced by the command invocation, or null when no
        /// result is available.
        /// </param>
        /// <param name="result">
        /// The result produced by the command invocation, or null when no
        /// result is available.
        /// </param>
        /// <param name="indentSpaces">
        /// The number of spaces used to indent multi-line argument or result
        /// values.
        /// </param>
        /// <param name="allowNewLines">
        /// When non-zero, multi-line values are preserved across multiple
        /// lines; otherwise, their whitespace is collapsed onto a single line.
        /// </param>
        /// <param name="entryId">
        /// Upon entry, the identifier for the log entry, or zero to allocate a
        /// new one.  Upon return, this contains the identifier that was used.
        /// </param>
        /// <returns>
        /// The formatted command log entry.
        /// </returns>
        public static string CommandLogEntry(
            Interpreter interpreter,
            IPluginData pluginData,
            IClientData clientData,
            ArgumentList arguments,
            ReturnCode? returnCode,
            Result result,
            int indentSpaces,
            bool allowNewLines,
            ref long entryId
            )
        {
            if (entryId == 0)
                entryId = GlobalState.NextEntryId();

            bool haveResult;

            if ((returnCode != null) || (result != null))
                haveResult = true;
            else
                haveResult = false;

            StringBuilder builder = StringBuilderFactory.Create();

            builder.AppendFormat(
                "{0}{1} #{2}, p:{3}, a:{4}, t:{5}, i:{6}, n:{7}, c:{8}{9}:",
                Characters.OpenBracket,
                haveResult ? "EXIT" : "ENTER",
                entryId, ProcessOps.GetId(),
                AppDomainOps.GetCurrentId(),
                GlobalState.GetCurrentSystemThreadId(),
                InterpreterNoThrow(interpreter, false),
                PluginName(pluginData, false),
                ClientDataName(clientData),
                Characters.CloseBracket);

            StringBuilder builder2; /* REUSED */
            string newLine; /* REUSED */
            string indent; /* REUSED */

            if (arguments != null)
            {
                int count = arguments.Count;

                if (count > 0)
                {
                    builder2 = StringBuilderFactory.Create();

                    for (int index = 0; index < count; index++)
                    {
                        Argument argument = arguments[index];

                        if (index > 0)
                            builder2.Append(Characters.Space);

                        builder2.Append(MaybeQuote(argument));
                    }

                    newLine = null;
                    indent = null;

                    if (StringOps.IsMultiLine(
                            builder2, indentSpaces, ref newLine,
                            ref indent))
                    {
                        if (allowNewLines)
                        {
                            builder2.Append(newLine);
                            builder2.Insert(0, indent);
                            builder2.Insert(0, newLine);
                        }
                        else
                        {
                            StringOps.FixupWhiteSpace(
                                builder2, Characters.Space,
                                WhiteSpaceFlags.LogUse);

                            builder.Append(Characters.Space);
                        }
                    }
                    else
                    {
                        builder.Append(Characters.Space);
                    }

                    builder.Append(
                        StringBuilderCache.GetStringAndRelease(
                        ref builder2));
                }
                else
                {
                    builder.Append(Characters.Space);
                    builder.Append(DisplayEmptyList);
                }
            }
            else
            {
                builder.Append(Characters.Space);
                builder.Append(DisplayNullList);
            }

            if (haveResult)
            {
                builder.AppendFormat(
                    " ==> {0}code {1}, result",
                    Characters.OpenBracket,
                    MaybeNull(returnCode));

                builder2 = StringBuilderFactory.Create();
                builder2.Append(MaybeQuote(result));

                newLine = null;
                indent = null;

                if (StringOps.IsMultiLine(
                        builder2, indentSpaces, ref newLine,
                        ref indent))
                {
                    if (allowNewLines)
                    {
                        builder2.Append(newLine);
                        builder2.Insert(0, indent);
                        builder2.Insert(0, newLine);
                    }
                    else
                    {
                        StringOps.FixupWhiteSpace(
                            builder2, Characters.Space,
                            WhiteSpaceFlags.LogUse);

                        builder.Append(Characters.Space);
                    }
                }
                else
                {
                    builder.Append(Characters.Space);
                }

                builder.Append(
                    StringBuilderCache.GetStringAndRelease(
                    ref builder2));

                builder.Append(Characters.CloseBracket);
            }

            return StringBuilderCache.GetStringAndRelease(
                ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the managed identifier of the specified thread
        /// for display.
        /// </summary>
        /// <param name="thread">
        /// The thread whose managed identifier should be formatted.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The formatted managed thread identifier, or a placeholder string if
        /// the specified thread is null.
        /// </returns>
        public static string ThreadIdOrNull(
            Thread thread
            )
        {
            if (thread == null)
                return DisplayNull;

            return thread.ManagedThreadId.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the managed identifier of the specified thread
        /// for display, catching and converting any exception into a
        /// placeholder string instead of allowing it to propagate.
        /// </summary>
        /// <param name="thread">
        /// The thread whose managed identifier should be formatted.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The formatted managed thread identifier, or a placeholder string if
        /// the specified thread is null or an exception is caught.
        /// </returns>
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

                return String.Format(DisplayErrorFormat0,
                    (type != null) ? type.Name : UnknownTypeName);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the salient properties of the specified thread
        /// into a human-readable string for display purposes.
        /// </summary>
        /// <param name="thread">
        /// The thread whose properties should be formatted.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted thread information, or a placeholder string if the
        /// specified thread is null or an exception is caught.
        /// </returns>
        public static string DisplayThread(
            Thread thread
            )
        {
            if (thread != null)
            {
                try
                {
                    StringBuilder result = StringBuilderFactory.Create();

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
                        "{0}: {1}, ", "IsAlive", ThreadOps.IsAlive(thread));

                    result.AppendFormat(
                        "{0}: {1}, ", "IsBackground", thread.IsBackground);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsThreadPoolThread", thread.IsThreadPoolThread);

                    result.AppendFormat(
                        "{0}: {1}, ", "CurrentCulture", WrapOrNull(thread.CurrentCulture));

                    result.AppendFormat(
                        "{0}: {1}", "CurrentUICulture", WrapOrNull(thread.CurrentUICulture));

                    return StringBuilderCache.GetStringAndRelease(ref result);
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayErrorFormat0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the body of a procedure for display, optionally
        /// prefixing each line with its line number.
        /// </summary>
        /// <param name="body">
        /// The procedure body text to format.  This value may be null or empty.
        /// </param>
        /// <param name="startLine">
        /// The line number to use for the first line of the body.
        /// </param>
        /// <param name="showLines">
        /// Non-zero if each line of the body should be prefixed with its line
        /// number.
        /// </param>
        /// <returns>
        /// The formatted procedure body.
        /// </returns>
        public static string ProcedureBody(
            string body,
            int startLine,
            bool showLines
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

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

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a diagnostic message together with its
        /// originating thread and object identifiers and any associated stack
        /// traces.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread associated with the message.
        /// </param>
        /// <param name="id">
        /// The object identifier associated with the message.
        /// </param>
        /// <param name="message">
        /// The message text to format.  This value may be null.
        /// </param>
        /// <param name="resultStackTrace">
        /// The optional stack trace associated with the result.  This value may
        /// be null or empty.
        /// </param>
        /// <param name="complainStackTrace">
        /// The optional stack trace associated with the complaint.  This value
        /// may be null or empty.
        /// </param>
        /// <returns>
        /// The formatted diagnostic message.
        /// </returns>
        private static string ThreadMessage(
            long threadId,
            long id,
            string message,
            string resultStackTrace,
            string complainStackTrace
            )
        {
            StringBuilder builder = StringBuilderFactory.Create(
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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified byte value as a hexadecimal
        /// string.
        /// </summary>
        /// <param name="value">
        /// The byte value to format.
        /// </param>
        /// <param name="prefix">
        /// Non-zero if the hexadecimal prefix should be included in the result.
        /// </param>
        /// <returns>
        /// The formatted hexadecimal string.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified unsigned short value as a
        /// hexadecimal string.
        /// </summary>
        /// <param name="value">
        /// The unsigned short value to format.
        /// </param>
        /// <param name="prefix">
        /// Non-zero if the hexadecimal prefix should be included in the result.
        /// </param>
        /// <returns>
        /// The formatted hexadecimal string.
        /// </returns>
        public static string Hexadecimal(
            ushort value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(UShortOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified unsigned long value as a
        /// hexadecimal string.
        /// </summary>
        /// <param name="value">
        /// The unsigned long value to format.
        /// </param>
        /// <param name="prefix">
        /// Non-zero if the hexadecimal prefix should be included in the result.
        /// </param>
        /// <returns>
        /// The formatted hexadecimal string.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified unsigned long value as a compact
        /// hexadecimal string.
        /// </summary>
        /// <param name="value">
        /// The unsigned long value to format.
        /// </param>
        /// <param name="prefix">
        /// Non-zero if the hexadecimal prefix should be included in the result.
        /// </param>
        /// <returns>
        /// The formatted compact hexadecimal string.
        /// </returns>
        private static string CompactHexadecimal(
            ulong value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(CompactOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified value type instance as a
        /// hexadecimal string.
        /// </summary>
        /// <param name="value">
        /// The value type instance to format.
        /// </param>
        /// <param name="prefix">
        /// Non-zero if the hexadecimal prefix should be included in the result.
        /// </param>
        /// <returns>
        /// The formatted hexadecimal string.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified unsigned long value as a
        /// hexavigesimal (base-26) string, optionally padded to a minimum
        /// width.
        /// </summary>
        /// <param name="value">
        /// The unsigned long value to format.
        /// </param>
        /// <param name="width">
        /// The minimum width of the result; if the formatted value is shorter
        /// than this width, it is padded on the left.
        /// </param>
        /// <returns>
        /// The formatted hexavigesimal string.
        /// </returns>
        public static string Hexavigesimal(
            ulong value,
            byte width
            )
        {
            StringBuilder result = StringBuilderFactory.CreateNoCache(); /* EXEMPT */

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
                result = StringBuilderFactory.CreateNoCache(
                    StringOps.StrReverse(result.ToString())); /* EXEMPT */
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
            {
                result.Insert(0, StringOps.StrRepeat(
                    width - result.Length, HexavigesimalAlphabet[0]));
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK && WEB
        /// <summary>
        /// This method formats the length, in bytes, of the specified byte
        /// array for display.
        /// </summary>
        /// <param name="bytes">
        /// The byte array whose length should be formatted.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted byte length, or a placeholder string if the specified
        /// array is null or empty.
        /// </returns>
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
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the length, in characters, of the specified
        /// string for display.
        /// </summary>
        /// <param name="text">
        /// The string whose length should be formatted.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted character length, or a placeholder string if the
        /// specified string is null or empty.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time using the package
        /// date/time format, optionally including a build-seconds component.
        /// </summary>
        /// <param name="value">
        /// The date and time value to format.
        /// </param>
        /// <returns>
        /// The formatted package date and time string.
        /// </returns>
        public static string PackageDateTime(
            DateTime value
            )
        {
            StringBuilder builder = StringBuilderFactory.Create(
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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates Tcl-style clock format specifiers within the
        /// specified format string into their equivalent managed format
        /// specifiers, optionally applying static replacements and/or delegate
        /// based replacements.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture to use when translating the format.  This value may be
        /// null.
        /// </param>
        /// <param name="timeZone">
        /// The time zone to use when translating the format.  This value may be
        /// null.
        /// </param>
        /// <param name="format">
        /// The format string to translate.  This value may be null or empty.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value associated with the translation.
        /// </param>
        /// <param name="epoch">
        /// The epoch date and time value associated with the translation.
        /// </param>
        /// <param name="useFormats">
        /// Non-zero if static format replacements should be applied.
        /// </param>
        /// <param name="useDelegates">
        /// Non-zero if delegate based format replacements should be applied.
        /// </param>
        /// <returns>
        /// The translated format string.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified date and time using a Tcl-style
        /// clock format string, translating it into the equivalent managed
        /// format before applying it.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture to use when formatting.  This value may be null.
        /// </param>
        /// <param name="timeZone">
        /// The time zone to use when formatting.  This value may be null.
        /// </param>
        /// <param name="format">
        /// The Tcl-style clock format string.  This value may be null or empty.
        /// </param>
        /// <param name="dateTime">
        /// The date and time value to format.
        /// </param>
        /// <param name="epoch">
        /// The epoch date and time value associated with the formatting.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
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
        /// <summary>
        /// This method normalizes the line endings within the specified string.
        /// It is retained mainly for cosmetic purposes, as it is only used when
        /// displaying strings.
        /// </summary>
        /// <param name="value">
        /// The string whose line endings should be normalized.  This value may
        /// be null.
        /// </param>
        /// <returns>
        /// The string with normalized line endings.
        /// </returns>
        public static string NormalizeNewLines(
            string value
            )
        {
            return StringOps.NormalizeLineEndings(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the line endings within the specified string
        /// with their display equivalents.
        /// </summary>
        /// <param name="value">
        /// The string whose line endings should be replaced.  This value may be
        /// null or empty.
        /// </param>
        /// <returns>
        /// The string with its line endings replaced, or the original value if
        /// it is null or empty.
        /// </returns>
        public static string ReplaceNewLines(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = StringBuilderFactory.Create(value);

            StringOps.FixupDisplayLineEndings(
                builder, extendedLineEndings, unicodeLineEndings);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the salient properties of the specified result
        /// into a human-readable string for display purposes.
        /// </summary>
        /// <param name="value">
        /// The result to format.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted result information, or a placeholder string if the
        /// specified result is null.
        /// </returns>
        public static string DisplayEngineResult(
            Result value
            )
        {
            if (value != null)
            {
                StringBuilder result = StringBuilderFactory.Create();

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

                return StringBuilderCache.GetStringAndRelease(ref result);
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified result string for display,
        /// optionally truncating it with an ellipsis and replacing its line
        /// endings.
        /// </summary>
        /// <param name="value">
        /// The result string to format.  This value may be null.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero if the result should be truncated with an ellipsis when it
        /// exceeds the configured limit.
        /// </param>
        /// <param name="replaceNewLines">
        /// Non-zero if the line endings within the result should be replaced
        /// with their display equivalents.
        /// </param>
        /// <returns>
        /// The formatted result string, or a placeholder string if the
        /// specified value is null or empty.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified string value for display,
        /// substituting placeholder strings for null, empty, and all-whitespace
        /// values.
        /// </summary>
        /// <param name="value">
        /// The string value to format.  This value may be null.
        /// </param>
        /// <returns>
        /// The original value, or a placeholder string if it is null, empty, or
        /// consists entirely of whitespace.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified result string, optionally
        /// truncating it with an ellipsis and replacing its line endings.
        /// </summary>
        /// <param name="value">
        /// The result string to format.  This value may be null.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero if the result should be truncated with an ellipsis when it
        /// exceeds the configured limit.
        /// </param>
        /// <param name="replaceNewLines">
        /// Non-zero if the line endings within the result should be normalized
        /// and replaced with their display equivalents.
        /// </param>
        /// <returns>
        /// The formatted result string.
        /// </returns>
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

        /// <summary>
        /// This method formats the performance, in microseconds per iteration,
        /// carried by the specified client data for display.
        /// </summary>
        /// <param name="clientData">
        /// The client data carrying the performance information.  This value may
        /// be null.
        /// </param>
        /// <returns>
        /// The formatted performance string, or a placeholder string if the
        /// specified client data does not carry performance information.
        /// </returns>
        public static string PerformanceMicroseconds(
            IClientData clientData
            )
        {
            PerformanceClientData performanceClientData =
                clientData as PerformanceClientData;

            return (performanceClientData != null) ?
                PerformanceMicroseconds(performanceClientData.Microseconds) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified number of microseconds per
        /// iteration for display.
        /// </summary>
        /// <param name="microseconds">
        /// The number of microseconds per iteration.
        /// </param>
        /// <returns>
        /// The formatted performance string.
        /// </returns>
        public static string PerformanceMicroseconds(
            double microseconds
            )
        {
            return PerformanceMicroseconds(microseconds, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified number of microseconds per
        /// iteration for display, including an optional suffix before the units.
        /// </summary>
        /// <param name="microseconds">
        /// The number of microseconds per iteration.
        /// </param>
        /// <param name="suffix">
        /// The optional suffix to include before the units.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted performance string.
        /// </returns>
        public static string PerformanceMicroseconds(
            double microseconds,
            string suffix
            )
        {
            return String.Format(
                "{0:0.####} {1}microseconds per iteration",
                microseconds, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified number of milliseconds for
        /// display.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds.
        /// </param>
        /// <returns>
        /// The formatted millisecond string.
        /// </returns>
        public static string PerformanceMilliseconds(
            double milliseconds
            )
        {
            return String.Format("{0:0.####}", milliseconds);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a human-readable summary of a performance
        /// measurement, including iteration counts, the return code, the
        /// result, and the average, minimum, and maximum timings.
        /// </summary>
        /// <param name="requestedIterations">
        /// The number of iterations that were requested.
        /// </param>
        /// <param name="actualIterations">
        /// The number of iterations that were actually performed.
        /// </param>
        /// <param name="resultIterations">
        /// The number of iterations used when computing the resulting timings.
        /// </param>
        /// <param name="code">
        /// The return code produced by the measured operation.
        /// </param>
        /// <param name="result">
        /// The result produced by the measured operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startCount">
        /// The raw performance counter value captured at the start of the
        /// measurement.
        /// </param>
        /// <param name="stopCount">
        /// The raw performance counter value captured at the end of the
        /// measurement.
        /// </param>
        /// <param name="minimumIterationCount">
        /// The minimum per-iteration count observed, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="maximumIterationCount">
        /// The maximum per-iteration count observed, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="obfuscate">
        /// Non-zero to omit and obfuscate the raw counts and timings so that
        /// they cannot be used to infer absolute performance characteristics.
        /// </param>
        /// <returns>
        /// The formatted, human-readable performance summary string.
        /// </returns>
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
                PerformanceOps.GetMicrosecondsFromCount(
                    startCount, stopCount, resultIterations, false) : 0;

            double minimumMicroseconds = PerformanceOps.GetMicrosecondsFromCount(
                (minimumIterationCount != null) ?
                    (long)minimumIterationCount : 0, 1, false);

            double maximumMicroseconds = PerformanceOps.GetMicrosecondsFromCount(
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

            localResult.Add(PerformanceMicroseconds(averageMicroseconds, "average "));
            localResult.Add(PerformanceMicroseconds(minimumMicroseconds, "minimum "));
            localResult.Add(PerformanceMicroseconds(maximumMicroseconds, "maximum "));

            return localResult.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// This method formats a single command history entry into a
        /// name/value pair suitable for display.
        /// </summary>
        /// <param name="count">
        /// The ordinal number of the history entry being formatted.
        /// </param>
        /// <param name="clientData">
        /// The client data containing the history entry to format.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the formatted value with an ellipsis when it
        /// exceeds the configured history limit.
        /// </param>
        /// <param name="replaceNewLines">
        /// Non-zero to replace any embedded new-line sequences in the formatted
        /// value.
        /// </param>
        /// <returns>
        /// The formatted name/value pair for the history entry, or null if the
        /// supplied client data does not contain a history entry.
        /// </returns>
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

        /// <summary>
        /// This method determines the ellipsis truncation limit, preferring the
        /// value of the associated environment variable when it is present and
        /// valid.
        /// </summary>
        /// <param name="default">
        /// The fallback limit to use when the environment variable is missing
        /// or does not contain a valid integer.
        /// </param>
        /// <returns>
        /// The configured ellipsis limit, or the fallback value.
        /// </returns>
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

        /// <summary>
        /// This method truncates a string using the default ellipsis limit and
        /// ellipsis text.
        /// </summary>
        /// <param name="value">
        /// The string value to truncate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The possibly-truncated string value.
        /// </returns>
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

        /// <summary>
        /// This method truncates a string to the specified limit using the
        /// default ellipsis text.
        /// </summary>
        /// <param name="value">
        /// The string value to truncate.  This parameter may be null.
        /// </param>
        /// <param name="limit">
        /// The maximum length, in characters, of the resulting string.
        /// </param>
        /// <param name="strict">
        /// Non-zero to reserve room for the ellipsis text within the limit so
        /// that the overall result does not exceed it.
        /// </param>
        /// <returns>
        /// The possibly-truncated string value.
        /// </returns>
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

        /// <summary>
        /// This method truncates a sub-range of a string to the specified limit
        /// using the default ellipsis text.
        /// </summary>
        /// <param name="value">
        /// The string value to truncate.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin within the string value.
        /// </param>
        /// <param name="length">
        /// The number of characters, starting at <paramref name="startIndex" />,
        /// to consider.
        /// </param>
        /// <param name="limit">
        /// The maximum length, in characters, of the resulting string.
        /// </param>
        /// <param name="strict">
        /// Non-zero to reserve room for the ellipsis text within the limit so
        /// that the overall result does not exceed it.
        /// </param>
        /// <returns>
        /// The possibly-truncated string value.
        /// </returns>
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

        /// <summary>
        /// This method truncates a sub-range of a string to the specified limit
        /// using the supplied ellipsis text.  This is the core implementation
        /// to which the other overloads delegate.
        /// </summary>
        /// <param name="value">
        /// The string value to truncate.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin within the string value.
        /// </param>
        /// <param name="length">
        /// The number of characters, starting at <paramref name="startIndex" />,
        /// to consider.
        /// </param>
        /// <param name="limit">
        /// The maximum length, in characters, of the resulting string.
        /// </param>
        /// <param name="strict">
        /// Non-zero to reserve room for the ellipsis text within the limit so
        /// that the overall result does not exceed it.
        /// </param>
        /// <param name="ellipsis">
        /// The ellipsis text to append to the truncated string.  This parameter
        /// may be null or empty, in which case no ellipsis is appended.
        /// </param>
        /// <returns>
        /// The possibly-truncated string value.
        /// </returns>
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
        /// <summary>
        /// This method formats the MD5 and SHA1 components of a hash into a
        /// human-readable list.
        /// </summary>
        /// <param name="hash">
        /// The hash whose components are to be formatted.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The formatted hash list, or the null display value when the hash is
        /// null.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the source identifier associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose source identifier is to be retrieved.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The fallback value to return when no source identifier is available.
        /// </param>
        /// <returns>
        /// The source identifier for the assembly, or the fallback value.
        /// </returns>
        public static string SourceId(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceId(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the source time stamp associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose source time stamp is to be retrieved.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The fallback value to return when no source time stamp is available.
        /// </param>
        /// <returns>
        /// The source time stamp for the assembly, or the fallback value.
        /// </returns>
        public static string SourceTimeStamp(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceTimeStamp(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the update base URI associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose update base URI is to be retrieved.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The fallback value to return when no update base URI is available.
        /// </param>
        /// <returns>
        /// The update base URI for the assembly, or the fallback value.
        /// </returns>
        public static string UpdateUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyUpdateBaseUri(assembly);
            return StringOps.GetStringFromObject(uri, @default, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the download base URI associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose download base URI is to be retrieved.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The fallback value to return when no download base URI is available.
        /// </param>
        /// <returns>
        /// The download base URI for the assembly, or the fallback value.
        /// </returns>
        public static string DownloadUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyDownloadBaseUri(assembly);
            return StringOps.GetStringFromObject(uri, @default, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the public key token associated with the
        /// specified assembly name, formatted as a string.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose public key token is to be retrieved.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The fallback value to return when no public key token is available.
        /// </param>
        /// <returns>
        /// The formatted public key token, or the fallback value.
        /// </returns>
        public static string PublicKeyToken(
            AssemblyName assemblyName,
            string @default
            )
        {
            string result = AssemblyOps.GetPublicKeyToken(assemblyName);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a public key token byte array as a hexadecimal
        /// string.
        /// </summary>
        /// <param name="publicKeyToken">
        /// The public key token bytes to format.  This parameter may be null
        /// or empty.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the public key token, the null
        /// display value when it is null, or the empty display value when it
        /// is empty.
        /// </returns>
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

        /// <summary>
        /// This method builds a fully-qualified type name from its namespace,
        /// type name, and (optionally) its assembly, producing an
        /// assembly-qualified name when appropriate.
        /// </summary>
        /// <param name="namespaceName">
        /// The namespace to prepend to the type name when it is not already
        /// present.  This parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The type name to qualify.  This parameter may be null or empty, in
        /// which case it is returned unchanged.
        /// </param>
        /// <param name="assembly">
        /// The assembly used to assembly-qualify the type name when it is not
        /// already assembly-qualified.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The qualified type name.
        /// </returns>
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

        /// <summary>
        /// This method formats the strong name information for an assembly,
        /// including its name, version, public key token, verification status,
        /// and any strong name tag.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose strong name information is to be formatted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="strongName">
        /// The strong name object associated with the assembly.  This parameter
        /// may be null.
        /// </param>
        /// <param name="verified">
        /// Non-zero to include the actual strong name verification status of the
        /// assembly in the formatted output.
        /// </param>
        /// <returns>
        /// The formatted strong name information, or an empty string when it
        /// cannot be determined.
        /// </returns>
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
                            _TracePriority.SecurityError);
                    }
                }
            }

            return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the certificate information for an assembly,
        /// optionally including its trust status.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose location is used when evaluating file trust.
        /// This parameter may be null.
        /// </param>
        /// <param name="certificate">
        /// The certificate whose information is to be formatted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to evaluate and include the actual file trust status.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include verbose certificate details in the output.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the formatted result, handling the null case.
        /// </param>
        /// <returns>
        /// The formatted certificate information.
        /// </returns>
        public static string Certificate(
            Interpreter interpreter,
            Assembly assembly,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            return Certificate(interpreter,
                (assembly != null) ? assembly.Location : null,
                certificate, trusted, verbose, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the certificate information for a certificate.
        /// </summary>
        /// <param name="certificate">
        /// The certificate whose information is to be formatted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include verbose certificate details in the output.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the formatted result, handling the null case.
        /// </param>
        /// <returns>
        /// The formatted certificate information.
        /// </returns>
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

        /// <summary>
        /// This method adds the file trust status to the supplied list when
        /// both the list and file name are available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list to which the trust status is added.  This parameter may be
        /// null, in which case nothing is added.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose trust status is evaluated.  This
        /// parameter may be null, in which case nothing is added.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to evaluate the actual file trust status; otherwise, false
        /// is recorded.
        /// </param>
        private static void MaybeAddFileTrusted(
            Interpreter interpreter,
            StringList list,
            string fileName,
            bool trusted
            )
        {
            if ((list != null) && (fileName != null))
            {
                list.Add("trusted", trusted ?
                    RuntimeOps.IsFileTrusted(
                        interpreter, null, fileName,
                        IntPtr.Zero).ToString() :
                    false.ToString());
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the certificate information for a file,
        /// optionally including its trust status.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="certificate">
        /// The certificate whose information is to be formatted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to evaluate and include the actual file trust status.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include verbose certificate details in the output.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the formatted result, handling the null case.
        /// </param>
        /// <returns>
        /// The formatted certificate information.
        /// </returns>
        public static string Certificate(
            Interpreter interpreter,
            string fileName,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            StringList list = RuntimeOps.CertificateToList(
                certificate, verbose);

            MaybeAddFileTrusted(
                interpreter, list, fileName, trusted);

            string result = StringOps.GetStringFromObject(list);

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the script file extension should be
        /// appended to the specified path value.
        /// </summary>
        /// <param name="value">
        /// The path value to examine.  This parameter may be null or empty, in
        /// which case no extension is needed.
        /// </param>
        /// <param name="extension">
        /// Upon success, receives the script file extension that should be
        /// appended.  This value is only meaningful when this method returns
        /// true.
        /// </param>
        /// <returns>
        /// True if the script file extension should be appended; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method builds the file name (optionally including a library
        /// path fragment) associated with the specified script type.
        /// </summary>
        /// <param name="scriptType">
        /// The script type, which may also be a file name, to convert.
        /// </param>
        /// <param name="packageType">
        /// The type of package the script belongs to, used to select the
        /// library path fragment.
        /// </param>
        /// <param name="fileNameOnly">
        /// Non-zero to return only the file name, omitting any library path
        /// fragment.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return null when the script type is null or an empty
        /// string; otherwise, the original value is returned in that case.
        /// </param>
        /// <returns>
        /// The resulting file name, or null upon failure.
        /// </returns>
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
                        case PackageType.Loader:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.LoaderPackage,
                                    result));

                                break;
                            }
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
                        case PackageType.Kit:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.KitPackage,
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

        /// <summary>
        /// This method returns a display-friendly name for the specified
        /// culture, accounting for the empty name used by the invariant
        /// culture.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture for which a name is needed.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to prefer the culture's display name.
        /// </param>
        /// <returns>
        /// The name of the culture, or null if no culture was specified.
        /// </returns>
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

        /// <summary>
        /// This method formats a diagnostic message used when a break or
        /// failure condition is encountered.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method associated with the break or failure.
        /// </param>
        /// <param name="strings">
        /// The additional strings to include in the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted diagnostic message.
        /// </returns>
        public static string BreakOrFail(
            string methodName,
            params string[] strings
            )
        {
            return String.Format("{0}: {1}", methodName,
                (strings != null) ? StringList.MakeList(strings) : DisplayNull);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a numeric time zone offset string from a total
        /// number of seconds.
        /// </summary>
        /// <param name="totalSeconds">
        /// The time zone offset, in seconds, to format.
        /// </param>
        /// <returns>
        /// The formatted numeric time zone offset.
        /// </returns>
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
            StringBuilder result = StringBuilderFactory.Create();

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

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps a numeric value to one of the words <c>never</c>,
        /// <c>always</c>, or <c>sometimes</c>.
        /// </summary>
        /// <param name="value">
        /// The value to map: negative yields <c>never</c>, zero yields
        /// <c>always</c>, and positive yields <c>sometimes</c>.
        /// </param>
        /// <returns>
        /// The word corresponding to the specified value.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified date and time using the ISO-8601
        /// update format.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
        public static string Iso8601UpdateDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601UpdateDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time using one of the
        /// trace date and time formats.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.
        /// </param>
        /// <param name="interactive">
        /// Non-zero to use the interactive trace format; otherwise, the
        /// standard trace format is used.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
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

        /// <summary>
        /// This method constructs a UTC date and time from the specified number
        /// of ticks, returning null if the value is out of range.
        /// </summary>
        /// <param name="ticks">
        /// The number of ticks representing the date and time.
        /// </param>
        /// <returns>
        /// The constructed UTC date and time, or null upon failure.
        /// </returns>
        public static DateTime? UtcOrNull(
            long ticks
            )
        {
            try
            {
                return new DateTime(
                    ticks, DateTimeKind.Utc); /* throw */
            }
            catch
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified nullable date and time using the
        /// ISO-8601 full date and time format.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted date and time string, or the null display value if no
        /// value was specified.
        /// </returns>
        public static string Iso8601FullDateTime(
            DateTime? value
            )
        {
            if (value == null)
                return DisplayNull;

            return Iso8601FullDateTime((DateTime)value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time using the ISO-8601
        /// full date and time format.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
        public static string Iso8601FullDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601FullDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// This method formats the specified nullable date and time using the
        /// ISO-8601 date and time format with seconds precision.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted date and time string, or the null display value if no
        /// value was specified.
        /// </returns>
        public static string Iso8601DateTimeSeconds(
            DateTime? value
            )
        {
            if (value == null)
                return DisplayNull;

            return Iso8601DateTimeSeconds((DateTime)value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time using the ISO-8601
        /// date and time format with seconds precision.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
        public static string Iso8601DateTimeSeconds(
            DateTime value
            )
        {
            return value.ToString(Iso8601DateTimeSecondsFormat);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified nullable date and time using the
        /// ISO-8601 date and time format, without a time zone offset.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
        public static string Iso8601DateTime(
            DateTime? value
            )
        {
            return Iso8601DateTime(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified nullable date and time using the
        /// ISO-8601 date and time format, optionally including a time zone
        /// offset.
        /// </summary>
        /// <param name="value">
        /// The date and time to format.  This parameter may be null, in which
        /// case the minimum date and time value is used.
        /// </param>
        /// <param name="timeZone">
        /// Non-zero to append the numeric time zone offset for local or UTC
        /// values.
        /// </param>
        /// <returns>
        /// The formatted date and time string.
        /// </returns>
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

        /// <summary>
        /// This method builds the setting key associated with the specified
        /// identifier name and variable index.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier providing the variable name.  This parameter may be
        /// null.
        /// </param>
        /// <param name="arrayValue">
        /// The array element dictionary associated with the variable, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The resulting setting key.  This method cannot return null.
        /// </returns>
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

        /// <summary>
        /// This method formats the error display name for the specified
        /// variable.
        /// </summary>
        /// <param name="variable">
        /// The variable associated with the error.  This parameter may be null.
        /// </param>
        /// <param name="linkIndex">
        /// The link index associated with the variable, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted error variable name.
        /// </returns>
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

        /// <summary>
        /// This method formats the error display name for the variable with the
        /// specified name.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <returns>
        /// The formatted error variable name.
        /// </returns>
        public static string ErrorVariableName(
            string varName
            )
        {
            return ErrorVariableName(varName, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the error display name for the variable with the
        /// specified name and array element index.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted error variable name.
        /// </returns>
        public static string ErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return SomeKindOfPrefixAndSuffix(
                VariableName(varName, varIndex));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wraps the specified value with the configured prefix and
        /// suffix.
        /// </summary>
        /// <param name="value">
        /// The value to wrap.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The wrapped value, or the null display value if no value was
        /// specified.  This method cannot return null.
        /// </returns>
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

        /// <summary>
        /// This method wraps the contents of the specified string builder with
        /// the configured prefix and suffix, in place.
        /// </summary>
        /// <param name="builder">
        /// The string builder whose contents are wrapped.  This parameter may
        /// be null.
        /// </param>
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

        /// <summary>
        /// This method formats the error message used when an array element
        /// does not exist.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type describing the operation being attempted.
        /// </param>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method formats the error message used when a variable is, or is
        /// not, an array as required.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type describing the operation being attempted.
        /// </param>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="isArray">
        /// Non-zero if the variable is an array; otherwise, zero.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method formats the error message used when a variable does not
        /// exist.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type describing the operation being attempted.
        /// </param>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="suffix">
        /// An additional suffix to append to the error message.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method formats the error message used when the values for a
        /// variable are unavailable.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type describing the operation being attempted.
        /// </param>
        /// <param name="varName">
        /// The name of the variable.
        /// </param>
        /// <param name="suffix">
        /// An additional suffix to append to the error message.
        /// </param>
        /// <returns>
        /// The formatted error message.
        /// </returns>
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

        /// <summary>
        /// This method builds the display name for the specified variable,
        /// including the array element index when present.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable.  This parameter may be null.
        /// </param>
        /// <param name="varIndex">
        /// The array element index, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted variable name, or null if no variable name was
        /// specified together with an array element index.
        /// </returns>
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

        /// <summary>
        /// This method returns the verb associated with the specified
        /// breakpoint type, using the empty string as the default.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type to translate into a verb.
        /// </param>
        /// <returns>
        /// The verb associated with the breakpoint type.
        /// </returns>
        public static string Breakpoint(
            BreakpointType breakpointType
            )
        {
            return Breakpoint(breakpointType, String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the verb associated with the specified
        /// breakpoint type, using the supplied default when there is no match.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type to translate into a verb.
        /// </param>
        /// <param name="default">
        /// The default verb to return when the breakpoint type does not map to
        /// a known verb.
        /// </param>
        /// <returns>
        /// The verb associated with the breakpoint type, or the default value.
        /// </returns>
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

        /// <summary>
        /// This method builds the fully qualified type name for the function
        /// with the specified name.
        /// </summary>
        /// <param name="name">
        /// The simple name of the function.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name with the configured prefix and
        /// suffix.
        /// </param>
        /// <returns>
        /// The fully qualified function type name.
        /// </returns>
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

        /// <summary>
        /// This method builds the fully qualified type name for the operator
        /// with the specified name.
        /// </summary>
        /// <param name="name">
        /// The simple name of the operator.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name with the configured prefix and
        /// suffix.
        /// </param>
        /// <returns>
        /// The fully qualified operator type name.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified byte array for display as a string
        /// of hexadecimal pairs.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted byte array, or the null or empty display value as
        /// appropriate.
        /// </returns>
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

        /// <summary>
        /// This method computes a hash of the specified bytes and formats it
        /// for display.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to hash.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted hash value, or an error display value upon failure.
        /// </returns>
        public static string HashBytes(
            byte[] bytes
            )
        {
            Result error = null; /* NOT USED */

            byte[] hashValue = HashOps.HashBytes(null, bytes, ref error);

            return (hashValue != null) ? Hash(hashValue) : DisplayErrorFormat0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified hash bytes as an unbroken string
        /// of hexadecimal characters.
        /// </summary>
        /// <param name="bytes">
        /// The hash bytes to format.
        /// </param>
        /// <returns>
        /// The formatted hash string.
        /// </returns>
        public static string Hash(
            byte[] bytes
            )
        {
            return BitConverter.ToString(bytes).Replace(
                Characters.MinusSign.ToString(), String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a call frame level, optionally marking it as
        /// absolute with a leading number sign.
        /// </summary>
        /// <param name="absolute">
        /// Non-zero to prefix the level with a number sign indicating an
        /// absolute level.
        /// </param>
        /// <param name="level">
        /// The level value to format.
        /// </param>
        /// <returns>
        /// The formatted level string.
        /// </returns>
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
        /// <summary>
        /// This method formats the cache flags dictionary as a list of enabled
        /// and disabled entries.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary mapping cache flags to their counts.  This parameter
        /// may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return display values for the null and empty cases.
        /// </param>
        /// <returns>
        /// The formatted list of cache flag entries, or null or a display value
        /// as appropriate.
        /// </returns>
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

        /// <summary>
        /// This method formats the specified namespace for display.
        /// </summary>
        /// <param name="namespace">
        /// The namespace to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted namespace, or the null display value if no namespace
        /// was specified.
        /// </returns>
        public static string DisplayNamespace(
            INamespace @namespace
            )
        {
            if (@namespace == null)
                return DisplayNull;

            return DisplayValue(@namespace.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified collection of
        /// script locations.
        /// </summary>
        /// <param name="scriptLocations">
        /// The collection of script locations to display.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The display string for the specified collection of script
        /// locations.
        /// </returns>
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
        /// <summary>
        /// This method builds a display string for the specified collection of
        /// script arguments.
        /// </summary>
        /// <param name="scriptArguments">
        /// The collection of script arguments to display.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The display string for the specified collection of script
        /// arguments.
        /// </returns>
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

        /// <summary>
        /// This method builds a display string for the specified regular
        /// expression.
        /// </summary>
        /// <param name="value">
        /// The regular expression to display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified regular expression.
        /// </returns>
        public static string DisplayString(
            Regex value
            )
        {
            if (value == null)
                return DisplayNull;

            return DisplayString(value.ToString(), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified string value.
        /// </summary>
        /// <param name="value">
        /// The string value to display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified string value.
        /// </returns>
        public static string DisplayString(
            string value
            )
        {
            return DisplayString(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified string value,
        /// optionally wrapping it.
        /// </summary>
        /// <param name="value">
        /// The string value to display.  This parameter may be null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting display string.
        /// </param>
        /// <returns>
        /// The display string for the specified string value.
        /// </returns>
        private static string DisplayString(
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

        /// <summary>
        /// This method builds a display string for the specified string
        /// builder.
        /// </summary>
        /// <param name="value">
        /// The string builder to display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified string builder.
        /// </returns>
        public static string DisplayString(
            StringBuilder value
            )
        {
            return DisplayString(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified string
        /// builder, optionally wrapping it.
        /// </summary>
        /// <param name="value">
        /// The string builder to display.  This parameter may be null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting display string.
        /// </param>
        /// <returns>
        /// The display string for the specified string builder.
        /// </returns>
        private static string DisplayString(
            StringBuilder value,
            bool wrap
            )
        {
            if (value != null)
            {
                if (value.Length > 0)
                    return wrap ? WrapOrNull(value.ToString()) : value.ToString();

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified array of
        /// characters.
        /// </summary>
        /// <param name="value">
        /// The array of characters to display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified array of characters.
        /// </returns>
        public static string DisplayChars(
            char[] value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value.Length == 0)
                return DisplayEmpty;

            StringBuilder builder = StringBuilderFactory.Create();

            builder.Append(value);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the specified width and
        /// height.
        /// </summary>
        /// <param name="width">
        /// The width value to display.
        /// </param>
        /// <param name="height">
        /// The height value to display.
        /// </param>
        /// <returns>
        /// The display string for the specified width and height.
        /// </returns>
        public static string DisplayWidthAndHeight(
            int width,
            int height
            )
        {
            return String.Format(
                "Width={0}, Height={1}", width, height);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the type of the specified
        /// exception.
        /// </summary>
        /// <param name="exception">
        /// The exception whose type is to be displayed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="innermost">
        /// Non-zero to use the innermost (base) exception instead of the
        /// specified exception.
        /// </param>
        /// <returns>
        /// The display string for the type of the specified exception.
        /// </returns>
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

        /// <summary>
        /// This method builds a display string for the keys of the specified
        /// dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are to be displayed.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The display string for the keys of the specified dictionary.
        /// </returns>
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

        /// <summary>
        /// This method builds a display string for the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified list.
        /// </returns>
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

        /// <summary>
        /// This method builds a display string for the specified list by
        /// traversing its elements and formatting each one individually.
        /// </summary>
        /// <param name="list">
        /// The list to traverse and display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display string for the specified list.
        /// </returns>
        public static string DisplayTraverseList(
            IList list
            )
        {
            if (list != null)
            {
                if (list.Count > 0)
                {
                    StringBuilder builder = StringBuilderFactory.Create();

                    foreach (object element in list)
                    {
                        if (builder.Length > 0)
                            builder.Append(Characters.CommaSpaceString);

                        if (element == null)
                        {
                            builder.Append(DisplayNullObject);
                            continue;
                        }

                        string stringValue1;

                        try
                        {
                            stringValue1 = element.ToString();
                        }
                        catch
                        {
                            builder.Append(DisplayToStringError);
                            continue;
                        }

                        if (stringValue1 == null)
                        {
                            builder.Append(DisplayNullString);
                            continue;
                        }

                        if (stringValue1.Length == 0)
                        {
                            builder.Append(DisplayEmptyString);
                            continue;
                        }

                        string prefix = WrapPrefix;
                        string suffix = WrapSuffix;
                        string stringValue2;

                        MaybeChangeWrapPrefixAndSuffix(
                            true, stringValue1, ref prefix,
                            ref suffix, out stringValue2);

                        builder.Append(WrapOrNull(
                            true, false, false, false, prefix,
                            stringValue2, suffix));
                    }

                    if (builder.Length > 0)
                    {
                        builder.Insert(0, Characters.OpenBracket);
                        builder.Append(Characters.CloseBracket);
                    }

                    return StringBuilderCache.GetStringAndRelease(
                        ref builder);
                }

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a combined name and version string from the
        /// specified components.
        /// </summary>
        /// <param name="name">
        /// The name component of the resulting string.  This parameter may be
        /// null.
        /// </param>
        /// <param name="version">
        /// The version component of the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <param name="build">
        /// The build component of the resulting string.  This parameter may be
        /// null.
        /// </param>
        /// <param name="extra">
        /// The extra trailing component of the resulting string.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The combined name and version string.
        /// </returns>
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

        /// <summary>
        /// This method builds a combined name and version string from the
        /// specified components.
        /// </summary>
        /// <param name="name">
        /// The name component of the resulting string.  This parameter may be
        /// null.
        /// </param>
        /// <param name="version">
        /// The version component of the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <param name="build">
        /// The build component of the resulting string.  This parameter may be
        /// null.
        /// </param>
        /// <param name="extra">
        /// The extra trailing component of the resulting string.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The combined name and version string.
        /// </returns>
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

        /// <summary>
        /// This method builds a fixed four-part version string from the
        /// specified version.
        /// </summary>
        /// <param name="version">
        /// The version to format.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display placeholder when the specified version
        /// is null; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The fixed four-part version string for the specified version.
        /// </returns>
        public static string FixedVersion(
            Version version,
            bool display
            )
        {
            if (version == null)
                return display ? DisplayNull : null;

            return FixedVersion(
                version.Major, version.Minor, version.Build,
                version.Revision, display);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a fixed four-part version string from the
        /// specified version components.
        /// </summary>
        /// <param name="major">
        /// The major component of the version.
        /// </param>
        /// <param name="minor">
        /// The minor component of the version.
        /// </param>
        /// <param name="build">
        /// The build component of the version.
        /// </param>
        /// <param name="revision">
        /// The revision component of the version.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-formatted result.
        /// </param>
        /// <returns>
        /// The fixed four-part version string for the specified components.
        /// </returns>
        public static string FixedVersion(
            int major,
            int minor,
            int build,
            int revision,
            bool display
            )
        {
            return String.Format(
                "{0}.{1}.{2}.{3}", major, minor, build,
                revision);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a major and minor version string, prefixed with
        /// the letter "v", for the specified version.
        /// </summary>
        /// <param name="version">
        /// The version to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The prefixed major and minor version string, or a display
        /// placeholder when the specified version is null.
        /// </returns>
        public static string VMajorMinorOrNull(
            Version version
            )
        {
            if (version == null)
                return DisplayNull;

            return MajorMinor(version, "v", null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a major and minor version string for the
        /// specified version.
        /// </summary>
        /// <param name="version">
        /// The version to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The major and minor version string for the specified version, or
        /// the empty string when the specified version is null.
        /// </returns>
        public static string MajorMinor(
            Version version
            )
        {
            return MajorMinor(version, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a major and minor version string for the
        /// specified version, surrounded by the specified prefix and suffix.
        /// </summary>
        /// <param name="version">
        /// The version to format.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to prepend to the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The major and minor version string for the specified version, or
        /// the empty string when the specified version is null.
        /// </returns>
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

        /// <summary>
        /// This method builds a full platform name string describing the
        /// runtime, configuration, platform, process bits, and machine
        /// associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which to build the full platform name.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The full platform name string for the specified assembly.
        /// </returns>
        public static string FullPlatformName(
            Assembly assembly
            )
        {
            StringList list = new StringList();

            string[] values = {
                RuntimeOps.GetAssemblyTextOrSuffix(assembly),
                ShortImageOrRuntimeVersion(assembly, null, null),
                AttributeOps.GetAssemblyConfiguration(assembly),
                PlatformOps.GetPlatformName(),
                String.Format("{0}-bit",
                    PlatformOps.GetProcessBits().ToString()),
                PlatformOps.GetMachineName()
            };

            foreach (string value in values)
            {
                if (value == null)
                {
                    list.Add(NoPlatformName);
                    continue;
                }

                list.Add(value);
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a short image runtime version (or runtime
        /// version) string for the specified assembly, accounting for the
        /// quirks of the Mono and .NET Core runtimes.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose image runtime version is to be formatted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="treatAsMono">
        /// Non-zero to treat the current runtime as Mono, zero to treat it as
        /// not Mono, or null to detect this automatically.
        /// </param>
        /// <param name="treatAsDotNetCore">
        /// Non-zero to treat the current runtime as .NET Core, zero to treat
        /// it as not .NET Core, or null to detect this automatically.
        /// </param>
        /// <returns>
        /// The short image runtime version string for the specified assembly.
        /// </returns>
        public static string ShortImageOrRuntimeVersion(
            Assembly assembly,
            bool? treatAsMono,
            bool? treatAsDotNetCore
            )
        {
            bool localTreatAsMono = (treatAsMono != null) ?
                (bool)treatAsMono : CommonOps.Runtime.IsMono();

            bool localTreatAsDotNetCore = (treatAsDotNetCore != null) ?
                (bool)treatAsDotNetCore : CommonOps.Runtime.IsDotNetCore();

            //
            // HACK: The image runtime version is mostly useless for the
            //       (modern) Mono and/or .NET Core runtimes as it will
            //       (basically) always be set to the value "v4.0.30319"
            //       for backward compatibility with the .NET Framework
            //       4.x.
            //
            string assemblyVersion = AssemblyOps.GetImageRuntimeVersion(
                assembly);

            if (SharedStringOps.SystemEquals(assemblyVersion,
                    CommonOps.Runtime.ImageRuntimeVersion4) &&
                (localTreatAsMono || localTreatAsDotNetCore))
            {
                //
                // NOTE: This is not using the image runtime version,
                //       but the runtime version (see above comment).
                //
                return ShortRuntimeVersion(
                    CommonOps.Runtime.GetRuntimeVersion());
            }
            else
            {
                string runtimeVersion =
                    CommonOps.Runtime.GetImageRuntimeVersion();

                if (SharedStringOps.SystemEquals(assemblyVersion,
                        runtimeVersion))
                {
                    return ShortImageRuntimeVersion(runtimeVersion);
                }
                else if (SharedStringOps.SystemEquals(assemblyVersion,
                        CommonOps.Runtime.ImageRuntimeVersion2) &&
                    SharedStringOps.SystemEquals(runtimeVersion,
                        CommonOps.Runtime.ImageRuntimeVersion4))
                {
                    //
                    // HACK: *SPECIAL* This is the case when a CLRv2
                    //       assembly is running on the CLRv4, which
                    //       is a non-standard usage.
                    //
                    return "CLRv2/4";
                }
                else
                {
                    return ShortImageRuntimeVersion(assemblyVersion);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a short name identifying the kind of runtime
        /// that is currently executing (e.g. .NET, Core, Mono, or CLR).
        /// </summary>
        /// <returns>
        /// The short runtime name for the currently executing runtime.
        /// </returns>
        private static string ShortRuntimeName()
        {
            if (CommonOps.Runtime.IsDotNetCore())
            {
                if (CommonOps.Runtime.IsDotNetCore5xOrHigher())
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

        /// <summary>
        /// This method builds a short runtime version string, combining the
        /// short runtime name with the major version of the specified version.
        /// </summary>
        /// <param name="value">
        /// The runtime version to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The short runtime version string, or null when the specified
        /// version is null.
        /// </returns>
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

        /// <summary>
        /// This method builds a short image runtime version string from the
        /// specified image runtime version value.
        /// </summary>
        /// <param name="value">
        /// The image runtime version value to format.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The short image runtime version string, or null when the specified
        /// value is null or cannot be parsed.
        /// </returns>
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

        /// <summary>
        /// This method combines the specified assembly text, runtime version,
        /// and configuration into a single string, surrounded by the specified
        /// prefix and suffix.
        /// </summary>
        /// <param name="text">
        /// The assembly text component.  This parameter may be null.
        /// </param>
        /// <param name="runtimeVersion">
        /// The runtime version component.  This parameter may be null.
        /// </param>
        /// <param name="configuration">
        /// The configuration component.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to prepend to the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to the resulting string.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The combined string, or the empty string when all of the
        /// components are missing.
        /// </returns>
        public static string AssemblyTextAndConfiguration(
            string text,
            string runtimeVersion,
            string configuration,
            string prefix,
            string suffix
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

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
            {
                StringBuilderCache.Release(ref builder);
                return String.Empty;
            }

            return String.Format("{0}{1}{2}", prefix,
                StringBuilderCache.GetStringAndRelease(
                ref builder), suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified error result and exception into
        /// a single string suitable for display.
        /// </summary>
        /// <param name="error">
        /// The error result component.  This parameter may be null.
        /// </param>
        /// <param name="exception">
        /// The exception component.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The combined error and exception string, or the empty string when
        /// both components are missing.
        /// </returns>
        public static string ErrorWithException(
            Result error,
            Exception exception
            )
        {
            string result;

            if (error != null)
            {
                if (exception != null)
                {
                    result = String.Format(
                        "{0}{1}{1}{2}", error,
                        Environment.NewLine,
                        exception);
                }
                else
                {
                    result = error;
                }
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

        /// <summary>
        /// This method builds a flattened variable name from the specified
        /// array name and index, replacing commas in the index with
        /// underscores.
        /// </summary>
        /// <param name="name">
        /// The array name component.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// The array index component.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The flattened variable name for the specified array name and index.
        /// </returns>
        public static string NestedArrayName(
            string name,
            string index
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (!String.IsNullOrEmpty(name))
            {
                result.Append(name);

                if (!String.IsNullOrEmpty(index))
                {
                    result.Append(Characters.Underscore);
                    result.Append(index.Replace(Characters.Comma, Characters.Underscore));
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the raw name of the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose raw name is to be displayed.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The display string for the raw name of the specified type.
        /// </returns>
        public static string InvokeRawTypeName(
            Type type
            )
        {
            if (type == null)
                return DisplayNull;

            return InvokeRawTypeName(type, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a display string for the raw name of the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the fully qualified type name; otherwise, only
        /// the simple type name is used.
        /// </param>
        /// <returns>
        /// The formatted type name string, or a placeholder string when the
        /// type is null.
        /// </returns>
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

        /// <summary>
        /// This method formats the raw, simple name of the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string RawTypeName(
            Type type
            )
        {
            return TypeName(type, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the raw, simple name of the type of the
        /// specified object instance.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string RawTypeName(
            object @object
            )
        {
            return TypeName(@object, null, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method formats the raw name, optionally fully qualified, of the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        private static string RawTypeNameOrFullName(
            Type type
            )
        {
            return TypeNameOrFullName(type, null, null, false);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the raw name, optionally fully qualified, of
        /// the type of the specified object instance.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string RawTypeNameOrFullName(
            object @object
            )
        {
            return TypeNameOrFullName(@object, null, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// This method formats a value that may be either a type or a string
        /// name for display.
        /// </summary>
        /// <param name="typeOrName">
        /// The value to format.  When it is a type, its type name is used;
        /// when it is a string, the string itself is used.
        /// </param>
        /// <returns>
        /// The formatted display string, or a type mismatch placeholder when
        /// the value is neither a type nor a string.
        /// </returns>
        public static string TypeOrName(
            object typeOrName
            )
        {
            if (typeOrName is Type)
                return TypeName((Type)typeOrName, DisplayNull, true);

            if (typeOrName is string)
                return WrapOrNull((string)typeOrName);

            return DisplayTypeMismatch;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name of the specified type for display.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <returns>
        /// The formatted type name string, or a placeholder string when the
        /// type is null.
        /// </returns>
        public static string TypeName(
            Type type
            )
        {
            return TypeName(type, DisplayNull, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name, optionally fully qualified, of the
        /// specified type for display.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <returns>
        /// The formatted type name string, or a placeholder string when the
        /// type is null.
        /// </returns>
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
        /// <summary>
        /// This method formats the name of the specified type for display,
        /// optionally wrapping the result.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the formatted type name; otherwise, the name is
        /// returned without wrapping.
        /// </param>
        /// <returns>
        /// The formatted type name string, or a placeholder string when the
        /// type is null.
        /// </returns>
        public static string TypeName(
            Type type,
            bool wrap
            )
        {
            return TypeName(type, DisplayNull, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name, optionally fully qualified, of the
        /// specified type for display, optionally wrapping the result.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the formatted type name; otherwise, the name is
        /// returned without wrapping.
        /// </param>
        /// <returns>
        /// The formatted type name string, or a placeholder string when the
        /// type is null.
        /// </returns>
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

        /// <summary>
        /// This method formats the fully qualified name of the specified type
        /// for display, optionally wrapping it.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <param name="default">
        /// The string to return when the type is null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string, or the default string when the
        /// type is null.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of the specified type for display,
        /// optionally using the fully qualified form and optionally wrapping
        /// it.
        /// </summary>
        /// <param name="type">
        /// The type whose name is to be formatted.  May be null.
        /// </param>
        /// <param name="default">
        /// The string to return when the type is null.
        /// </param>
        /// <param name="full">
        /// Non-zero to use the fully qualified type name; otherwise, only the
        /// simple type name is used.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string, or the default string when the
        /// type is null.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of the type of the specified object
        /// instance for display.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string TypeName(
            object @object
            )
        {
            return TypeName(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name, optionally fully qualified, of the
        /// type of the specified object instance for display.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string TypeNameOrFullName(
            object @object
            )
        {
            return TypeNameOrFullName(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name of the type of the specified object
        /// instance for display, optionally wrapping it.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        public static string TypeName(
            object @object,
            bool wrap
            )
        {
            return TypeName(
                @object, DisplayNull, DisplayProxy, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name, optionally fully qualified, of the
        /// type of the specified object instance for display, optionally
        /// wrapping it.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
        private static string TypeNameOrFullName(
            object @object,
            bool wrap
            )
        {
            return TypeNameOrFullName(
                @object, DisplayNull, DisplayProxy, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the name of the type of the specified object
        /// instance for display, accounting for null values and transparent
        /// proxies and optionally wrapping the result.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <param name="nullTypeName">
        /// The string to use when the underlying type cannot be determined.
        /// </param>
        /// <param name="proxyTypeName">
        /// The string to use when the object instance is a transparent proxy.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
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

        /// <summary>
        /// This method formats the name, optionally fully qualified, of the
        /// type of the specified object instance for display, accounting for
        /// null values and transparent proxies and optionally wrapping the
        /// result.
        /// </summary>
        /// <param name="object">
        /// The object instance whose type name is to be formatted.  May be
        /// null.
        /// </param>
        /// <param name="nullTypeName">
        /// The string to use when the underlying type cannot be determined.
        /// </param>
        /// <param name="proxyTypeName">
        /// The string to use when the object instance is a transparent proxy.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting name; otherwise, the name is
        /// returned unwrapped.
        /// </param>
        /// <returns>
        /// The formatted type name string.
        /// </returns>
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
        /// <summary>
        /// This method formats an IP address together with a port number for
        /// display.
        /// </summary>
        /// <param name="address">
        /// The IP address to format.  When null, a wildcard address is used.
        /// </param>
        /// <param name="port">
        /// The port number to format.  When invalid, only the address is
        /// formatted.
        /// </param>
        /// <returns>
        /// The formatted address and port string.
        /// </returns>
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

        /// <summary>
        /// This method formats a network host name or address together with a
        /// port name or number for display.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or address to format.  May be null.
        /// </param>
        /// <param name="portNameOrNumber">
        /// The port name or number to format.  When null, only the host is
        /// formatted.
        /// </param>
        /// <returns>
        /// The formatted host and port string.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of the specified member for display.
        /// </summary>
        /// <param name="memberInfo">
        /// The member whose name is to be formatted.  May be null.
        /// </param>
        /// <returns>
        /// The formatted member name string, or a placeholder string when the
        /// member is null.
        /// </returns>
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
        /// <summary>
        /// This method formats the bridge name that links a Tcl interpreter
        /// with a command name.
        /// </summary>
        /// <param name="interpName">
        /// The name of the Tcl interpreter.
        /// </param>
        /// <param name="commandName">
        /// The name of the command.
        /// </param>
        /// <returns>
        /// The formatted bridge name string.
        /// </returns>
        public static string TclBridgeName(
            string interpName,
            string commandName
            )
        {
            return StringList.MakeList(interpName, commandName);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a package name together with an optional
        /// version for display.
        /// </summary>
        /// <param name="name">
        /// The package name to format.  May be null.
        /// </param>
        /// <param name="version">
        /// The package version to format.  When null, only the name is
        /// formatted.
        /// </param>
        /// <returns>
        /// The formatted package name string.
        /// </returns>
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

        /// <summary>
        /// This method formats the directory name used for a package of the
        /// specified name and version.
        /// </summary>
        /// <param name="name">
        /// The package name.
        /// </param>
        /// <param name="version">
        /// The package version.
        /// </param>
        /// <param name="full">
        /// Non-zero to prepend the standard library directory prefix;
        /// otherwise, only the package-specific portion is used.
        /// </param>
        /// <returns>
        /// The formatted package directory name string.
        /// </returns>
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

        /// <summary>
        /// This method formats the identifier and main module file name of
        /// the specified process for display.
        /// </summary>
        /// <param name="process">
        /// The process to format.  May be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a placeholder string when the process is null;
        /// otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The formatted process string, a placeholder string, or null.
        /// </returns>
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

        /// <summary>
        /// This method formats the managed identifier and name of the
        /// specified thread for display.
        /// </summary>
        /// <param name="thread">
        /// The thread to format.  May be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a placeholder string when the thread is null;
        /// otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The formatted thread string, a placeholder string, or null.
        /// </returns>
        public static string ThreadName(
            Thread thread,
            bool display
            )
        {
            if (thread != null)
            {
                int id = 0;

                try
                {
                    id = thread.ManagedThreadId;
                }
                catch
                {
                    // do nothing.
                }

                string threadName = thread.Name;

                if (!String.IsNullOrEmpty(threadName))
                    return StringList.MakeList(id, threadName);
                else
                    return id.ToString();
            }

            return display ? DisplayUnknown : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a numeric range for display, producing either a
        /// range expression or a single exact value.
        /// </summary>
        /// <param name="lowerBound">
        /// The lower bound of the range.
        /// </param>
        /// <param name="upperBound">
        /// The upper bound of the range.
        /// </param>
        /// <returns>
        /// A range expression when the bounds differ, the single value when
        /// they are equal, or a placeholder string when no value is
        /// available.
        /// </returns>
        public static string BetweenOrExact(
            int lowerBound,
            int upperBound
            )
        {
            if (lowerBound != upperBound)
            {
                return String.Format(
                    "between {0} and {1}",
                    lowerBound, upperBound);
            }
            else if (lowerBound != Index.Invalid)
            {
                return lowerBound.ToString();
            }
            else
            {
                return DisplayNull;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the display name for the specified identifier,
        /// resolving an ensemble sub-command name from the supplied arguments
        /// when applicable.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier whose display name is to be formatted.  May be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to resolve an ensemble sub-command.  May be
        /// null.
        /// </param>
        /// <param name="arguments">
        /// The arguments that may contain the ensemble sub-command name.  May
        /// be null.
        /// </param>
        /// <returns>
        /// The formatted display name string.
        /// </returns>
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

        /// <summary>
        /// This method formats the display name for the specified identifier.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier whose display name is to be formatted.  May be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted display name string, or a placeholder string when
        /// the identifier is null.
        /// </returns>
        public static string DisplayName(
            IIdentifierName identifierName
            )
        {
            return (identifierName != null) ?
                DisplayName(identifierName.Name) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified name for display, quoting it when
        /// necessary.
        /// </summary>
        /// <param name="name">
        /// The name to format.  May be null.
        /// </param>
        /// <returns>
        /// The formatted display name string, or a placeholder string when
        /// the name is null or empty.
        /// </returns>
        public static string DisplayName(
            string name
            )
        {
            if (name == null)
                return DisplayNull;

            if (name.Length == 0)
                return DisplayEmpty;

            if (name.IndexOf(
                    Characters.QuotationMark) != Index.Invalid)
            {
                return Parser.Quote(name,
                    ListElementFlags.DontQuoteHash);
            }
            else
            {
                return String.Format("{0}{1}{0}",
                    Characters.QuotationMark, /* EXEMPT */
                    name);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified file system path for display.
        /// </summary>
        /// <param name="path">
        /// The path to format.  May be null.
        /// </param>
        /// <returns>
        /// The formatted path string, or a placeholder string when the path
        /// is null.
        /// </returns>
        public static string DisplayPath(
            string path
            )
        {
            if (path == null)
                return DisplayNull;

            return WrapOrNull(PathOps.MaybeTrimEnd(path));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the full name, module version identifier,
        /// location, and code base of the specified assembly for display.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to format.  May be null.
        /// </param>
        /// <returns>
        /// The formatted assembly description string, or a placeholder string
        /// when the assembly is null.
        /// </returns>
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
            catch (Exception e) // (NotSupportedException)
            {
                Type type = (e != null) ? e.GetType() : null;

                location = String.Format(DisplayErrorFormat0,
                    (type != null) ? type.Name : UnknownTypeName);
            }

            if (location == null)
                location = DisplayNull;

            string codeBase;

            try
            {
                codeBase = assembly.CodeBase;
            }
            catch (Exception e)
            {
                Type type = (e != null) ? e.GetType() : null;

                codeBase = String.Format(DisplayErrorFormat0,
                    (type != null) ? type.Name : UnknownTypeName);
            }

            if (codeBase == null)
                codeBase = DisplayNull;

            return String.Format(
                "[{0}, {1}, {2}, {3}]", assembly.FullName,
                AssemblyOps.GetModuleVersionId(assembly),
                location, codeBase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified assembly name, optionally
        /// including an identifier and path information, for display.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name to format.  May be null.
        /// </param>
        /// <param name="id">
        /// An identifier to include in the result.  When zero, no identifier
        /// is included.
        /// </param>
        /// <param name="paths">
        /// Non-zero to include path information in the result.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting string; otherwise, it is returned
        /// unwrapped.
        /// </param>
        /// <returns>
        /// The formatted assembly name string, or null when there is no
        /// assembly name and wrapping is not requested.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of the specified assembly, including
        /// its module version identifier and optionally an identifier and path
        /// information, for display.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to format.  May be null.
        /// </param>
        /// <param name="id">
        /// An identifier to include in the result.  When zero, no identifier
        /// is included.
        /// </param>
        /// <param name="paths">
        /// Non-zero to include path information in the result.
        /// </param>
        /// <param name="wrap">
        /// Non-zero to wrap the resulting string; otherwise, it is returned
        /// unwrapped.
        /// </param>
        /// <returns>
        /// The formatted assembly name string, or null when there is no
        /// assembly and wrapping is not requested.
        /// </returns>
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
                    catch // (NotSupportedException)
                    {
                        list.Add((string)null);
                    }

                    try
                    {
                        list.Add(assembly.CodeBase);
                    }
                    catch // (PlatformNotSupportedException)
                    {
                        list.Add((string)null);
                    }
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

        /// <summary>
        /// This method builds a unique event name by combining the specified
        /// prefix and name with information that uniquely identifies the
        /// current process, thread, and application domain, along with an
        /// ever-increasing serial number that is unique within the application
        /// domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose identifier should be included in the event
        /// name, if any.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to include at the start of the event name, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The base name to include in the event name, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id">
        /// An extra identifier to include in the event name.  A value of zero
        /// is omitted.
        /// </param>
        /// <returns>
        /// The constructed unique event name.
        /// </returns>
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
                AppDomainOps.GetCurrentId().ToString(), /* EXEMPT */
                (interpreter != null) ?
                    interpreter.IdNoThrow.ToString() : null,
                Interlocked.Increment(ref nextEventId).ToString(),
                (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the default host associated with the specified
        /// interpreter so that it treats subsequent output as a fatal error
        /// when the specified trace priority includes the fatal flag.  It does
        /// nothing when there is no interpreter, no trace priority, or no
        /// default host.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags to check, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter whose default host should be updated.  This
        /// parameter may be null.
        /// </param>
        private static void MaybeTreatAsFatalError(
            _TracePriority? priority,
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
                    (_TracePriority)priority, _TracePriority.Fatal, true));
            }
            catch
            {
                // do nothing.
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a duplicate method name prefix from the start of
        /// the specified message when the use of real class and method names is
        /// enabled.
        /// </summary>
        /// <param name="methodName">
        /// The fully qualified method name whose final component may appear as
        /// a prefix on the message.  This parameter may be null.
        /// </param>
        /// <param name="method">
        /// Non-zero if real method names are enabled and the prefix should be
        /// considered for removal.
        /// </param>
        /// <param name="message">
        /// The message to modify.  Upon return, any duplicate method name
        /// prefix is removed from the start of this message.
        /// </param>
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

        /// <summary>
        /// This method formats the specified trace priority as a string,
        /// optionally including its non-base flag bits in hexadecimal.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to format.
        /// </param>
        /// <param name="baseOnly">
        /// Non-zero to return only the name of the base trace priority;
        /// otherwise, the remaining flag bits are appended in hexadecimal.
        /// </param>
        /// <param name="shortName">
        /// Non-zero to use the short name of the base trace priority.
        /// </param>
        /// <returns>
        /// The formatted trace priority string.
        /// </returns>
        public static string TracePriority(
            _TracePriority priority,
            bool baseOnly,
            bool shortName
            )
        {
            _TracePriority basePriority = TraceOps.MaskTracePriority(priority);
            string name = TraceOps.GetTracePriorityName(basePriority, shortName);

            if (baseOnly)
            {
                return name;
            }
            else
            {
                _TracePriority flags = priority & ~basePriority;

                return String.Format(
                    "{0} {1}{2}", name, HexadecimalPrefix,
                    EnumOps.ToUIntOrULong(flags).ToString(
                    TracePriorityFormat));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Great care must be taken in this method because it is directly
        //       called by DebugTrace, which is used everywhere.  Accessing the
        //       interpreter requires a lock and a try/catch block.
        //
        /// <summary>
        /// This method formats the specified interpreter as a string suitable
        /// for use in trace output, taking care not to throw an exception even
        /// when the interpreter has been disposed or cannot be locked.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the interpreter, or a placeholder value
        /// indicating that it is null, disposed, busy, or in error.
        /// </returns>
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
            // HACK: Always grab the interpreter integer identifier, even if we
            //       cannot obtain the lock.
            //
            long id = interpreter.IdNoThrow;

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
                        return String.Format(DisplayDisposedFormat, id);

                    return id.ToString(); /* EXEMPT */
                }
                else
                {
                    return String.Format(DisplayBusyFormat, id);
                }
            }
            catch (Exception e)
            {
                Type type = (e != null) ? e.GetType() : null;

                return String.Format(DisplayErrorFormat1, id,
                    (type != null) ? type.Name : UnknownTypeName);
            }
            finally
            {
                interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified trace category and message into a
        /// single string.
        /// </summary>
        /// <param name="message">
        /// The trace message.
        /// </param>
        /// <param name="category">
        /// The trace category to prepend to the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The message prefixed with the category, or the message alone when
        /// the category is null.
        /// </returns>
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

        /// <summary>
        /// This method calculates the set of trace indicator flags that apply
        /// to the current trace operation based on the specified trace
        /// priority, interpreter, and the current thread.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags to examine, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the trace operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="includeListeners">
        /// Non-zero to include indicator flags derived from the configured
        /// trace listeners.
        /// </param>
        /// <returns>
        /// The calculated trace indicator flags.
        /// </returns>
        private static TraceIndicatorFlags TraceIndicators(
            _TracePriority? priority,
            Interpreter interpreter,
            bool includeListeners
            )
        {
            TraceIndicatorFlags result =  TraceIndicatorFlags.None;

            if (includeListeners)
                result |= DebugOps.CalculateListeners();

            if (priority != null)
            {
                _TracePriority localPriority = (_TracePriority)priority;

                if (FlagOps.HasFlags(
                        localPriority, _TracePriority.External, true))
                {
                    result |= TraceIndicatorFlags.External;
                }

                if (FlagOps.HasFlags(
                        localPriority, _TracePriority.FromPlugin, true))
                {
                    result |= TraceIndicatorFlags.FromPlugin;
                }

                if (FlagOps.HasFlags(
                        localPriority, _TracePriority.FromSdk, true))
                {
                    result |= TraceIndicatorFlags.FromSdk;
                }

                if (FlagOps.HasFlags(
                        localPriority, _TracePriority.ViaWrapper, true))
                {
                    result |= TraceIndicatorFlags.ViaWrapper;
                }
            }

            Thread currentThread = Thread.CurrentThread;

            if (currentThread != null)
            {
                if (currentThread.IsBackground)
                    result |= TraceIndicatorFlags.Background;

                if (currentThread.IsThreadPoolThread)
                    result |= TraceIndicatorFlags.ThreadPool;
            }

            if (interpreter != null)
            {
                if (interpreter.IsPrimarySystemThread())
                    result |= TraceIndicatorFlags.PrimaryThread;
            }
            else
            {
                result |= TraceIndicatorFlags.NoInterpreter;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally augments the specified trace output format
        /// string with a representation of the applicable trace indicator
        /// flags.
        /// </summary>
        /// <param name="format">
        /// The base trace output format string.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags used when calculating the trace indicators,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used when calculating the trace indicators, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The format string, possibly augmented with the trace indicator
        /// flags.
        /// </returns>
        private static string GetTraceOutputFormat(
            string format,
            _TracePriority? priority,
            Interpreter interpreter
            )
        {
            TraceIndicatorFlags indicatorFlags = useTraceIndicators ?
                TraceIndicators(
                    priority, interpreter, seeTraceListeners) :
                TraceIndicatorFlags.None;

            if (indicatorFlags != TraceIndicatorFlags.None)
            {
                if (rawTraceIndicators)
                {
                    return String.Format(TraceIndicatorsFormat,
                        indicatorFlags, format);
                }
                else
                {
                    return String.Format(TraceIndicatorsFormat,
                        CompactHexadecimal((ulong)indicatorFlags,
                        false), format);
                }
            }
            else
            {
                return format;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a complete trace message from its constituent
        /// parts, automatically determining the calling method name and
        /// discarding the resolved trace category and method name.
        /// </summary>
        /// <param name="format">
        /// The format string used to lay out the trace message.
        /// </param>
        /// <param name="prefix">
        /// The prefix to include in the trace message, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="dateTime">
        /// The date and time to include in the trace message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="serverName">
        /// The name of the web server associated with the message, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="testName">
        /// The name of the test associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the message, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with the message, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="message">
        /// The trace message text.
        /// </param>
        /// <param name="method">
        /// Non-zero to include the calling method name in the trace message.
        /// </param>
        /// <param name="stack">
        /// Non-zero to include a stack trace in the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of stack frames to skip when determining the calling
        /// method name.
        /// </param>
        /// <returns>
        /// The formatted trace message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TraceOutput(
            string format,
            string prefix,
            DateTime? dateTime,
            _TracePriority? priority,
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

        /// <summary>
        /// This method formats a complete trace message from its constituent
        /// parts, automatically determining the calling method name and
        /// reporting the resolved trace category and method name to the caller.
        /// </summary>
        /// <param name="format">
        /// The format string used to lay out the trace message.
        /// </param>
        /// <param name="prefix">
        /// The prefix to include in the trace message, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="dateTime">
        /// The date and time to include in the trace message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="serverName">
        /// The name of the web server associated with the message, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="testName">
        /// The name of the test associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain associated with the message, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the message, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with the message, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="message">
        /// The trace message text.
        /// </param>
        /// <param name="method">
        /// Non-zero to include the calling method name in the trace message.
        /// </param>
        /// <param name="stack">
        /// Non-zero to include a stack trace in the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of stack frames to skip when determining the calling
        /// method name.
        /// </param>
        /// <param name="category">
        /// The trace category.  Upon return, this may be set to the type name
        /// of the calling method when it could be determined.
        /// </param>
        /// <param name="methodName">
        /// Upon return, this is set to the name of the calling method, when it
        /// could be determined.
        /// </param>
        /// <returns>
        /// The formatted trace message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string TraceOutput(
            string format,
            string prefix,
            DateTime? dateTime,
            _TracePriority? priority,
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
                bool isThisAssembly;
                string typeName;

                DebugOps.GetMethodName(
                    0, skipNames, true, false, null,
                    DefaultGetMethodNameAnywhere,
                    out isThisAssembly, out typeName,
                    out methodName);

                //
                // HACK: Maybe change trace category for
                //       any core library types that are
                //       within namespaces that may have
                //       ambiguous names like "Default",
                //       etc.
                //
                if ((category == null) && isThisAssembly &&
                    !StringOps.Match(
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
                        if (DefaultDisplayMethodFullName &&
                            TraceOps.CanDisplayMethodName(typeName))
                        {
                            displayMethodName = String.Format(
                                "{0}{1}{2}", typeName, Type.Delimiter,
                                methodName);
                        }
                        else
                        {
                            displayMethodName = methodName;
                        }
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

            return String.Format(
                GetTraceOutputFormat(format, priority, interpreter),
                prefix, (dateTime != null) ?
                TraceDateTime((DateTime)dateTime, false) : DisplayNull,
                (priority != null) ? TracePriority(
                    (_TracePriority)priority, false, false) : DisplayNull,
#if WEB && !NET_STANDARD_20
                (serverName != null) ? serverName : DisplayNull,
#else
                DisplayUnavailable,
#endif
                (testName != null) ? testName : DisplayNull,
                AppDomainOps.GetIdString(appDomain, true),
                TraceInterpreter(interpreter), (threadId != null) ?
                threadId.ToString() : DisplayNull, displayMethodName,
                displayStackTrace, message, Environment.NewLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified exception as a string for use in
        /// trace output, optionally including the current stack trace.
        /// </summary>
        /// <param name="exception">
        /// The exception to format.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags controlling the formatting.  When the
        /// <c>ForException</c> flag is set, the current stack trace is also
        /// included.
        /// </param>
        /// <returns>
        /// The formatted exception string.
        /// </returns>
        public static string TraceException(
            Exception exception,
            _TracePriority priority
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            builder.AppendFormat("{0}", exception);

            //
            // NOTE: When the "ForException" trace priority flag is set,
            //       also include the full stack trace the exception is
            //       being reported from.  This is different from where
            //       the specified exception was actually caught.
            //
            if (FlagOps.HasFlags(
                    priority, _TracePriority.ForException, true))
            {
                string stackTrace = DebugOps.GetStackTraceString();

                if (!String.IsNullOrEmpty(stackTrace))
                {
                    builder.AppendLine();
                    builder.AppendFormat("[[STACK TRACE: {0}]]", stackTrace);
                    builder.AppendLine();
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string identifying the method where the
        /// specified exception was thrown.
        /// </summary>
        /// <param name="exception">
        /// The exception to examine.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a placeholder value when the method cannot be
        /// determined; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The string identifying the throwing method, a placeholder value, or
        /// null.
        /// </returns>
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

        /// <summary>
        /// This method builds an identifier string from the specified prefix,
        /// name, and integer identifier.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="id">
        /// The integer identifier to include.  A value of zero is omitted.
        /// </param>
        /// <returns>
        /// The constructed identifier string.
        /// </returns>
        public static string Id(
            string prefix,
            string name,
            long id
            )
        {
            return Id(prefix, name, (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds an identifier string from the specified prefix,
        /// name, and string identifier.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="id">
        /// The string identifier to include, if any.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The constructed identifier string.
        /// </returns>
        public static string Id(
            string prefix,
            string name,
            string id
            )
        {
            return Id(prefix, name, id, null, null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds an identifier string by joining the specified
        /// prefix, name, and up to six string identifiers, separating the
        /// non-empty components with a number sign.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name to include, if any.  This parameter may be null.
        /// </param>
        /// <param name="id1">
        /// The first identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id2">
        /// The second identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id3">
        /// The third identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id4">
        /// The fourth identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id5">
        /// The fifth identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="id6">
        /// The sixth identifier component to include, if any.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The constructed identifier string.
        /// </returns>
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
            StringBuilder result = StringBuilderFactory.Create();

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

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER || SHELL
        /// <summary>
        /// This method formats interactive loop data into a human-readable
        /// string suitable for use in trace and diagnostic output.
        /// </summary>
        /// <param name="loopData">
        /// The interactive loop data to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the interactive loop data.
        /// </returns>
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
        /// <summary>
        /// This method formats a date/time value associated with an update
        /// check into a string using the standard update date/time format.
        /// </summary>
        /// <param name="value">
        /// The date/time value to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the date/time value, or an
        /// empty string if it is null.
        /// </returns>
        public static string UpdateDateTime(
            DateTime? value
            )
        {
            if (value == null)
                return String.Empty;

            return ((DateTime)value).ToString(UpdateDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats shell callback data into a human-readable
        /// string suitable for use in trace and diagnostic output.
        /// </summary>
        /// <param name="callbackData">
        /// The shell callback data to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the shell callback data.
        /// </returns>
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

        /// <summary>
        /// This method formats update data into a human-readable string
        /// suitable for use in trace and diagnostic output.
        /// </summary>
        /// <param name="updateData">
        /// The update data to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the update data.
        /// </returns>
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

        /// <summary>
        /// This method formats an interpreter into a human-readable string
        /// suitable for use in trace and diagnostic output, quoting the
        /// result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the interpreter.
        /// </returns>
        public static string InterpreterNoThrow(
            Interpreter interpreter
            )
        {
            return InterpreterNoThrow(interpreter, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an interpreter into a human-readable string
        /// suitable for use in trace and diagnostic output, optionally quoting
        /// the result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to be formatted.  This value may be null.
        /// </param>
        /// <param name="quote">
        /// Non-zero to surround the resulting value with quotation marks.
        /// </param>
        /// <returns>
        /// The formatted string representation of the interpreter.
        /// </returns>
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

        /// <summary>
        /// This method formats an optional enabled flag into a human-readable
        /// string describing whether something is enabled, disabled, or
        /// unknown.
        /// </summary>
        /// <param name="enabled">
        /// The optional enabled flag to be formatted.  This value may be null
        /// to indicate an unknown state.
        /// </param>
        /// <returns>
        /// The formatted string representation of the enabled flag.
        /// </returns>
        public static string MaybeEnabled(
            bool? enabled
            )
        {
            if (enabled == null)
                return DisplayMaybeIsEnabled;

            return (bool)enabled ? DisplayMaybeSetEnabled : DisplayMaybeSetDisabled;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an optional enabled flag into a human-readable
        /// string describing whether something is currently enabled, disabled,
        /// or unknown.
        /// </summary>
        /// <param name="enabled">
        /// The optional enabled flag to be formatted.  This value may be null
        /// to indicate an unknown state.
        /// </param>
        /// <returns>
        /// The formatted string representation of the enabled flag.
        /// </returns>
        public static string IsEnabled(
            bool? enabled
            )
        {
            if (enabled == null)
                return DisplayIsUnknown;

            return (bool)enabled ? DisplayIsEnabled : DisplayIsDisabled;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an optional enabled flag into a human-readable
        /// string describing whether something was previously enabled,
        /// disabled, or unknown.
        /// </summary>
        /// <param name="enabled">
        /// The optional enabled flag to be formatted.  This value may be null
        /// to indicate an unknown state.
        /// </param>
        /// <returns>
        /// The formatted string representation of the enabled flag.
        /// </returns>
        public static string WasEnabled(
            bool? enabled
            )
        {
            if (enabled == null)
                return DisplayWasUnknown;

            return (bool)enabled ? DisplayWasEnabled : DisplayWasDisabled;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an enabled flag together with an associated
        /// value into a human-readable string.
        /// </summary>
        /// <param name="enabled">
        /// The enabled flag to be formatted.
        /// </param>
        /// <param name="value">
        /// The associated value to be included in the formatted string.
        /// </param>
        /// <returns>
        /// The formatted string containing the enabled flag and value.
        /// </returns>
        public static string EnabledAndValue(
            bool enabled,
            string value
            )
        {
            return String.Format("{0} ({1})", enabled, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
        /// <summary>
        /// This method formats the result of a native wait operation into a
        /// human-readable string describing which handle, if any, was
        /// signaled.
        /// </summary>
        /// <param name="count">
        /// The number of handles that were involved in the wait operation.
        /// </param>
        /// <param name="index">
        /// The raw result index returned by the wait operation.
        /// </param>
        /// <returns>
        /// The formatted string representation of the wait result.
        /// </returns>
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

        /// <summary>
        /// This method formats an operator name into a human-readable string
        /// suitable for display.
        /// </summary>
        /// <param name="name">
        /// The operator name to be formatted.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the operator name.
        /// </returns>
        public static string OperatorName(
            string name
            )
        {
            return DisplayString(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an operator name, obtained from an identifier,
        /// together with its lexeme into a human-readable string suitable for
        /// display.
        /// </summary>
        /// <param name="identifierName">
        /// The identifier whose name represents the operator.  This value may
        /// be null.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme associated with the operator.
        /// </param>
        /// <returns>
        /// The formatted string representation of the operator name.
        /// </returns>
        public static string OperatorName(
            IIdentifierName identifierName,
            Lexeme lexeme
            )
        {
            return OperatorName(
                (identifierName != null) ? identifierName.Name : null,
                lexeme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an operator name together with its lexeme into
        /// a human-readable string suitable for display.
        /// </summary>
        /// <param name="name">
        /// The operator name to be formatted.  This value may be null.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme associated with the operator.
        /// </param>
        /// <returns>
        /// The formatted string representation of the operator name.
        /// </returns>
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
        /// <summary>
        /// This method constructs a unique name for a database connection
        /// object based on its type and the associated interpreter.
        /// </summary>
        /// <param name="object">
        /// The database connection object for which a name is needed.  This
        /// value may be null.
        /// </param>
        /// <param name="dbConnectionType">
        /// The type of the database connection.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to obtain a unique identifier, or null to use
        /// the global state.
        /// </param>
        /// <returns>
        /// The constructed unique name for the database connection object.
        /// </returns>
        public static string DatabaseConnectionName(
            object @object,                    /* in */
            DbConnectionType dbConnectionType, /* in */
            Interpreter interpreter            /* in */
            )
        {
            long id = (interpreter != null) ?
                interpreter.NextId() : GlobalState.NextId();

            return DatabaseObjectName(
                @object, String.Format("{0}Connection",
                dbConnectionType), id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs a unique name for a database transaction
        /// object based on the associated interpreter.
        /// </summary>
        /// <param name="object">
        /// The database transaction object for which a name is needed.  This
        /// value may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter used to obtain a unique identifier, or null to use
        /// the global state.
        /// </param>
        /// <returns>
        /// The constructed unique name for the database transaction object.
        /// </returns>
        public static string DatabaseTransactionName(
            object @object,         /* in */
            Interpreter interpreter /* in */
            )
        {
            long id = (interpreter != null) ?
                interpreter.NextId() : GlobalState.NextId();

            return DatabaseObjectName(
                @object, "Transaction", id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs a unique name for a database object based on
        /// its runtime type, a default name, and a unique identifier.
        /// </summary>
        /// <param name="object">
        /// The database object for which a name is needed.  This value may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The default name to use when the object is null or its type cannot
        /// be determined.
        /// </param>
        /// <param name="id">
        /// The unique identifier to incorporate into the constructed name.
        /// </param>
        /// <returns>
        /// The constructed unique name for the database object.
        /// </returns>
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

        /// <summary>
        /// This method obtains the file system location of the assembly that
        /// contains the specified type.
        /// </summary>
        /// <param name="type">
        /// The type whose containing assembly location is needed.  This value
        /// may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-friendly placeholder when the location
        /// cannot be determined; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The location of the containing assembly, or a placeholder or null
        /// when it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified type belongs to one of
        /// the core system assemblies (mscorlib or System).
        /// </summary>
        /// <param name="type">
        /// The type to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the type belongs to a core system assembly; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified type belongs to the
        /// same assembly as the Eagle library.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the type belongs to the Eagle assembly; otherwise, false.
        /// </returns>
        public static bool IsSameAssembly(Type type)
        {
            return (type != null) && GlobalState.IsAssembly(type.Assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the name of a type for use in an opaque object
        /// handle, optionally using its fully qualified name.
        /// </summary>
        /// <param name="type">
        /// The type whose name is needed.  This value may be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to use the fully qualified type name; otherwise, the
        /// simple type name is used.
        /// </param>
        /// <returns>
        /// The type name, or a placeholder when the type is null.
        /// </returns>
        public static string ObjectHandleTypeName(
            Type type,
            bool full
            )
        {
            return (type != null) ? (full ? type.FullName : type.Name) : UnknownTypeName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs an opaque object handle string from a
        /// prefix, a name, and a unique identifier.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to use for the handle.  This value may be null.
        /// </param>
        /// <param name="name">
        /// The name to incorporate into the handle.  This value may be null.
        /// </param>
        /// <param name="id">
        /// The unique identifier to incorporate into the handle.
        /// </param>
        /// <returns>
        /// The constructed opaque object handle string.
        /// </returns>
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

        /// <summary>
        /// This method constructs an opaque object handle string from a
        /// prefix, a name, and the hash code of the specified value.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to use for the handle.  This value may be null.
        /// </param>
        /// <param name="name">
        /// The name to incorporate into the handle.  This value may be null.
        /// </param>
        /// <param name="value">
        /// The value whose hash code is incorporated into the handle.  This
        /// value may be null.
        /// </param>
        /// <returns>
        /// The constructed opaque object handle string.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of a type, optionally using its full
        /// name and/or its assembly-qualified name.
        /// </summary>
        /// <param name="type">
        /// The type whose name is needed.  This value may be null.
        /// </param>
        /// <param name="fullName">
        /// Non-zero to use the fully qualified type name.
        /// </param>
        /// <param name="qualified">
        /// Non-zero to include the containing assembly information.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-friendly placeholder when the type is
        /// null; otherwise, an empty string is returned in that case.
        /// </param>
        /// <returns>
        /// The formatted type name.
        /// </returns>
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

        /// <summary>
        /// This method formats the assembly-qualified name of a type.
        /// </summary>
        /// <param name="type">
        /// The type whose qualified name is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The assembly-qualified name of the type, or an empty string when
        /// the type is null.
        /// </returns>
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

        /// <summary>
        /// This method formats the assembly-qualified name of a type given its
        /// type name and the name of its containing assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the containing assembly.  This value may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type.  This value may be null or empty.
        /// </param>
        /// <param name="full">
        /// Non-zero to use the full assembly name; otherwise, the simple
        /// assembly name is used.
        /// </param>
        /// <returns>
        /// The formatted qualified name, or null when both the assembly name
        /// and type name are unavailable.
        /// </returns>
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

        /// <summary>
        /// This method combines a parent name and a child name into a single
        /// qualified name using the type name delimiter.
        /// </summary>
        /// <param name="parentName">
        /// The parent name.  This value may be null or empty.
        /// </param>
        /// <param name="childName">
        /// The child name.  This value may be null or empty.
        /// </param>
        /// <returns>
        /// The combined qualified name, or an empty string when both names are
        /// unavailable.
        /// </returns>
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

        /// <summary>
        /// This method constructs the qualified name of a delegate method from
        /// its containing type name and method name.
        /// </summary>
        /// <param name="typeName">
        /// The name of the type that contains the method.  This value may be
        /// null or empty.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null or empty.
        /// </param>
        /// <returns>
        /// The constructed qualified delegate method name.
        /// </returns>
        public static string DelegateMethodName(
            string typeName,
            string methodName
            )
        {
            return QualifiedName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs the qualified name of the method that backs
        /// the specified delegate.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose backing method name is needed.  This value may
        /// be null.
        /// </param>
        /// <param name="assembly">
        /// Non-zero to include the containing assembly information.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-friendly placeholder when the delegate
        /// is null; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The constructed qualified delegate method name.
        /// </returns>
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

        /// <summary>
        /// This method constructs the qualified name of the specified method.
        /// </summary>
        /// <param name="methodBase">
        /// The method whose qualified name is needed.  This value may be null.
        /// </param>
        /// <param name="assembly">
        /// Non-zero to include the containing assembly information.
        /// </param>
        /// <param name="display">
        /// Non-zero to return a display-friendly placeholder when the method
        /// is null; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// The constructed qualified method name.
        /// </returns>
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

        /// <summary>
        /// This method constructs the qualified name of a method given its
        /// containing type and method name, applying special handling for
        /// certain well-known Eagle types.
        /// </summary>
        /// <param name="type">
        /// The type that contains the method.  This value may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null or empty.
        /// </param>
        /// <param name="assembly">
        /// Non-zero to include the containing assembly information.
        /// </param>
        /// <returns>
        /// The constructed qualified method name.
        /// </returns>
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

        /// <summary>
        /// This method formats the name of an argument together with its
        /// positional index into a human-readable string.
        /// </summary>
        /// <param name="position">
        /// The positional index of the argument.
        /// </param>
        /// <param name="name">
        /// The name of the argument.  This value may be null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the argument name.
        /// </returns>
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

        /// <summary>
        /// This method formats the results of evaluating a set of policies
        /// into a human-readable string suitable for use in trace and
        /// diagnostic output.
        /// </summary>
        /// <param name="allPolicies">
        /// The collection of all policies that were evaluated.  This value may
        /// be null.
        /// </param>
        /// <param name="failedPolicies">
        /// The collection of policies that failed.  This value may be null.
        /// </param>
        /// <param name="methodFlags">
        /// The method flags associated with the policy evaluation.
        /// </param>
        /// <param name="policyFlags">
        /// The policy flags associated with the policy evaluation.
        /// </param>
        /// <param name="fileName">
        /// The name of the file associated with the policy evaluation.  This
        /// value may be null.
        /// </param>
        /// <param name="code">
        /// The return code produced by the policy evaluation.
        /// </param>
        /// <param name="decision">
        /// The decision produced by the policy evaluation.
        /// </param>
        /// <param name="result">
        /// The result produced by the policy evaluation.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The formatted string representation of the policy results.
        /// </returns>
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
                "PolicyResults: {0} --> {1}, methodFlags = {2}, " +
                "policyFlags = {3}, fileName = {4}, decision = {5}, " +
                "code = {6}, result = {7}", success ? "SUCCESS" : "FAILURE",
                success ? WrapOrNull(allPolicies) : WrapOrNull(failedPolicies),
                WrapOrNull(methodFlags), WrapOrNull(policyFlags),
                WrapOrNull(fileName), decision, code, WrapOrNull(result));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs a human-readable name describing the policy
        /// implemented by the specified delegate.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose policy name is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The constructed policy delegate name, or null when it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method constructs a human-readable name describing the method
        /// that backs the specified trace delegate.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose method name is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The constructed trace delegate name, or null when it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method builds a qualified name for the target method of the
        /// specified delegate.
        /// </summary>
        /// <param name="delegate">
        /// The delegate whose target method name is needed.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The qualified method name, or null if the delegate or its method
        /// information is unavailable.
        /// </returns>
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

        /// <summary>
        /// This method builds a method name that is qualified with the simple
        /// type name only when the type does not belong to this assembly.
        /// </summary>
        /// <param name="type">
        /// The type that declares the method.  This value may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <returns>
        /// The qualified method name.
        /// </returns>
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

        /// <summary>
        /// This method builds a method name that is qualified with the full
        /// type name only when the type does not belong to this assembly.
        /// </summary>
        /// <param name="type">
        /// The type that declares the method.  This value may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <returns>
        /// The qualified method name.
        /// </returns>
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

        /// <summary>
        /// This method builds a method name qualified with the full name of the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The type that declares the method.  This value may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <returns>
        /// The qualified method name.
        /// </returns>
        public static string MethodFullName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.FullName : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a method name qualified with the simple name of
        /// the specified type.
        /// </summary>
        /// <param name="type">
        /// The type that declares the method.  This value may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <returns>
        /// The qualified method name.
        /// </returns>
        public static string MethodName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.Name : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a method name qualified with the specified type
        /// or object name.
        /// </summary>
        /// <param name="typeOrObjectName">
        /// The type or object name used to qualify the method name.  This value
        /// may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <returns>
        /// The qualified method name.
        /// </returns>
        public static string MethodName(
            string typeOrObjectName,
            string methodName
            )
        {
            return QualifiedName(typeOrObjectName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a comma-separated list of type names to the
        /// specified string builder.
        /// </summary>
        /// <param name="builder">
        /// The string builder to append to.  If this value is null, this method
        /// does nothing.
        /// </param>
        /// <param name="types">
        /// The list of types whose names should be appended.  This value may be
        /// null.
        /// </param>
        /// <param name="default">
        /// The text to append when the list of types is null.  This value may
        /// be null.
        /// </param>
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

        /// <summary>
        /// This method appends a textual method signature -- return type,
        /// qualified method name, and parameter types -- to the specified
        /// string builder.
        /// </summary>
        /// <param name="builder">
        /// The string builder to append to.  If this value is null, this method
        /// does nothing.
        /// </param>
        /// <param name="qualifiedMethodName">
        /// The qualified name of the method.  This value may be null.
        /// </param>
        /// <param name="returnInfo">
        /// The parameter information describing the return value.  This value
        /// may be null.
        /// </param>
        /// <param name="parameterInfo">
        /// The array of parameter information describing the method parameters.
        /// This value may be null.
        /// </param>
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

        /// <summary>
        /// This method builds a string describing a single method overload,
        /// optionally including its index, signature, and qualifying object
        /// name.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the overload, or <see cref="Index.Invalid" />
        /// to omit the index prefix.
        /// </param>
        /// <param name="objectName">
        /// The object name used to qualify the method name.  This value may be
        /// null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method.  This value may be null.
        /// </param>
        /// <param name="returnInfo">
        /// The parameter information describing the return value.  This value
        /// may be null.
        /// </param>
        /// <param name="parameterInfo">
        /// The array of parameter information describing the method parameters.
        /// This value may be null.
        /// </param>
        /// <param name="marshalFlags">
        /// The flags used to control how the overload is formatted.
        /// </param>
        /// <returns>
        /// The formatted description of the method overload.
        /// </returns>
        public static string MethodOverload(
            int index,
            string objectName,
            string methodName,
            ParameterInfo returnInfo,
            ParameterInfo[] parameterInfo,
            MarshalFlags marshalFlags
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            string maybeQualifiedMethodName = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.UnqualifiedNames, true) ?
                methodName : QualifiedName(objectName, methodName);

            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.NamesOnly, true))
            {
                builder.Append(maybeQualifiedMethodName);
            }
            else
            {
                if (AlwaysShowSignatures || FlagOps.HasFlags(
                        marshalFlags, MarshalFlags.ShowSignatures, true))
                {
                    MaybeAddSignature(
                        builder, maybeQualifiedMethodName, returnInfo,
                        parameterInfo);

                    /* NO RESULT */
                    SomeKindOfPrefixAndSuffix(builder);
                }
                else
                {
                    builder.Append(SomeKindOfPrefixAndSuffix(
                        maybeQualifiedMethodName));
                }

                if (index != Index.Invalid)
                {
                    builder.Insert(0, String.Format("{0}{1}{2}",
                        Characters.NumberSign, index, Characters.Space));
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a hexadecimal representation of the runtime
        /// hash code for the specified value.
        /// </summary>
        /// <param name="value">
        /// The value whose hash code is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The hexadecimal hash code, or null if the value is null.
        /// </returns>
        private static string MaybeHashCode(
            object value
            )
        {
            if (value == null)
                return null;

            return Hexadecimal(RuntimeOps.GetHashCode(value), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a hexadecimal representation of the runtime
        /// hash code for the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose hash code is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The hexadecimal hash code, or null if the object is null.
        /// </returns>
        private static string MaybeHashCode(
            IObject @object
            )
        {
            if (@object == null)
                return null;

            return Hexadecimal(RuntimeOps.GetHashCode(@object), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a wrapped hexadecimal representation of the
        /// runtime hash code for the specified value.
        /// </summary>
        /// <param name="value">
        /// The value whose hash code is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The wrapped hexadecimal hash code, or a representation of null when
        /// the value is null.
        /// </returns>
        public static string WrapHashCode(
            object value
            )
        {
            return WrapOrNull(MaybeHashCode(value));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a wrapped hexadecimal representation of the
        /// runtime hash code for the specified object.
        /// </summary>
        /// <param name="object">
        /// The object whose hash code is needed.  This value may be null.
        /// </param>
        /// <returns>
        /// The wrapped hexadecimal hash code, or a representation of null when
        /// the object is null.
        /// </returns>
        public static string WrapHashCode(
            IObject @object
            )
        {
            return WrapOrNull(MaybeHashCode(@object));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing the current application
        /// domain.
        /// </summary>
        /// <returns>
        /// The string describing the current application domain.
        /// </returns>
        public static string DisplayAppDomain()
        {
            return DisplayAppDomain(AppDomainOps.GetCurrent());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing the specified application
        /// domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to describe.  This value may be null.
        /// </param>
        /// <returns>
        /// The string describing the application domain, or a representation of
        /// null when the application domain is null.
        /// </returns>
        public static string DisplayAppDomain(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                try
                {
                    StringBuilder result = StringBuilderFactory.Create();

                    result.AppendFormat(
                        "[id = {0}, default = {1}]",
                        AppDomainOps.GetIdString(appDomain, true),
                        AppDomainOps.IsDefault(appDomain));

                    return StringBuilderCache.GetStringAndRelease(ref result);
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayErrorFormat0,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing a list of home directory
        /// flag and path pairs.
        /// </summary>
        /// <param name="value">
        /// The list of home directory pairs to describe.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The string describing the home directory pairs, or a representation
        /// of null or empty when appropriate.
        /// </returns>
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

        /// <summary>
        /// This method builds a friendly name for an application domain from a
        /// file name and type name.
        /// </summary>
        /// <param name="fileName">
        /// The file name component of the friendly name.  This value may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The type name component of the friendly name.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The friendly name for the application domain.
        /// </returns>
        public static string AppDomainFriendlyName(
            string fileName,
            string typeName
            )
        {
            return StringList.MakeList(fileName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a friendly name for an application domain from an
        /// assembly name and type name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name component of the friendly name.  This value may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The type name component of the friendly name.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The friendly name for the application domain.
        /// </returns>
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

        /// <summary>
        /// This method builds an assembly-qualified name for a plugin from an
        /// assembly name and type name.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the assembly that contains the plugin.  This value may
        /// be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type.  This value may be null.
        /// </param>
        /// <returns>
        /// The assembly-qualified name of the plugin.
        /// </returns>
        public static string PluginName(
            string assemblyName,
            string typeName
            )
        {
            // return QualifiedName(assemblyName, typeName);
            return Assembly.CreateQualifiedName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the simple assembly name associated with the
        /// specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose simple name is needed.  This value may be
        /// null.
        /// </param>
        /// <returns>
        /// The simple assembly name of the plugin, or null if it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method builds a command name for a plugin from its assembly,
        /// plugin name, type, and type name.
        /// </summary>
        /// <param name="assembly">
        /// The assembly that contains the plugin.  This value may be null.
        /// </param>
        /// <param name="pluginName">
        /// The fallback plugin name used when the assembly name is unavailable.
        /// This value may be null.
        /// </param>
        /// <param name="type">
        /// The plugin type.  This value may be null.
        /// </param>
        /// <param name="typeName">
        /// The fallback type name used when the type is unavailable.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The command name for the plugin.
        /// </returns>
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

        /// <summary>
        /// This method produces a human-readable "about" description for the
        /// specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to describe.  This value may be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to include the full type name in the description; otherwise,
        /// the simple type name is used.
        /// </param>
        /// <param name="extra">
        /// Extra text to append to the description.  This value may be null.
        /// </param>
        /// <returns>
        /// The "about" description for the plugin, or null if the plugin data
        /// is null.
        /// </returns>
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
                    appDomainId = AppDomainOps.GetIdString(
                        pluginData.AppDomain, true);
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

        /// <summary>
        /// This method produces a Tcl-compatible stardate string for the
        /// specified date and time.
        /// </summary>
        /// <param name="value">
        /// The date and time to convert to a stardate.
        /// </param>
        /// <returns>
        /// The formatted stardate string.
        /// </returns>
        private static string Stardate(
            DateTime value
            ) // COMPAT: Tcl
        {
            long part1;
            long part2;
            long part3;

            TimeOps.CalculateStardate(
                value, out part1, out part2, out part3);

            return String.Format(
                StardateOutputFormat, part1, part2,
                Characters.Period, part3);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing how many attempts were made
        /// and the approximate elapsed time.
        /// </summary>
        /// <param name="tries">
        /// The number of attempts that were made.
        /// </param>
        /// <param name="delay">
        /// The delay, in milliseconds, between attempts.
        /// </param>
        /// <param name="limit">
        /// The maximum number of attempts allowed, or a negative value to
        /// indicate no limit.
        /// </param>
        /// <returns>
        /// The string describing the attempts that were made.
        /// </returns>
        public static string Tries(
            int tries,
            int delay,
            int limit
            )
        {
            if (tries > 0)
            {
                StringBuilder builder = StringBuilderFactory.Create();

                builder.AppendFormat(
                    "after {0} of {1} tries", tries, (limit >= 0) ?
                    limit.ToString() : "unlimited");

                long milliseconds = (delay > 0) ? ((long)delay * tries) : 0;

                if (milliseconds > 0)
                {
                    builder.AppendFormat(
                        " or about {2} milliseconds", milliseconds);
                }

                return StringBuilderCache.GetStringAndRelease(ref builder);
            }
            else
            {
                return "without trying";
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// This method appends a sub-list of items, optionally preceded by a
        /// header item, to the specified string/pair list.
        /// </summary>
        /// <param name="list">
        /// The string/pair list to append to.
        /// </param>
        /// <param name="subList">
        /// The list of items to append.  This value may be null.
        /// </param>
        /// <param name="item">
        /// The header item to insert before the sub-list.  This value may be
        /// null.
        /// </param>
        /// <param name="empty">
        /// Non-zero to append the header and count even when the sub-list is
        /// empty.
        /// </param>
        /// <returns>
        /// The number of entries that were added to the list.
        /// </returns>
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
